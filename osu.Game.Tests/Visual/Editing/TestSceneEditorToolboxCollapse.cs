// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets.Edit;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Folding a group in the editor's sidebars away. Its own fixture, because which groups are folded lives in the
    /// config and would otherwise carry into the next test.
    /// </summary>
    [TestFixture]
    public partial class TestSceneEditorToolboxCollapse : OsuManualInputManagerTestScene
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private ExpandingToolboxContainer toolbox = null!;

        [SetUpSteps]
        public void SetUpCollapseSteps()
        {
            AddStep("start with nothing folded", () => config.SetValue(OsuSetting.EditorCollapsedToolboxGroups, string.Empty));
        }

        [TearDownSteps]
        public void TearDownCollapseSteps()
        {
            AddStep("restore the order", () => config.GetBindable<string>(OsuSetting.EditorToolboxGroupOrder).SetDefault());
            AddStep("restore folded groups", () => config.GetBindable<string>(OsuSetting.EditorCollapsedToolboxGroups).SetDefault());
            AddStep("restore pinned groups", () => config.GetBindable<string>(OsuSetting.EditorPinnedToolboxGroups).SetDefault());
        }

        private void createToolbox() => AddStep("create toolbox", () => Child = toolbox = new ExpandingToolboxContainer(50, 200)
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Children = new Drawable[]
            {
                new EditorToolboxGroup("first") { Child = new Box { RelativeSizeAxes = Axes.X, Height = 60 } },
                new EditorToolboxGroup("second") { Child = new Box { RelativeSizeAxes = Axes.X, Height = 60 } },
                new EditorToolboxGroup("third") { Child = new Box { RelativeSizeAxes = Axes.X, Height = 60 } },
            }
        });

        private EditorToolboxGroup group(string title) => toolbox.ChildrenOfType<EditorToolboxGroup>().Single(g => g.Title == title);

        private float topOf(string title) => group(title).ScreenSpaceDrawQuad.TopLeft.Y;

        [Test]
        public void TestGroupsCanBeMovedAndTheOrderIsRemembered()
        {
            AddStep("start in the built order", () => config.SetValue(OsuSetting.EditorToolboxGroupOrder, string.Empty));

            createToolbox();

            AddAssert("built order holds", () => topOf("first"), () => Is.LessThan(topOf("second")));

            AddStep("move the second up", () => group("second").RequestReorder?.Invoke(-1));
            AddUntilStep("they swapped", () => topOf("second"), () => Is.LessThan(topOf("first")));
            AddAssert("the order was written", () => config.Get<string>(OsuSetting.EditorToolboxGroupOrder), () => Is.EqualTo("second|first|third"));

            createToolbox();
            AddUntilStep("it came back that way", () => topOf("second"), () => Is.LessThan(topOf("first")));
        }

        /// <remarks>
        /// Reordering is a rare, deliberate thing, and three permanently visible controls in every header is a lot of
        /// furniture for something a mapper touches once.
        /// </remarks>
        [Test]
        public void TestTheArrowsStayOutOfTheWayUntilPointedAt()
        {
            createToolbox();

            AddStep("point away", () => InputManager.MoveMouseTo(Vector2.Zero));
            AddUntilStep("the arrows are gone", () => arrowsOf("second").All(b => b.Alpha == 0));

            AddStep("point at the group", () => InputManager.MoveMouseTo(group("second")));
            AddUntilStep("they came back", () => arrowsOf("second").All(b => b.Alpha > 0));

            AddStep("point away again", () => InputManager.MoveMouseTo(Vector2.Zero));
            AddUntilStep("and they went", () => arrowsOf("second").All(b => b.Alpha == 0));
        }

        private IconButton[] arrowsOf(string title) =>
            group(title).ChildrenOfType<IconButton>().Where(b => b.Icon.Equals(FontAwesome.Solid.ChevronUp) || b.Icon.Equals(FontAwesome.Solid.ChevronDown)).ToArray();

        [Test]
        public void TestTheEndsOfTheListCannotMoveFurther()
        {
            createToolbox();

            AddAssert("the top cannot go up", () => !group("first").CanMoveUp.Value);
            AddAssert("the top can go down", () => group("first").CanMoveDown.Value);
            AddAssert("the bottom cannot go down", () => !group("third").CanMoveDown.Value);

            AddStep("ask the top to go up anyway", () => group("first").RequestReorder?.Invoke(-1));
            AddAssert("nothing moved", () => config.Get<string>(OsuSetting.EditorToolboxGroupOrder), () => Is.Empty);
        }

        [Test]
        public void TestFoldIsRemembered()
        {
            createToolbox();

            AddStep("fold the first group", () => group("first").Expanded.Value = false);
            AddAssert("fold was written", () => config.Get<string>(OsuSetting.EditorCollapsedToolboxGroups), () => Is.EqualTo("first"));

            AddStep("unfold it", () => group("first").Expanded.Value = true);
            AddAssert("nothing is folded", () => config.Get<string>(OsuSetting.EditorCollapsedToolboxGroups), () => Is.Empty);
        }

        [Test]
        public void TestFoldIsRestored()
        {
            AddStep("store a fold", () => config.SetValue(OsuSetting.EditorCollapsedToolboxGroups, "second"));

            createToolbox();

            AddAssert("second came back folded", () => !group("second").Expanded.Value);
            AddAssert("first did not", () => group("first").Expanded.Value);
        }

        /// <remarks>
        /// Stock behaviour is to show a folded group's contents for as long as the cursor is over it. In a sidebar the
        /// cursor has to cross to reach anything else, that hands back the room the mapper just folded away.
        /// </remarks>
        [Test]
        public void TestFoldedGroupStaysFoldedUnderTheCursor()
        {
            createToolbox();

            float foldedHeight = 0;

            AddStep("fold the first group", () => group("first").Expanded.Value = false);
            AddUntilStep("it shrank", () => (foldedHeight = group("first").DrawHeight), () => Is.LessThan(60));

            AddStep("point at it", () => InputManager.MoveMouseTo(group("first")));
            AddUntilStep("it stays folded", () => group("first").DrawHeight, () => Is.EqualTo(foldedHeight).Within(1f));

            AddStep("point away", () => InputManager.MoveMouseTo(Vector2.Zero));
            AddAssert("still folded", () => !group("first").Expanded.Value);
        }
    }
}
