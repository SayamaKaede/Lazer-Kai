// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.BeatmapListing;
using osu.Game.Rulesets;
using osuTK;

namespace osu.Game.Overlays.Profile.Sections.Beatmaps
{
    /// <summary>
    /// Beatmaps hosted by somebody else which this user made a difficulty for, in the states the profile's own
    /// guest listing leaves out.
    /// </summary>
    /// <remarks>
    /// <c>users/{id}/beatmapsets/guest</c> reports only sets whose difficulties are scoreable — ranked, approved,
    /// qualified or loved — and takes no parameter to widen that, so a guest difficulty on a pending or
    /// graveyarded map appears nowhere on a profile. The beatmap search does reach them: its <c>creator=</c>
    /// operator resolves the name to a user and matches each difficulty's owners rather than the set's host, so
    /// asking for one status at a time returns every set the user has a difficulty in. The user's own sets come
    /// back the same way and are dropped here, since they have listings of their own already.
    ///
    /// Because the user's own sets cannot be left out of the search, its reported total is not this listing's
    /// total, and there is no cheaper way to a number than reading the whole listing. So the whole listing is
    /// read — the counter is then the real one, and expanding costs nothing — while only a first few are shown,
    /// the way the listings above this one behave.
    ///
    /// One thing follows from the search being the source: its results come from the search index rather than the
    /// database the rest of the profile reads, so a freshly submitted map can take a moment to appear here.
    /// </remarks>
    public partial class GuestBeatmapSetsContainer : ProfileSubsection
    {
        private const float panel_padding = 10f;

        /// <summary>
        /// How many are shown before the listing is expanded, matching the beatmap listings above it.
        /// </summary>
        private const int initial_items = 6;

        /// <summary>
        /// How many more each expansion shows.
        /// </summary>
        private const int items_per_expansion = 50;

        /// <summary>
        /// A guard against paging without end, which only a server answering with a cursor forever could cause.
        /// Forty pages is two thousand sets in one state, which is past anybody this will be pointed at.
        /// </summary>
        private const int max_pages = 40;

        private readonly IReadOnlyList<SearchCategory> categories;
        private readonly List<SearchStream> streams = new List<SearchStream>();

        /// <summary>
        /// Sets whose place in the listing is settled, waiting on room to be shown.
        /// </summary>
        private readonly List<APIBeatmapSet> waiting = new List<APIBeatmapSet>();

        /// <summary>
        /// How many are on screen or on their way there. Counted separately from the flow's own children, which
        /// do not arrive until they have finished loading — and more can be asked for in the meantime.
        /// </summary>
        private int claimed;

        private int displayLimit;

        private ReverseChildIDFillFlowContainer<Drawable> itemsContainer = null!;
        private ShowMoreButton moreButton = null!;

        private CancellationTokenSource? loadCancellation;

        /// <summary>
        /// The whole listing, once the searches have been read to their ends. Replaced whenever the user changes.
        /// </summary>
        /// <remarks>
        /// The listing is read in full anyway, to count it, so asking for all of it costs nothing more than waiting for
        /// that to finish.
        /// </remarks>
        private TaskCompletionSource<IReadOnlyList<APIBeatmapSet>> wholeListing = new TaskCompletionSource<IReadOnlyList<APIBeatmapSet>>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Whether a search failed partway, leaving the listing short. What arrived is still shown, but downloading
        /// "all" of a listing known to be short would quietly leave the rest behind.
        /// </summary>
        private bool readFailed;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        /// <param name="user">The profile being shown.</param>
        /// <param name="headerText">The heading to sit above the listing.</param>
        /// <param name="categories">
        /// The states to gather. More than one is needed for pending, which the search splits into pending and
        /// WIP while the profile's own listing counts them together.
        /// </param>
        public GuestBeatmapSetsContainer(Bindable<UserProfileData?> user, LocalisableString headerText, params SearchCategory[] categories)
            : base(user, headerText, CounterVisibilityState.AlwaysVisible)
        {
            this.categories = categories;
        }

        protected override Drawable CreateContent() => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Children = new Drawable[]
            {
                // reverse ID flow is required for correct Z-ordering of the beatmap cards, which overhang when expanded.
                itemsContainer = new ReverseChildIDFillFlowContainer<Drawable>
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Spacing = new Vector2(panel_padding),
                    // ensure the container and its contents are in front of the "more" button.
                    Depth = float.MinValue
                },
                moreButton = new ShowMoreButton
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Alpha = 0,
                    Margin = new MarginPadding { Top = 10 },
                    Action = showMore,
                }
            }
        };

        protected override Drawable CreateHeaderTrailing() => new DownloadAllControl(readWholeListing);

        private Task<IReadOnlyList<APIBeatmapSet>> readWholeListing(CancellationToken cancellationToken) => wholeListing.Task.WaitAsync(cancellationToken);

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // most people have never made a guest difficulty, and a heading standing over nothing on every
            // profile reads as something failing to load. It appears once there is something under it.
            //
            // the heading and the listing are what get hidden rather than this container, which has to stay
            // present to keep running its scheduler — the search's replies arrive through it, and hiding this
            // would leave the listing waiting on callbacks that never run. With both of its children gone it
            // takes no height, which is all that hiding it was for.
            setContentVisible(false);

            User.BindValueChanged(onUserChanged, true);
        }

        private void setContentVisible(bool visible)
        {
            foreach (var child in Children)
                child.FadeTo(visible ? 1 : 0, visible ? 200 : 0, Easing.OutQuint);
        }

        private void onUserChanged(ValueChangedEvent<UserProfileData?> e)
        {
            cancel();

            itemsContainer.Clear();
            streams.Clear();
            waiting.Clear();

            claimed = 0;
            displayLimit = initial_items;

            moreButton.Alpha = 0;
            SetCount(0);
            setContentVisible(false);

            // whoever was waiting on the previous user's listing is not waiting on this one.
            wholeListing.TrySetCanceled();
            wholeListing = new TaskCompletionSource<IReadOnlyList<APIBeatmapSet>>(TaskCreationOptions.RunContinuationsAsynchronously);
            readFailed = false;

            if (e.NewValue?.User == null)
            {
                wholeListing.SetResult(Array.Empty<APIBeatmapSet>());
                return;
            }

            foreach (var category in categories)
                streams.Add(new SearchStream(category));

            loadCancellation = new CancellationTokenSource();
            moreButton.IsLoading = true;

            read(0);
        }

        /// <summary>
        /// Reads the searches through to their ends, showing what has arrived after each round.
        /// </summary>
        private void read(int pagesRead)
        {
            var unfinished = streams.Where(s => !s.Exhausted).ToList();

            if (unfinished.Count == 0 || pagesRead >= max_pages)
            {
                collect();
                return;
            }

            int outstanding = unfinished.Count;

            foreach (var stream in unfinished)
            {
                var request = createRequest(stream);

                request.Success += response => Schedule(() =>
                {
                    if (cancelled)
                        return;

                    receive(stream, response);
                    done();
                });

                // a stream that cannot be read is finished rather than retried: the rest of the listing is still
                // worth showing, and a failure that repeats would otherwise repeat once per page.
                request.Failure += _ => Schedule(() =>
                {
                    if (cancelled)
                        return;

                    stream.Exhausted = true;
                    readFailed = true;
                    done();
                });

                stream.Request = request;
                api.Queue(request);

                void done()
                {
                    if (--outstanding > 0)
                        return;

                    read(pagesRead + 1);
                }
            }
        }

        private SearchBeatmapSetsRequest createRequest(SearchStream stream) => new SearchBeatmapSetsRequest(
            // an id rather than a username: the server resolves either, and an id survives a name change and
            // carries no spaces to be parsed around.
            $@"creator={User.Value!.User.Id}",
            // no ruleset, so a difficulty in any of them counts.
            new RulesetInfo(),
            stream.Cursor,
            searchCategory: stream.Category,
            sortCriteria: SortCriteria.Updated,
            sortDirection: SortDirection.Descending,
            // the profile shows a user's maps whatever they are marked as; hiding some of them here would read
            // as the map having gone missing.
            explicitContent: SearchExplicit.Show);

        private void receive(SearchStream stream, SearchBeatmapSetsResponse response)
        {
            int userId = User.Value!.User.Id;

            foreach (var set in response.BeatmapSets ?? Enumerable.Empty<APIBeatmapSet>())
            {
                // the search has no way to leave out sets this user hosts, and those are in the profile's own
                // pending and graveyard listings already.
                if (set.AuthorID == userId)
                    continue;

                if (set.OnlineID <= 0)
                    continue;

                stream.Buffer.Add(set);
            }

            stream.Cursor = response.Cursor;
            stream.Exhausted = response.Cursor == null;
        }

        /// <summary>
        /// Puts what the searches read into one listing, ordered the way the listings above this one are.
        /// </summary>
        /// <remarks>
        /// The order is worked out here rather than taken from the server. Asking the search for
        /// <c>sort=updated_desc</c> is not the same as getting it — a request the server declines to read the
        /// parameters of is answered with its own default ordering and no error, which was measured against the
        /// live site on 2026-08-28. Sorting what came back cannot be wrong that way, and it costs nothing when
        /// the whole listing is in hand already.
        ///
        /// It also settles the two searches behind the pending listing, which arrive as separate listings and
        /// have to read as one.
        /// </remarks>
        private void collect()
        {
            var listing = streams.SelectMany(s => s.Buffer)
                                 // a cursor walking an index that is being written to can hand the same set
                                 // back twice.
                                 .GroupBy(s => s.OnlineID)
                                 .Select(g => g.First())
                                 .OrderByDescending(s => s.LastUpdated ?? DateTimeOffset.MinValue)
                                 .ToArray();

            waiting.AddRange(listing);

            foreach (var stream in streams)
                stream.Buffer.Clear();

            if (readFailed)
                wholeListing.TrySetException(new InvalidOperationException(@"A search failed partway, so the listing is incomplete."));
            else
                wholeListing.TrySetResult(listing);

            display();
        }

        private void showMore()
        {
            displayLimit += items_per_expansion;
            display();
        }

        private void display()
        {
            int room = Math.Max(displayLimit - claimed, 0);
            var taking = waiting.Take(room).ToArray();

            waiting.RemoveRange(0, taking.Length);
            claimed += taking.Length;

            updateCounter();

            if (taking.Length == 0)
            {
                settleMoreButton();
                return;
            }

            LoadComponentsAsync(taking.Select(createCard), drawables =>
            {
                itemsContainer.AddRange(drawables);

                setContentVisible(true);
                settleMoreButton();
            }, loadCancellation!.Token);
        }

        /// <summary>
        /// Reports what has been read so far, which is the whole listing once the searches have finished.
        /// </summary>
        private void updateCounter() => SetCount(claimed + waiting.Count + streams.Sum(s => s.Buffer.Count));

        private void settleMoreButton()
        {
            bool more = waiting.Count > 0 || streams.Any(s => !s.Exhausted || s.Buffer.Count > 0);

            moreButton.IsLoading = false;
            moreButton.FadeTo(more ? 1 : 0);
        }

        private Drawable createCard(APIBeatmapSet set) => new BeatmapCardNormal(set)
        {
            Anchor = Anchor.TopCentre,
            Origin = Anchor.TopCentre
        };

        private bool cancelled => loadCancellation?.IsCancellationRequested != false;

        private void cancel()
        {
            foreach (var stream in streams)
                stream.Request?.Cancel();

            loadCancellation?.Cancel();
            loadCancellation = null;
        }

        protected override void Dispose(bool isDisposing)
        {
            cancel();
            wholeListing.TrySetCanceled();
            base.Dispose(isDisposing);
        }

        /// <summary>
        /// One state being read through.
        /// </summary>
        private class SearchStream
        {
            public readonly SearchCategory Category;

            /// <summary>
            /// Sets read from the search and not yet placed in the listing, newest first.
            /// </summary>
            public readonly List<APIBeatmapSet> Buffer = new List<APIBeatmapSet>();

            /// <summary>
            /// Where the next page starts, or <c>null</c> before the first page and after the last.
            /// </summary>
            public Cursor? Cursor;

            /// <summary>
            /// Whether the search has nothing further to give. Distinct from an empty <see cref="Buffer"/>, which
            /// a page can leave behind when all of it belonged to the user.
            /// </summary>
            public bool Exhausted;

            public SearchBeatmapSetsRequest? Request;

            public SearchStream(SearchCategory category)
            {
                Category = category;
            }
        }
    }
}
