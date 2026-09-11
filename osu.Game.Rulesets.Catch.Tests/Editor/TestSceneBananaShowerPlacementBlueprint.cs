// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Rulesets.Catch.Edit.Blueprints;
using osu.Game.Rulesets.Catch.Edit.Blueprints.Components;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Objects.Drawables;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osuTK.Input;

namespace osu.Game.Rulesets.Catch.Tests.Editor
{
    public partial class TestSceneBananaShowerPlacementBlueprint : CatchPlacementBlueprintTestScene
    {
        protected override DrawableHitObject CreateHitObject(HitObject hitObject) => new DrawableBananaShower((BananaShower)hitObject);

        protected override HitObjectPlacementBlueprint CreateBlueprint() => new BananaShowerPlacementBlueprint();

        protected override void AddHitObject(DrawableHitObject hitObject)
        {
            // Create nested bananas (but positions are not randomized because beatmap processing is not done).
            hitObject.HitObject.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            base.AddHitObject(hitObject);
        }

        [Test]
        public void TestBasicPlacement()
        {
            const double start_time = 100;
            const double end_time = 500;

            AddMoveStep(start_time, 0);
            AddClickStep(MouseButton.Left);
            AddMoveStep(end_time, 0);
            AddClickStep(MouseButton.Right);
            AddAssert("banana shower is placed", () => LastObject is DrawableBananaShower);
            AddAssert("start time is correct", () => Precision.AlmostEquals(LastObject.HitObject.StartTime, start_time));
            AddAssert("end time is correct", () => Precision.AlmostEquals(LastObject.HitObject.GetEndTime(), end_time));
        }

        [Test]
        public void TestReversePlacement()
        {
            const double start_time = 100;
            const double end_time = 500;

            AddMoveStep(end_time, 0);
            AddClickStep(MouseButton.Left);

            AddMoveStep(start_time, 0);
            AddAssert("duration is positive", () => ((BananaShower)CurrentBlueprint.HitObject).Duration > 0);

            AddClickStep(MouseButton.Right);
            AddAssert("start time is correct", () => Precision.AlmostEquals(LastObject.HitObject.StartTime, start_time));
            AddAssert("end time is correct", () => Precision.AlmostEquals(LastObject.HitObject.GetEndTime(), end_time));
        }

        [Test]
        public void TestFinishWithZeroDuration()
        {
            AddMoveStep(100, 0);
            AddClickStep(MouseButton.Left);
            AddClickStep(MouseButton.Right);
            AddAssert("banana shower is not placed", () => LastObject == null);
            AddAssert("state is waiting", () => CurrentBlueprint?.PlacementActive == PlacementBlueprint.PlacementState.Waiting);
        }

        [Test]
        public void TestOpacity()
        {
            AddMoveStep(100, 0);
            AddClickStep(MouseButton.Left);
            AddUntilStep("outline is semitransparent", () => Precision.DefinitelyBigger(1, timeSpanOutline.Alpha));
            AddMoveStep(200, 0);
            AddUntilStep("outline is opaque", () => Precision.AlmostEquals(timeSpanOutline.Alpha, 1));
            AddMoveStep(100, 0);
            AddUntilStep("outline is semitransparent", () => Precision.DefinitelyBigger(1, timeSpanOutline.Alpha));
        }

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [TearDownSteps]
        public void RestoreButtons() => AddStep("buttons back as they were", () => config.GetBindable<bool>(OsuSetting.SwapRightAndMiddleMouse).SetDefault());

        /// <summary>
        /// With right and middle traded, the physical right button reaches here as the middle one — the trade happens
        /// where input comes in, so pressing middle here is what the right button does in the game. It still has to be
        /// the one that finishes, the way it is for osu!'s spinners.
        /// </summary>
        [Test]
        public void TestTheRightButtonStillFinishesWithTheButtonsSwapped()
        {
            AddStep("swap right and middle", () => config.SetValue(OsuSetting.SwapRightAndMiddleMouse, true));

            AddMoveStep(100, 0);
            AddClickStep(MouseButton.Left);
            AddMoveStep(500, 0);
            AddClickStep(MouseButton.Middle);

            AddAssert("banana shower is placed", () => LastObject is DrawableBananaShower);
        }

        /// <summary>
        /// And the physical middle button, which now arrives as the right one, does not.
        /// </summary>
        [Test]
        public void TestTheMiddleButtonDoesNotFinishWithTheButtonsSwapped()
        {
            AddStep("swap right and middle", () => config.SetValue(OsuSetting.SwapRightAndMiddleMouse, true));

            AddMoveStep(100, 0);
            AddClickStep(MouseButton.Left);
            AddMoveStep(500, 0);
            AddClickStep(MouseButton.Right);

            AddAssert("still being placed", () => LastObject == null);
        }

        private TimeSpanOutline timeSpanOutline => Content.ChildrenOfType<TimeSpanOutline>().Single();
    }
}
