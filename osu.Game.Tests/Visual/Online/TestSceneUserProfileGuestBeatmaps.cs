// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.BeatmapListing;
using osu.Game.Overlays.Profile;
using osu.Game.Overlays.Profile.Sections;
using osu.Game.Overlays.Profile.Sections.Beatmaps;
using osu.Game.Rulesets.Osu;

namespace osu.Game.Tests.Visual.Online
{
    /// <summary>
    /// The listing of guest difficulties on beatmaps that are not ranked, which is assembled from the beatmap
    /// search rather than read from a profile endpoint.
    /// </summary>
    [TestFixture]
    public partial class TestSceneUserProfileGuestBeatmaps : OsuTestScene
    {
        private const int user_id = 4242;
        private const int somebody_else = 99;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Green);

        private readonly Bindable<UserProfileData?> user = new Bindable<UserProfileData?>();

        private readonly List<SearchBeatmapSetsRequest> requests = new List<SearchBeatmapSetsRequest>();
        private readonly Dictionary<SearchCategory, Queue<SearchBeatmapSetsResponse>> pages = new Dictionary<SearchCategory, Queue<SearchBeatmapSetsResponse>>();

        private GuestBeatmapSetsContainer container = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("answer the search", () =>
            {
                requests.Clear();
                pages.Clear();

                ((DummyAPIAccess)API).HandleRequest = request =>
                {
                    if (request is not SearchBeatmapSetsRequest search)
                        return false;

                    requests.Add(search);

                    if (pages.TryGetValue(search.SearchCategory, out var queued) && queued.Count > 0)
                        search.TriggerSuccess(queued.Dequeue());
                    else
                        search.TriggerSuccess(new SearchBeatmapSetsResponse { BeatmapSets = Array.Empty<APIBeatmapSet>(), Cursor = null });

                    return true;
                };
            });
        }

        /// <summary>
        /// The whole feature rests on the request being shaped a particular way: the profile's own guest listing
        /// cannot reach these maps, and the search only can because <c>creator=</c> matches the owner of each
        /// difficulty. Asking for the wrong thing here returns a plausible-looking listing of the wrong maps.
        /// </summary>
        [Test]
        public void TestAsksTheSearchForDifficultiesThisUserOwns()
        {
            page(SearchCategory.Graveyard, sets: new[] { guestSet(1, daysAgo: 1) }, more: false);
            createContainer(SearchCategory.Graveyard);

            AddUntilStep("it asked", () => requests.Count, () => Is.EqualTo(1));

            AddAssert("by user id, not name", () => requests[0].Query, () => Is.EqualTo($"creator={user_id}"));
            AddAssert("for graveyarded maps", () => requests[0].SearchCategory, () => Is.EqualTo(SearchCategory.Graveyard));
            AddAssert("newest first", () => (requests[0].SortCriteria, requests[0].SortDirection), () => Is.EqualTo((SortCriteria.Updated, SortDirection.Descending)));
            AddAssert("without hiding explicit maps", () => requests[0].ExplicitContent, () => Is.EqualTo(SearchExplicit.Show));
        }

        /// <summary>
        /// The profile's pending listing counts WIP as pending; the search keeps them apart. Both have to be
        /// asked for, or a guest difficulty on a map its host marked WIP is reported nowhere at all.
        /// </summary>
        [Test]
        public void TestPendingAsksForWipAsWell()
        {
            createContainer(SearchCategory.Pending, SearchCategory.Wip);

            AddUntilStep("both were asked for", () => requests.Select(r => r.SearchCategory).OrderBy(c => c),
                () => Is.EqualTo(new[] { SearchCategory.Pending, SearchCategory.Wip }.OrderBy(c => c)));
        }

        /// <summary>
        /// The search cannot leave out the maps this user hosts, so they arrive mixed in with the guest ones and
        /// have to be dropped here. They are listed elsewhere on the same profile, so showing them again would
        /// make the section a duplicate rather than a new one.
        /// </summary>
        [Test]
        public void TestMapsThisUserHostsAreLeftOut()
        {
            page(SearchCategory.Graveyard, sets: new[]
            {
                ownSet(1, daysAgo: 1),
                guestSet(2, daysAgo: 2),
                ownSet(3, daysAgo: 3),
            }, more: false);

            createContainer(SearchCategory.Graveyard);

            AddUntilStep("only the guest one is shown", shownIds, () => Is.EqualTo(new[] { 2 }));
        }

        /// <summary>
        /// Pending and WIP arrive as two separate listings, each newest first, and have to end up as one listing
        /// in that same order — which is how the profile's own pending listing reads.
        /// </summary>
        [Test]
        public void TestPendingAndWipAreOrderedTogether()
        {
            page(SearchCategory.Pending, sets: new[] { guestSet(1, daysAgo: 1), guestSet(3, daysAgo: 3) }, more: false);
            page(SearchCategory.Wip, sets: new[] { guestSet(2, daysAgo: 2) }, more: false);

            createContainer(SearchCategory.Pending, SearchCategory.Wip);

            AddUntilStep("in date order across both", shownIds, () => Is.EqualTo(new[] { 1, 2, 3 }));
        }

        /// <summary>
        /// A mapper's own maps can fill a whole page of the search, and their guest work sits further down it.
        /// Stopping at a page that turned up nothing would hide those behind a section that looks empty.
        /// </summary>
        [Test]
        public void TestAPageOfThisUsersOwnMapsIsNotTheEnd()
        {
            page(SearchCategory.Graveyard, sets: new[] { ownSet(1, daysAgo: 1), ownSet(2, daysAgo: 2) }, more: true);
            page(SearchCategory.Graveyard, sets: new[] { guestSet(3, daysAgo: 3) }, more: false);

            createContainer(SearchCategory.Graveyard);

            AddUntilStep("it kept looking", shownIds, () => Is.EqualTo(new[] { 3 }));
        }

        /// <summary>
        /// The order is decided here rather than taken from the server.
        /// </summary>
        /// <remarks>
        /// Asking the search to sort by last update is not the same as getting it: measured against the live site
        /// on 2026-08-28, a request whose parameters the server declines to read is answered in its own order,
        /// with no error to say so. Handing back a page out of order therefore has to change nothing.
        /// </remarks>
        [Test]
        public void TestItOrdersWhatTheServerHandsBack()
        {
            page(SearchCategory.Graveyard, sets: new[]
            {
                guestSet(3, daysAgo: 3),
                guestSet(1, daysAgo: 1),
                guestSet(2, daysAgo: 2),
            }, more: false);

            createContainer(SearchCategory.Graveyard);

            AddUntilStep("newest first regardless", shownIds, () => Is.EqualTo(new[] { 1, 2, 3 }));
        }

        /// <summary>
        /// A long listing opens folded to a few, the way the beatmap listings above it do.
        /// </summary>
        [Test]
        public void TestOnlyTheFirstFewAreShown()
        {
            page(SearchCategory.Graveyard, sets: guestSets(10), more: false);

            createContainer(SearchCategory.Graveyard);

            AddUntilStep("six are shown", () => shownIds().Length, () => Is.EqualTo(6));
            AddAssert("the six most recent", shownIds, () => Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
            AddAssert("there is a way to the rest", () => moreButton().IsPresent, () => Is.True);
        }

        /// <summary>
        /// And expanding it shows the rest, without going back to the server: the whole listing was read to
        /// count it, so it is already here.
        /// </summary>
        [Test]
        public void TestExpandingShowsTheRest()
        {
            page(SearchCategory.Graveyard, sets: guestSets(10), more: false);

            createContainer(SearchCategory.Graveyard);
            AddUntilStep("six are shown", () => shownIds().Length, () => Is.EqualTo(6));

            int asked = 0;
            AddStep("expand it", () =>
            {
                asked = requests.Count;
                moreButton().TriggerClick();
            });

            AddUntilStep("all ten are shown", shownIds, () => Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
            AddAssert("nothing further was asked for", () => requests.Count, () => Is.EqualTo(asked));
            AddUntilStep("and there is no more to show", () => moreButton().IsPresent, () => Is.False);
        }

        /// <summary>
        /// The count is the whole listing rather than the part of it on screen, which is what the counters on the
        /// listings above it mean. The search cannot report it — its own total counts the user's own maps too —
        /// so it is only right because the whole listing gets read.
        /// </summary>
        [Test]
        public void TestTheCountIsTheWholeListing()
        {
            page(SearchCategory.Graveyard, sets: guestSets(10).Concat(new[] { ownSet(99, daysAgo: 99) }).ToArray(), more: false);

            createContainer(SearchCategory.Graveyard);

            AddUntilStep("six are shown", () => shownIds().Length, () => Is.EqualTo(6));
            AddAssert("it counts all ten", counter, () => Is.EqualTo(10));
        }

        /// <summary>
        /// Most people have never made a guest difficulty. A heading standing over nothing on every profile
        /// reads as something that failed to load, so the section keeps out of the way until it has something.
        /// </summary>
        [Test]
        public void TestNothingFoundLeavesNoHeading()
        {
            createContainer(SearchCategory.Graveyard);

            AddUntilStep("the search finished", () => requests.Count, () => Is.EqualTo(1));
            AddAssert("it takes up no room", () => container.DrawHeight, () => Is.Zero);
        }

        /// <summary>
        /// And it appears once there is something to appear for.
        /// </summary>
        [Test]
        public void TestItShowsItselfOnceSomethingIsFound()
        {
            page(SearchCategory.Graveyard, sets: new[] { guestSet(1, daysAgo: 1) }, more: false);

            createContainer(SearchCategory.Graveyard);

            AddUntilStep("it is shown", () => container.DrawHeight, () => Is.GreaterThan(0));
        }

        private void createContainer(params SearchCategory[] categories) => AddStep("create the listing", () =>
        {
            Child = container = new GuestBeatmapSetsContainer(user, "guest difficulties", categories)
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Width = 0.8f,
            };

            user.Value = new UserProfileData(new APIUser { Id = user_id, Username = "test" }, new OsuRuleset().RulesetInfo);
        });

        private void page(SearchCategory category, APIBeatmapSet[] sets, bool more) => AddStep($"queue a page of {category}", () =>
        {
            if (!pages.TryGetValue(category, out var queued))
                pages[category] = queued = new Queue<SearchBeatmapSetsResponse>();

            queued.Enqueue(new SearchBeatmapSetsResponse
            {
                BeatmapSets = sets,
                Cursor = more ? new Cursor() : null,
            });
        });

        /// <summary>
        /// Read off the screen rather than out of the container, in the order somebody reads a page: the cards
        /// wrap into rows, and the container hands them out back to front so that an expanding card overhangs
        /// the ones after it. Neither is the order they are laid out in.
        /// </summary>
        private int[] shownIds() => container.ChildrenOfType<BeatmapCard>()
                                             .OrderBy(c => c.ScreenSpaceDrawQuad.TopLeft.Y)
                                             .ThenBy(c => c.ScreenSpaceDrawQuad.TopLeft.X)
                                             .Select(c => c.BeatmapSet.OnlineID)
                                             .ToArray();

        private ShowMoreButton moreButton() => container.ChildrenOfType<ShowMoreButton>().Single();

        private int counter() => container.ChildrenOfType<ProfileSubsectionHeader>().Single().Current.Value;

        /// <summary>
        /// Sets numbered from the most recent, so that the ids read in the order they are expected to show in.
        /// </summary>
        private APIBeatmapSet[] guestSets(int count) => Enumerable.Range(1, count).Select(i => guestSet(i, daysAgo: i)).ToArray();

        private APIBeatmapSet guestSet(int id, int daysAgo) => makeSet(id, somebody_else, daysAgo);

        private APIBeatmapSet ownSet(int id, int daysAgo) => makeSet(id, user_id, daysAgo);

        private APIBeatmapSet makeSet(int id, int authorId, int daysAgo)
        {
            var set = CreateAPIBeatmapSet(new OsuRuleset().RulesetInfo);

            set.OnlineID = id;
            set.AuthorID = authorId;
            set.Title = $@"set {id}";
            set.LastUpdated = DateTimeOffset.Now - TimeSpan.FromDays(daysAgo);

            return set;
        }
    }
}
