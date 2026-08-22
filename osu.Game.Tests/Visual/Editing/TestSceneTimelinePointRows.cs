// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The velocity and volume rows above and below the timeline's objects can be turned off, and the objects take the
    /// space. Its own fixture, because the setting lives in the config and would otherwise carry into the next test.
    /// </summary>
    [TestFixture]
    public partial class TestSceneTimelinePointRows : TimelineTestScene
    {
        public override Drawable CreateTestComponent() => new TimelineBlueprintContainer(Composer);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [SetUpSteps]
        public void SetUpPointRowSteps()
        {
            AddStep("show the rows", () => config.SetValue(OsuSetting.EditorHideTimelinePoints, false));

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
        public void TearDownPointRowSteps()
        {
            AddStep("restore the rows", () => config.GetBindable<bool>(OsuSetting.EditorHideTimelinePoints).SetDefault());
        }

        private TimelineHitObjectBlueprint blueprint => this.ChildrenOfType<TimelineHitObjectBlueprint>().Single();

        /// <summary>
        /// Whether a piece is drawn at all, which the pieces on a slider's repeats are not by being hidden together
        /// rather than one at a time.
        /// </summary>
        private static bool drawn<T>(T piece) where T : Drawable
        {
            for (Drawable? current = piece; current != null && current is not TimelineHitObjectBlueprint; current = current.Parent)
            {
                if (!current.IsPresent)
                    return false;
            }

            return true;
        }

        [Test]
        public void TestRowsCanBeHidden()
        {
            float shownHeight = 0;

            AddStep("read height", () => shownHeight = blueprint.DrawHeight);
            AddAssert("velocity row is there", () => this.ChildrenOfType<DifficultyPointPiece>().Any(drawn));
            AddAssert("volume row is there", () => this.ChildrenOfType<SamplePointPiece>().Any(drawn));

            AddStep("hide the rows", () => config.SetValue(OsuSetting.EditorHideTimelinePoints, true));

            AddUntilStep("object grew", () => blueprint.DrawHeight, () => Is.GreaterThan(shownHeight));
            AddAssert("velocity row is gone", () => this.ChildrenOfType<DifficultyPointPiece>().All(p => !drawn(p)));
            AddAssert("volume row is gone", () => this.ChildrenOfType<SamplePointPiece>().All(p => !drawn(p)));

            AddStep("show the rows again", () => config.SetValue(OsuSetting.EditorHideTimelinePoints, false));

            AddUntilStep("object went back", () => blueprint.DrawHeight, () => Is.EqualTo(shownHeight));
            AddAssert("volume row is back", () => this.ChildrenOfType<SamplePointPiece>().Any(drawn));
        }

        /// <remarks>
        /// The point of the setting is a bigger target to grab, so the object has to be selectable over the space it
        /// has taken, not just drawn over it.
        /// </remarks>
        [Test]
        public void TestGrownObjectIsSelectableOverItsWholeHeight()
        {
            AddStep("hide the rows", () => config.SetValue(OsuSetting.EditorHideTimelinePoints, true));

            AddUntilStep("object is selectable at its top edge", () =>
            {
                var quad = blueprint.SelectionQuad;
                return blueprint.ReceivePositionalInputAt(new Vector2(quad.Centre.X, quad.TopLeft.Y + 1));
            });
        }
    }
}
