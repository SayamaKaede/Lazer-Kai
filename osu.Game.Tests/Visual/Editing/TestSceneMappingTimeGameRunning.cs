// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Overlays;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The statistics count the game's running time up to the moment they are opened.
    /// </summary>
    public partial class TestSceneMappingTimeGameRunning : OsuGameTestScene
    {
        /// <summary>
        /// Reported on 2026-10-20: after three minutes of editing the game had been running for one.
        /// </summary>
        [Test]
        public void TestTheTimeRunningSoFarIsCountedWhenOpened()
        {
            DateTimeOffset started = default;
            DateTimeOffset opened = default;

            AddStep("note the time", () => started = DateTimeOffset.Now);
            AddUntilStep("a couple of seconds go by", () => DateTimeOffset.Now - started > TimeSpan.FromSeconds(2));

            AddStep("open the statistics", () =>
            {
                opened = DateTimeOffset.Now;
                Game.ChildrenOfType<MappingTimeOverlay>().Single().Show();
            });

            AddUntilStep("the game has run up to then", () => new MappingTimeStore(Game.Storage).Load()
                                                                                            .Where(s => s.Kind == MappingSpanKind.Game)
                                                                                            .Select(s => s.End)
                                                                                            .DefaultIfEmpty()
                                                                                            .Max(),
                () => Is.GreaterThanOrEqualTo(opened));
        }
    }
}
