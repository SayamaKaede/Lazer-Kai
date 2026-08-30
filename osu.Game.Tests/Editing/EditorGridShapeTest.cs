// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class EditorGridShapeTest
    {
        /// <summary>
        /// The grid shape a mapper picks in the options is stored as <see cref="EditorGridShape"/>, because the
        /// settings live where osu!'s own <see cref="PositionSnapGridType"/> cannot be reached from. The editor
        /// then picks the grid button at that position, so the two lists have to say the same thing in the same
        /// order — and nothing in the compiler makes them.
        /// </summary>
        [Test]
        public void TestTheTwoListsAgree()
        {
            Assert.That(Enum.GetNames<EditorGridShape>(), Is.EqualTo(Enum.GetNames<PositionSnapGridType>()));

            Assert.That(Enum.GetValues<EditorGridShape>().Select(v => (int)v),
                Is.EqualTo(Enum.GetValues<PositionSnapGridType>().Select(v => (int)v)));
        }
    }
}
