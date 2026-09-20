// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Moving and scaling a selection that reaches outside the play area, which the setting allows.
    /// </summary>
    /// <remarks>
    /// A selection is pushed back inside the play area after every move, and its scale limited to what keeps it
    /// inside, so a selection already reaching outside -- a slider ending past an edge, an anchor dragged off the
    /// grid -- cannot be moved or scaled at all: the correction cancels out whatever was dragged. Allowing objects
    /// outside the play area drops the correction. The verify tab still reports objects that ended up off screen.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneSelectionOutsideThePlayArea : TestSceneOsuEditor
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(Ruleset.Value, false);

        [TearDownSteps]
        public void PutTheSettingBack()
        {
            AddStep("back to the stock answer", () => config.SetValue(OsuSetting.EditorAllowOutsidePlayArea, false));
        }

        private void allowObjectsOutside(bool allow)
        {
            AddStep($"objects outside the play area: {allow}", () => config.SetValue(OsuSetting.EditorAllowOutsidePlayArea, allow));
        }

        /// <summary>
        /// The tail is past the right edge, so a nudge is undone by the same correction that brings the tail back in.
        /// </summary>
        [TestCase(true, 501f)]
        [TestCase(false, 452f)]
        public void TestNudgingASelectionWhoseEndIsOutside(bool allowed, float expectedX)
        {
            Slider slider = null!;

            allowObjectsOutside(allowed);

            AddStep("add a slider ending past the right edge", () =>
            {
                EditorBeatmap.Add(slider = sliderWithAnchorAt(new Vector2(500, 100), new Vector2(60, 0)));
            });

            AddStep("select it", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            AddStep("nudge it right", () =>
            {
                InputManager.PressKey(Key.LControl);
                InputManager.Key(Key.Right);
                InputManager.ReleaseKey(Key.LControl);
            });

            AddAssert("it ended up where the setting says", () => slider.Position.X, () => Is.EqualTo(expectedX));
        }

        /// <summary>
        /// The box around this selection is outside on two sides, which is the case that threw while it was clamped:
        /// no scale brings the far corner back inside, so the two axes' ranges share nothing.
        /// </summary>
        [Test]
        public void TestScalingASelectionReachingOutsideOnTwoSides()
        {
            Slider left = null!;
            Slider right = null!;

            allowObjectsOutside(true);

            AddStep("add sliders with anchors past the top and the right", () =>
            {
                EditorBeatmap.Add(left = sliderWithAnchorAt(new Vector2(20, 30), new Vector2(0, -130)));
                EditorBeatmap.Add(right = sliderWithAnchorAt(new Vector2(450, 20), new Vector2(250, 10)));
            });

            AddStep("select them", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(new[] { left, right });
            });

            AddStep("halve it about the far corner", () => halveAboutTheFarCorner());

            // about (20, -100), halved: (20, 30) -> (20, -35) and (450, 20) -> (235, -40).
            AddAssert("the first slider halved its distance from the corner", () => left.Position, () => Is.EqualTo(new Vector2(20, -35)));
            AddAssert("so did the second", () => right.Position, () => Is.EqualTo(new Vector2(235, -40)));
        }

        /// <summary>
        /// The same selection with the setting off, which is where the clamp used to throw and take the editor with it.
        /// </summary>
        [Test]
        public void TestScalingTheSameSelectionWhileHeldInside()
        {
            Slider left = null!;

            allowObjectsOutside(false);

            AddStep("add sliders with anchors past the top and the right", () =>
            {
                EditorBeatmap.Add(left = sliderWithAnchorAt(new Vector2(20, 30), new Vector2(0, -130)));
                EditorBeatmap.Add(sliderWithAnchorAt(new Vector2(450, 20), new Vector2(250, 10)));
            });

            AddStep("select them", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects.OfType<Slider>());
            });

            AddStep("halve it about the far corner", () => halveAboutTheFarCorner());

            AddAssert("it was held inside instead", () => left.Position.Y, () => Is.GreaterThanOrEqualTo(0));
        }

        /// <summary>
        /// A single slider is scaled by a path of its own, which puts the whole scale back as soon as either end of it
        /// leaves the play area.
        /// </summary>
        [TestCase(true, 300f)]
        [TestCase(false, 150f)]
        public void TestScalingASingleSliderPastTheEdge(bool allowed, float expectedLength)
        {
            Slider slider = null!;

            allowObjectsOutside(allowed);

            AddStep("add a slider along the right half", () =>
            {
                EditorBeatmap.Add(slider = sliderWithAnchorAt(new Vector2(300, 100), new Vector2(150, 0)));
            });

            AddStep("select it", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            AddStep("stretch it past the edge", () =>
            {
                var scaling = Editor.ChildrenOfType<OsuSelectionScaleHandler>().Single();

                scaling.Begin();
                scaling.Update(new Vector2(2, 1), scaling.OriginalSurroundingQuad!.Value.TopLeft, Axes.X);
                scaling.Commit();
            });

            AddAssert("the path is as long as the setting allows", () => slider.Path.ControlPoints.Last().Position.X, () => Is.EqualTo(expectedLength));
        }

        private void halveAboutTheFarCorner()
        {
            var scaling = Editor.ChildrenOfType<OsuSelectionScaleHandler>().Single();

            scaling.Begin();
            // a scale handle scales about the corner of the selection box opposite the one it sits on.
            scaling.Update(new Vector2(0.5f), scaling.OriginalSurroundingQuad!.Value.TopLeft, Axes.Both);
            scaling.Commit();
        }

        private static Slider sliderWithAnchorAt(Vector2 position, Vector2 anchor) => new Slider
        {
            StartTime = 1000,
            Position = position,
            Path = new SliderPath(new[]
            {
                new PathControlPoint(new Vector2(0), PathType.LINEAR),
                new PathControlPoint(anchor),
            })
        };
    }
}
