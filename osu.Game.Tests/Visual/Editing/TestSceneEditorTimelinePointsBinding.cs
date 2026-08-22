// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
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
    /// The key binding for hiding the timeline's velocity and volume rows. Its own fixture, because the setting it
    /// works on lives in the config and would otherwise carry into the next test.
    /// </summary>
    [TestFixture]
    public partial class TestSceneEditorTimelinePointsBinding : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [TearDownSteps]
        public void TearDownBindingSteps()
        {
            AddStep("restore the rows", () => config.GetBindable<bool>(OsuSetting.EditorHideTimelinePoints).SetDefault());
        }

        [Test]
        public void TestActionTogglesTheRows()
        {
            AddAssert("rows start shown", () => !config.Get<bool>(OsuSetting.EditorHideTimelinePoints));

            AddStep("press the binding", () => press());
            AddAssert("rows are hidden", () => config.Get<bool>(OsuSetting.EditorHideTimelinePoints));

            AddStep("press it again", () => press());
            AddAssert("rows are back", () => !config.Get<bool>(OsuSetting.EditorHideTimelinePoints));
        }

        /// <remarks>
        /// A binding with nothing on screen to show for it leaves the user unsure whether the key registered, which is
        /// what the menu's own reminder of the key is for.
        /// </remarks>
        [Test]
        public void TestMenuItemNamesTheBinding()
        {
            AddAssert("menu item carries the hotkey",
                () => findMenuItem()?.Hotkey.GlobalAction,
                () => Is.EqualTo(GlobalAction.EditorToggleTimelinePoints));
        }

        private void press() => Editor.OnPressed(new KeyBindingPressEvent<GlobalAction>(new InputState(), GlobalAction.EditorToggleTimelinePoints));

        private OsuMenuItem? findMenuItem()
        {
            var bar = Editor.ChildrenOfType<EditorMenuBar>().Single();
            return search(bar.Items);

            static OsuMenuItem? search(IEnumerable<MenuItem> items)
            {
                foreach (var item in items)
                {
                    if (item is OsuMenuItem osuItem && item.Text.Value.Equals(EditorStrings.TimelineHidePoints))
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
