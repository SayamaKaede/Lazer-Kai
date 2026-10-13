// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Tests.Visual;
using osuTK.Input;

namespace osu.Game.Rulesets.Mania.Tests.Editor
{
    public partial class TestSceneObjectPlacement : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new ManiaRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Test]
        public void TestPlacementBeforeTrackStart()
        {
            AddStep("Seek to 0", () => EditorClock.Seek(0));
            AddStep("Select note", () => InputManager.Key(Key.Number2));
            AddStep("Hover negative span", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<Container>().First(x => x.Name == "Icons").Children[0]);
            });
            AddStep("Click", () => InputManager.Click(MouseButton.Left));
            AddAssert("No notes placed", () => EditorBeatmap.HitObjects.All(x => x.StartTime >= 0));
        }

        [Test]
        public void TestSeekOnNotePlacement()
        {
            double? initialTime = null;

            AddStep("store initial time", () => initialTime = EditorClock.CurrentTime);
            AddStep("change seek setting to true", () => config.SetValue(OsuSetting.EditorAutoSeekOnPlacement, true));
            placeObject();
            AddUntilStep("wait for seek to complete", () => !EditorClock.IsSeeking);
            AddAssert("seeked forward to object", () => EditorClock.CurrentTime, () => Is.GreaterThan(initialTime!));
        }

        [Test]
        public void TestNoSeekOnNotePlacement()
        {
            double? initialTime = null;

            AddStep("store initial time", () => initialTime = EditorClock.CurrentTime);
            AddStep("change seek setting to false", () => config.SetValue(OsuSetting.EditorAutoSeekOnPlacement, false));
            placeObject();
            AddAssert("not seeking", () => !EditorClock.IsSeeking);
            AddAssert("time is unchanged", () => EditorClock.CurrentTime, () => Is.EqualTo(initialTime));
        }

        [Test]
        public void TestNoTwoObjectsAtSameTimeAndColumn()
        {
            AddStep("change seek setting to false", () => config.SetValue(OsuSetting.EditorAutoSeekOnPlacement, false));
            AddStep("clear beatmap", () => EditorBeatmap.Clear());

            AddStep("select note placement tool", () => InputManager.Key(Key.Number2));
            AddStep("move mouse to centre of last column", () => InputManager.MoveMouseTo(this.ChildrenOfType<Column>().Last().ScreenSpaceDrawQuad.Centre));
            AddStep("place note", () => InputManager.Click(MouseButton.Left));
            AddAssert("beatmap has 1 object", () => EditorBeatmap.HitObjects, () => Has.Count.EqualTo(1));

            AddStep("select note placement tool", () => InputManager.Key(Key.Number2));
            AddStep("move mouse to centre of first column", () => InputManager.MoveMouseTo(this.ChildrenOfType<Column>().First().ScreenSpaceDrawQuad.Centre));
            AddStep("place note", () => InputManager.Click(MouseButton.Left));
            AddAssert("beatmap has 2 objects", () => EditorBeatmap.HitObjects, () => Has.Count.EqualTo(2));

            AddStep("select note placement tool", () => InputManager.Key(Key.Number2));
            AddStep("move mouse to centre of last column", () => InputManager.MoveMouseTo(this.ChildrenOfType<Column>().Last().ScreenSpaceDrawQuad.Centre));
            AddStep("place note", () => InputManager.Click(MouseButton.Left));
            AddAssert("beatmap has 2 objects", () => EditorBeatmap.HitObjects, () => Has.Count.EqualTo(2));
        }

        /// <summary>
        /// Seeking on by the spacing before a note just placed has a setting of its own in osu!mania, off unless it is
        /// turned on: notes are put down side by side at one time here, and the setting of the other rulesets, made for
        /// placing a rhythm one object after another, takes the playhead away after the first of them (2026-10-14).
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void TestSeekingOnByThePreviousSpacingIsOsuManiasOwnSetting(bool on)
        {
            double? initialTime = null;

            AddAssert("off unless it is turned on", () => config.GetBindable<bool>(OsuSetting.EditorManiaAdvanceByPreviousSpacingOnPlacement).Default, () => Is.False);

            AddStep("no seeking to what is placed", () => config.SetValue(OsuSetting.EditorAutoSeekOnPlacement, false));
            AddStep("the setting of the other rulesets on", () => config.SetValue(OsuSetting.EditorAdvanceByPreviousSpacingOnPlacement, true));
            AddStep($"osu!mania's own {(on ? "on" : "off")}", () => config.SetValue(OsuSetting.EditorManiaAdvanceByPreviousSpacingOnPlacement, on));

            AddStep("only a note at the start, for there to be a spacing", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(new Note { StartTime = 0, Column = 0 });
            });
            AddStep("seek on a little", () => EditorClock.Seek(1000));
            AddStep("store initial time", () => initialTime = EditorClock.CurrentTime);

            placeObject();
            AddUntilStep("wait for seek to complete", () => !EditorClock.IsSeeking);

            if (on)
                AddAssert("seeked on", () => EditorClock.CurrentTime, () => Is.GreaterThan(initialTime!));
            else
                AddAssert("time is unchanged", () => EditorClock.CurrentTime, () => Is.EqualTo(initialTime));

            AddStep("settings back", () =>
            {
                config.GetBindable<bool>(OsuSetting.EditorAutoSeekOnPlacement).SetDefault();
                config.GetBindable<bool>(OsuSetting.EditorAdvanceByPreviousSpacingOnPlacement).SetDefault();
                config.GetBindable<bool>(OsuSetting.EditorManiaAdvanceByPreviousSpacingOnPlacement).SetDefault();
            });
        }

        private void placeObject()
        {
            AddStep("select note placement tool", () => InputManager.Key(Key.Number2));
            AddStep("move mouse to centre of last column", () => InputManager.MoveMouseTo(this.ChildrenOfType<Column>().Last().ScreenSpaceDrawQuad.Centre));
            AddStep("place note", () => InputManager.Click(MouseButton.Left));
        }
    }
}
