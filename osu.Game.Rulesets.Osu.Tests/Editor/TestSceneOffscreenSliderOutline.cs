// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The red outline drawn over a slider that leaves the play area.
    /// </summary>
    /// <remarks>
    /// The offscreen test itself was never the problem — the verifier and the marking agree on the reported
    /// slider. What went wrong was here: the outline had no size, so it was a drawable one point across, and a
    /// point that falls outside the masking bounds is a drawable that is never drawn. A slider reaching a long
    /// way out of the play area put that point a long way out with it.
    /// </remarks>
    public partial class TestSceneOffscreenSliderOutline : OsuTestScene
    {
        private OsuOffscreenDisplay.OffscreenSliderOutline outline = null!;

        private void show(Slider slider)
        {
            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Child = outline = new OsuOffscreenDisplay.OffscreenSliderOutline(slider),
            };

            outline.UpdateFrom(slider);
        }

        /// <summary>
        /// The slider that was reported: <c>169,133,133138,6,0,P|195:-137|-9:42,2,700,...</c>
        /// </summary>
        [Test]
        public void TestTheOutlineCoversTheReportedSlider()
        {
            Slider slider = null!;

            AddStep("show the reported slider", () => show(slider = reportedSlider()));

            AddUntilStep("the outline has a size", () => outline.DrawSize, () => Is.Not.EqualTo(Vector2.Zero));

            AddAssert("and it is as tall as the slider it is about",
                () => outline.DrawHeight, () => Is.GreaterThanOrEqualTo(pathHeight(slider)));
        }

        [Test]
        public void TestAnOrdinarySliderIsCoveredToo()
        {
            Slider slider = null!;

            AddStep("show a plain slider", () => show(slider = straightSlider()));

            AddUntilStep("the outline has a size", () => outline.DrawSize, () => Is.Not.EqualTo(Vector2.Zero));
            AddAssert("as wide as the slider", () => outline.DrawWidth, () => Is.GreaterThanOrEqualTo(pathWidth(slider)));
        }

        /// <summary>
        /// How far the slider reaches vertically, which the outline has to cover to be drawn over it.
        /// </summary>
        private static float pathHeight(Slider slider)
        {
            float min = float.MaxValue, max = float.MinValue;

            foreach (var point in slider.Path.CalculatedPath)
            {
                min = Math.Min(min, point.Y);
                max = Math.Max(max, point.Y);
            }

            return max - min;
        }

        private static float pathWidth(Slider slider)
        {
            float min = float.MaxValue, max = float.MinValue;

            foreach (var point in slider.Path.CalculatedPath)
            {
                min = Math.Min(min, point.X);
                max = Math.Max(max, point.X);
            }

            return max - min;
        }

        private static Slider reportedSlider()
        {
            var head = new Vector2(169, 133);

            var slider = new Slider
            {
                StartTime = 133138,
                Position = head,
                Path = new SliderPath(PathType.PERFECT_CURVE, new[]
                {
                    Vector2.Zero,
                    new Vector2(195, -137) - head,
                    new Vector2(-9, 42) - head,
                }, 700),
                RepeatCount = 2,
            };

            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            return slider;
        }

        private static Slider straightSlider()
        {
            var slider = new Slider
            {
                Position = new Vector2(100, 200),
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(200, 0) }),
            };

            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            return slider;
        }
    }
}
