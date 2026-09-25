// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Settings.Sections.LazerKai;

namespace osu.Game.Tests.Visual.Settings
{
    /// <summary>
    /// Applying the recommended settings from the options, with the settings as they were kept to go back to.
    /// </summary>
    public partial class TestSceneSettingsPresets : OsuTestScene
    {
        [Cached(typeof(IDialogOverlay))]
        private readonly DialogOverlay dialogOverlay = new DialogOverlay();

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private Storage storage { get; set; } = null!;

        private readonly Container content;

        private SettingsPresetSettings subsection = null!;

        private LazerKaiSettingsPreset settingsBefore = null!;

        public TestSceneSettingsPresets()
        {
            // the overlay lives as long as the scene: replacing it would leave the next subsection holding a disposed one.
            Children = new Drawable[]
            {
                content = new Container { RelativeSizeAxes = Axes.Both },
                dialogOverlay,
            };
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("nothing kept, settings away from the recommended", () =>
            {
                settingsBefore = LazerKaiSettingsPreset.Capture(config);

                if (storage.ExistsDirectory(LazerKaiSettingsPresetStore.DIRECTORY))
                    storage.DeleteDirectory(LazerKaiSettingsPresetStore.DIRECTORY);

                config.SetValue(OsuSetting.EditorAutoBackupKeepCount, 20);
                config.SetValue(OsuSetting.StableCompatibility, false);
            });

            AddStep("create the subsection", () => content.Child = subsection = new SettingsPresetSettings
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 400,
            });
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("settings as they were", () => settingsBefore.ApplyTo(config));
        }

        [Test]
        public void TestApplyingTheRecommendedSettings()
        {
            AddStep("apply", () => subsection.ChildrenOfType<SettingsButton>().First().TriggerClick());
            AddUntilStep("asked first", () => dialogOverlay.CurrentDialog, () => Is.InstanceOf<SettingsPresetSettings.ApplySettingsPresetDialog>());
            AddAssert("nothing changed yet", () => config.Get<int>(OsuSetting.EditorAutoBackupKeepCount), () => Is.EqualTo(20));

            AddStep("confirm", () => dialogOverlay.CurrentDialog!.PerformAction<PopupDialogOkButton>());

            AddAssert("the recommended count", () => config.Get<int>(OsuSetting.EditorAutoBackupKeepCount), () => Is.EqualTo(100));
            AddAssert("compatibility left alone", () => config.Get<bool>(OsuSetting.StableCompatibility), () => Is.False);
            AddAssert("the settings before are kept", () => new LazerKaiSettingsPresetStore(storage).LoadBeforeApplying()!.Values[OsuSetting.EditorAutoBackupKeepCount],
                () => Is.EqualTo(@"20"));
        }

        [Test]
        public void TestCancellingChangesNothing()
        {
            AddStep("apply", () => subsection.ChildrenOfType<SettingsButton>().First().TriggerClick());
            AddUntilStep("asked first", () => dialogOverlay.CurrentDialog, () => Is.InstanceOf<SettingsPresetSettings.ApplySettingsPresetDialog>());
            AddStep("cancel", () => dialogOverlay.CurrentDialog!.PerformAction<PopupDialogCancelButton>());

            AddAssert("unchanged", () => config.Get<int>(OsuSetting.EditorAutoBackupKeepCount), () => Is.EqualTo(20));
            AddAssert("nothing kept", () => new LazerKaiSettingsPresetStore(storage).LoadBeforeApplying(), () => Is.Null);
        }
    }
}
