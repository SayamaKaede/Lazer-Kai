// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.Profile;
using osu.Game.Overlays.Profile.Sections;
using osu.Game.Overlays.Profile.Sections.Beatmaps;
using osu.Game.Rulesets.Osu;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Online
{
    /// <summary>
    /// A profile listing's "download all", from the hold through to the downloads it starts.
    /// </summary>
    public partial class TestSceneProfileDownloadAll : OsuManualInputManagerTestScene
    {
        private const int user_id = 4242;

        /// <summary>
        /// Far from any set the test game could have imported, so none of these counts as downloaded already.
        /// </summary>
        private const int first_set = 900_001;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Pink);

        private BeatmapModelDownloader downloader = null!;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

            // the game's own downloader talks to the real API, so its downloads would never reach the requests this test
            // answers — they went out to the network before this was here. This one goes through the scene's dummy.
            dependencies.Cache(downloader = new BeatmapModelDownloader(parent.Get<BeatmapManager>(), dependencies.Get<IAPIProvider>()));

            return dependencies;
        }

        private readonly Bindable<UserProfileData?> user = new Bindable<UserProfileData?>();
        private readonly List<DownloadBeatmapSetRequest> downloads = new List<DownloadBeatmapSetRequest>();

        private APIBeatmapSet[] listing = Array.Empty<APIBeatmapSet>();
        private PaginatedBeatmapContainer container = null!;

        /// <summary>
        /// Whether the button has been held — before it, the only pages asked for are the listing's own first few.
        /// </summary>
        private bool held;

        private int pagesAsked;
        private int? failingPage;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("answer requests", () =>
            {
                downloads.Clear();
                held = false;
                pagesAsked = 0;
                failingPage = null;

                ((DummyAPIAccess)API).HandleRequest = request =>
                {
                    switch (request)
                    {
                        case GetUserBeatmapsRequest page:
                            if (!held)
                            {
                                page.TriggerSuccess(listing.Take(7).ToList());
                                return true;
                            }

                            int index = pagesAsked++;

                            if (index == failingPage)
                                page.TriggerFailure(new WebException(@"the page could not be read"));
                            else
                                page.TriggerSuccess(listing.Skip(index * 50).Take(50).ToList());

                            return true;

                        // left running, so what is running at once can be counted.
                        case DownloadBeatmapSetRequest download:
                            downloads.Add(download);
                            return true;
                    }

                    return false;
                };
            });
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            // the downloader outlives the test, and a download left running would make the next test's sets look taken.
            // Each cancelled one lets the batch start another, so this goes round until there are none.
            AddUntilStep("cancel every download", () =>
            {
                foreach (var download in downloads.ToArray())
                    download.Cancel();

                return listing.All(s => downloader.GetExistingDownload(s) == null);
            });
        }

        [Test]
        public void TestFavouritesHaveNoButton()
        {
            createListing(BeatmapSetType.Favourite, 10);

            AddUntilStep("the listing is showing", () => container.ChildrenOfType<ProfileSubsectionHeader>().Single().Current.Value, () => Is.EqualTo(10));
            AddAssert("there is no button", () => container.ChildrenOfType<DownloadAllButton>(), () => Is.Empty);
        }

        [TestCase(BeatmapSetType.Ranked)]
        [TestCase(BeatmapSetType.Loved)]
        [TestCase(BeatmapSetType.Guest)]
        [TestCase(BeatmapSetType.Pending)]
        [TestCase(BeatmapSetType.Graveyard)]
        [TestCase(BeatmapSetType.Nominated)]
        public void TestEveryOtherListingHasOne(BeatmapSetType type)
        {
            createListing(type, 10);

            AddUntilStep("the button is showing", () => control()?.IsPresent == true);
        }

        /// <summary>
        /// It acts on the whole listing, and an empty one leaves it nothing to act on.
        /// </summary>
        [Test]
        public void TestAnEmptyListingShowsNoButton()
        {
            createListing(BeatmapSetType.Ranked, 0);

            AddWaitStep("give it a moment", 3);
            AddAssert("the button is not showing", () => control()?.IsPresent, () => Is.False);
        }

        /// <summary>
        /// The listing on screen holds six; the button has to go through every page to the end, and then start only a
        /// few at a time, moving on as each ends.
        /// </summary>
        [Test]
        public void TestHoldingReadsEveryPageAndDownloadsAFewAtATime()
        {
            createListing(BeatmapSetType.Ranked, 60);
            hold();

            AddUntilStep("three are downloading", () => downloads.Count, () => Is.EqualTo(BulkBeatmapDownload.CONCURRENT_DOWNLOADS));
            AddAssert("both pages were read", () => pagesAsked, () => Is.EqualTo(2));
            AddAssert("from the top of the listing", () => downloads.Select(d => d.Model.OnlineID), () => Is.EqualTo(new[] { first_set, first_set + 1, first_set + 2 }));

            AddWaitStep("give it a moment", 5);
            AddAssert("still only three", () => downloads.Count, () => Is.EqualTo(BulkBeatmapDownload.CONCURRENT_DOWNLOADS));

            AddStep("one of them fails", () => downloads[0].TriggerFailure(new WebException(@"the download failed")));

            AddUntilStep("the next one starts", () => downloads.Count, () => Is.EqualTo(BulkBeatmapDownload.CONCURRENT_DOWNLOADS + 1));
            AddAssert("the fourth set", () => downloads.Last().Model.OnlineID, () => Is.EqualTo(first_set + 3));
            AddAssert("the button waits for the run to end", () => button()!.Enabled.Value, () => Is.False);
        }

        /// <summary>
        /// A page that cannot be read leaves the listing short, and downloading "all" of what did arrive would quietly
        /// leave the rest behind. So nothing is downloaded, and the button can be held again to retry.
        /// </summary>
        [Test]
        public void TestAPageThatCannotBeReadStartsNothing()
        {
            createListing(BeatmapSetType.Ranked, 60);
            AddStep("the second page will fail", () => failingPage = 1);
            hold();

            AddUntilStep("both pages were asked for", () => pagesAsked, () => Is.EqualTo(2));
            AddUntilStep("the button can be held again", () => button()!.Enabled.Value);
            AddAssert("nothing was downloaded", () => downloads, () => Is.Empty);
        }

        private void createListing(BeatmapSetType type, int count) => AddStep($"show a {type} listing of {count}", () =>
        {
            listing = Enumerable.Range(first_set, count).Select(makeSet).ToArray();

            Child = container = new PaginatedBeatmapContainer(type, user, $@"{type}")
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Width = 0.8f,
            };

            user.Value = new UserProfileData(new APIUser
            {
                Id = user_id,
                Username = @"test",
                FavouriteBeatmapsetCount = count,
                RankedBeatmapsetCount = count,
                LovedBeatmapsetCount = count,
                GuestBeatmapsetCount = count,
                PendingBeatmapsetCount = count,
                GraveyardBeatmapsetCount = count,
                NominatedBeatmapsetCount = count,
            }, new OsuRuleset().RulesetInfo);
        });

        private void hold()
        {
            AddUntilStep("the button is showing", () => control()?.IsPresent == true);
            AddStep("hold it", () =>
            {
                held = true;
                InputManager.MoveMouseTo(button()!);
                InputManager.PressButton(MouseButton.Left);
            });
            AddUntilStep("it went through", () => pagesAsked, () => Is.GreaterThan(0));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        private DownloadAllButton? button() => container.ChildrenOfType<DownloadAllButton>().SingleOrDefault();

        /// <summary>
        /// What the heading hides for an empty listing. Asked rather than the button, whose own presence says nothing
        /// about whether its parent is showing.
        /// </summary>
        private DownloadAllControl? control() => container.ChildrenOfType<DownloadAllControl>().SingleOrDefault();

        private APIBeatmapSet makeSet(int id)
        {
            var set = CreateAPIBeatmapSet(new OsuRuleset().RulesetInfo);

            set.OnlineID = id;
            set.Title = $@"set {id}";

            return set;
        }
    }
}
