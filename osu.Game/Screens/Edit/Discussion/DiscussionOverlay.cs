// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.ObjectExtensions;
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
using osu.Game.Configuration;
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
        private const float header_height = 44;
        private const float bars_height = 86;
        private const float list_height = 510;

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

        [Resolved]
        private DiscussionMarks marks { get; set; } = null!;

        private Container panel = null!;
        private OsuScrollContainer scroll = null!;
        private FillFlowContainer list = null!;
        private OsuSpriteText status = null!;
        private DiscussionTabs tabs = null!;
        private UserDropdown users = null!;

        private readonly List<APIBeatmapsetDiscussion> discussions = new List<APIBeatmapsetDiscussion>();
        private readonly Dictionary<int, string> names = new Dictionary<int, string>();

        /// <summary>
        /// Every row made so far, kept while the tabs are switched so that replies already opened stay open.
        /// </summary>
        private readonly Dictionary<long, DiscussionRow> rows = new Dictionary<long, DiscussionRow>();

        private readonly Bindable<DiscussionFilter> filter = new Bindable<DiscussionFilter>(DiscussionFilter.All);

        /// <summary>
        /// Whose threads are listed, or <see cref="UserDropdown.EVERYONE"/>.
        /// </summary>
        private readonly Bindable<int> user = new Bindable<int>(UserDropdown.EVERYONE);

        private readonly Bindable<DiscussionSort> sort = new Bindable<DiscussionSort>();
        private readonly BindableFloat opacity = new BindableFloat(1);

        /// <summary>
        /// Whether the whole discussion has been read, so that there is something for the tabs to divide.
        /// </summary>
        private bool loaded;

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
        private void load(OsuConfigManager config)
        {
            RelativeSizeAxes = Axes.Both;

            config.BindWith(OsuSetting.EditorDiscussionOpacity, opacity);

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
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = header_height + bars_height + list_height,
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Y = header_height + bars_height,
                                Height = list_height,
                                Children = new Drawable[]
                                {
                                    status = new OsuSpriteText
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Colour = colourProvider.Foreground1,
                                    },
                                    scroll = new OsuScrollContainer
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
                            // after the list, so that the user list opening downwards is drawn over it.
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Children = new[]
                                {
                                    createHeader(),
                                    createBars(),
                                }
                            },
                        }
                    },
                }
            };

            InternalChildren = new Drawable[] { panel, imageViewer };
        }

        private Drawable createBars() => new Container
        {
            RelativeSizeAxes = Axes.X,
            Height = bars_height,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                },
                tabs = new DiscussionTabs
                {
                    Margin = new MarginPadding { Left = 14, Top = 6 },
                    Current = { BindTarget = filter },
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 40,
                    Y = 36,
                    Padding = new MarginPadding { Horizontal = 14 },
                    Children = new Drawable[]
                    {
                        users = new UserDropdown(names)
                        {
                            Width = 180,
                            Current = { BindTarget = user },
                        },
                        new DiscussionSortSwitch
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            Margin = new MarginPadding { Top = 12 },
                            Current = { BindTarget = sort },
                        },
                    }
                },
                new Box
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    RelativeSizeAxes = Axes.X,
                    Height = 1,
                    Colour = colourProvider.Background3,
                },
            }
        };

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
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding { Right = 80 },
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(8, 0),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = DiscussionStrings.Opacity,
                            Font = OsuFont.Default.With(size: 12),
                            Colour = colourProvider.Content2,
                        },
                        new RoundedSliderBar<float>
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Current = opacity,
                            Width = 110,
                        },
                    }
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

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // the panel rather than the whole of this, so that showing and hiding it still fades from nothing to
            // whatever the mapper set, and an image looked at whole is not faded with it.
            opacity.BindValueChanged(v => panel.Alpha = v.NewValue, true);

            filter.BindValueChanged(_ => showDiscussions());
            user.BindValueChanged(_ => showDiscussions());
            sort.BindValueChanged(_ => showDiscussions());

            marks.Picked += bringIntoView;
        }

        /// <summary>
        /// Opens the panel at a thread clicked on the timeline.
        /// </summary>
        private void bringIntoView(APIBeatmapsetDiscussion discussion)
        {
            Show();

            if (!rows.TryGetValue(discussion.Id, out var row) || row.Parent == null)
                return;

            // once the list has laid the row out where it is going to be.
            ScheduleAfterChildren(() =>
            {
                scroll.ScrollIntoView(row);
                row.Flash();
            });
        }

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
            clearRows();
            loaded = false;
            marks.Timeline.Value = Array.Empty<APIBeatmapsetDiscussion>();
            tabs.SetCounts(new Dictionary<DiscussionFilter, int>());
            users.SetUsers(Array.Empty<int>());

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
                {
                    loaded = true;
                    users.SetUsers(Discussions.ForDifficulty(discussions, editorBeatmap.BeatmapInfo.OnlineID).Select(d => d.UserId));
                    showDiscussions();
                }
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
            if (!loaded)
                return;

            // the rows are taken out without being thrown away: a row keeps the replies already opened in it.
            foreach (var child in list.Children.ToArray())
                list.Remove(child, disposeImmediately: child is not DiscussionRow);

            int beatmapId = editorBeatmap.BeatmapInfo.OnlineID;
            int localUserId = api.LocalUser.Value.Id;

            // the tabs count what is listed for whoever is picked, the way the website's do.
            var forUser = Discussions.ForDifficulty(discussions, beatmapId)
                                     .Where(d => user.Value == UserDropdown.EVERYONE || d.UserId == user.Value)
                                     .ToArray();

            tabs.SetCounts(Enum.GetValues<DiscussionFilter>().ToDictionary(f => f, f => forUser.Count(d => Discussions.Matches(d, f, localUserId))));

            bool listed(APIBeatmapsetDiscussion d) => forUser.Contains(d) && Discussions.Matches(d, filter.Value, localUserId);

            var timeline = Discussions.Timeline(discussions, beatmapId).Where(listed).ToArray();
            marks.Timeline.Value = timeline;

            // the timeline first: it is what is gone through while the beatmap is being changed.
            addSection(DiscussionStrings.Timeline, timeline);
            addSection(DiscussionStrings.ThisDifficulty, Discussions.ThisDifficulty(discussions, beatmapId).Where(listed));
            addSection(DiscussionStrings.WholeSet, Discussions.WholeSet(discussions).Where(listed));

            showStatus(discussions.Count == 0 ? DiscussionStrings.NothingPosted : default);
        }

        private void addSection(LocalisableString title, IEnumerable<APIBeatmapsetDiscussion> section)
        {
            var threads = Discussions.Sort(section, sort.Value).ToArray();

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
            {
                if (!rows.TryGetValue(discussion.Id, out var row))
                    rows[discussion.Id] = row = new DiscussionRow(discussion, names.GetValueOrDefault(discussion.UserId, @"?"));

                list.Add(row);
            }
        }

        /// <summary>
        /// Throws away every row, including those the tabs have taken out of the list.
        /// </summary>
        private void clearRows()
        {
            // those taken out by the tabs, which are no longer anybody's child to be thrown away with.
            var outside = rows.Values.Where(r => r.Parent == null).ToArray();

            list.Clear();

            foreach (var row in outside)
                row.Dispose();

            rows.Clear();
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

            if (marks.IsNotNull())
                marks.Picked -= bringIntoView;

            foreach (var row in rows.Values.Where(r => r.Parent == null))
                row.Dispose();

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
