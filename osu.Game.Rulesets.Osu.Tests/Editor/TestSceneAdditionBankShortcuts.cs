// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Alt+Q/W/E/R over the composer sets the addition bank of the selection, the way shift does for the normal bank.
    /// </summary>
    public partial class TestSceneAdditionBankShortcuts : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [TestCase(Key.W, HitSampleInfo.BANK_NORMAL)]
        [TestCase(Key.E, HitSampleInfo.BANK_SOFT)]
        [TestCase(Key.R, HitSampleInfo.BANK_DRUM)]
        public void TestAltSetsTheAdditionBank(Key key, string expected)
        {
            AddStep("place a circle with a whistle on soft", () =>
            {
                EditorBeatmap.RemoveRange(EditorBeatmap.HitObjects.ToArray());

                var circle = new HitCircle { StartTime = 1000, Position = new Vector2(100, 100) };
                circle.Samples =
                [
                    new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_SOFT),
                    new HitSampleInfo(HitSampleInfo.HIT_WHISTLE, HitSampleInfo.BANK_SOFT, editorAutoBank: false),
                ];
                EditorBeatmap.Add(circle);
            });

            AddStep("select it", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects.First());
            });

            AddStep($"press alt+{key}", () =>
            {
                InputManager.PressKey(Key.AltLeft);
                InputManager.Key(key);
                InputManager.ReleaseKey(Key.AltLeft);
            });

            AddAssert("the addition bank changed", additionBank, () => Is.EqualTo(expected));
        }

        private string additionBank() => EditorBeatmap.HitObjects.First().Samples
                                                     .Single(s => s.Name == HitSampleInfo.HIT_WHISTLE).Bank;
    }
}
