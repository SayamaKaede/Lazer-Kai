// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Game.IO;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Changelog;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Settings.Sections.LazerKai;

namespace osu.Game.Tests.Visual.Online
{
    /// <summary>
    /// The fork's changelogs, shown the way osu!'s changelog shows a build.
    /// </summary>
    public partial class TestSceneLazerKaiChangelogOverlay : OsuTestScene
    {
        private static readonly LazerKaiChangelog.Release[] releases =
        {
            LazerKaiChangelog.Parse(@"20261008.0", string.Join('\n',
                @"**Lazer-Kai 20261008.0**",
                @"",
                @"**変更**",
                @"- 「スライダーの操作をStableに近づける」がオンのときの動作をさらに最適化しました",
                @"- 「小数点以下の座標に配置できないようにする」がオンのとき、Stableと同じく小数点以下を切り捨てるようにしました",
                @"  スライダーのアンカーや、連打の変換・Generateで作ったノーツも整数になります",
                @"",
                @"**修正**",
                @"- Lazer-Kaiで作ったスタックが、Stableで開くと崩れたり1pxずれたりする問題を修正")),
            LazerKaiChangelog.Parse(@"20261005.0", string.Join('\n',
                @"**Lazer-Kai 20261005.0**",
                @"",
                @"- Tachyon 2026.1001.0がリリースされたので、ベースのバージョンを変えました",
                @"",
                @"**機能追加**",
                @"- F6の緑線の一覧に「選択した線を現在位置から貼り付け」を追加しました")),
        };

        private LazerKaiChangelogOverlay overlay = null!;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create the overlay", () => Child = overlay = new LazerKaiChangelogOverlay());
        }

        [Test]
        public void TestShowsTheReleasesGiven()
        {
            AddStep("show both", () => overlay.ShowReleases(releases));

            AddUntilStep("shown", () => overlay.State.Value, () => Is.EqualTo(Visibility.Visible));
            AddAssert("both releases, in the order given",
                () => overlay.ChildrenOfType<LazerKaiChangelogRelease>().Select(r => r.Release.Version),
                () => Is.EqualTo(new[] { @"20261008.0", @"20261005.0" }));

            AddStep("show only the newer", () => overlay.ShowReleases(releases.Take(1).ToArray()));
            AddAssert("the older one is gone",
                () => overlay.ChildrenOfType<LazerKaiChangelogRelease>().Select(r => r.Release.Version),
                () => Is.EqualTo(new[] { @"20261008.0" }));
        }

        /// <summary>
        /// In any language but Japanese, a release with an English changelog is shown in English.
        /// </summary>
        [Test]
        public void TestTheLanguageOfTheGameDecidesTheLanguageShown()
        {
            var withEnglish = releases[0] with { EnglishLines = LazerKaiChangelog.Parse(@"20261008.0", "**Lazer-Kai 20261008.0**\n\n**Fixes**\n- Fixed a thing\n").Lines };

            AddStep("the game in Japanese", () => game.CurrentLanguage.Value = Language.ja);
            AddStep("show a release with English and one without", () => overlay.ShowReleases(new[] { withEnglish, releases[1] }));
            AddAssert("both from the Japanese", () => shownInJapanese(), () => Is.EqualTo(new[] { true, true }));
            AddAssert("in Japanese", () => textShown(), () => Does.Contain(@"修正").And.Not.Contain(@"Fixes"));

            AddStep("the game in English", () => game.CurrentLanguage.Value = Language.en);
            AddAssert("in English where there is one", () => textShown(), () => Does.Contain(@"Fixes"));
            AddAssert("and in Japanese where there is not", () => textShown(), () => Does.Contain(@"機能追加"));

            AddStep("the game in German", () => game.CurrentLanguage.Value = Language.de);
            AddAssert("English still", () => textShown(), () => Does.Contain(@"Fixes"));

            AddStep("back to English", () => game.CurrentLanguage.Value = Language.en);

            bool[] shownInJapanese() => overlay.ChildrenOfType<LazerKaiChangelogRelease>().Select(r => r.InJapanese).ToArray();

            // the flow cuts a line into words, and Japanese into characters, so they are put back together.
            string textShown() => string.Concat(overlay.ChildrenOfType<LazerKaiChangelogRelease>().SelectMany(r => r.ChildrenOfType<SpriteText>()).Select(t => t.Text.ToString()));
        }

        [Test]
        public void TestTheSettingsButtonShowsEveryRelease()
        {
            ManualSettings settings = null!;

            AddStep("create the settings", () =>
            {
                Child = new DependencyProvidingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    CachedDependencies = new (System.Type, object)[]
                    {
                        (typeof(LazerKaiChangelogOverlay), overlay = new LazerKaiChangelogOverlay()),
                        (typeof(OverlayColourProvider), new OverlayColourProvider(OverlayColourScheme.Purple)),
                    },
                    Children = new Drawable[]
                    {
                        overlay,
                        settings = new ManualSettings(null, null, releases)
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Width = 400,
                        },
                    },
                };
            });

            AddUntilStep("only the changelog button, with no manual", () => settings.ChildrenOfType<SettingsButton>().Count(), () => Is.EqualTo(1));
            AddStep("press it", () => settings.ChildrenOfType<SettingsButton>().Single().TriggerClick());

            AddUntilStep("shown", () => overlay.State.Value, () => Is.EqualTo(Visibility.Visible));
            AddAssert("every release", () => overlay.ChildrenOfType<LazerKaiChangelogRelease>().Count(), () => Is.EqualTo(2));
        }
    }
}
