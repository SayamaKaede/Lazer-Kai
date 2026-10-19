// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.IO.Stores;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Chat;
using osu.Game.Overlays;
using osu.Game.Screens.Edit.Collab;
using osuTK;

namespace osu.Game.Screens.Edit.Discussion
{
    /// <summary>
    /// The beatmap's discussion from the website, read beside the beatmap it is about, with the times in it going
    /// there when clicked (2026-10-18 request).
    /// </summary>
    /// <remarks>
    /// Read only. It is read once, the first time the panel is opened, and again only when asked to; the replies of
    /// each thread are asked for when that thread is opened, since the website hands them out one thread at a time.
    /// </remarks>
    public partial class DiscussionOverlay : VisibilityContainer, IKeyBindingHandler<GlobalAction>
    {
        private const float panel_width = 480;
        private const float list_height = 560;

        /// <summary>
        /// How many pages of threads are read before stopping. At 50 a page, more than any beatmapset is given.
        /// </summary>
        private const int max_pages = 20;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        private Container panel = null!;
        private FillFlowContainer list = null!;
        private OsuSpriteText status = null!;

        private readonly List<APIBeatmapsetDiscussion> discussions = new List<APIBeatmapsetDiscussion>();
        private readonly Dictionary<int, string> names = new Dictionary<int, string>();

        private GetBeatmapsetDiscussionsRequest? request;

        /// <summary>
        /// Whether the discussion has been asked for and has not failed, so that opening the panel again does not ask
        /// again. Not uploaded and not logged in leave it unset: either can have changed by the next time it is opened.
        /// </summary>
        private bool asked;

        /// <summary>
        /// The images of the discussion shown whole, over the whole editor rather than inside the panel.
        /// </summary>
        private readonly DiscussionImageViewer imageViewer = new DiscussionImageViewer { State = { Value = Visibility.Hidden } };

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs<ILinkHandler>(new TimestampLinks(parent.Get<ILinkHandler>(), timestamp => editor?.HandleTimestamp(timestamp, notifyOnError: true)));
            dependencies.Cache(imageViewer);

            // one handed down from above is a test's, which hands out images without the network.
            if (parent.Get<DiscussionImageStore>() == null)
            {
                var host = parent.Get<GameHost>();

                dependencies.Cache(ownImages = new DiscussionImageStore(
                    parent.Get<LargeTextureStore>(),
                    new LargeTextureStore(host.Renderer, host.CreateTextureLoaderStore(new OnlineStore())),
                    parent.Get<IAPIProvider>().Endpoints.WebsiteUrl));
            }

            return dependencies;
        }

        /// <summary>
        /// The image store made here, as opposed to one a test handed down, which is this panel's to dispose of.
        /// </summary>
        private DiscussionImageStore? ownImages;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            // the same blocker as the collab panel's: a click meant for the panel does not reach the beatmap behind.
            panel = new CollabInputBlocker
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                X = -20,
                Width = panel_width,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 10,
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Colour = Color4Extensions.FromHex("000").Opacity(0.4f),
                    Radius = 10,
                },
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            createHeader(),
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = list_height,
                                Children = new Drawable[]
                                {
                                    status = new OsuSpriteText
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Colour = colourProvider.Foreground1,
                                    },
                                    new OsuScrollContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Child = list = new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Padding = new MarginPadding(10),
                                            Spacing = new Vector2(0, 6),
                                        }
                                    },
                                }
                            },
                        }
                    },
                }
            };

            InternalChildren = new Drawable[] { panel, imageViewer };
        }

        private Drawable createHeader() => new HeaderBar(() => panel)
        {
            RelativeSizeAxes = Axes.X,
            Height = 44,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background3,
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 14 },
                    Text = DiscussionStrings.Header,
                    Font = OsuFont.Default.With(size: 16, weight: FontWeight.SemiBold),
                },
                new IconButton
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding { Right = 40 },
                    Icon = FontAwesome.Solid.Redo,
                    Scale = new Vector2(0.6f),
                    TooltipText = DiscussionStrings.Reload,
                    Action = reload,
                },
                new IconButton
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding { Right = 6 },
                    Icon = FontAwesome.Solid.Times,
                    Scale = new Vector2(0.7f),
                    Action = Hide,
                },
            }
        };

        /// <summary>
        /// Throws away what has been read and asks for all of it again.
        /// </summary>
        private void reload()
        {
            request?.Cancel();
            request = null;
            asked = false;

            discussions.Clear();
            names.Clear();
            list.Clear();

            int beatmapSetId = editorBeatmap.BeatmapInfo.BeatmapSet?.OnlineID ?? -1;

            // the same test the file menu's link to the discussion page is offered by.
            if (beatmapSetId <= 0 || editorBeatmap.BeatmapInfo.OnlineID <= 0)
            {
                showStatus(DiscussionStrings.NotUploaded);
                return;
            }

            if (!api.IsLoggedIn)
            {
                showStatus(DiscussionStrings.NotLoggedIn);
                return;
            }

            asked = true;
            showStatus(DiscussionStrings.Loading);
            askFor(beatmapSetId, 1);
        }

        private void askFor(int beatmapSetId, int page)
        {
            var req = request = new GetBeatmapsetDiscussionsRequest(beatmapSetId, page);

            req.Success += response => Schedule(() =>
            {
                // a reload has asked again since.
                if (req != request)
                    return;

                discussions.AddRange(response.Discussions);

                foreach (var user in response.Users)
                    names[user.Id] = user.Username;

                if (response.CursorString != null && page < max_pages)
                    askFor(beatmapSetId, page + 1);
                else
                    showDiscussions();
            });

            req.Failure += _ => Schedule(() =>
            {
                if (req != request)
                    return;

                // asked again the next time the panel is opened, rather than left saying it failed.
                asked = false;
                showStatus(DiscussionStrings.LoadFailed);
            });

            api.Queue(req);
        }

        private void showDiscussions()
        {
            list.Clear();

            int beatmapId = editorBeatmap.BeatmapInfo.OnlineID;

            // the timeline first: it is what is gone through while the beatmap is being changed.
            addSection(DiscussionStrings.Timeline, Discussions.Timeline(discussions, beatmapId));
            addSection(DiscussionStrings.ThisDifficulty, Discussions.ThisDifficulty(discussions, beatmapId));
            addSection(DiscussionStrings.WholeSet, Discussions.WholeSet(discussions));

            showStatus(list.Count == 0 ? DiscussionStrings.NothingPosted : default);
        }

        private void addSection(LocalisableString title, IEnumerable<APIBeatmapsetDiscussion> section)
        {
            var threads = section.ToArray();

            if (threads.Length == 0)
                return;

            list.Add(new OsuSpriteText
            {
                Text = title,
                Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold),
                Colour = colourProvider.Content2,
                Margin = new MarginPadding { Top = list.Count == 0 ? 0 : 8 },
            });

            foreach (var discussion in threads)
                list.Add(new DiscussionRow(discussion, names.GetValueOrDefault(discussion.UserId, @"?")));
        }

        private void showStatus(LocalisableString text)
        {
            status.Text = text;
            status.Alpha = string.IsNullOrEmpty(text.ToString()) ? 0 : 1;
        }

        protected override void PopIn()
        {
            if (!asked)
                reload();

            this.FadeIn(200, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            imageViewer.Hide();
            this.FadeOut(200, Easing.OutQuint);
        }

        /// <summary>
        /// Escape puts away an image being looked at, then closes the panel, rather than leaving the editor.
        /// </summary>
        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Action != GlobalAction.Back || State.Value != Visibility.Visible)
                return false;

            if (imageViewer.State.Value == Visibility.Visible)
                imageViewer.Hide();
            else
                Hide();

            return true;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            request?.Cancel();
            base.Dispose(isDisposing);
            ownImages?.Dispose();
        }

        /// <summary>
        /// Takes the times in the discussion to this editor, and hands every other link on to the game.
        /// </summary>
        /// <remarks>
        /// The game sends a time to whatever editor is on top, which is this one; it is done here so that it does not
        /// depend on that, and so that the beatmap it goes to is the one the panel was opened from.
        /// </remarks>
        private class TimestampLinks : ILinkHandler
        {
            private readonly ILinkHandler? game;
            private readonly Action<string> goTo;

            public TimestampLinks(ILinkHandler? game, Action<string> goTo)
            {
                this.game = game;
                this.goTo = goTo;
            }

            public void HandleLink(string url) => HandleLink(MessageFormatter.GetLinkDetails(url));

            public void HandleLink(LinkDetails link)
            {
                if (link.Action == LinkAction.OpenEditorTimestamp)
                    goTo(link.Argument.ToString() ?? string.Empty);
                else
                    game?.HandleLink(link);
            }
        }

        /// <summary>
        /// The bar along the top: dragging it moves the panel.
        /// </summary>
        private partial class HeaderBar : Container
        {
            private readonly Func<Container> panel;

            public HeaderBar(Func<Container> panel)
            {
                this.panel = panel;
            }

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void OnDrag(DragEvent e) => panel().Position += e.Delta;
        }
    }
}
