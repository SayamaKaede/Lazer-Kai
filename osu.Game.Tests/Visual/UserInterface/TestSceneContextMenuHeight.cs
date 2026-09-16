// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Tests.Visual.UserInterface
{
    /// <summary>
    /// A context menu with many items shows them all rather than hiding the rest behind a scroll.
    /// </summary>
    public partial class TestSceneContextMenuHeight : OsuManualInputManagerTestScene
    {
        private const int item_count = 20;

        private Container target = null!;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Child = new OsuContextMenuContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = target = new ManyItemsContainer
                {
                    Size = new Vector2(200),
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(50),
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Green,
                    }
                }
            };
        });

        [Test]
        public void TestEveryItemIsShown()
        {
            AddStep("open the menu", () =>
            {
                InputManager.MoveMouseTo(target);
                InputManager.Click(MouseButton.Right);
            });

            AddUntilStep("the menu is open", () => menu().State == MenuState.Open);
            AddUntilStep("it is as tall as its items", () =>
            {
                var items = menu().ChildrenOfType<DrawableOsuMenuItem>().ToArray();

                return items.Length == item_count && items.All(i => menu().ScreenSpaceDrawQuad.Contains(i.ScreenSpaceDrawQuad.Centre));
            });
        }

        private OsuContextMenu menu() => this.ChildrenOfType<OsuContextMenu>().Single();

        private partial class ManyItemsContainer : Container, IHasContextMenu
        {
            public MenuItem[] ContextMenuItems => Enumerable.Range(0, item_count)
                                                            .Select(i => (MenuItem)new OsuMenuItem($"item {i}"))
                                                            .ToArray();
        }
    }
}
