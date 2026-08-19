// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Which bank an addition takes when it is put on a note that had none.
    /// </summary>
    /// <remarks>
    /// An addition bank cannot be stored on an object until an addition sample is on it, so setting it note by note is
    /// the only way without a default to start from.
    /// </remarks>
    public partial class TestSceneDefaultAdditionBank : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [TestCase(EditorAdditionBank.Soft, HitSampleInfo.BANK_SOFT)]
        [TestCase(EditorAdditionBank.Drum, HitSampleInfo.BANK_DRUM)]
        [TestCase(EditorAdditionBank.Normal, HitSampleInfo.BANK_NORMAL)]
        public void TestAddedWhistleTakesTheDefaultBank(EditorAdditionBank setting, string expected)
        {
            setDefault(setting);
            placeAndSelectAPlainCircle();

            AddStep("add a whistle", () => selectionHandler().AddHitSample(HitSampleInfo.HIT_WHISTLE));

            AddAssert("the whistle took the default bank", () => whistleBank(), () => Is.EqualTo(expected));
        }

        /// <summary>
        /// Auto has to stay available, and has to behave as it always did.
        /// </summary>
        [Test]
        public void TestAutoLeavesTheBankAlone()
        {
            setDefault(EditorAdditionBank.Auto);
            placeAndSelectAPlainCircle();

            AddStep("add a whistle", () => selectionHandler().AddHitSample(HitSampleInfo.HIT_WHISTLE));

            AddAssert("the whistle was left on auto", () => EditorBeatmap.HitObjects.First().Samples
                                                                        .Single(s => s.Name == HitSampleInfo.HIT_WHISTLE).EditorAutoBank,
                () => Is.True);
        }

        /// <summary>
        /// Changing the setting has to reach a selection made afterwards, not only the next editor session.
        /// </summary>
        [Test]
        public void TestChangingTheSettingTakesEffectWithoutReopening()
        {
            setDefault(EditorAdditionBank.Auto);
            placeAndSelectAPlainCircle();

            setDefault(EditorAdditionBank.Drum);

            // the pre-selected bank belongs to the selection, so it is picked up on the next one, not torn out from
            // under the current one. Deselecting and selecting again is what a mapper does anyway.
            AddStep("deselect", () => EditorBeatmap.SelectedHitObjects.Clear());
            AddStep("select again", () => EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects.First()));

            AddStep("add a whistle", () => selectionHandler().AddHitSample(HitSampleInfo.HIT_WHISTLE));
            AddAssert("the whistle took drum", () => whistleBank(), () => Is.EqualTo(HitSampleInfo.BANK_DRUM));
        }

        private void setDefault(EditorAdditionBank bank)
            => AddStep($"default bank is {bank}", () => config.SetValue(OsuSetting.EditorDefaultAdditionBank, bank));

        private void placeAndSelectAPlainCircle()
        {
            AddStep("place a circle with no additions", () =>
            {
                EditorBeatmap.RemoveRange(EditorBeatmap.HitObjects.ToArray());
                EditorBeatmap.Add(new HitCircle { StartTime = 1000, Position = new Vector2(100, 100) });
            });

            AddStep("select it", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects.First());
            });
        }

        private string whistleBank() => EditorBeatmap.HitObjects.First().Samples.Single(s => s.Name == HitSampleInfo.HIT_WHISTLE).Bank;

        private EditorSelectionHandler selectionHandler() => Editor.ChildrenOfType<EditorSelectionHandler>().First();
    }
}
