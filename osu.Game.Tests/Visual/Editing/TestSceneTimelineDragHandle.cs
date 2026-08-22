// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK;
using static osu.Game.Screens.Edit.Compose.Components.Timeline.TimelineHitObjectBlueprint;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The circle at the end of a timeline object, which is dragged to lengthen it. Its own fixture, because the size
    /// it rests at lives in the config and would otherwise carry into the next test.
    /// </summary>
    [TestFixture]
    public partial class TestSceneTimelineDragHandle : TimelineTestScene
    {
        public override Drawable CreateTestComponent() => new TimelineBlueprintContainer(Composer);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [SetUpSteps]
        public void SetUpDragHandleSteps()
        {
            AddStep("add slider", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(new Slider
                {
                    Position = new Vector2(256, 256),
                    StartTime = 2700,
                    Path = new SliderPath(PathType.LINEAR, [Vector2.Zero, new Vector2(100, 0)]),
                });
            });
        }

        [TearDownSteps]
        public void TearDownDragHandleSteps()
        {
            AddStep("restore handle size", () => config.GetBindable<float>(OsuSetting.EditorTimelineDragAreaScale).SetDefault());
        }

        // distinguishes the handle itself from the shadow drawn under it.
        private DragArea handle => this.ChildrenOfType<DragArea>().Single(d => d.HandlePositionalInput);

        [Test]
        public void TestRestingSizeFollowsSetting()
        {
            AddUntilStep("handle rests at half", () => handle.Scale.X, () => Is.EqualTo(0.5f).Within(0.01f));

            AddStep("fill the bar", () => config.SetValue(OsuSetting.EditorTimelineDragAreaScale, 1f));
            AddUntilStep("handle fills the bar", () => handle.Scale.X, () => Is.EqualTo(1f).Within(0.01f));
        }

        /// <remarks>
        /// The handle animates between three sizes of its own as it is pointed at and pressed, and the setting used to
        /// write over the same scale — so pointing at the handle threw the setting away and it never came back.
        /// </remarks>
        [Test]
        public void TestSettingSurvivesBeingPointedAt()
        {
            AddStep("fill the bar", () => config.SetValue(OsuSetting.EditorTimelineDragAreaScale, 1f));
            AddUntilStep("handle fills the bar", () => handle.Scale.X, () => Is.EqualTo(1f).Within(0.01f));

            AddStep("point at the handle", () => InputManager.MoveMouseTo(handle));
            AddUntilStep("handle grew from there", () => handle.Scale.X, () => Is.GreaterThan(1f));

            AddStep("point away", () => InputManager.MoveMouseTo(Vector2.Zero));
            AddUntilStep("handle returns to the setting", () => handle.Scale.X, () => Is.EqualTo(1f).Within(0.01f));
        }

        [Test]
        public void TestPointingAtHandleStillGrowsItAtTheDefault()
        {
            AddStep("point at the handle", () => InputManager.MoveMouseTo(handle));
            AddUntilStep("handle grew", () => handle.Scale.X, () => Is.GreaterThan(0.5f));

            AddStep("point away", () => InputManager.MoveMouseTo(Vector2.Zero));
            AddUntilStep("handle shrank back", () => Precision.AlmostEquals(handle.Scale.X, 0.5f, 0.01f));
        }
    }
}
