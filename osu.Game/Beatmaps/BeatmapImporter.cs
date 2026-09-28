// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps.Formats;
using osu.Game.Collections;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.IO;
using osu.Game.IO.Archives;
using osu.Game.Models;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects.Types;
using Realms;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Handles the storage and retrieval of Beatmaps/WorkingBeatmaps.
    /// </summary>
    public class BeatmapImporter : RealmArchiveModelImporter<BeatmapSetInfo>
    {
        public override IEnumerable<string> HandledExtensions => new[] { ".osz", ".olz" };

        protected override string[] HashableFileTypes => new[] { ".osu" };

        public ProcessBeatmapDelegate? ProcessBeatmap { private get; set; }

        public BeatmapImporter(Storage storage, RealmAccess realm)
            : base(storage, realm)
        {
        }

        public override async Task<Live<BeatmapSetInfo>?> ImportAsUpdate(ProgressNotification notification, ImportTask importTask, BeatmapSetInfo original)
        {
            var originalDateAdded = original.DateAdded;

            Guid originalId = original.ID;

            var imported = await Import(notification, new[] { importTask }).ConfigureAwait(false);

            if (!imported.Any())
                return null;

            Debug.Assert(imported.Count() == 1);

            var first = imported.First();

            // If there were no changes, ensure we don't accidentally nuke ourselves.
            if (first.ID == originalId)
            {
                first.PerformWrite(s =>
                {
                    // Transfer local values which should be persisted across a beatmap update.
                    s.DateAdded = originalDateAdded;

                    // Re-run processing even in this case. We might have outdated metadata.
                    ProcessBeatmap?.Invoke(s, MetadataLookupScope.OnlineFirst);
                });
                return first;
            }

            first.PerformWrite(updated =>
            {
                try
                {
                    var realm = updated.Realm;

                    // Re-fetch as we are likely on a different thread.
                    original = realm!.Find<BeatmapSetInfo>(originalId)!;

                    // Generally the import process will do this for us if the OnlineIDs match,
                    // but that isn't a guarantee (ie. if the .osu file doesn't have OnlineIDs populated).
                    original.DeletePending = true;

                    // Transfer local values which should be persisted across a beatmap update.
                    updated.DateAdded = originalDateAdded;

                    transferCollectionReferences(realm, original, updated);

                    foreach (var beatmap in original.Beatmaps.ToArray())
                    {
                        var updatedBeatmap = updated.Beatmaps.FirstOrDefault(b => b.Hash == beatmap.Hash);

                        if (updatedBeatmap != null)
                        {
                            // If the updated beatmap matches an existing one, transfer any user data across..
                            if (beatmap.Scores.Any())
                            {
                                Logger.Log($"Transferring {beatmap.Scores.Count()} scores for unchanged difficulty \"{beatmap}\"", LoggingTarget.Database);

                                foreach (var score in beatmap.Scores)
                                    score.BeatmapInfo = updatedBeatmap;
                            }

                            // ..then nuke the old beatmap completely.
                            // this is done instead of a soft deletion to avoid a user potentially creating weird
                            // interactions, like restoring the outdated beatmap then updating a second time
                            // (causing user data to be wiped).
                            original.Beatmaps.Remove(beatmap);

                            realm.Remove(beatmap.Metadata);
                            realm.Remove(beatmap);
                        }
                        // A difficulty written here and not uploaded -- a guest difficulty being made, most often -- is
                        // the user's own work, and is carried over rather than going down with the set being replaced,
                        // which is where it used to end up (2026-09-16 request). Saving in the editor is what marks it,
                        // so a difficulty the mapper took out of the set upstream is still removed, as before.
                        // One the update still has under the same name is left alone too: that is an edited copy of an
                        // online difficulty, and two of the same name in one set is worse than the old behaviour.
                        else if (beatmap.Status == BeatmapOnlineStatus.LocallyModified
                                 && updated.Beatmaps.All(b => !string.Equals(b.DifficultyName, beatmap.DifficultyName, StringComparison.Ordinal)))
                        {
                            keep(beatmap, original, updated);
                        }
                        else
                        {
                            // If the beatmap differs in the original, leave it in a soft-deleted state but reset online info.
                            // This caters to the case where a user has made modifications they potentially want to restore,
                            // but after restoring we want to ensure it can't be used to trigger an update of the beatmap.
                            beatmap.ResetOnlineInfo();
                        }
                    }

                    // If the original has no beatmaps left, delete the set as well.
                    if (!original.Beatmaps.Any())
                        realm.Remove(original);

                    Logger.Log($"Beatmap \"{updated}\" update completed successfully", LoggingTarget.Database);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to update beatmap \"{updated}\"", LoggingTarget.Database);
                    throw;
                }
            });

            return first;
        }

        /// <summary>
        /// Moves a difficulty the update does not have from the set being replaced into the updated set, with the file
        /// it is read from.
        /// </summary>
        private static void keep(BeatmapInfo beatmap, BeatmapSetInfo original, BeatmapSetInfo updated)
        {
            var file = beatmap.File;

            if (file == null)
            {
                // without its .osu there is nothing to carry over; leave it behind the way it was before.
                beatmap.ResetOnlineInfo();
                return;
            }

            string filename = file.Filename;

            // the updated set decides what its filenames mean, so one already taken is given way to.
            for (int attempt = 1; updated.Files.Any(f => string.Equals(f.Filename, filename, StringComparison.OrdinalIgnoreCase)); attempt++)
                filename = $"{Path.GetFileNameWithoutExtension(file.Filename)} ({attempt}){Path.GetExtension(file.Filename)}";

            updated.Files.Add(new RealmNamedFileUsage(file.File, filename));

            // the audio and background it was made with go along when the update has nothing under their names --
            // one renaming its audio left the difficulty with no track or waveform in the editor (DOTNET-8).
            // One the update has under the same name is what it reads from from now on, as with any other difficulty.
            foreach (string? referenced in new[] { beatmap.Metadata.AudioFile, beatmap.Metadata.BackgroundFile })
            {
                if (string.IsNullOrEmpty(referenced) || updated.GetFile(referenced) != null)
                    continue;

                var usage = original.GetFile(referenced);

                if (usage != null)
                    updated.Files.Add(new RealmNamedFileUsage(usage.File, usage.Filename));
            }

            original.Beatmaps.Remove(beatmap);
            updated.Beatmaps.Add(beatmap);
            beatmap.BeatmapSet = updated;

            // it is not part of the online set any more, so it must not be able to ask for an update of its own.
            beatmap.ResetOnlineInfo();

            Logger.Log($"Keeping difficulty \"{beatmap.DifficultyName}\", which the update does not have", LoggingTarget.Database);
        }

        private static void transferCollectionReferences(Realm realm, BeatmapSetInfo original, BeatmapSetInfo updated)
        {
            // First check if every beatmap in the original set is in any collections.
            // In this case, we will assume they also want any newly added difficulties added to the collection.
            foreach (var c in realm.All<BeatmapCollection>())
            {
                if (original.Beatmaps.Select(b => b.MD5Hash).All(c.BeatmapMD5Hashes.Contains))
                {
                    foreach (var b in original.Beatmaps)
                        c.BeatmapMD5Hashes.Remove(b.MD5Hash);

                    foreach (var b in updated.Beatmaps)
                        c.BeatmapMD5Hashes.Add(b.MD5Hash);
                }
            }

            // Handle collections using permissive difficulty name to track difficulties.
            foreach (var originalBeatmap in original.Beatmaps)
            {
                updated.Beatmaps
                       .FirstOrDefault(b => b.DifficultyName == originalBeatmap.DifficultyName)?
                       .TransferCollectionReferences(realm, originalBeatmap.MD5Hash);
            }
        }

        protected override bool ShouldDeleteArchive(string path) => HandledExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

        protected override void Populate(BeatmapSetInfo beatmapSet, ArchiveReader? archive, Realm realm, CancellationToken cancellationToken = default)
        {
            if (archive != null)
                beatmapSet.Beatmaps.AddRange(createBeatmapDifficulties(beatmapSet, realm));

            beatmapSet.DateAdded = getDateAdded(archive);

            foreach (BeatmapInfo b in beatmapSet.Beatmaps)
            {
                b.BeatmapSet = beatmapSet;

                // ensure we aren't trying to add a new ruleset to the database
                // this can happen in tests, mostly
                if (!b.Ruleset.IsManaged)
                    b.Ruleset = realm.Find<RulesetInfo>(b.Ruleset.ShortName) ?? throw new ArgumentNullException(nameof(b.Ruleset));
            }

            validateOnlineIds(beatmapSet, realm);

            bool hadOnlineIDs = beatmapSet.Beatmaps.Any(b => b.OnlineID > 0);

            // TODO: this may no longer be valid as we aren't doing an online population at this point.
            // ensure at least one beatmap was able to retrieve or keep an online ID, else drop the set ID.
            if (hadOnlineIDs && !beatmapSet.Beatmaps.Any(b => b.OnlineID > 0))
            {
                if (beatmapSet.OnlineID > 0)
                {
                    beatmapSet.OnlineID = -1;
                    LogForModel(beatmapSet, "Disassociating beatmap set ID due to loss of all beatmap IDs");
                }
            }
        }

        protected override void PreImport(BeatmapSetInfo beatmapSet, Realm realm)
        {
            // We are about to import a new beatmap. Before doing so, ensure that no other set shares the online IDs used by the new one.
            // Note that this means if the previous beatmap is restored by the user, it will no longer be linked to its online IDs.
            // If this is ever an issue, we can consider marking as pending delete but not resetting the IDs (but care will be required for
            // beatmaps, which don't have their own `DeletePending` state).

            if (beatmapSet.OnlineID > 0)
            {
                // Required local for iOS. Will cause runtime crash if inlined.
                int onlineId = beatmapSet.OnlineID;

                // OnlineID should really be unique, but to avoid catastrophic failure let's iterate just to be sure.
                foreach (var existingSetWithSameOnlineID in realm.All<BeatmapSetInfo>().Where(b => b.OnlineID == onlineId))
                {
                    existingSetWithSameOnlineID.DeletePending = true;
                    existingSetWithSameOnlineID.OnlineID = -1;

                    foreach (var b in existingSetWithSameOnlineID.Beatmaps)
                        b.ResetOnlineInfo();

                    LogForModel(beatmapSet, $"Found existing beatmap set with same OnlineID ({beatmapSet.OnlineID}). It will be disassociated and marked for deletion.");
                }
            }
        }

        protected override void PostImport(BeatmapSetInfo model, Realm realm, ImportParameters parameters)
        {
            base.PostImport(model, realm, parameters);

            // Scores are stored separately from beatmaps, and persist even when a beatmap is modified or deleted.
            // Let's reattach any matching scores that exist in the database, based on hash.
            foreach (BeatmapInfo beatmap in model.Beatmaps)
            {
                beatmap.UpdateLocalScores(realm);
            }

            ProcessBeatmap?.Invoke(model, parameters.Batch ? MetadataLookupScope.LocalCacheFirst : MetadataLookupScope.OnlineFirst);
        }

        private void validateOnlineIds(BeatmapSetInfo beatmapSet, Realm realm)
        {
            var beatmapIds = beatmapSet.Beatmaps.Where(b => b.OnlineID > 0).Select(b => b.OnlineID).ToList();

            // ensure all IDs are unique
            if (beatmapIds.GroupBy(b => b).Any(g => g.Count() > 1))
            {
                LogForModel(beatmapSet, "Found non-unique IDs, resetting...");
                resetIds();
                return;
            }

            // find any existing beatmaps in the database that have matching online ids
            List<BeatmapInfo> existingBeatmaps = new List<BeatmapInfo>();

            foreach (int id in beatmapIds)
                existingBeatmaps.AddRange(realm.All<BeatmapInfo>().Where(b => b.OnlineID == id));

            if (existingBeatmaps.Any())
            {
                // reset the import ids (to force a re-fetch) *unless* they match the candidate CheckForExisting set.
                // we can ignore the case where the new ids are contained by the CheckForExisting set as it will either be used (import skipped) or deleted.

                var existing = CheckForExisting(beatmapSet, realm);

                if (existing == null || existingBeatmaps.Any(b => !existing.Beatmaps.Contains(b)))
                {
                    LogForModel(beatmapSet, "Found existing import with online IDs already, resetting...");
                    resetIds();
                }
            }

            void resetIds() => beatmapSet.Beatmaps.ForEach(b => b.ResetOnlineInfo());
        }

        protected override bool CanSkipImport(BeatmapSetInfo existing, BeatmapSetInfo import)
        {
            if (!base.CanSkipImport(existing, import))
                return false;

            return existing.Beatmaps.Any(b => b.OnlineID > 0);
        }

        protected override bool CanReuseExisting(BeatmapSetInfo existing, BeatmapSetInfo import)
        {
            if (!base.CanReuseExisting(existing, import))
                return false;

            var existingIds = existing.Beatmaps.Select(b => b.OnlineID).Order();
            var importIds = import.Beatmaps.Select(b => b.OnlineID).Order();

            // force re-import if we are not in a sane state.
            return existing.OnlineID == import.OnlineID && existingIds.SequenceEqual(importIds);
        }

        protected override void UndeleteForReuse(BeatmapSetInfo existing)
        {
            if (!existing.DeletePending)
                return;

            base.UndeleteForReuse(existing);
            existing.DateAdded = DateTimeOffset.UtcNow;
        }

        public override string HumanisedModelName => "beatmap";

        protected override BeatmapSetInfo? CreateModel(ArchiveReader reader, ImportParameters parameters)
        {
            // let's make sure there are actually .osu files to import.
            string? mapName = reader.Filenames.FirstOrDefault(f => f.EndsWith(".osu", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(mapName))
            {
                Logger.Log($"No beatmap files found in the beatmap archive ({reader.Name}).", LoggingTarget.Database);
                return null;
            }

            Beatmap beatmap;

            using (var stream = new LineBufferedReader(reader.GetStream(mapName)))
            {
                if (stream.PeekLine() == null)
                {
                    Logger.Log($"No content found in first .osu file of beatmap archive ({reader.Name} / {mapName})", LoggingTarget.Database);
                    return null;
                }

                beatmap = Decoder.GetDecoder<Beatmap>(stream).Decode(stream);
            }

            return new BeatmapSetInfo
            {
                OnlineID = beatmap.BeatmapInfo.BeatmapSet?.OnlineID ?? -1,
            };
        }

        /// <summary>
        /// Determine the date a given beatmapset has been added to the game.
        /// For legacy imports, we can use the oldest file write time for any `.osu` file in the directory.
        /// For any other import types, use "now".
        /// </summary>
        private DateTimeOffset getDateAdded(ArchiveReader? reader)
        {
            DateTimeOffset dateAdded = DateTimeOffset.UtcNow;

            if (reader is DirectoryArchiveReader legacyReader)
            {
                var beatmaps = reader.Filenames.Where(f => f.EndsWith(".osu", StringComparison.OrdinalIgnoreCase));

                dateAdded = File.GetLastWriteTimeUtc(legacyReader.GetFullPath(beatmaps.First()));

                foreach (string beatmapName in beatmaps)
                {
                    var currentDateAdded = File.GetLastWriteTimeUtc(legacyReader.GetFullPath(beatmapName));

                    if (currentDateAdded < dateAdded)
                        dateAdded = currentDateAdded;
                }
            }

            return dateAdded;
        }

        /// <summary>
        /// Create all required <see cref="BeatmapInfo"/>s for the provided archive.
        /// </summary>
        private List<BeatmapInfo> createBeatmapDifficulties(BeatmapSetInfo beatmapSet, Realm realm)
        {
            var beatmaps = new List<BeatmapInfo>();

            // stable appears to ignore `.osu` files which are not placed at the top level of the beatmap archive.
            // the logic that achieves this is very difficult to make sense of, but appears to be located somewhere around
            // https://github.com/peppy/osu-stable-reference/blob/67795dba3c308e7d0493b296149dcb073ca47ecb/osu!/GameplayElements/Beatmaps/BeatmapManager.cs#L207-L208
            // only testing the `/` path separator character is sufficient as `RealmNamedFileUsage`s are normalised to use the front slash unix path separator convention
            foreach (var file in beatmapSet.Files.Where(f => !f.Filename.Contains('/') && f.Filename.EndsWith(@".osu", StringComparison.OrdinalIgnoreCase)))
            {
                using (var memoryStream = new MemoryStream(Files.Store.Get(file.File.GetStoragePath()))) // we need a memory stream so we can seek
                {
                    IBeatmap decoded;

                    using (var lineReader = new LineBufferedReader(memoryStream, true))
                    {
                        if (lineReader.PeekLine() == null)
                        {
                            LogForModel(beatmapSet, $"No content found in beatmap file {file.Filename}.");
                            continue;
                        }

                        decoded = Decoder.GetDecoder<Beatmap>(lineReader).Decode(lineReader);
                    }

                    string hash = memoryStream.ComputeSHA2Hash();

                    if (beatmaps.Any(b => b.Hash == hash))
                    {
                        LogForModel(beatmapSet, $"Skipping import of {file.Filename} due to duplicate file content.");
                        continue;
                    }

                    var decodedInfo = decoded.BeatmapInfo;
                    var decodedDifficulty = decodedInfo.Difficulty;

                    var ruleset = realm.All<RulesetInfo>().FirstOrDefault(r => r.OnlineID == decodedInfo.Ruleset.OnlineID);

                    if (ruleset?.Available != true)
                    {
                        LogForModel(beatmapSet, $"Skipping import of {file.Filename} due to missing local ruleset {decodedInfo.Ruleset.OnlineID}.");
                        continue;
                    }

                    var difficulty = new BeatmapDifficulty
                    {
                        DrainRate = decodedDifficulty.DrainRate,
                        CircleSize = decodedDifficulty.CircleSize,
                        OverallDifficulty = decodedDifficulty.OverallDifficulty,
                        ApproachRate = decodedDifficulty.ApproachRate,
                        SliderMultiplier = decodedDifficulty.SliderMultiplier,
                        SliderTickRate = decodedDifficulty.SliderTickRate
                    };

                    var metadata = new BeatmapMetadata
                    {
                        Title = decoded.Metadata.Title,
                        TitleUnicode = decoded.Metadata.TitleUnicode,
                        Artist = decoded.Metadata.Artist,
                        ArtistUnicode = decoded.Metadata.ArtistUnicode,
                        Author =
                        {
                            OnlineID = decoded.Metadata.Author.OnlineID,
                            Username = decoded.Metadata.Author.Username
                        },
                        Source = decoded.Metadata.Source,
                        Tags = decoded.Metadata.Tags,
                        PreviewTime = decoded.Metadata.PreviewTime,
                        AudioFile = decoded.Metadata.AudioFile,
                        BackgroundFile = decoded.Metadata.BackgroundFile,
                    };

                    var beatmap = new BeatmapInfo(ruleset, difficulty, metadata)
                    {
                        Hash = hash,
                        DifficultyName = decodedInfo.DifficultyName,
                        OnlineID = decodedInfo.OnlineID,
                        BeatDivisor = decodedInfo.BeatDivisor,
                        MD5Hash = memoryStream.ComputeMD5Hash(),
                        EndTimeObjectCount = decoded.HitObjects.Count(h => h is IHasDuration),
                        TotalObjectCount = decoded.HitObjects.Count
                    };

                    beatmaps.Add(beatmap);
                }
            }

            if (!beatmaps.Any())
                throw new ArgumentException("No valid beatmap files found in the beatmap archive.");

            return beatmaps;
        }
    }
}
