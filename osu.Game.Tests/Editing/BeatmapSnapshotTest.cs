// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Screens.Edit.Snapshots;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// What changed between two versions of a difficulty, and which versions are kept to be compared (2026-11-10).
    /// </summary>
    [TestFixture]
    public class BeatmapSnapshotTest
    {
        private static string file(string[]? objects = null, string[]? lines = null, string title = @"Song", string bookmarks = @"", string[]? events = null) => string.Join("\r\n",
            new[]
            {
                @"osu file format v14",
                @"",
                @"[General]",
                @"AudioFilename: audio.mp3",
                @"",
                @"[Editor]",
                $@"Bookmarks: {bookmarks}",
                @"TimelineZoom: 1",
                @"",
                @"[Metadata]",
                $@"Title:{title}",
                @"",
                @"[Difficulty]",
                @"ApproachRate:9",
                @"",
                @"[Events]",
                @"//Background and Video events",
            }
            .Concat(events ?? new[] { @"0,0,""bg.jpg"",0,0" })
            .Concat(new[] { @"", @"[TimingPoints]" })
            .Concat(lines ?? new[] { @"0,400,4,2,1,80,1,0" })
            .Concat(new[] { @"", @"[HitObjects]" })
            .Concat(objects ?? new[] { @"256,192,1000,1,0,0:0:0:0:" }));

        private static (SnapshotChangeKind, SnapshotSubject, double?, SnapshotAspects)[] changes(string before, string after) =>
            BeatmapSnapshotDiff.Compare(before, after).Select(c => (c.Kind, c.Subject, c.Time, c.Aspects)).ToArray();

        [Test]
        public void TestTheSameFileHasNoChanges()
        {
            Assert.That(BeatmapSnapshotDiff.Compare(file(), file()), Is.Empty);
        }

        [Test]
        public void TestObjectsAddedAndRemovedAreListedAtTheirTimes()
        {
            string before = file(new[] { @"256,192,1000,1,0,0:0:0:0:", @"100,100,2000,1,0,0:0:0:0:" });
            string after = file(new[] { @"256,192,1000,1,0,0:0:0:0:", @"100,100,3000,2,0,B|200:200,1,140" });

            Assert.That(changes(before, after), Is.EqualTo(new (SnapshotChangeKind, SnapshotSubject, double?, SnapshotAspects)[]
            {
                (SnapshotChangeKind.Removed, SnapshotSubject.Circle, 2000, SnapshotAspects.None),
                (SnapshotChangeKind.Added, SnapshotSubject.Slider, 3000, SnapshotAspects.None),
            }));
        }

        [Test]
        public void TestWhatOfAnObjectWasChangedIsSaid()
        {
            string before = file(new[] { @"256,192,1000,1,0,0:0:0:0:", @"100,100,2000,2,0,B|200:200,1,140,0|0,0:0|0:0,0:0:0:0:" });
            string after = file(new[] { @"300,192,1000,5,2,0:0:0:0:", @"100,100,2000,2,0,B|250:250,2,180,0|0,0:0|0:0,0:0:0:0:" });

            Assert.That(changes(before, after), Is.EqualTo(new (SnapshotChangeKind, SnapshotSubject, double?, SnapshotAspects)[]
            {
                (SnapshotChangeKind.Changed, SnapshotSubject.Circle, 1000, SnapshotAspects.Position | SnapshotAspects.NewCombo | SnapshotAspects.Hitsound),
                (SnapshotChangeKind.Changed, SnapshotSubject.Slider, 2000, SnapshotAspects.Shape | SnapshotAspects.Repeats | SnapshotAspects.Length),
            }));
        }

        /// <summary>
        /// Where things are and were is listed with them, the anchors of a slider among it (2026-11-11 request).
        /// </summary>
        [Test]
        public void TestCoordinatesAndAnchorsAreListed()
        {
            string before = file(new[] { @"256,192,1000,1,0,0:0:0:0:", @"100,100,2000,2,0,B|200:200,1,140", @"50,60,4000,1,0,0:0:0:0:" });
            string after = file(new[] { @"300,192.5,1000,1,0,0:0:0:0:", @"100,100,2000,2,0,P|250:250|300:100,2,180", @"64,64,5000,2,0,L|128:64,1,64" });

            var found = BeatmapSnapshotDiff.Compare(before, after);

            Assert.That(found.Select(c => (c.Kind, c.Time, string.Join(@" / ", c.Details.Select(d => $@"{d.Aspect}: {d.Before} > {d.After}")))), Is.EqualTo(new[]
            {
                (SnapshotChangeKind.Changed, (double?)1000, @"Position: (256, 192) > (300, 192.5)"),
                (SnapshotChangeKind.Changed, (double?)2000, @"Shape: Bezier (200, 200) > Perfect (250, 250) (300, 100) / Repeats: 0 > 1 / Length: 140 > 180"),
                (SnapshotChangeKind.Removed, (double?)4000, @"Position: (50, 60) > "),
                (SnapshotChangeKind.Added, (double?)5000, @"Position:  > (64, 64) / Shape:  > Linear (128, 64)"),
            }));
        }

        /// <summary>
        /// What a red or a green line was set to is listed with it.
        /// </summary>
        [Test]
        public void TestWhatALineWasSetToIsListed()
        {
            string before = file(lines: new[] { @"0,400,4,2,1,80,1,0", @"1000,-100,4,2,1,80,0,0" });
            string after = file(lines: new[] { @"0,375,3,2,1,80,1,0", @"1000,-50,4,2,1,60,0,0", @"2000,-80,4,2,1,60,0,0" });

            var found = BeatmapSnapshotDiff.Compare(before, after);

            Assert.That(found.Select(c => string.Join(@" / ", c.Details.Select(d => $@"{d.Aspect}: {d.Before} > {d.After}"))), Is.EqualTo(new[]
            {
                @"Bpm: 150 > 160 / Meter: 4/4 > 3/4",
                @"SliderVelocity: x1.00 > x2.00 / Volume: 80% > 60%",
                @"SliderVelocity:  > x1.25",
            }));
        }

        /// <summary>
        /// A number written another way is the same number: one format writes 256 where another writes 256.0.
        /// </summary>
        [Test]
        public void TestTheSameNumberWrittenAnotherWayIsNotAChange()
        {
            Assert.That(BeatmapSnapshotDiff.Compare(file(new[] { @"256,192,1000,1,0,0:0:0:0:" }), file(new[] { @"256.0,192.0,1000,1,0,0:0:0:0:" })), Is.Empty);
        }

        /// <summary>
        /// Two notes at one time, as in osu!mania, one of them moved: the one left alone is not listed.
        /// </summary>
        [Test]
        public void TestOfTwoAtOneTimeOnlyTheOneChangedIsListed()
        {
            string before = file(new[] { @"64,192,1000,1,0,0:0:0:0:", @"192,192,1000,1,0,0:0:0:0:" });
            string after = file(new[] { @"320,192,1000,1,0,0:0:0:0:", @"64,192,1000,1,0,0:0:0:0:" });

            Assert.That(changes(before, after), Is.EqualTo(new (SnapshotChangeKind, SnapshotSubject, double?, SnapshotAspects)[]
            {
                (SnapshotChangeKind.Changed, SnapshotSubject.Circle, 1000, SnapshotAspects.Position),
            }));
        }

        [Test]
        public void TestRedAndGreenLinesAreToldApart()
        {
            string before = file(lines: new[] { @"0,400,4,2,1,80,1,0", @"1000,-100,4,2,1,80,0,0" });
            string after = file(lines: new[] { @"0,375,4,2,1,80,1,0", @"1000,-50,4,2,1,60,0,1", @"2000,-100,4,2,1,80,0,0" });

            Assert.That(changes(before, after), Is.EqualTo(new (SnapshotChangeKind, SnapshotSubject, double?, SnapshotAspects)[]
            {
                (SnapshotChangeKind.Changed, SnapshotSubject.RedLine, 0, SnapshotAspects.Bpm),
                (SnapshotChangeKind.Changed, SnapshotSubject.GreenLine, 1000, SnapshotAspects.SliderVelocity | SnapshotAspects.Volume | SnapshotAspects.Kiai),
                (SnapshotChangeKind.Added, SnapshotSubject.GreenLine, 2000, SnapshotAspects.None),
            }));
        }

        [Test]
        public void TestASettingChangedIsListedWithBothValues()
        {
            var change = BeatmapSnapshotDiff.Compare(file(title: @"Song"), file(title: @"Song (TV Size)")).Single();

            Assert.That(change.Subject, Is.EqualTo(SnapshotSubject.Setting));
            Assert.That(change.Kind, Is.EqualTo(SnapshotChangeKind.Changed));
            Assert.That(change.Time, Is.Null);
            Assert.That((change.Name, change.Before, change.After), Is.EqualTo((@"Title", @"Song", @"Song (TV Size)")));
        }

        /// <summary>
        /// The rest of the editor's section is where the editor was left, and differs after every sitting.
        /// </summary>
        [Test]
        public void TestOfTheEditorsSectionOnlyTheBookmarksAreCompared()
        {
            string before = file(bookmarks: @"1000,2000");
            string after = file(bookmarks: @"1000,3000").Replace(@"TimelineZoom: 1", @"TimelineZoom: 2.5");

            Assert.That(changes(before, after), Is.EqualTo(new (SnapshotChangeKind, SnapshotSubject, double?, SnapshotAspects)[]
            {
                (SnapshotChangeKind.Removed, SnapshotSubject.Bookmark, 2000, SnapshotAspects.None),
                (SnapshotChangeKind.Added, SnapshotSubject.Bookmark, 3000, SnapshotAspects.None),
            }));
        }

        [Test]
        public void TestBreaksAndTheBackgroundAreListed()
        {
            string before = file(events: new[] { @"0,0,""bg.jpg"",0,0", @"2,5000,9000" });
            string after = file(events: new[] { @"0,0,""new.jpg"",0,0", @"2,5000,9500", @"2,20000,25000" });

            var found = BeatmapSnapshotDiff.Compare(before, after);

            Assert.That(found.Select(c => (c.Kind, c.Subject, c.Time, c.Name)), Is.EqualTo(new (SnapshotChangeKind, SnapshotSubject, double?, string?)[]
            {
                (SnapshotChangeKind.Removed, SnapshotSubject.Event, null, @"0,0,""bg.jpg"",0,0"),
                (SnapshotChangeKind.Added, SnapshotSubject.Event, null, @"0,0,""new.jpg"",0,0"),
                (SnapshotChangeKind.Changed, SnapshotSubject.Break, 5000, null),
                (SnapshotChangeKind.Added, SnapshotSubject.Break, 20000, null),
            }));
        }

        [Test]
        public void TestFilesOfTheSetAreComparedByWhatTheyHold()
        {
            var before = new Dictionary<string, string> { [@"audio.mp3"] = @"a", [@"bg.jpg"] = @"b", [@"old.wav"] = @"c" };
            var after = new Dictionary<string, string> { [@"audio.mp3"] = @"a", [@"bg.jpg"] = @"B", [@"new.wav"] = @"d" };

            Assert.That(BeatmapSnapshotDiff.CompareFiles(before, after).Select(c => (c.Kind, c.Name)), Is.EqualTo(new (SnapshotChangeKind, string?)[]
            {
                (SnapshotChangeKind.Changed, @"bg.jpg"),
                (SnapshotChangeKind.Added, @"new.wav"),
                (SnapshotChangeKind.Removed, @"old.wav"),
            }));
        }

        /// <summary>
        /// A day's snapshot is its last save, compared with the day before's, and on the first day with the file as the
        /// editor first opened it.
        /// </summary>
        [Test]
        public void TestADayIsItsLastSaveComparedWithTheDayBefore()
        {
            using (var storage = new TemporaryNativeStorage($@"snapshots-{Guid.NewGuid()}"))
            {
                var day = new DateTime(2026, 11, 10);
                var store = new BeatmapSnapshotStore(storage) { Today = () => day };
                var beatmap = new BeatmapInfo { BeatmapSet = new BeatmapSetInfo() };
                var files = new Dictionary<string, string> { [@"audio.mp3"] = @"a" };

                store.SetEnabled(beatmap, true);

                store.RecordOpened(beatmap, bytes(@"opened"), files);
                Assert.That(store.DaysOf(beatmap), Is.Empty);

                store.RecordSaved(beatmap, bytes(@"first save"), files);
                store.RecordSaved(beatmap, bytes(@"second save"), files);

                Assert.That(store.DaysOf(beatmap), Is.EqualTo(new[] { day }));
                Assert.That(store.DifficultyOn(beatmap, day), Is.EqualTo(((string?)@"opened", @"second save")));

                // opened again another day: what was opened is not the start of anything, the day before is.
                day = new DateTime(2026, 11, 12);
                store.RecordOpened(beatmap, bytes(@"second save"), files);

                // saved with nothing changed: the day is not listed.
                store.RecordSaved(beatmap, bytes(@"second save"), files);
                Assert.That(store.DaysOf(beatmap), Is.EqualTo(new[] { new DateTime(2026, 11, 10) }));

                store.RecordSaved(beatmap, bytes(@"next day"), new Dictionary<string, string> { [@"audio.mp3"] = @"a", [@"bg.jpg"] = @"b" });

                Assert.That(store.DaysOf(beatmap), Is.EqualTo(new[] { day, new DateTime(2026, 11, 10) }));
                Assert.That(store.DifficultyOn(beatmap, day), Is.EqualTo(((string?)@"second save", @"next day")));

                var set = store.SetOn(beatmap, day);

                Assert.That(set, Is.Not.Null);
                Assert.That(BeatmapSnapshotDiff.CompareFiles(set!.Value.before!, set.Value.after).Select(c => (c.Kind, c.Name)),
                    Is.EqualTo(new (SnapshotChangeKind, string?)[] { (SnapshotChangeKind.Added, @"bg.jpg") }));
            }
        }

        /// <summary>
        /// Another difficulty of the set saved on a day this one was not: the day is listed, for the set's sake.
        /// </summary>
        [Test]
        public void TestDifficultiesOfOneSetAreKeptApart()
        {
            using (var storage = new TemporaryNativeStorage($@"snapshots-{Guid.NewGuid()}"))
            {
                var day = new DateTime(2026, 11, 10);
                var store = new BeatmapSnapshotStore(storage) { Today = () => day };
                var set = new BeatmapSetInfo();
                var hard = new BeatmapInfo { BeatmapSet = set };
                var insane = new BeatmapInfo { BeatmapSet = set };
                var files = new Dictionary<string, string> { [@"audio.mp3"] = @"a" };

                store.SetEnabled(hard, true);
                Assert.That(store.IsEnabled(insane), Is.True, "chosen for the set, not for one difficulty of it");

                store.RecordSaved(hard, bytes(@"hard"), files);

                Assert.That(store.DaysOf(insane), Is.EqualTo(new[] { day }));
                Assert.That(store.DifficultyOn(insane, day), Is.Null);
                Assert.That(store.DifficultyOn(hard, day), Is.EqualTo(((string?)null, @"hard")));
            }
        }

        /// <summary>
        /// Every set of a collection would be a lot kept for little (2026-11-11): nothing is, until it is turned on.
        /// </summary>
        [Test]
        public void TestNothingIsKeptUntilTurnedOn()
        {
            using (var storage = new TemporaryNativeStorage($@"snapshots-{Guid.NewGuid()}"))
            {
                var day = new DateTime(2026, 11, 10);
                var store = new BeatmapSnapshotStore(storage) { Today = () => day };
                var beatmap = new BeatmapInfo { BeatmapSet = new BeatmapSetInfo() };
                var files = new Dictionary<string, string> { [@"audio.mp3"] = @"a" };

                store.RecordOpened(beatmap, bytes(@"opened"), files);
                store.RecordSaved(beatmap, bytes(@"saved"), files);

                Assert.That(store.DaysOf(beatmap), Is.Empty);
                Assert.That(storage.ExistsDirectory(BeatmapSnapshotStore.DIRECTORY), Is.False);

                // the mapper's own are turned on without asking, but not back on once turned off.
                store.EnableAsOwn(beatmap);
                Assert.That(store.IsEnabled(beatmap), Is.True);

                store.SetEnabled(beatmap, false);
                store.EnableAsOwn(beatmap);
                Assert.That(store.IsEnabled(beatmap), Is.False);
            }
        }

        /// <summary>
        /// An update from the website, or a change made outside the game: noticed as the editor opens the difficulty,
        /// with no save needed (2026-11-11).
        /// </summary>
        [Test]
        public void TestWhatChangedWithoutASaveIsKeptWhenOpened()
        {
            using (var storage = new TemporaryNativeStorage($@"snapshots-{Guid.NewGuid()}"))
            {
                var day = new DateTime(2026, 11, 10);
                var store = new BeatmapSnapshotStore(storage) { Today = () => day };
                var beatmap = new BeatmapInfo { BeatmapSet = new BeatmapSetInfo() };
                var files = new Dictionary<string, string> { [@"audio.mp3"] = @"a" };

                store.SetEnabled(beatmap, true);
                store.RecordOpened(beatmap, bytes(@"as it was"), files);

                day = new DateTime(2026, 11, 12);
                store.RecordOpened(beatmap, bytes(@"updated"), files);

                Assert.That(store.DaysOf(beatmap), Is.EqualTo(new[] { day }));
                Assert.That(store.DifficultyOn(beatmap, day), Is.EqualTo(((string?)@"as it was", @"updated")));

                // opened again the same day with nothing changed, then with something: still the one day.
                store.RecordOpened(beatmap, bytes(@"updated"), files);
                store.RecordOpened(beatmap, bytes(@"updated again"), files);

                Assert.That(store.DaysOf(beatmap), Is.EqualTo(new[] { day }));
                Assert.That(store.DifficultyOn(beatmap, day), Is.EqualTo(((string?)@"as it was", @"updated again")));
            }
        }

        /// <summary>
        /// An update from the website is a new set to the game, with new IDs, and the first upload gives a set the
        /// website's IDs: the history goes on through both.
        /// </summary>
        [Test]
        public void TestTheHistoryGoesOnThroughAnUploadAndAnUpdate()
        {
            using (var storage = new TemporaryNativeStorage($@"snapshots-{Guid.NewGuid()}"))
            {
                var day = new DateTime(2026, 11, 10);
                var store = new BeatmapSnapshotStore(storage) { Today = () => day };
                var files = new Dictionary<string, string> { [@"audio.mp3"] = @"a" };

                var local = new BeatmapInfo { BeatmapSet = new BeatmapSetInfo() };

                store.SetEnabled(local, true);
                store.RecordOpened(local, bytes(@"local"), files);

                // uploaded: the same to the game, and now known to the website.
                local.OnlineID = 4000;
                local.BeatmapSet!.OnlineID = 2000;

                day = new DateTime(2026, 11, 11);
                store.RecordSaved(local, bytes(@"uploaded"), files);

                // updated from the website: the website's IDs, and new ones of the game's.
                var updated = new BeatmapInfo { OnlineID = 4000, BeatmapSet = new BeatmapSetInfo { OnlineID = 2000 } };

                // a store made anew, as after the game is started again.
                store = new BeatmapSnapshotStore(storage) { Today = () => day };

                Assert.That(store.IsEnabled(updated), Is.True);

                day = new DateTime(2026, 11, 12);
                store.RecordOpened(updated, bytes(@"updated"), files);

                Assert.That(store.DaysOf(updated), Is.EqualTo(new[] { new DateTime(2026, 11, 12), new DateTime(2026, 11, 11) }));
                Assert.That(store.DifficultyOn(updated, new DateTime(2026, 11, 11)), Is.EqualTo(((string?)@"local", @"uploaded")));
                Assert.That(store.DifficultyOn(updated, new DateTime(2026, 11, 12)), Is.EqualTo(((string?)@"uploaded", @"updated")));
            }
        }

        /// <summary>
        /// A set told how many days to keep: what is older goes, but for the latest of it, which the earliest day kept
        /// is compared with.
        /// </summary>
        [Test]
        public void TestOnlyTheDaysAskedForAreKept()
        {
            using (var storage = new TemporaryNativeStorage($@"snapshots-{Guid.NewGuid()}"))
            {
                var day = new DateTime(2026, 11, 1);
                var store = new BeatmapSnapshotStore(storage) { Today = () => day };
                var beatmap = new BeatmapInfo { BeatmapSet = new BeatmapSetInfo() };
                var files = new Dictionary<string, string> { [@"audio.mp3"] = @"a" };

                store.SetEnabled(beatmap, true);
                store.RecordOpened(beatmap, bytes(@"opened"), files);

                foreach (int date in new[] { 1, 2, 3, 10 })
                {
                    day = new DateTime(2026, 11, date);
                    store.RecordSaved(beatmap, bytes($@"day {date}"), files);
                }

                Assert.That(store.DaysOf(beatmap).Count, Is.EqualTo(4), "all of them, until told otherwise");

                store.SetKeepDays(beatmap, 5);

                day = new DateTime(2026, 11, 11);
                store.RecordSaved(beatmap, bytes(@"day 11"), files);

                Assert.That(store.DaysOf(beatmap), Is.EqualTo(new[] { new DateTime(2026, 11, 11), new DateTime(2026, 11, 10), new DateTime(2026, 11, 3) }));
                Assert.That(store.DifficultyOn(beatmap, new DateTime(2026, 11, 10)), Is.EqualTo(((string?)@"day 3", @"day 10")));
                Assert.That(store.SettingsOf(beatmap).KeepDays, Is.EqualTo(5));
            }
        }

        private static byte[] bytes(string text) => Encoding.UTF8.GetBytes(text);
    }
}
