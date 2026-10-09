// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.IO;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Overlays.Changelog;

namespace osu.Game.Tests.Visual.Navigation
{
    /// <summary>
    /// The changelog the game shows by itself on the first start after an update.
    /// </summary>
    public partial class TestSceneLazerKaiChangelogAfterUpdate : OsuGameTestScene
    {
        private string lastVersion = string.Empty;
        private string runningVersion = string.Empty;

        // each test starts its own game, from the versions it sets first.
        public override void SetUpSteps()
        {
        }

        [Test]
        public void TestShownOnceTheMainMenuIsReached()
        {
            startGame(@"20260929.0", @"20261008.0");

            AddUntilStep("the changelog is shown", () => overlay.State.Value, () => Is.EqualTo(Visibility.Visible));
            AddAssert("with the releases since the last one run",
                () => overlay.ChildrenOfType<LazerKaiChangelogRelease>().Select(r => r.Release.Version),
                () => Is.EqualTo(new[] { @"20261008.0", @"20261005.0" }));

            AddWaitStep("let the menu settle", 10);
            AddAssert("still shown", () => overlay.State.Value, () => Is.EqualTo(Visibility.Visible));
        }

        [Test]
        public void TestNotShownWithoutAnUpdate()
        {
            startGame(@"20261008.0", @"20261008.0");

            AddWaitStep("let the menu settle", 10);
            AddAssert("no changelog", () => overlay.State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        private LazerKaiChangelogOverlay overlay => Game.ChildrenOfType<LazerKaiChangelogOverlay>().Single();

        private void startGame(string last, string running)
        {
            AddStep($"last ran {last}, now {running}", () =>
            {
                lastVersion = last;
                runningVersion = running;
            });

            CreateNewGame();
            ConfirmAtMainMenu();

            // the game loads its overlays one after another, and here can reach the menu before this one is in.
            AddUntilStep("the overlay is loaded", () => Game.ChildrenOfType<LazerKaiChangelogOverlay>().Any());
        }

        protected override TestOsuGame CreateTestGame() => new UpdatedGame(LocalStorage, API, lastVersion, runningVersion);

        private partial class UpdatedGame : TestOsuGame
        {
            private static readonly IReadOnlyList<LazerKaiChangelog.Release> releases = new[] { @"20261008.0", @"20261005.0", @"20260929.0" }
                                                                                        .Select(v => LazerKaiChangelog.Parse(v, "**修正**\n- 変更\n"))
                                                                                        .ToArray();

            private readonly string lastVersion;
            private readonly string runningVersion;

            public override string Version => runningVersion;

            public UpdatedGame(Storage storage, IAPIProvider api, string lastVersion, string runningVersion)
                : base(storage, api)
            {
                this.lastVersion = lastVersion;
                this.runningVersion = runningVersion;
            }

            protected override IReadOnlyList<LazerKaiChangelog.Release> ReadLazerKaiChangelog() => releases;

            protected override void LoadComplete()
            {
                // what the game finds in its configuration as the version run last.
                LocalConfig.SetValue(OsuSetting.Version, lastVersion);

                base.LoadComplete();
            }
        }
    }
}
