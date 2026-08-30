// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Writing into Mapping Tools' own settings file, which is the only way to hand it a beatmap.
    /// </summary>
    /// <remarks>
    /// This is another program's file and it is read on the way up: getting it wrong does not show as a mark in
    /// the wrong place, it shows as Mapping Tools not starting. Everything here is about leaving the rest of the
    /// file exactly as it was found.
    /// </remarks>
    [TestFixture]
    public class MappingToolsHandoffTest
    {
        private static readonly DateTime now = new DateTime(2026, 8, 30, 4, 5, 6, DateTimeKind.Local);

        [Test]
        public void TestBeatmapGoesToTheTop()
        {
            string result = MappingToolsHandoff.PutAtTopOfRecentMaps(configWith(@"C:\old.osu"), @"C:\new.osu", now);

            Assert.That(paths(result), Is.EqualTo(new[] { @"C:\new.osu", @"C:\old.osu" }));
        }

        /// <summary>
        /// Mapping Tools reads the date beside the path to show in its list, so it has to be the shape it writes.
        /// </summary>
        [Test]
        public void TestTheDateIsWrittenTheWayMappingToolsWritesIt()
        {
            string result = MappingToolsHandoff.PutAtTopOfRecentMaps(configWith(), @"C:\new.osu", now);

            Assert.That((string?)recent(result)[0]![1], Is.EqualTo(@"2026/08/30 4:05:06"));
        }

        /// <summary>
        /// A beatmap handed over twice is moved rather than repeated, the same as Mapping Tools does to its own
        /// list. Otherwise the list fills with one beatmap and pushes everything else off the end.
        /// </summary>
        [Test]
        public void TestTheSameBeatmapIsMovedNotRepeated()
        {
            string result = MappingToolsHandoff.PutAtTopOfRecentMaps(configWith(@"C:\a.osu", @"C:\new.osu", @"C:\b.osu"), @"C:\new.osu", now);

            Assert.That(paths(result), Is.EqualTo(new[] { @"C:\new.osu", @"C:\a.osu", @"C:\b.osu" }));
        }

        [Test]
        public void TestTheMatchIgnoresCase()
        {
            string result = MappingToolsHandoff.PutAtTopOfRecentMaps(configWith(@"C:\NEW.osu"), @"C:\new.osu", now);

            Assert.That(paths(result), Is.EqualTo(new[] { @"C:\new.osu" }));
        }

        [Test]
        public void TestTheListIsKeptToTwenty()
        {
            string result = MappingToolsHandoff.PutAtTopOfRecentMaps(
                configWith(Enumerable.Range(0, 20).Select(i => $@"C:\{i}.osu").ToArray()), @"C:\new.osu", now);

            Assert.That(recent(result), Has.Count.EqualTo(20));
            Assert.That(paths(result).First(), Is.EqualTo(@"C:\new.osu"));
            Assert.That(paths(result), Does.Not.Contain(@"C:\19.osu"));
        }

        /// <summary>
        /// The whole point of going through the file rather than replacing it.
        /// </summary>
        [Test]
        public void TestEverythingElseIsLeftAlone()
        {
            const string config = @"{
              ""RecentMaps"": [],
              ""OsuPath"": ""C:\\osu!"",
              ""MainWindowRestoreBounds"": { ""X"": 10, ""Y"": 20 },
              ""FavoriteTools"": [ ""Map Cleaner"" ]
            }";

            var result = JObject.Parse(MappingToolsHandoff.PutAtTopOfRecentMaps(config, @"C:\new.osu", now));

            Assert.That((string?)result[@"OsuPath"], Is.EqualTo(@"C:\osu!"));
            Assert.That((int?)result[@"MainWindowRestoreBounds"]![@"X"], Is.EqualTo(10));
            Assert.That(result[@"FavoriteTools"]!.Values<string>(), Is.EqualTo(new[] { @"Map Cleaner" }));
        }

        /// <summary>
        /// A settings file from a fresh install has no recent list at all.
        /// </summary>
        [Test]
        public void TestAConfigWithNoRecentListGainsOne()
        {
            string result = MappingToolsHandoff.PutAtTopOfRecentMaps(@"{ ""OsuPath"": ""C:\\osu!"" }", @"C:\new.osu", now);

            Assert.That(paths(result), Is.EqualTo(new[] { @"C:\new.osu" }));
        }

        /// <summary>
        /// Several beatmaps at once are held as one entry with the paths joined by a pipe, so an entry is matched
        /// whole rather than by looking for a path inside it.
        /// </summary>
        [Test]
        public void TestAMultipleMapEntryIsLeftIntact()
        {
            string result = MappingToolsHandoff.PutAtTopOfRecentMaps(configWith(@"C:\a.osu|C:\b.osu"), @"C:\a.osu", now);

            Assert.That(paths(result), Is.EqualTo(new[] { @"C:\a.osu", @"C:\a.osu|C:\b.osu" }));
        }

        /// <summary>
        /// A path that was set and really is Mapping Tools is the one used, whatever is installed elsewhere.
        /// </summary>
        /// <remarks>
        /// Only run where there is a Mapping Tools to point at, since the check reads the file rather than its
        /// name and nothing that can be written here would pass it.
        /// </remarks>
        [Test]
        public void TestAConfiguredPathIsUsed()
        {
            if (!File.Exists(MappingToolsHandoff.DefaultExecutablePath))
                Assert.Ignore(@"Mapping Tools is not installed on this machine.");

            Assert.That(MappingToolsHandoff.FindExecutable(MappingToolsHandoff.DefaultExecutablePath),
                Is.EqualTo(MappingToolsHandoff.DefaultExecutablePath));
        }

        /// <summary>
        /// A path pointing at something that is not Mapping Tools is refused, however real the file is.
        /// </summary>
        /// <remarks>
        /// Handing over writes into Mapping Tools' own settings before starting anything, so a path typed wrong
        /// must not get as far as starting: it would edit Mapping Tools' settings and then run something else.
        /// </remarks>
        [Test]
        public void TestSomethingThatIsNotMappingToolsIsRefused()
        {
            string path = Path.Combine(Path.GetTempPath(), $"lazer-kai-decoy-{Guid.NewGuid()}.exe");
            File.WriteAllText(path, @"this is not a program at all");

            try
            {
                Assert.That(MappingToolsHandoff.IsMappingTools(path), Is.False);
                Assert.That(MappingToolsHandoff.FindExecutable(path), Is.Null);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// A path that was set and is not there answers with nothing, rather than quietly starting whatever
        /// happens to be installed in the usual place instead.
        /// </summary>
        /// <remarks>
        /// The fallback is for people who have set nothing. Applying it to someone who has said exactly which
        /// copy they meant would start a different one and never mention that the path was wrong — and the
        /// button would look ready on a machine where it is not.
        /// </remarks>
        [Test]
        public void TestAConfiguredPathThatIsNotThereDoesNotFallBack()
        {
            Assert.That(MappingToolsHandoff.FindExecutable(Path.Combine(Path.GetTempPath(), $"lazer-kai-missing-{Guid.NewGuid()}.exe")), Is.Null);
        }

        /// <summary>
        /// Blank is not "set to nowhere", it is "not set", and that is what the usual install is for.
        /// </summary>
        [TestCase("")]
        [TestCase("   ")]
        public void TestNothingSetLooksForTheUsualInstall(string configured)
        {
            Assert.That(MappingToolsHandoff.FindExecutable(configured),
                Is.EqualTo(MappingToolsHandoff.IsMappingTools(MappingToolsHandoff.DefaultExecutablePath) ? MappingToolsHandoff.DefaultExecutablePath : null));
        }

        private static string configWith(params string[] recentPaths)
        {
            var config = new JObject
            {
                [@"RecentMaps"] = new JArray(recentPaths.Select(p => new JArray(p, @"2026/08/01 1:00:00"))),
            };

            return config.ToString();
        }

        private static JArray recent(string configJson) => (JArray)JObject.Parse(configJson)[@"RecentMaps"]!;

        private static string?[] paths(string configJson) => recent(configJson).Select(e => (string?)e[0]).ToArray();
    }
}
