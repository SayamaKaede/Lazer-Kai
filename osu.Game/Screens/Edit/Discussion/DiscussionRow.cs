// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Discussion
{
    /// <summary>
    /// One thread of the discussion: what kind of post started it, where it is, who wrote it and what they wrote, with
    /// its replies underneath once they have been asked for.
    /// </summary>
    public partial class DiscussionRow : CompositeDrawable
    {
        /// <summary>
        /// How many pages of replies are read before stopping, so that a thread argued at length cannot keep asking.
        /// </summary>
        private const int max_pages = 20;

        public readonly APIBeatmapsetDiscussion Discussion;

        private readonly string author;

        /// <summary>
        /// The time the thread is about, which goes there when clicked. Empty where it is about no time in particular.
        /// </summary>
        public LinkFlowContainer TimeLink { get; private set; } = null!;

        /// <summary>
        /// The post that started the thread, with the times written in it going there when clicked.
        /// </summary>
        public LinkFlowContainer Message { get; private set; } = null!;

        public Drawable RepliesButton => repliesButton;

        public DiscussionCopyButton CopyButton { get; private set; } = null!;

        private Box flash = null!;

        /// <summary>
        /// Lights the row for a moment, so that the one clicked on the timeline is found among the rest.
        /// </summary>
        public void Flash() => flash.FadeTo(0.25f).FadeOut(800, Easing.OutQuint);

        private FillFlowContainer images = null!;
        private OsuHoverContainer repliesButton = null!;
        private OsuSpriteText repliesText = null!;
        private FillFlowContainer replies = null!;

        private RepliesState state;
        private GetBeatmapsetDiscussionPostsRequest? request;

        private readonly List<APIBeatmapsetDiscussionPost> posts = new List<APIBeatmapsetDiscussionPost>();
        private readonly Dictionary<int, string> names = new Dictionary<int, string>();

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public DiscussionRow(APIBeatmapsetDiscussion discussion, string author)
        {
            Discussion = discussion;
            this.author = author;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, OsuColour colours)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Masking = true;
            CornerRadius = 5;

            (IconUsage icon, Color4 colour) = MarkOf(Discussion, colours, colourProvider);

            // a resolved thread darkened, so the ones still open stand out among all of them (2026-10-21).
            float alpha = Discussion.Resolved ? 0.45f : 1;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background3,
                },
                new Box
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = 4,
                    Colour = colour,
                    Alpha = alpha,
                },
                new FillFlowContainer
                {
                    Alpha = alpha,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding { Left = 14, Right = 10, Vertical = 8 },
                    Spacing = new Vector2(0, 4),
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(6, 0),
                            Children = new Drawable[]
                            {
                                new SpriteIcon
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Icon = icon,
                                    Size = new Vector2(12),
                                    Colour = colour,
                                },
                                new Container
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    AutoSizeAxes = Axes.Both,
                                    Masking = true,
                                    CornerRadius = 4,
                                    Children = new Drawable[]
                                    {
                                        // framed like a button, as clicking it goes there (2026-10-21).
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = colourProvider.Background5,
                                            Alpha = Discussion.Timestamp != null ? 1 : 0,
                                        },
                                        TimeLink = new LinkFlowContainer(s => s.Font = OsuFont.Default.With(size: 13, weight: FontWeight.SemiBold))
                                        {
                                            AutoSizeAxes = Axes.Both,
                                            Padding = Discussion.Timestamp != null ? new MarginPadding { Horizontal = 6, Vertical = 1 } : new MarginPadding(),
                                        },
                                    }
                                },
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Text = author,
                                    Font = OsuFont.Default.With(size: 13, weight: FontWeight.SemiBold),
                                    Colour = colourProvider.Content2,
                                },
                            }
                        },
                        Message = new LinkFlowContainer(s => s.Font = OsuFont.Default.With(size: 14))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                        },
                        images = CreateImages(),
                        repliesButton = new OsuHoverContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            IdleColour = colourProvider.Content2,
                            HoverColour = colourProvider.Light1,
                            Action = toggleReplies,
                            Child = repliesText = new OsuSpriteText
                            {
                                Text = DiscussionStrings.ShowReplies,
                                Font = OsuFont.Default.With(size: 12),
                            },
                        },
                        replies = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 6),
                            Padding = new MarginPadding { Left = 10 },
                            Alpha = 0,
                        },
                    }
                },
                CopyButton = new DiscussionCopyButton(Discussions.StartingText(Discussion))
                {
                    Margin = new MarginPadding { Top = 1, Right = 2 },
                },
                flash = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Light1,
                    Alpha = 0,
                },
            };

            if (Discussion.Timestamp is int time)
            {
                string text = ((double)time).ToEditorFormattedString();
                TimeLink.AddLink(text, LinkAction.OpenEditorTimestamp, text);
            }

            Write(Discussions.StartingText(Discussion), Message, images);
        }

        /// <summary>
        /// Where the images of a post go, under its text.
        /// </summary>
        internal static FillFlowContainer CreateImages() => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Full,
            Spacing = new Vector2(6),
        };

        /// <summary>
        /// Puts a post's text in, with its links and times to be clicked, and its images under it.
        /// </summary>
        internal static void Write(string post, LinkFlowContainer text, FillFlowContainer images)
        {
            var (remaining, found) = DiscussionImageLinks.Extract(post);

            var formatted = MessageFormatter.FormatText(remaining);
            text.AddLinks(formatted.Text, formatted.Links);

            foreach (string url in found)
                images.Add(new DiscussionImage(url));

            // nothing to show, and no gap to leave for it.
            images.Alpha = found.Count > 0 ? 1 : 0;
        }

        /// <summary>
        /// The icon and colour the website marks each kind of post with, so that a problem reads as one at a glance.
        /// </summary>
        /// <summary>
        /// What a thread is marked with: a resolved one with the website's green ring and tick in place of its kind,
        /// the way the website marks it, and the rest by their kind.
        /// </summary>
        internal static (IconUsage, Color4) MarkOf(APIBeatmapsetDiscussion discussion, OsuColour colours, OverlayColourProvider colourProvider)
            => discussion.Resolved
                ? (FontAwesome.Regular.CheckCircle, colours.Green)
                : KindOf(discussion.MessageType, colours, colourProvider);

        internal static (IconUsage, Color4) KindOf(string type, OsuColour colours, OverlayColourProvider colourProvider)
        {
            switch (type)
            {
                case @"problem":
                    return (FontAwesome.Solid.ExclamationCircle, colours.Red);

                case @"suggestion":
                    return (FontAwesome.Regular.Circle, colours.Yellow);

                case @"praise":
                    return (FontAwesome.Solid.Heart, colours.Blue);

                case @"hype":
                    return (FontAwesome.Solid.Bullhorn, colours.Blue);

                case @"mapper_note":
                    return (FontAwesome.Solid.StickyNote, colours.Purple);

                default:
                    return (FontAwesome.Regular.Comment, colourProvider.Foreground1);
            }
        }

        private void toggleReplies()
        {
            switch (state)
            {
                case RepliesState.NotAsked:
                    askFor(1);
                    break;

                case RepliesState.Shown:
                    replies.Hide();
                    repliesText.Text = DiscussionStrings.ShowReplies;
                    state = RepliesState.Hidden;
                    break;

                case RepliesState.Hidden:
                    replies.Show();
                    repliesText.Text = DiscussionStrings.HideReplies;
                    state = RepliesState.Shown;
                    break;
            }
        }

        private void askFor(int page)
        {
            state = RepliesState.Loading;
            repliesText.Text = DiscussionStrings.Loading;

            var req = request = new GetBeatmapsetDiscussionPostsRequest(Discussion.Id, page);

            req.Success += response => Schedule(() =>
            {
                foreach (var user in response.Users)
                    names[user.Id] = user.Username;

                posts.AddRange(response.Posts);

                if (response.CursorString != null && page < max_pages)
                    askFor(page + 1);
                else
                    showReplies();
            });

            // left to be asked again with another click, rather than saying there are none.
            req.Failure += _ => Schedule(() =>
            {
                posts.Clear();
                repliesText.Text = DiscussionStrings.LoadFailed;
                state = RepliesState.NotAsked;
            });

            api.Queue(req);
        }

        private void showReplies()
        {
            if (posts.Count == 0)
            {
                repliesText.Text = DiscussionStrings.NoReplies;
                repliesButton.Enabled.Value = false;
                state = RepliesState.None;
                return;
            }

            foreach (var post in posts)
                replies.Add(new DiscussionReply(post, names.GetValueOrDefault(post.UserId, @"?")));

            replies.Show();
            repliesText.Text = DiscussionStrings.HideReplies;
            state = RepliesState.Shown;
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            base.Dispose(isDisposing);
        }

        private enum RepliesState
        {
            NotAsked,
            Loading,
            Shown,
            Hidden,
            None,
        }
    }

    /// <summary>
    /// A reply in a thread, under the post it answers.
    /// </summary>
    public partial class DiscussionReply : CompositeDrawable
    {
        public readonly APIBeatmapsetDiscussionPost Post;

        public DiscussionCopyButton CopyButton { get; private set; } = null!;

        private readonly string author;

        public DiscussionReply(APIBeatmapsetDiscussionPost post, string author)
        {
            Post = post;
            this.author = author;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            LinkFlowContainer message;
            FillFlowContainer images;

            InternalChildren = new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 2),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = author,
                            Font = OsuFont.Default.With(size: 12, weight: FontWeight.SemiBold),
                            Colour = colourProvider.Content2,
                        },
                        message = new LinkFlowContainer(s => s.Font = OsuFont.Default.With(size: 13))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                        },
                        images = DiscussionRow.CreateImages(),
                    }
                },
                CopyButton = new DiscussionCopyButton(Post.Message)
                {
                    Margin = new MarginPadding { Top = -6 },
                },
            };

            DiscussionRow.Write(Post.Message, message, images);
        }
    }
}
