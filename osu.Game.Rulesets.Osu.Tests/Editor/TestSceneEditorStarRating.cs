// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Tests.Beatmaps;
using osu.Framework.Testing;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// What the editor's star rating is calculated with.
    /// </summary>
    /// <remarks>
    /// The rating itself cannot be asserted here: it is read through <see cref="BeatmapDifficultyCache"/>, which looks
    /// the beatmap up in the beatmap manager, and a test scene's beatmap is not there to be found. The mods handed to
    /// the calculation are what this build changed, so those are what is checked.
    /// </remarks>
    public partial class TestSceneEditorStarRating : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        private readonly RecordingDifficultyCache difficultyCache = new RecordingDifficultyCache();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs<BeatmapDifficultyCache>(difficultyCache);
            return dependencies;
        }

        [Test]
        public void TestTestPlayModsAlwaysReachTheRating()
        {
            AddStep("do not preview mods in the composer", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, false));

            AddStep("pick double time", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModDoubleTime() });
            AddUntilStep("rating asked for with double time", lastMods, () => Is.EqualTo(new[] { "DT" }));

            AddStep("pick easy instead", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModEasy() });
            AddUntilStep("rating asked for with easy", lastMods, () => Is.EqualTo(new[] { "EZ" }));

            AddStep("clear the mods", () => Editor.TestPlayMods.Mods.Value = []);
            AddUntilStep("rating asked for unmodded", lastMods, () => Is.Empty);
        }

        [Test]
        public void TestModsReachTheRatingWhilePreviewingInComposer()
        {
            AddStep("preview mods in the composer", () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, true));

            AddStep("pick hard rock", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModHardRock() });
            AddUntilStep("rating asked for with hard rock", lastMods, () => Is.EqualTo(new[] { "HR" }));
        }

        /// <summary>
        /// The status panel carries the same mods as an icon strip, so the rating shown next to it can be read in context.
        /// </summary>
        [Test]
        public void TestStatusPanelShowsTestPlayMods()
        {
            // mods chosen for test play are persisted, so another test in this fixture may have left some behind.
            AddStep("start with no mods", () => Editor.TestPlayMods.Mods.Value = []);
            AddUntilStep("no mod row while there are none", () => modIcons(), () => Is.Empty);

            AddStep("pick two mods", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModHardRock(), new OsuModDoubleTime() });
            AddUntilStep("both icons shown", () => modIcons(), () => Is.EquivalentTo(new[] { "HR", "DT" }));

            AddStep("clear the mods", () => Editor.TestPlayMods.Mods.Value = []);
            AddUntilStep("icons gone", () => modIcons(), () => Is.Empty);
        }

        /// <summary>
        /// A mod's own settings can be edited without the selection itself changing, and the rating has to follow.
        /// </summary>
        [Test]
        public void TestSettingChangeReachesTheRating()
        {
            AddStep("pick double time", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModDoubleTime() });
            AddUntilStep("rating asked for at 1.5x", lastSpeed, () => Is.EqualTo(1.5));

            // reached for through the selection rather than held onto: the mod screen swaps in instances of its own.
            AddStep("slow it to 1.25x", () => Editor.TestPlayMods.Mods.Value.OfType<OsuModDoubleTime>().Single().SpeedChange.Value = 1.25);
            AddUntilStep("rating asked for at 1.25x", lastSpeed, () => Is.EqualTo(1.25));
        }

        private double? lastSpeed() => difficultyCache.LastMods?.OfType<OsuModDoubleTime>().SingleOrDefault()?.SpeedChange.Value;

        private string[] modIcons() => Editor.ChildrenOfType<ComposeStatusDisplay>()
                                             .SelectMany(d => d.ChildrenOfType<ModIcon>())
                                             .Select(i => i.Mod.Acronym)
                                             .ToArray();

        private string[] lastMods() => difficultyCache.LastMods?.Select(m => m.Acronym).ToArray() ?? [];

        private partial class RecordingDifficultyCache : BeatmapDifficultyCache
        {
            public IReadOnlyList<Mod>? LastMods { get; private set; }

            public override Task<StarDifficulty?> GetDifficultyAsync(IBeatmapInfo beatmapInfo, IRulesetInfo? rulesetInfo = null, IEnumerable<Mod>? mods = null,
                                                                     CancellationToken cancellationToken = default, int computationDelay = 0)
            {
                LastMods = mods?.ToArray() ?? [];
                return Task.FromResult<StarDifficulty?>(new StarDifficulty(1, 0));
            }
        }
    }
}
