// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osuTK.Graphics;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class UiPaletteTest
    {
        private static UiPaletteValues kai(float hue = 205, float pull = 1, float saturation = 1, float gamma = 1, float greyTint = 0, float shear = 0.12f) =>
            new UiPaletteValues(UiTheme.LazerKai, hue, pull, saturation, gamma, greyTint, shear);

        [Test]
        public void TestTheBaseThemeIsExactlyWhatTheGameHadBefore()
        {
            var look = new UiPaletteValues(UiTheme.Lazer, 205, 1, 0.2f, 2, 0.3f, 0.01f);

            // every number above is set to something that would show if it were being applied.
            Assert.That(look.Resolve(333, 0.4f, 0.75f), Is.EqualTo((Color4)Colour4.FromHSL(333 / 360f, 0.4f, 0.75f)));
            Assert.That(look.Grey(0.6f), Is.EqualTo(new Color4(0.6f, 0.6f, 0.6f, 1f)));
            Assert.That(look.Shear.X, Is.EqualTo(UiPaletteValues.LAZER_SHEAR));
        }

        [Test]
        public void TestNoPullLeavesEveryOverlayItsOwnHue()
        {
            var look = kai(pull: 0);

            Assert.That(look.Resolve(333, 0.4f, 0.75f), Is.EqualTo((Color4)Colour4.FromHSL(333 / 360f, 0.4f, 0.75f)));
        }

        [Test]
        public void TestFullPullPaintsEveryOverlayTheSameHue()
        {
            var look = kai(hue: 205);

            var fromBlue = look.Resolve(200, 0.4f, 0.75f);
            var fromPink = look.Resolve(333, 0.4f, 0.75f);

            Assert.That(fromPink, Is.EqualTo(fromBlue));
            Assert.That(fromBlue, Is.EqualTo((Color4)Colour4.FromHSL(205 / 360f, 0.4f, 0.75f)));
        }

        [Test]
        public void TestTheHueTakesTheShortWayRoundEvenWhenThatCrossesZero()
        {
            // 340 to 20 is 40 degrees going up through 0, and 320 going down through 180. Taking the long way
            // would walk a pink overlay through green on its way to becoming red.
            var look = kai(hue: 20, pull: 0.5f);

            var halfway = look.Resolve(340, 1f, 0.5f);

            Assert.That(halfway, Is.EqualTo((Color4)Colour4.FromHSL(0 / 360f, 1f, 0.5f)));
        }

        [Test]
        public void TestSaturationScales()
        {
            var look = kai(pull: 0, saturation: 0.5f);

            Assert.That(look.Resolve(200, 0.8f, 0.5f), Is.EqualTo((Color4)Colour4.FromHSL(200 / 360f, 0.4f, 0.5f)));
        }

        [Test]
        public void TestSaturationCannotBePushedPastFull()
        {
            var look = kai(pull: 0, saturation: 4);

            Assert.That(look.Resolve(200, 0.8f, 0.5f), Is.EqualTo((Color4)Colour4.FromHSL(200 / 360f, 1f, 0.5f)));
        }

        [Test]
        public void TestBendingTheLightnessDarkensTheMiddleAndLeavesTheEndsAlone()
        {
            var look = kai(pull: 0, gamma: 2);

            Assert.That(look.Resolve(200, 0f, 0.5f).R, Is.EqualTo(0.25f).Within(0.01f));

            // black and white are already where the curve starts and ends, so nothing can move them.
            Assert.That(look.Resolve(200, 0f, 0f).R, Is.EqualTo(0f).Within(0.001f));
            Assert.That(look.Resolve(200, 0f, 1f).R, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void TestGreyStaysGreyUntilItIsGivenATint()
        {
            Assert.That(kai().Grey(0.4f), Is.EqualTo(new Color4(0.4f, 0.4f, 0.4f, 1f)));

            var tinted = kai(greyTint: 0.2f).Grey(0.4f);

            Assert.That(tinted.R, Is.Not.EqualTo(tinted.B).Within(0.01f));

            // the lightness is what was asked for either way: a grey is picked to sit at a depth, and tinting it
            // must not move it.
            Assert.That(((Colour4)tinted).ToHSL().Z, Is.EqualTo(0.4f).Within(0.01f));
        }

        [Test]
        public void TestTheForksThemeCarriesItsOwnSlant()
        {
            Assert.That(kai(shear: 0.05f).Shear.X, Is.EqualTo(0.05f));
            Assert.That(Precision.AlmostEquals(UiPaletteValues.Default.Shear.X, UiPaletteValues.LAZER_SHEAR), Is.False,
                "the fork's theme is meant to look different from the base one");
        }

        [Test]
        public void TestTheSlantIsMeasuredAgainstUpstreams()
        {
            // what the layouts holding a hand-picked number for the slant multiply it by: all of it at upstream's,
            // none of it once the slant is gone, which is where this fork's theme now sits.
            Assert.That(UiPaletteValues.For(UiTheme.Lazer).ShearScale, Is.EqualTo(1).Within(0.001f));
            Assert.That(kai(shear: 0.1f).ShearScale, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(UiPaletteValues.Default.ShearScale, Is.EqualTo(0));
        }

        [Test]
        public void TestOnlyTheThemeChangesWhenSwitchingTheme()
        {
            var swapped = UiPaletteValues.For(UiTheme.Lazer);

            Assert.That(swapped, Is.EqualTo(UiPaletteValues.Default with { Theme = UiTheme.Lazer }));
        }
    }
}
