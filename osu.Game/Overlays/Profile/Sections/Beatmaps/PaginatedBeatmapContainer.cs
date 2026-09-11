// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osuTK;
using APIUser = osu.Game.Online.API.Requests.Responses.APIUser;

namespace osu.Game.Overlays.Profile.Sections.Beatmaps
{
    public partial class PaginatedBeatmapContainer : PaginatedProfileSubsection<APIBeatmapSet>
    {
        private const float panel_padding = 10f;

        /// <summary>
        /// How many sets each page read for "download all" asks for — as many as the listing itself asks for per expansion.
        /// </summary>
        private const int whole_listing_page_size = 50;

        /// <summary>
        /// A guard against paging without end, which only a server answering with full pages forever could cause.
        /// Two hundred pages is ten thousand sets, past any listing on any profile.
        /// </summary>
        private const int whole_listing_max_pages = 200;

        private readonly BeatmapSetType type;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        protected override int InitialItemsCount => type == BeatmapSetType.Graveyard ? 2 : 6;

        public PaginatedBeatmapContainer(BeatmapSetType type, Bindable<UserProfileData?> user, LocalisableString headerText)
            : base(user, headerText)
        {
            this.type = type;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            ItemsContainer.Spacing = new Vector2(panel_padding);
        }

        protected override int GetCount(APIUser user)
        {
            switch (type)
            {
                case BeatmapSetType.Favourite:
                    return user.FavouriteBeatmapsetCount;

                case BeatmapSetType.Graveyard:
                    return user.GraveyardBeatmapsetCount;

                case BeatmapSetType.Loved:
                    return user.LovedBeatmapsetCount;

                case BeatmapSetType.Ranked:
                    return user.RankedBeatmapsetCount;

                case BeatmapSetType.Pending:
                    return user.PendingBeatmapsetCount;

                case BeatmapSetType.Guest:
                    return user.GuestBeatmapsetCount;

                case BeatmapSetType.Nominated:
                    return user.NominatedBeatmapsetCount;

                default:
                    return 0;
            }
        }

        protected override APIRequest<List<APIBeatmapSet>> CreateRequest(UserProfileData user, PaginationParameters pagination) =>
            new GetUserBeatmapsRequest(user.User.Id, type, pagination);

        // favourites are other people's maps collected for keeping, not a body of work to have in full.
        protected override Drawable? CreateHeaderTrailing() => type == BeatmapSetType.Favourite ? null : new DownloadAllControl(readWholeListing);

        /// <summary>
        /// Reads the whole listing, page after page, the way expanding it would.
        /// </summary>
        /// <remarks>
        /// A page that cannot be read fails the whole read rather than ending it: downloading "all" of what happened to
        /// arrive would quietly leave the rest behind.
        /// </remarks>
        private async Task<IReadOnlyList<APIBeatmapSet>> readWholeListing(CancellationToken cancellationToken)
        {
            var sets = new List<APIBeatmapSet>();

            // read now, before the first wait: the profile may have moved on to somebody else by the time pages arrive.
            int? userId = User.Value?.User.Id;

            if (userId == null)
                return sets;

            for (int page = 0; page < whole_listing_max_pages; page++)
            {
                var request = new GetUserBeatmapsRequest(userId.Value, type, new PaginationParameters(page * whole_listing_page_size, whole_listing_page_size));
                var received = new TaskCompletionSource<List<APIBeatmapSet>>(TaskCreationOptions.RunContinuationsAsynchronously);

                request.Success += response => received.TrySetResult(response);
                request.Failure += error => received.TrySetException(error);

                using (cancellationToken.Register(() => request.Cancel()))
                {
                    api.Queue(request);
                    sets.AddRange(await received.Task.ConfigureAwait(false));
                }

                if (request.Response!.Count < whole_listing_page_size)
                    break;
            }

            return sets;
        }

        protected override Drawable? CreateDrawableItem(APIBeatmapSet model) => model.OnlineID > 0
            ? new BeatmapCardNormal(model)
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre
            }
            : null;
    }
}
