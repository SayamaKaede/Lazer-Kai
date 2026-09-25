// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Overlays;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Lazer-Kai's settings all at once, from the recommended set or one a mapper saved.
    /// </summary>
    [TestFixture]
    public class LazerKaiSettingsPresetTest
    {
        /// <summary>
        /// Every recommended value is one the setting takes: a value written wrong would quietly be the default instead.
        /// </summary>
        [Test]
        public void TestEveryRecommendedValueIsTaken()
        {
            using (var storage = new TemporaryNativeStorage(nameof(LazerKaiSettingsPresetTest)))
            using (var config = new OsuConfigManager(storage))
            {
                new LazerKaiSettingsPreset(LazerKaiSettingsPreset.RECOMMENDED).ApplyTo(config);

                foreach (var (setting, value) in LazerKaiSettingsPreset.RECOMMENDED)
                    Assert.That(config.GetText(setting), Is.EqualTo(value), setting.ToString());
            }
        }

        /// <summary>
        /// Always left alone: stable compatibility, sending error reports and the Mapping Tools path.
        /// </summary>
        [TestCase(OsuSetting.StableCompatibility)]
        [TestCase(OsuSetting.ErrorReporting)]
        [TestCase(OsuSetting.MappingToolsPath)]
        public void TestLeftAlone(OsuSetting setting)
        {
            Assert.That(LazerKaiSettingsPreset.Settings, Does.Not.Contain(setting));
            Assert.That(LazerKaiSettingsPreset.RECOMMENDED.Keys, Does.Not.Contain(setting));
        }

        /// <summary>
        /// The colours and the skin: in a preset a mapper saves, not in the recommended one.
        /// </summary>
        [TestCase(OsuSetting.EditorColourScheme)]
        [TestCase(OsuSetting.EditorSelectionColour)]
        [TestCase(OsuSetting.UiTheme)]
        [TestCase(OsuSetting.EditorTimelineUseSkin)]
        [TestCase(OsuSetting.EditorBeatmapHitsounds)]
        public void TestTheLooksAreOnlyInSavedPresets(OsuSetting setting)
        {
            Assert.That(LazerKaiSettingsPreset.Settings, Does.Contain(setting));
            Assert.That(LazerKaiSettingsPreset.RECOMMENDED.Keys, Does.Not.Contain(setting));
        }

        [Test]
        public void TestASavedPresetCarriesTheColoursAndTheRecommendedOneLeavesThem()
        {
            using (var storage = new TemporaryNativeStorage(nameof(LazerKaiSettingsPresetTest)))
            using (var config = new OsuConfigManager(storage))
            {
                config.SetValue(OsuSetting.EditorColourScheme, OverlayColourScheme.Purple);
                var saved = LazerKaiSettingsPreset.Capture(config);

                config.SetValue(OsuSetting.EditorColourScheme, OverlayColourScheme.Green);
                saved.ApplyTo(config);
                Assert.That(config.Get<OverlayColourScheme>(OsuSetting.EditorColourScheme), Is.EqualTo(OverlayColourScheme.Purple), "a saved preset's");

                config.SetValue(OsuSetting.EditorColourScheme, OverlayColourScheme.Green);
                new LazerKaiSettingsPreset(LazerKaiSettingsPreset.RECOMMENDED).ApplyTo(config);
                Assert.That(config.Get<OverlayColourScheme>(OsuSetting.EditorColourScheme), Is.EqualTo(OverlayColourScheme.Green), "left by the recommended");
            }
        }

        [Test]
        public void TestASavedPresetComesBackTheSame()
        {
            using (var storage = new TemporaryNativeStorage(nameof(LazerKaiSettingsPresetTest)))
            using (var config = new OsuConfigManager(storage))
            {
                config.SetValue(OsuSetting.EditorAutoBackupKeepCount, 42);
                config.SetValue(OsuSetting.EditorTimelineInspector, false);

                var store = new LazerKaiSettingsPresetStore(storage);
                store.Save(@"mine", LazerKaiSettingsPreset.Capture(config));

                config.SetValue(OsuSetting.EditorAutoBackupKeepCount, 5);
                config.SetValue(OsuSetting.EditorTimelineInspector, true);

                store.Load(@"mine")!.ApplyTo(config);

                Assert.That(config.Get<int>(OsuSetting.EditorAutoBackupKeepCount), Is.EqualTo(42));
                Assert.That(config.Get<bool>(OsuSetting.EditorTimelineInspector), Is.False);
                Assert.That(store.Names(), Is.EqualTo(new[] { @"mine" }));
            }
        }

        /// <summary>
        /// A preset says nothing of a setting added after it was saved, which is left as it is; and one it names that
        /// is not a preset's, is not touched.
        /// </summary>
        [Test]
        public void TestOnlyWhatThePresetHoldsAndMayChangeIsSet()
        {
            using (var storage = new TemporaryNativeStorage(nameof(LazerKaiSettingsPresetTest)))
            using (var config = new OsuConfigManager(storage))
            {
                config.SetValue(OsuSetting.EditorAutoBackupKeepCount, 7);
                config.SetValue(OsuSetting.StableCompatibility, false);

                new LazerKaiSettingsPreset(new Dictionary<OsuSetting, string>
                {
                    [OsuSetting.EditorTimelineInspector] = @"False",
                    [OsuSetting.StableCompatibility] = @"True",
                }).ApplyTo(config);

                Assert.That(config.Get<bool>(OsuSetting.EditorTimelineInspector), Is.False);
                Assert.That(config.Get<int>(OsuSetting.EditorAutoBackupKeepCount), Is.EqualTo(7), "not in the preset");
                Assert.That(config.Get<bool>(OsuSetting.StableCompatibility), Is.False, "not a preset's to change");
            }
        }

        /// <summary>
        /// The settings before applying are kept apart from the presets a mapper saved, which cannot be named as it is.
        /// </summary>
        [Test]
        public void TestTheSettingsBeforeApplyingAreNotAPreset()
        {
            using (var storage = new TemporaryNativeStorage(nameof(LazerKaiSettingsPresetTest)))
            using (var config = new OsuConfigManager(storage))
            {
                var store = new LazerKaiSettingsPresetStore(storage);
                store.SaveBeforeApplying(LazerKaiSettingsPreset.Capture(config));

                Assert.That(store.LoadBeforeApplying(), Is.Not.Null);
                Assert.That(store.Names(), Is.Empty);
                Assert.That(LazerKaiSettingsPresetStore.NameFor(@".before-applying"), Is.EqualTo(@"before-applying"));
                Assert.That(LazerKaiSettingsPresetStore.NameFor(@"   "), Is.Empty);
            }
        }
    }
}
