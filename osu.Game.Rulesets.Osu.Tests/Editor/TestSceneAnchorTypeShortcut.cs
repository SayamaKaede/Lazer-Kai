// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Ctrl and a number pick the shape a new anchor makes, which is otherwise a trip to the settings.
    /// </summary>
    [TestFixture]
    public partial class TestSceneAnchorTypeShortcut : TestSceneOsuEditor
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private SliderAnchorType anchorType => config.Get<SliderAnchorType>(OsuSetting.EditorSliderAnchorType);

        [SetUpSteps]
        public void StartFromTheStockAnswers()
        {
            AddStep("back to the stock answers", setTheStockAnswers);
        }

        [TearDownSteps]
        public void PutTheSettingsBack()
        {
            AddStep("back to the stock answers", setTheStockAnswers);
        }

        private void setTheStockAnswers()
        {
            config.SetValue(OsuSetting.EditorStableSliderControls, true);
            config.SetValue(OsuSetting.EditorSliderAnchorType, SliderAnchorType.PerfectCurve);
        }

        [Test]
        public void TestEachNumberPicksItsShape()
        {
            press(Key.Number1);
            AddAssert("straight", () => anchorType, () => Is.EqualTo(SliderAnchorType.Linear));

            press(Key.Number2);
            AddAssert("bezier", () => anchorType, () => Is.EqualTo(SliderAnchorType.Bezier));

            press(Key.Number3);
            AddAssert("perfect curve", () => anchorType, () => Is.EqualTo(SliderAnchorType.PerfectCurve));

            press(Key.Number4);
            AddAssert("catmull", () => anchorType, () => Is.EqualTo(SliderAnchorType.Catmull));
        }

        /// <summary>
        /// The shape is only used while sliders are shaped stable's way, so the keys say nothing while that is off.
        /// </summary>
        [Test]
        public void TestTheKeysDoNothingWithStableShapingOff()
        {
            AddStep("stop shaping stable's way", () => config.SetValue(OsuSetting.EditorStableSliderControls, false));

            press(Key.Number1);

            AddAssert("the shape is as it was", () => anchorType, () => Is.EqualTo(SliderAnchorType.PerfectCurve));
        }

        private void press(Key key)
        {
            AddStep($"ctrl+{key}", () =>
            {
                InputManager.PressKey(Key.LControl);
                InputManager.Key(key);
                InputManager.ReleaseKey(Key.LControl);
            });
        }
    }
}
