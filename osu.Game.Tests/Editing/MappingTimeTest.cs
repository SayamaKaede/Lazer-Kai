// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class MappingTimeTest
    {
        private static readonly TimeSpan offset = TimeSpan.FromHours(9);
        private static readonly TimeZoneInfo zone = TimeZoneInfo.CreateCustomTimeZone("test", offset, "test", "test");
        private static readonly DateTimeOffset t0 = new DateTimeOffset(2026, 10, 20, 21, 0, 0, offset);

        private TemporaryNativeStorage storage = null!;
        private MappingTimeStore store = null!;
        private MappingTimeRecorder recorder = null!;

        [SetUp]
        public void SetUp()
        {
            storage = new TemporaryNativeStorage("mapping-time");
            store = new MappingTimeStore(storage);
            recorder = new MappingTimeRecorder(store);
        }

        [TearDown]
        public void TearDown() => storage.Dispose();

        [Test]
        public void TestOpeningWithoutChangesCountsNothing()
        {
            recorder.Stop(t0 + TimeSpan.FromHours(1));

            Assert.That(store.Load(), Is.Empty);
        }

        [Test]
        public void TestChangesCloseTogetherAreOneStretchEndingWhereTheEditorIsLeft()
        {
            var beatmap = createBeatmap();

            recorder.Changed(beatmap, t0);
            recorder.Changed(beatmap, t0 + TimeSpan.FromMinutes(3));
            recorder.Changed(beatmap, t0 + TimeSpan.FromMinutes(7));
            recorder.Stop(t0 + TimeSpan.FromMinutes(8));

            var span = store.Load().Single();
            Assert.That(span.Start, Is.EqualTo(t0));
            Assert.That(span.End, Is.EqualTo(t0 + TimeSpan.FromMinutes(8)));
        }

        [Test]
        public void TestTimeWithoutChangesAfterTheLastIsCountedOnlyUpToTheLimit()
        {
            var beatmap = createBeatmap();

            recorder.Changed(beatmap, t0);
            recorder.Stop(t0 + TimeSpan.FromHours(2));

            Assert.That(store.Load().Single().End, Is.EqualTo(t0 + MappingTimeRecorder.IDLE_LIMIT));
        }

        [Test]
        public void TestALongPauseStartsAnotherStretch()
        {
            var beatmap = createBeatmap();

            recorder.Changed(beatmap, t0);
            recorder.Changed(beatmap, t0 + TimeSpan.FromMinutes(20));
            recorder.Stop(t0 + TimeSpan.FromMinutes(21));

            var spans = store.Load();
            Assert.That(spans.Select(s => (s.Start, s.End)), Is.EqualTo(new[]
            {
                (t0, t0 + MappingTimeRecorder.IDLE_LIMIT),
                (t0 + TimeSpan.FromMinutes(20), t0 + TimeSpan.FromMinutes(21)),
            }));
        }

        [Test]
        public void TestAStretchKeptGoingIsWrittenBeforeTheEditorIsLeft()
        {
            var beatmap = createBeatmap();

            recorder.Changed(beatmap, t0);
            recorder.Changed(beatmap, t0 + TimeSpan.FromMinutes(2));

            // nothing has stopped it: this is what is left if the game closes here.
            Assert.That(store.Load().Single().End, Is.EqualTo(t0 + TimeSpan.FromMinutes(2) + MappingTimeRecorder.IDLE_LIMIT));
        }

        [Test]
        public void TestDaysSplitAtMidnightAndSetsGatherTheirDifficulties()
        {
            var set = new BeatmapSetInfo();
            var insane = createBeatmap(set, "Insane");
            var hard = createBeatmap(set, "Hard");
            var other = createBeatmap(new BeatmapSetInfo(), "Normal", "Other");

            var midnight = new DateTimeOffset(2026, 10, 21, 0, 0, 0, offset);

            recorder.Changed(hard, t0);
            recorder.Stop(t0 + TimeSpan.FromMinutes(4));
            recorder.Changed(other, t0 + TimeSpan.FromMinutes(30));
            recorder.Stop(t0 + TimeSpan.FromMinutes(32));
            recorder.Changed(insane, midnight - TimeSpan.FromMinutes(10));
            recorder.Changed(insane, midnight - TimeSpan.FromMinutes(6));
            recorder.Changed(insane, midnight - TimeSpan.FromMinutes(2));
            recorder.Changed(insane, midnight + TimeSpan.FromMinutes(2));
            recorder.Stop(midnight + TimeSpan.FromMinutes(5));

            var summary = new MappingTimeSummary(store.Load(), zone);
            var day = new DateTime(2026, 10, 20);

            Assert.That(summary.TotalOn(day), Is.EqualTo(TimeSpan.FromMinutes(4 + 2 + 10)));
            Assert.That(summary.TotalOn(day.AddDays(1)), Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(summary.TotalIn(2026, 10), Is.EqualTo(TimeSpan.FromMinutes(21)));

            var sets = summary.SetsOn(day);
            Assert.That(sets.Select(s => (s.Title, s.Total)), Is.EqualTo(new[]
            {
                ("Song", TimeSpan.FromMinutes(14)),
                ("Other", TimeSpan.FromMinutes(2)),
            }));
            Assert.That(sets[0].Difficulties.Select(d => (d.DifficultyName, d.Total)), Is.EqualTo(new[]
            {
                ("Insane", TimeSpan.FromMinutes(10)),
                ("Hard", TimeSpan.FromMinutes(4)),
            }));

            Assert.That(summary.PiecesOn(day.AddDays(1)).Single().From, Is.EqualTo(day.AddDays(1)));
        }

        [Test]
        public void TestRunningTimeIsWrittenAsItGoesAndEndsWhereStopped()
        {
            var running = RunningTimeRecorder.ForGame(store, t0);

            running.Tick(t0 + TimeSpan.FromSeconds(30));
            Assert.That(store.Load().Single().End, Is.EqualTo(t0 + TimeSpan.FromSeconds(30)));

            running.Tick(t0 + TimeSpan.FromMinutes(2));
            Assert.That(store.Load().Single().End, Is.EqualTo(t0 + TimeSpan.FromMinutes(2)));

            running.Stop(t0 + TimeSpan.FromMinutes(3));
            Assert.That(store.Load().Single().End, Is.EqualTo(t0 + TimeSpan.FromMinutes(3)));
            Assert.That(store.Load().Single().Kind, Is.EqualTo(MappingSpanKind.Game));
        }

        [Test]
        public void TestTheTimeOpenCountsOnlyForWhatWasChangedThatDay()
        {
            var changed = createBeatmap(difficultyName: "Insane");
            var lookedAt = createBeatmap(difficultyName: "Hard", title: "Looked at");

            var game = RunningTimeRecorder.ForGame(store, t0 - TimeSpan.FromHours(1));

            var open = RunningTimeRecorder.ForBeatmap(store, changed, t0);
            recorder.Changed(changed, t0 + TimeSpan.FromMinutes(1));
            recorder.Stop(t0 + TimeSpan.FromMinutes(11));
            open.Stop(t0 + TimeSpan.FromMinutes(20));

            RunningTimeRecorder.ForBeatmap(store, lookedAt, t0 + TimeSpan.FromMinutes(30)).Stop(t0 + TimeSpan.FromMinutes(50));

            game.Stop(t0 + TimeSpan.FromHours(1));

            var summary = new MappingTimeSummary(store.Load(), zone);
            var day = new DateTime(2026, 10, 20);

            Assert.That(summary.GameOn(day), Is.EqualTo(TimeSpan.FromHours(2)));
            Assert.That(summary.OpenOn(day), Is.EqualTo(TimeSpan.FromMinutes(20)));
            // one change, so the stretch is the limit after it.
            Assert.That(summary.TotalOn(day), Is.EqualTo(MappingTimeRecorder.IDLE_LIMIT));

            var set = summary.SetsOn(day).Single();
            Assert.That((set.Total, set.Open), Is.EqualTo((MappingTimeRecorder.IDLE_LIMIT, TimeSpan.FromMinutes(20))));
            Assert.That(set.Difficulties.Single().Open, Is.EqualTo(TimeSpan.FromMinutes(20)));
        }

        private static BeatmapInfo createBeatmap(BeatmapSetInfo? set = null, string difficultyName = "Insane", string title = "Song")
        {
            var beatmap = new BeatmapInfo
            {
                DifficultyName = difficultyName,
                Metadata = new BeatmapMetadata { Artist = "Artist", Title = title },
                BeatmapSet = set ?? new BeatmapSetInfo(),
            };

            return beatmap;
        }
    }
}
