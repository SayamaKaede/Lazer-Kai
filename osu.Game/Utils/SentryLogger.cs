// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Development;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Models;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Skinning;
using Sentry;
using Sentry.Protocol;

namespace osu.Game.Utils
{
    /// <summary>
    /// Report errors to sentry.
    /// </summary>
    /// <remarks>
    /// Lazer-Kai's own project, not the osu! team's: they have no use for a fork's errors. Nothing that says who sent a
    /// report goes with it — no account, no machine name, and the Windows account name and the osu! username are taken
    /// out of everything else (<see cref="Scrub"/>). Somebody who would rather not send anything turns it off in the
    /// options, and nothing is sent until those have been read.
    /// </remarks>
    public class SentryLogger : IDisposable
    {
        /// <summary>
        /// Where reports go. It is in every copy of the game anyway, and it can only be sent to, not read from.
        /// </summary>
        private const string dsn = @"https://b600614892ac193b68d09c2543137ee4@o4512145916297216.ingest.us.sentry.io/4512145926914048";

        /// <summary>
        /// The address a report says it came from, in place of the one it did.
        /// </summary>
        internal const string NO_ADDRESS = @"0.0.0.0";

        private IBindable<APIUser>? localUser;

        private readonly Bindable<bool> reporting = new Bindable<bool>();

        private readonly SentryReportLimit limit = new SentryReportLimit();

        private readonly IDisposable? sentrySession;

        private readonly OsuGame game;

        public SentryLogger(OsuGame game, Storage? storage = null)
        {
            this.game = game;

            if (Environment.GetEnvironmentVariable("OSU_DISABLE_ERROR_REPORTING") == "1")
                return;

            if (DebugUtils.IsNUnitRunning)
                return;

            // a build from the working tree is the one being worked on: its errors are seen as they happen.
            if (!game.IsDeployedBuild)
                return;

            sentrySession = SentrySdk.Init(options =>
            {
                Configure(options, dsn, game.Version, () => localUser?.Value.Username);
                options.IsGlobalModeEnabled = true;
                options.CacheDirectoryPath = storage?.GetFullPath(string.Empty);
            });

            Logger.NewEntry += processLogEntry;
        }

        /// <summary>
        /// Whether errors are being sent: there is somewhere to send them, and the options say to.
        /// </summary>
        public bool Reporting => sentrySession != null && reporting.Value;

        /// <summary>
        /// Everything about a report that is not particular to this game's instance, so that what is sent can be tested.
        /// </summary>
        /// <param name="options">The options to set.</param>
        /// <param name="dsn">Where to send to.</param>
        /// <param name="version">The game's version, as the release a report came from.</param>
        /// <param name="osuUsername">The osu! account signed in, if any, to take out of what is sent.</param>
        internal static void Configure(SentryOptions options, string dsn, string version, Func<string?> osuUsername)
        {
            options.Dsn = dsn;
            options.Release = $"lazer-kai@{version}";

            // who is playing, and on what: none of it is sent. These are the defaults, set here so that they stay so.
            options.SendDefaultPii = false;
            options.IsEnvironmentUser = false;

            // a session is a record of the game being opened and closed, which is not an error.
            options.AutoSessionTracking = false;

            options.SetBeforeSend((e, _) => Scrub(e, osuUsername()));
        }

        /// <summary>
        /// The same report, with the Windows account name, the machine's name and the osu! username taken out of every
        /// text in it: the exception's message, the paths in its stack trace, the log lines that led up to it.
        /// </summary>
        /// <returns>The report to send, or null to send nothing when it could not be gone through.</returns>
        internal static SentryEvent? Scrub(SentryEvent report, string? osuUsername)
        {
            try
            {
                var names = new List<string> { Environment.MachineName };

                if (!string.IsNullOrWhiteSpace(osuUsername))
                    names.Add(osuUsername);

                var original = new ArrayBufferWriter<byte>();

                using (var writer = new Utf8JsonWriter(original))
                    report.WriteTo(writer, null);

                var scrubbed = new ArrayBufferWriter<byte>();

                using (var document = JsonDocument.Parse(original.WrittenMemory))
                using (var writer = new Utf8JsonWriter(scrubbed))
                    writeScrubbed(document.RootElement, writer, names);

                SentryEvent result;

                using (var document = JsonDocument.Parse(scrubbed.WrittenMemory))
                    result = SentryEvent.FromJson(document.RootElement);

                // a report that gives no address of its own is put down to the one it arrived from, and placed from
                // that down to the town (2026-09-25, with the project told not to store addresses). This one gives one
                // that places it nowhere.
                result.User.IpAddress = NO_ADDRESS;
                return result;
            }
            catch
            {
                // a report that might still name somebody is not sent.
                return null;
            }
        }

        /// <summary>
        /// Copies a report's JSON through, masking the strings in it. Only values: a name matching part of a key would
        /// otherwise rename what the report's fields are.
        /// </summary>
        private static void writeScrubbed(JsonElement element, Utf8JsonWriter writer, IReadOnlyList<string> names)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();

                    foreach (var property in element.EnumerateObject())
                    {
                        writer.WritePropertyName(property.Name);
                        writeScrubbed(property.Value, writer, names);
                    }

                    writer.WriteEndObject();
                    break;

                case JsonValueKind.Array:
                    writer.WriteStartArray();

                    foreach (var item in element.EnumerateArray())
                        writeScrubbed(item, writer, names);

                    writer.WriteEndArray();
                    break;

                case JsonValueKind.String:
                    string text = PathDisplay.MaskIn(element.GetString()!);

                    // whole words only, and not a part of a dotted name: "osu" or "Game" taken out of every frame of a
                    // stack trace ("osu.Game.Screens.Edit") would leave nothing to read it by. A name that ends a
                    // sentence is still a whole word.
                    foreach (string name in names)
                        text = Regex.Replace(text, $@"(?<![\p{{L}}\p{{N}}_]\.?){Regex.Escape(name)}(?!\.?[\p{{L}}\p{{N}}_])", PathDisplay.MASK, RegexOptions.IgnoreCase);

                    writer.WriteStringValue(text);
                    break;

                default:
                    element.WriteTo(writer);
                    break;
            }
        }

        ~SentryLogger()
        {
            Dispose(false);
        }

        /// <summary>
        /// Starts sending, if the options say to, and learns the osu! account to keep out of what is sent.
        /// </summary>
        /// <remarks>
        /// The osu! team's reports carry the account, so that they can find a player again. These do not: the account is
        /// only here to be taken out.
        /// </remarks>
        public void Attach(OsuConfigManager config, IBindable<APIUser> user)
        {
            if (sentrySession == null)
                return;

            Debug.Assert(localUser == null);

            localUser = user.GetBoundCopy();
            config.BindWith(OsuSetting.ErrorReporting, reporting);
        }

        private void processLogEntry(LogEntry entry)
        {
            if (entry.Level < LogLevel.Verbose) return;

            if (!reporting.Value) return;

            var exception = entry.Exception;

            if (exception != null)
            {
                if (!shouldSubmitException(exception)) return;

                if (!limit.Allows(exception)) return;

                // framework does some weird exception redirection which means sentry does not see unhandled exceptions using its automatic methods.
                // but all unhandled exceptions still arrive via this pathway. we just need to mark them as unhandled for tagging purposes.
                // easiest solution is to check the message matches what the framework logs this as.
                // see https://github.com/ppy/osu-framework/blob/f932f8df053f0011d755c95ad9a2ed61b94d136b/osu.Framework/Platform/GameHost.cs#L336
                bool wasUnhandled = entry.Message == @"An unhandled error has occurred.";
                bool wasUnobserved = entry.Message == @"An unobserved error has occurred.";

                if (wasUnobserved)
                {
                    // see https://github.com/getsentry/sentry-dotnet/blob/c6a660b1affc894441c63df2695a995701671744/src/Sentry/Integrations/TaskUnobservedTaskExceptionIntegration.cs#L39
                    exception.Data[Mechanism.MechanismKey] = @"UnobservedTaskException";
                }

                if (wasUnhandled)
                {
                    // see https://github.com/getsentry/sentry-dotnet/blob/main/src/Sentry/Integrations/AppDomainUnhandledExceptionIntegration.cs#L38-L39
                    exception.Data[Mechanism.MechanismKey] = @"AppDomain.UnhandledException";
                }

                exception.Data[Mechanism.HandledKey] = !wasUnhandled;

                SentrySdk.CaptureEvent(new SentryEvent(exception)
                {
                    Message = entry.Message,
                    Level = getSentryLevel(entry.Level),
                }, scope =>
                {
                    var beatmap = game.Dependencies.Get<IBindable<WorkingBeatmap>>().Value.BeatmapInfo;
                    var ruleset = game.Dependencies.Get<IBindable<RulesetInfo>>().Value;

                    scope.Contexts[@"config"] = new
                    {
                        Game = game.Dependencies.Get<OsuConfigManager>().GetCurrentConfigurationForLogging(),
                        Framework = game.Dependencies.Get<FrameworkConfigManager>().GetCurrentConfigurationForLogging(),
                    };

                    game.Dependencies.Get<RealmAccess>().Run(realm =>
                    {
                        scope.Contexts[@"realm"] = new
                        {
                            Counts = new
                            {
                                BeatmapSets = realm.All<BeatmapSetInfo>().Count(),
                                Beatmaps = realm.All<BeatmapInfo>().Count(),
                                Files = realm.All<RealmFile>().Count(),
                                Rulesets = realm.All<RulesetInfo>().Count(),
                                RulesetsAvailable = realm.All<RulesetInfo>().Count(r => r.Available),
                                Skins = realm.All<SkinInfo>().Count(),
                            }
                        };
                    });

                    scope.Contexts[@"global statistics"] = GlobalStatistics.GetStatistics()
                                                                           .GroupBy(s => s.Group)
                                                                           .ToDictionary(g => g.Key, items => items.ToDictionary(i => i.Name, g => g.DisplayValue));

                    scope.Contexts[@"beatmap"] = new
                    {
                        Name = beatmap.ToString(),
                        Ruleset = beatmap.Ruleset.InstantiationInfo,
                        beatmap.OnlineID,
                    };

                    scope.Contexts[@"ruleset"] = new
                    {
                        ruleset.ShortName,
                        ruleset.Name,
                        ruleset.InstantiationInfo,
                        ruleset.OnlineID
                    };

                    scope.Contexts[@"clocks"] = new
                    {
                        Audio = game.Dependencies.Get<MusicController>().CurrentTrack.CurrentTime,
                        Game = game.Clock.CurrentTime,
                    };

                    scope.SetTag(@"beatmap", $"{beatmap.OnlineID}");
                    scope.SetTag(@"ruleset", ruleset.ShortName);
                    scope.SetTag(@"os", $"{RuntimeInfo.OS} ({Environment.OSVersion})");
                    scope.SetTag(@"version hash", game.VersionHash);
                    scope.SetTag(@"processor count", Environment.ProcessorCount.ToString());
                });
            }
            else
                SentrySdk.AddBreadcrumb(entry.Message, entry.Target.ToString(), "navigation", level: getBreadcrumbLevel(entry.Level));
        }

        private BreadcrumbLevel getBreadcrumbLevel(LogLevel entryLevel)
        {
            switch (entryLevel)
            {
                case LogLevel.Debug:
                    return BreadcrumbLevel.Debug;

                case LogLevel.Verbose:
                    return BreadcrumbLevel.Info;

                case LogLevel.Important:
                    return BreadcrumbLevel.Warning;

                case LogLevel.Error:
                    return BreadcrumbLevel.Error;

                default:
                    throw new ArgumentOutOfRangeException(nameof(entryLevel), entryLevel, null);
            }
        }

        private SentryLevel getSentryLevel(LogLevel entryLevel)
        {
            switch (entryLevel)
            {
                case LogLevel.Debug:
                    return SentryLevel.Debug;

                case LogLevel.Verbose:
                    return SentryLevel.Info;

                case LogLevel.Important:
                    return SentryLevel.Warning;

                case LogLevel.Error:
                    return SentryLevel.Error;

                default:
                    throw new ArgumentOutOfRangeException(nameof(entryLevel), entryLevel, null);
            }
        }

        private static readonly HashSet<int> ignored_io_exception_hresults =
        [
            // see https://stackoverflow.com/a/9294382 for how these are synthesised
            unchecked((int)0x80070020), // ERROR_SHARING_VIOLATION
            unchecked((int)0x80070027), // ERROR_HANDLE_DISK_FULL
            unchecked((int)0x80070070), // ERROR_DISK_FULL
        ];

        private bool shouldSubmitException(Exception exception)
        {
            if (IsLocalUserConnectivityException(exception))
                return false;

            switch (exception)
            {
                // disk I/O failures, invalid formats, etc.

                case IOException ioe:
                    if (ignored_io_exception_hresults.Contains(ioe.HResult))
                        return false;

                    break;

                case UnauthorizedAccessException:
                case SharpCompress.Common.InvalidFormatException:
                    return false;

                // stuff that should really never make it to sentry
                case APIAccess.WebRequestFlushedException:
                case TaskCanceledException:
                    return false;
            }

            return true;
        }

        public static bool IsLocalUserConnectivityException(Exception exception)
        {
            switch (exception)
            {
                case TimeoutException te:
                    return te.Message.Contains(@"elapsed without receiving a message from the server");

                case WebException we:
                    // more statuses may need to be blocked as we come across them.
                    return we.Status == WebExceptionStatus.Timeout;

                case WebSocketException:
                case SocketException:
                    return true;
            }

            return false;
        }

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            Logger.NewEntry -= processLogEntry;
            sentrySession?.Dispose();
        }

        #endregion
    }
}
