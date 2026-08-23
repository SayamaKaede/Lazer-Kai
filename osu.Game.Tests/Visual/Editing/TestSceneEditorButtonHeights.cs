// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osu.Game.Screens.Edit.Components;
using osu.Game.Screens.Edit.Components.RadioButtons;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osu.Game.Screens.Edit.Timing;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Each column of toolbox buttons is stretched towards square on a setting of its own. Its own fixture, because
    /// those settings live in the config and would otherwise carry into the next test.
    /// </summary>
    /// <remarks>
    /// The columns share button types — the grid types are radio buttons like the composer's tools, and the sample
    /// banks are ternary buttons like the toggles — so which setting a button listens to cannot be read off its type,
    /// and wiring one to the wrong column is a mistake that looks entirely fine until someone drags the slider that
    /// should not have moved it.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneEditorButtonHeights : OsuTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [TearDownSteps]
        public void TearDownHeightSteps()
        {
            AddStep("restore the heights", () =>
            {
                config.GetBindable<float>(OsuSetting.EditorSampleBankButtonSquareness).SetDefault();
                config.GetBindable<float>(OsuSetting.EditorGridButtonSquareness).SetDefault();
                config.GetBindable<float>(OsuSetting.EditorActionButtonSquareness).SetDefault();
                config.GetBindable<float>(OsuSetting.EditorSliderVelocityPresetSquareness).SetDefault();
            });
        }

        private static OsuSetting settingOf(Drawable button) => button.ChildrenOfType<EditorButtonSquareness>().Single().Setting;

        [Test]
        public void TestSampleBankButtonsAreSizedOnTheirOwn()
        {
            SampleBankTernaryButton bank = null!;

            AddStep("create a bank button", () => Child = bank = new SampleBankTernaryButton("normal")
            {
                Width = 200,
                CreateIcon = Empty,
                CreateCompactIcon = Empty,
                NormalHotkey = GlobalAction.EditorToggleNormalNormalBank,
                AdditionsHotkey = GlobalAction.EditorToggleAdditionNormalBank,
            });

            AddAssert("not wired to the toggles", () => settingOf(bank.NormalButton), () => Is.EqualTo(OsuSetting.EditorSampleBankButtonSquareness));
            AddAssert("both halves agree", () => settingOf(bank.AdditionsButton), () => Is.EqualTo(OsuSetting.EditorSampleBankButtonSquareness));

            AddStep("raise the bank setting", () => config.SetValue(OsuSetting.EditorSampleBankButtonSquareness, 0.3f));
            AddUntilStep("the button grew", () => bank.NormalButton.DrawHeight, () => Is.GreaterThan(EditorButtonSquareness.STOCK_HEIGHT));

            AddStep("flatten it again", () => config.SetValue(OsuSetting.EditorSampleBankButtonSquareness, 0f));
            AddUntilStep("it went back", () => bank.NormalButton.DrawHeight, () => Is.EqualTo(EditorButtonSquareness.STOCK_HEIGHT));
        }

        [Test]
        public void TestRadioButtonsCanBeSizedApart()
        {
            EditorRadioButton tool = null!;
            EditorRadioButton grid = null!;

            AddStep("create both kinds", () => Child = new FillFlowContainer
            {
                Width = 200,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    tool = new EditorRadioButton("tool", () => { }),
                    grid = new EditorRadioButton("grid", () => { }) { SquarenessSetting = OsuSetting.EditorGridButtonSquareness },
                }
            });

            AddAssert("the tool takes the composer's setting", () => settingOf(tool), () => Is.EqualTo(OsuSetting.EditorToolButtonSquareness));
            AddAssert("the grid type takes its own", () => settingOf(grid), () => Is.EqualTo(OsuSetting.EditorGridButtonSquareness));

            AddStep("raise the grid setting", () => config.SetValue(OsuSetting.EditorGridButtonSquareness, 0.3f));
            AddUntilStep("the grid button grew", () => grid.DrawHeight, () => Is.GreaterThan(EditorButtonSquareness.STOCK_HEIGHT));
            AddAssert("the tool stayed put", () => tool.DrawHeight, () => Is.EqualTo(EditorButtonSquareness.STOCK_HEIGHT));
        }

        /// <remarks>
        /// These are half the height of a toolbox button and were being pushed up to one, because the height a button
        /// starts at was a constant on the sizing component rather than something each button says for itself.
        /// </remarks>
        [Test]
        public void TestVelocityPresetsKeepTheirOwnHeight()
        {
            SliderVelocityAdjustmentControl.SliderVelocityPresetTernaryButton preset = null!;

            AddStep("create a preset button", () => Child = preset = new SliderVelocityAdjustmentControl.SliderVelocityPresetTernaryButton(1.5) { Description = "1.50x" });

            AddAssert("wired to the preset setting", () => settingOf(preset), () => Is.EqualTo(OsuSetting.EditorSliderVelocityPresetSquareness));
            AddUntilStep("left at its own height", () => preset.DrawHeight, () => Is.EqualTo(25f));

            AddStep("raise the preset setting", () => config.SetValue(OsuSetting.EditorSliderVelocityPresetSquareness, 1f));
            AddUntilStep("it grew to its own width", () => preset.DrawHeight, () => Is.EqualTo(50f));
        }

        [Test]
        public void TestTransformAndGenerateButtonsAreSized()
        {
            EditorToolButton button = null!;

            // the button offers a popover, which needs somewhere to be shown even when nothing opens it.
            AddStep("create an action button", () => Child = new PopoverContainer
            {
                Width = 200,
                AutoSizeAxes = Axes.Y,
                Child = button = new EditorToolButton("action", Empty, () => null),
            });

            AddAssert("wired to the action setting", () => settingOf(button), () => Is.EqualTo(OsuSetting.EditorActionButtonSquareness));

            AddStep("raise the action setting", () => config.SetValue(OsuSetting.EditorActionButtonSquareness, 0.3f));
            AddUntilStep("the button grew", () => button.DrawHeight, () => Is.GreaterThan(EditorButtonSquareness.STOCK_HEIGHT));
        }
    }
}
