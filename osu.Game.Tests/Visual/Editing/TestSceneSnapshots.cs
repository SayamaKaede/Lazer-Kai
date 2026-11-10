// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Snapshots;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// What changed on a day, listed beside the beatmap, with the times going there when clicked (2026-11-10).
    /// </summary>
    public partial class TestSceneSnapshots : EditorSavingTestScene
    {
        protected override Ruleset CreateRuleset() => new OsuRuleset();

        private SnapshotOverlay panel => Editor.ChildrenOfType<SnapshotOverlay>().Single();

        private BeatmapSnapshotStore store => (BeatmapSnapshotStore)Editor.Dependencies.Get(typeof(BeatmapSnapshotStore));

        private EditorClock clock => (EditorClock)Editor.Dependencies.Get(typeof(EditorClock));

        [Test]
        public void TestWhatWasSavedOnADayIsListedAndItsTimeGoneTo()
        {
            AddStep("the tenth", () => store.Today = () => new DateTime(2026, 11, 10));
            AddStep("a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 200, Position = new Vector2(100) }));
            SaveEditor();
            AddUntilStep("the tenth is kept", () => store.DaysOf(EditorBeatmap.BeatmapInfo), () => Is.EqualTo(new[] { new DateTime(2026, 11, 10) }));

            AddStep("the eleventh", () => store.Today = () => new DateTime(2026, 11, 11));
            AddStep("another circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 600, Position = new Vector2(200) }));
            SaveEditor();
            AddUntilStep("the eleventh is kept", () => store.DaysOf(EditorBeatmap.BeatmapInfo).Count, () => Is.EqualTo(2));

            AddStep("open the panel", () => panel.Show());
            AddUntilStep("the later day is the one shown", () => panel.Days.Current.Value, () => Is.EqualTo(@"2026-11-11"));

            AddUntilStep("the circle added that day is listed", () => panel.Rows.Select(r => (r.Change.Kind, r.Change.Subject, r.Change.Time)),
                () => Does.Contain((SnapshotChangeKind.Added, SnapshotSubject.Circle, (double?)600)));
            AddAssert("the one from the day before is not", () => panel.Rows.Any(r => r.Change.Time == 200), () => Is.False);

            AddStep("click its time", () =>
            {
                InputManager.MoveMouseTo(panel.Rows.First(r => r.Change.Time == 600).Timestamp!);
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("the editor is there", () => clock.CurrentTime, () => Is.EqualTo(600).Within(1));
        }

        /// <summary>
        /// A set of the mapper's own is recorded without asking; turned off in the panel, a save keeps nothing, and
        /// how long it is kept for is the set's to say (2026-11-11).
        /// </summary>
        [Test]
        public void TestRecordingIsTurnedOffAndOnFromThePanel()
        {
            AddAssert("on for the mapper's own", () => store.IsEnabled(EditorBeatmap.BeatmapInfo), () => Is.True);

            AddStep("the tenth", () => store.Today = () => new DateTime(2026, 11, 10));
            AddStep("open the panel", () => panel.Show());
            AddUntilStep("ticked", () => panel.Recording.Current.Value, () => Is.True);

            AddStep("untick", () => panel.Recording.Current.Value = false);
            AddAssert("off", () => store.IsEnabled(EditorBeatmap.BeatmapInfo), () => Is.False);

            AddStep("a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 200, Position = new Vector2(100) }));
            SaveEditor();
            AddWaitStep("let it save", 5);
            AddAssert("nothing kept", () => store.DaysOf(EditorBeatmap.BeatmapInfo), () => Is.Empty);

            AddStep("tick", () => panel.Recording.Current.Value = true);
            AddAssert("on", () => store.IsEnabled(EditorBeatmap.BeatmapInfo), () => Is.True);

            AddStep("the eleventh", () => store.Today = () => new DateTime(2026, 11, 11));
            AddStep("another circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 400, Position = new Vector2(150) }));
            SaveEditor();
            // of the difficulty, that is: the set's own list has the difficulty's file in it as one that changed.
            AddUntilStep("only what came after it was turned on is listed", () => panel.Rows.Where(r => r.Change.Subject != SnapshotSubject.File).Select(r => r.Change.Time),
                () => Is.EqualTo(new double?[] { 400 }));

            AddStep("keep thirty days", () => panel.KeepDaysBox.Current.Value = @"30");
            AddUntilStep("written down", () => store.SettingsOf(EditorBeatmap.BeatmapInfo).KeepDays, () => Is.EqualTo(30));
            AddAssert("the slider is there too", () => panel.KeepDaysSlider.Current.Value, () => Is.EqualTo(30));

            AddStep("emptied", () => panel.KeepDaysBox.Current.Value = string.Empty);
            AddUntilStep("all of them again", () => store.SettingsOf(EditorBeatmap.BeatmapInfo).KeepDays, () => Is.EqualTo(0));
        }

        /// <summary>
        /// Saved again while the panel is open: what is listed is brought up to date without closing it.
        /// </summary>
        [Test]
        public void TestASaveWhileOpenIsListedAtOnce()
        {
            AddStep("the tenth", () => store.Today = () => new DateTime(2026, 11, 10));
            AddStep("a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 200, Position = new Vector2(100) }));
            SaveEditor();

            AddStep("the eleventh", () => store.Today = () => new DateTime(2026, 11, 11));
            AddStep("another circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 400, Position = new Vector2(150) }));
            SaveEditor();
            AddUntilStep("the eleventh is kept", () => store.DaysOf(EditorBeatmap.BeatmapInfo).Count, () => Is.EqualTo(2));

            AddStep("open the panel", () => panel.Show());
            AddUntilStep("the day's circle is listed", () => panel.Rows.Any(r => r.Change.Time == 400));

            // the track of a beatmap made from nothing is a second long, and the times are kept inside it.
            AddStep("a third circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 600, Position = new Vector2(200) }));
            SaveEditor();

            AddUntilStep("it is listed", () => panel.Rows.Any(r => r.Change.Time == 600));
            AddAssert("the day is still the one looked at", () => panel.Days.Current.Value, () => Is.EqualTo(@"2026-11-11"));
        }
    }
}
