// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Screens.Edit;
using osu.Game.Skinning;
using osu.Game.Tests.Beatmaps;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// In the editor, a slider's ticks and slide and a spinner's spin sound at the volume of the green line in effect as
    /// they play, as in stable, rather than at the volume the object carries.
    /// </summary>
    /// <remarks>
    /// The lines: a red line at 0 saying 60, a green line halfway through the slider saying 20, and one halfway through
    /// the spinner saying 10. The objects themselves say 100.
    /// </remarks>
    public partial class TestSceneLinesInsideObjects : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset, false);

        private Slider slider = null!;
        private Spinner spinner = null!;

        private double sliderMiddle => slider.StartTime + slider.Duration / 2;
        private double spinnerMiddle => spinner.StartTime + spinner.Duration / 2;

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("add a slider and a spinner", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(slider = new Slider
                {
                    Position = new Vector2(100, 192),
                    StartTime = 1000,
                    Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(400, 0) }),
                    Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: 100) },
                });
                EditorBeatmap.Add(spinner = new Spinner { StartTime = 6000, EndTime = 8000, Position = new Vector2(256, 192), Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: 100) } });
            });

            AddStep("the lines", () =>
            {
                var lines = Editor.ChildrenOfType<SavedTimingLines>().Single().Lines;

                lines.Clear();
                lines.AddRange(new[]
                {
                    "0,500,4,2,0,60,1,0",
                    formatTime(sliderMiddle) + ",-100,4,2,0,20,0,0",
                    formatTime(spinnerMiddle) + ",-100,4,2,0,10,0,0",
                }.Select(l => SavedTimingLines.Parse(l)!.Value));
            });
        }

        private static string formatTime(double time) => System.Math.Floor(time).ToString(System.Globalization.CultureInfo.InvariantCulture);

        [Test]
        public void TestTheSlideFollowsTheLines()
        {
            AddStep("a quarter through the slider", () => EditorClock.Seek(slider.StartTime + slider.Duration / 4));
            AddUntilStep("the slide is at 60", () => slideSound()?.AggregateVolume.Value, () => Is.EqualTo(0.6).Within(0.001));

            AddStep("three quarters through", () => EditorClock.Seek(slider.StartTime + slider.Duration * 3 / 4));
            AddUntilStep("the slide is at 20", () => slideSound()?.AggregateVolume.Value, () => Is.EqualTo(0.2).Within(0.001));
        }

        [Test]
        public void TestEachTickTakesTheLineWhereItIs()
        {
            AddStep("look at the slider", () => EditorClock.Seek(slider.StartTime));

            AddUntilStep("a tick before the line is at 60", () => tickVolume(t => t.StartTime < sliderMiddle), () => Is.EqualTo(0.6).Within(0.001));
            AddUntilStep("a tick after the line is at 20", () => tickVolume(t => t.StartTime > sliderMiddle), () => Is.EqualTo(0.2).Within(0.001));
        }

        [Test]
        public void TestTheSpinFollowsTheLines()
        {
            AddStep("a quarter through the spinner", () => EditorClock.Seek(spinner.StartTime + spinner.Duration / 4));
            AddUntilStep("the spin is at 20", () => drawableSpinner()?.LineVolume, () => Is.EqualTo(0.2).Within(0.001));

            AddStep("three quarters through", () => EditorClock.Seek(spinner.StartTime + spinner.Duration * 3 / 4));
            AddUntilStep("the spin is at 10", () => drawableSpinner()?.LineVolume, () => Is.EqualTo(0.1).Within(0.001));
        }

        /// <summary>
        /// The editor always sounds the lines (it was a setting until 2026-09-25), so it is only a beatmap without any
        /// that sounds the objects' own.
        /// </summary>
        [Test]
        public void TestNothingFollowsWithoutLines()
        {
            AddStep("no lines", () => Editor.ChildrenOfType<SavedTimingLines>().Single().Lines.Clear());
            AddStep("three quarters through the slider", () => EditorClock.Seek(slider.StartTime + slider.Duration * 3 / 4));

            AddUntilStep("the slide is at the slider's own volume", () => slideSound()?.AggregateVolume.Value, () => Is.EqualTo(1).Within(0.001));
            AddUntilStep("its sounds are the slider's", () => slideSound()?.Samples.OfType<HitSampleInfo>().Select(s => s.Volume).Distinct(), () => Is.EqualTo(new[] { 100 }));
        }

        private DrawableSlider? drawableSlider() => this.ChildrenOfType<DrawableSlider>().FirstOrDefault(d => d.HitObject == slider);

        private DrawableSpinner? drawableSpinner() => this.ChildrenOfType<DrawableSpinner>().FirstOrDefault(d => d.HitObject == spinner);

        private PausableSkinnableSound? slideSound() => drawableSlider()?.ChildrenOfType<PausableSkinnableSound>()
                                                                         .FirstOrDefault(s => s.Samples.OfType<HitSampleInfo>().Any(h => h.Name == @"sliderslide"));

        private double? tickVolume(System.Func<HitObject, bool> which)
        {
            var tick = drawableSlider()?.ChildrenOfType<DrawableSliderTick>().FirstOrDefault(t => which(t.HitObject));
            return tick?.ChildrenOfType<PausableSkinnableSound>().FirstOrDefault()?.AggregateVolume.Value;
        }
    }
}
