// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Settings.Sections.LazerKai;
using osu.Game.Screens.Edit;
using osu.Game.Utils;

namespace osu.Game.Tests.Visual.Settings
{
    /// <summary>
    /// The Mapping Tools path box saying, while empty, which Mapping Tools it found on its own.
    /// </summary>
    public partial class TestSceneMappingToolsPathSetting : OsuTestScene
    {
        private MappingToolsPathTextBox setting = null!;
        private ExternalToolSettings subsection = null!;

        // the line under each of this fork's subsection headers takes its colour from here (2026-09-05).
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create setting", () => Child = setting = new MappingToolsPathTextBox
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 400,
            });
        }

        /// <summary>
        /// The path is not on screen until it is asked for. Settings get opened on stream and in screenshots for
        /// reasons that have nothing to do with this row, and a path very often carries an account name.
        /// </summary>
        [Test]
        public void TestThePathIsOutOfSightUntilAskedFor()
        {
            AddStep("create the whole subsection", () => Child = subsection = new ExternalToolSettings
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 400,
            });

            AddUntilStep("the path row is there", () => subsection.ChildrenOfType<MappingToolsPathTextBox>().Any());
            AddAssert("but not showing", () => subsection.ChildrenOfType<MappingToolsPathTextBox>().Single().Alpha, () => Is.Zero);

            AddStep("ask for it", () => subsection.ChildrenOfType<SettingsButton>().First().TriggerClick());
            AddUntilStep("now it shows", () => subsection.ChildrenOfType<MappingToolsPathTextBox>().Single().Alpha, () => Is.EqualTo(1));

            AddStep("put it away again", () => subsection.ChildrenOfType<SettingsButton>().First().TriggerClick());
            AddUntilStep("out of sight again", () => subsection.ChildrenOfType<MappingToolsPathTextBox>().Single().Alpha, () => Is.Zero);
        }

        /// <summary>
        /// An empty box that says nothing looks the same whether Mapping Tools was found or not, which is the
        /// one thing somebody opens this setting to find out.
        /// </summary>
        [Test]
        public void TestTheEmptyBoxSaysWhatWasFound()
        {
            AddAssert("the box carries the placeholder",
                () => setting.ChildrenOfType<OutlinedTextBox>().Single().PlaceholderText.ToString(),
                () => Is.EqualTo(MappingToolsPathTextBox.Detected.ToString()));

            AddAssert("and it is not blank",
                () => setting.ChildrenOfType<OutlinedTextBox>().Single().PlaceholderText.ToString(), () => Is.Not.Empty);
        }

        /// <summary>
        /// On a machine with Mapping Tools installed, what it says is where it is — with any account name in it
        /// masked, since this is a path put on screen without anybody asking for it.
        /// </summary>
        [Test]
        public void TestItNamesTheInstallItFound()
        {
            if (!MappingToolsHandoff.IsMappingTools(MappingToolsHandoff.DefaultExecutablePath))
                Assert.Ignore(@"Mapping Tools is not installed on this machine.");

            AddAssert("it names the install",
                () => setting.ChildrenOfType<OutlinedTextBox>().Single().PlaceholderText.ToString(),
                () => Is.EqualTo(PathDisplay.Mask(MappingToolsHandoff.DefaultExecutablePath)));

            AddAssert("with no account name left in it",
                () => setting.ChildrenOfType<OutlinedTextBox>().Single().PlaceholderText.ToString(),
                () => Does.Not.Contain(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Split('\\', '/')[^1]));
        }
    }
}
