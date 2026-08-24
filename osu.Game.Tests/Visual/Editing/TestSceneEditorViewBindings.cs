// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit.Components.Menus;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The key bindings for the view menu's switches: that each one moves the setting it belongs to, and that the
    /// menu item says which key does it.
    /// </summary>
    /// <remarks>
    /// Its own fixture, because every setting here lives in the config and would otherwise carry into the next test.
    /// The pairing is what is worth asserting — an action wired to the wrong setting is the mistake that looks right
    /// in review and is instantly obvious in use.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneEditorViewBindings : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private static readonly Dictionary<GlobalAction, LocalisableString> menu_labels = new Dictionary<GlobalAction, LocalisableString>
        {
            [GlobalAction.EditorToggleDifficultyGraph] = EditorStrings.ShowDifficultyGraph,
            [GlobalAction.EditorTogglePlayAreaBorder] = EditorStrings.ShowPlayAreaBorder,
            [GlobalAction.EditorToggleOffscreenDetection] = EditorStrings.DetectOffscreen,
            [GlobalAction.EditorToggleLeftToolboxExpansion] = EditorStrings.AlwaysExpandLeftToolbox,
            [GlobalAction.EditorToggleTimelinePointLabels] = EditorStrings.AlwaysExpandTimelinePointLabels,
        };

        [TearDownSteps]
        public void TearDownViewBindingSteps()
        {
            AddStep("restore every setting", () =>
            {
                foreach (var setting in new[]
                         {
                             OsuSetting.EditorShowDifficultyGraph,
                             OsuSetting.EditorShowPlayAreaBorder,
                             OsuSetting.EditorDetectOffscreen,
                             OsuSetting.EditorAlwaysExpandLeftToolbox,
                             OsuSetting.EditorAlwaysExpandTimelinePointLabels,
                         })
                {
                    config.GetBindable<bool>(setting).SetDefault();
                }
            });
        }

        [TestCase(GlobalAction.EditorToggleDifficultyGraph, OsuSetting.EditorShowDifficultyGraph)]
        [TestCase(GlobalAction.EditorTogglePlayAreaBorder, OsuSetting.EditorShowPlayAreaBorder)]
        [TestCase(GlobalAction.EditorToggleOffscreenDetection, OsuSetting.EditorDetectOffscreen)]
        [TestCase(GlobalAction.EditorToggleLeftToolboxExpansion, OsuSetting.EditorAlwaysExpandLeftToolbox)]
        [TestCase(GlobalAction.EditorToggleTimelinePointLabels, OsuSetting.EditorAlwaysExpandTimelinePointLabels)]
        public void TestActionMovesItsOwnSetting(GlobalAction action, OsuSetting setting)
        {
            bool started = false;

            AddStep("remember where it started", () => started = config.Get<bool>(setting));

            press(action);
            AddAssert("it flipped", () => config.Get<bool>(setting), () => Is.EqualTo(!started));

            press(action);
            AddAssert("it flipped back", () => config.Get<bool>(setting), () => Is.EqualTo(started));
        }

        /// <remarks>
        /// Nothing else on screen says which key does this, and the switches start unbound — so a mapper who binds one
        /// finds out here that it took.
        /// </remarks>
        [TestCase(GlobalAction.EditorToggleDifficultyGraph)]
        [TestCase(GlobalAction.EditorTogglePlayAreaBorder)]
        [TestCase(GlobalAction.EditorToggleOffscreenDetection)]
        [TestCase(GlobalAction.EditorToggleLeftToolboxExpansion)]
        [TestCase(GlobalAction.EditorToggleTimelinePointLabels)]
        public void TestMenuItemNamesTheBinding(GlobalAction action)
        {
            AddAssert("menu item carries the hotkey",
                () => findMenuItem(menu_labels[action])?.Hotkey.GlobalAction,
                () => Is.EqualTo(action));
        }

        private void press(GlobalAction action) => AddStep($"press {action}",
            () => Editor.OnPressed(new KeyBindingPressEvent<GlobalAction>(new InputState(), action)));

        private OsuMenuItem? findMenuItem(LocalisableString label)
        {
            var bar = Editor.ChildrenOfType<EditorMenuBar>().Single();
            return search(bar.Items);

            OsuMenuItem? search(IEnumerable<MenuItem> items)
            {
                foreach (var item in items)
                {
                    if (item is OsuMenuItem osuItem && item.Text.Value.Equals(label))
                        return osuItem;

                    var found = search(item.Items);

                    if (found != null)
                        return found;
                }

                return null;
            }
        }
    }
}
