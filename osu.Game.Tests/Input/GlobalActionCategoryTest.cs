// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Input.Bindings;

namespace osu.Game.Tests.Input
{
    [TestFixture]
    public class GlobalActionCategoryTest
    {
        /// <summary>
        /// Which category an action is filed under decides both the heading it appears under and whether it is
        /// registered at all. An action left out of every category is silently unbindable and never fires.
        /// </summary>
        [Test]
        public void TestEveryActionIsFiledUnderACategory()
        {
            var categorised = Enum.GetValues<GlobalActionCategory>()
                                  .SelectMany(GlobalActionContainer.GetGlobalActionsFor)
                                  .ToHashSet();

            // left out on purpose, so that it never fires: upstream's upload to the official servers, which this build does
            // not do (see TestSceneScreenshotsStayLocal).
            var leftOut = new[] { GlobalAction.TakeAndUploadScreeshot };

            var missing = Enum.GetValues<GlobalAction>().Where(a => !categorised.Contains(a) && !leftOut.Contains(a)).ToArray();

            Assert.That(missing, Is.Empty, $"not filed under any category: {string.Join(", ", missing)}");
        }

        /// <summary>
        /// Splitting this fork's editor bindings out for a heading of their own must not take them out of the stock
        /// editor grouping's job of being checked before the overlay bindings.
        /// </summary>
        [Test]
        public void TestForkEditorActionsAreCheckedAlongsideTheStockOnes()
        {
            var order = new GlobalActionContainer(null).DefaultKeyBindings.Select(b => (GlobalAction)b.Action).ToArray();

            int lastEditor = Array.LastIndexOf(order, GlobalActionCategory.Editor.ToActions().Last());
            int firstOverlay = Array.IndexOf(order, GlobalActionCategory.Overlays.ToActions().First());

            foreach (var action in GlobalActionCategory.EditorLazerKai.ToActions())
            {
                int at = Array.IndexOf(order, action);

                Assert.That(at, Is.GreaterThan(lastEditor), $"{action} is checked before the stock editor bindings");
                Assert.That(at, Is.LessThan(firstOverlay), $"{action} is checked after the overlay bindings");
            }
        }
    }

    internal static class GlobalActionCategoryExtensions
    {
        public static GlobalAction[] ToActions(this GlobalActionCategory category) => GlobalActionContainer.GetGlobalActionsFor(category).ToArray();
    }
}
