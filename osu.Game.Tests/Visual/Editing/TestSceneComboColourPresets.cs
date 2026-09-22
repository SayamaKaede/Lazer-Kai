// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Setup;
using osu.Game.Skinning;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The presets button beside the combo colours on the setup screen.
    /// </summary>
    [HeadlessTest]
    public partial class TestSceneComboColourPresets : OsuManualInputManagerTestScene
    {
        private static readonly Colour4[] trio = { Colour4.Red, Colour4.Lime, Colour4.Blue };
        private static readonly Colour4[] pair = { Colour4.White, Colour4.Black };

        private TemporaryNativeStorage? storage;
        private LegacyBeatmapSkin skin = null!;
        private ColoursSection section = null!;

        private FormColourPalette palette => section.ChildrenOfType<FormColourPalette>().Single();

        private ComboColourPresetPopover? popover => this.ChildrenOfType<ComboColourPresetPopover>().SingleOrDefault();

        private ComboColourPresetStore store => new ComboColourPresetStore(storage!);

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create colours section", () =>
            {
                storage?.Dispose();
                storage = new TemporaryNativeStorage($"{nameof(TestSceneComboColourPresets)}-{Guid.NewGuid()}");
                skin = new LegacyBeatmapSkin(new BeatmapInfo(), null);

                Child = new DependencyProvidingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    CachedDependencies =
                    [
                        (typeof(EditorBeatmap), new EditorBeatmap(new Beatmap
                        {
                            BeatmapInfo = { Ruleset = new OsuRuleset().RulesetInfo }
                        }, skin)),
                        (typeof(OverlayColourProvider), new OverlayColourProvider(OverlayColourScheme.Aquamarine)),
                        (typeof(Storage), storage),
                    ],
                    // the way the editor has them: context menus around everything, popovers around each screen.
                    Child = new OsuContextMenuContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = new PopoverContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = section = new ColoursSection
                            {
                                RelativeSizeAxes = Axes.X,
                            }
                        }
                    }
                };
            });
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("delete the presets", () => storage?.Dispose());
        }

        /// <remarks>
        /// With the right and middle buttons swapped, the physical right button is what arrives as the middle one: the
        /// swap is made where input enters the game, which this scene's input does not pass through.
        /// </remarks>
        [Test]
        public void TestOnlyTheMiddleButtonOffersDeleting()
        {
            setPalette(trio);
            openPresets();
            clickTab("Save as a preset");
            typeName("mine");
            clickSaveButton("Save");

            AddStep("press the right button on the preset", () =>
            {
                InputManager.MoveMouseTo(row("mine"));
                InputManager.Click(MouseButton.Right);
            });
            AddWaitStep("give it time", 5);
            AddAssert("delete button not shown", () => row("mine").DeleteButton.Alpha, () => Is.EqualTo(0));

            AddStep("press the middle button on the preset", () => InputManager.Click(MouseButton.Middle));
            AddUntilStep("delete button shown", () => row("mine").DeleteButton.Alpha, () => Is.EqualTo(1));
        }

        [Test]
        public void TestSavedColoursComeBackInPlaceOfTheOthers()
        {
            setPalette(trio);
            openPresets();
            clickTab("Save as a preset");
            typeName("trio");
            clickSaveButton("Save");

            AddAssert("list shows the preset", () => rowNames(), () => Is.EqualTo(new[] { "trio" }));
            AddAssert("preset is on disk", () => store.Load().Find("trio")?.ToColours(), () => Is.EqualTo(trio));

            setPalette(pair);
            AddStep("click the preset", () => clickOn(row("trio")));

            AddAssert("palette is the preset", () => palette.Colours, () => Is.EqualTo(trio));
            AddAssert("beatmap has three colours", () => skin.Configuration.CustomComboColours.Count, () => Is.EqualTo(3));
            AddUntilStep("popover closed", () => popover?.State.Value != Visibility.Visible);
        }

        [Test]
        public void TestPresetsSavedElsewhereAreOffered()
        {
            AddStep("save a preset from another beatmap", () =>
            {
                var list = new ComboColourPresetList();
                list.Save("from elsewhere", pair);
                store.Save(list);
            });

            openPresets();

            AddAssert("list shows the preset", () => rowNames(), () => Is.EqualTo(new[] { "from elsewhere" }));
        }

        [Test]
        public void TestSavingUnderATakenNameOverwrites()
        {
            setPalette(trio);
            openPresets();
            clickTab("Save as a preset");
            typeName("mine");
            clickSaveButton("Save");

            setPalette(pair);
            clickTab("Save as a preset");
            typeName("mine");
            clickSaveButton("Overwrite");

            AddAssert("list has it once", () => rowNames(), () => Is.EqualTo(new[] { "mine" }));
            AddAssert("preset holds the new colours", () => store.Load().Find("mine")?.ToColours(), () => Is.EqualTo(pair));
        }

        [Test]
        public void TestDeletingWithTheMiddleButton()
        {
            setPalette(trio);
            openPresets();
            clickTab("Save as a preset");
            typeName("gone");
            clickSaveButton("Save");
            clickTab("Save as a preset");
            typeName("kept");
            clickSaveButton("Save");

            AddStep("middle click the first preset", () =>
            {
                InputManager.MoveMouseTo(row("gone"));
                InputManager.Click(MouseButton.Middle);
            });
            AddUntilStep("delete button shown", () => row("gone").DeleteButton.Alpha, () => Is.EqualTo(1));
            AddAssert("nothing deleted yet", () => rowNames(), () => Is.EqualTo(new[] { "gone", "kept" }));
            AddAssert("colours left alone", () => palette.Colours, () => Is.EqualTo(trio));

            AddStep("click delete", () => clickOn(row("gone").DeleteButton));

            AddAssert("only the other is left", () => rowNames(), () => Is.EqualTo(new[] { "kept" }));
            AddAssert("only the other is on disk", () => store.Load().Presets.Select(p => p.Name), () => Is.EqualTo(new[] { "kept" }));
            AddAssert("popover still open", () => popover?.State.Value, () => Is.EqualTo(Visibility.Visible));
        }

        [Test]
        public void TestMovingOffARowTakesItsDeleteButtonBack()
        {
            setPalette(trio);
            openPresets();
            clickTab("Save as a preset");
            typeName("first");
            clickSaveButton("Save");
            clickTab("Save as a preset");
            typeName("second");
            clickSaveButton("Save");

            AddStep("middle click the first preset", () =>
            {
                InputManager.MoveMouseTo(row("first"));
                InputManager.Click(MouseButton.Middle);
            });
            AddUntilStep("delete button shown", () => row("first").DeleteButton.Alpha, () => Is.EqualTo(1));

            AddStep("move to the second", () => InputManager.MoveMouseTo(row("second")));
            AddUntilStep("delete button gone", () => row("first").DeleteButton.Alpha, () => Is.EqualTo(0));
        }

        private void setPalette(Colour4[] colours) => AddStep($"set palette to {colours.Length} colours", () =>
        {
            palette.Colours.Clear();
            palette.Colours.AddRange(colours);
        });

        private void openPresets()
        {
            AddStep("click presets button", () => clickOn(section.ChildrenOfType<ComboColourPresetButton>().Single()));
            AddUntilStep("popover open", () => popover?.State.Value == Visibility.Visible && popover.IsLoaded);
        }

        // the tab control keeps a dropdown of the same items for when they do not fit, so the tab is found by its kind.
        private void clickTab(string text) => AddStep($"click \"{text}\"", () =>
            clickOn(popover!.ChildrenOfType<TabItem>().Single(i => i.ChildrenOfType<SpriteText>().Any(t => t.Text.ToString() == text))));

        private void typeName(string name) => AddStep($"type \"{name}\"", () =>
            // the tab control's dropdown has a search box of its own.
            popover!.ChildrenOfType<OsuTextBox>().Single(t => t.PlaceholderText.ToString() == "Preset name").Current.Value = name);

        private void clickSaveButton(string text) => AddStep($"click {text}", () =>
            clickOn(popover!.ChildrenOfType<RoundedButton>().Single(b => b.Text.ToString() == text)));

        private string[] rowNames() => popover!.ChildrenOfType<ComboColourPresetPopover.PresetRow>().Select(r => r.Preset.Name).ToArray();

        private ComboColourPresetPopover.PresetRow row(string name) => popover!.ChildrenOfType<ComboColourPresetPopover.PresetRow>().Single(r => r.Preset.Name == name);

        private void clickOn(Drawable drawable)
        {
            InputManager.MoveMouseTo(drawable);
            InputManager.Click(MouseButton.Left);
        }
    }
}
