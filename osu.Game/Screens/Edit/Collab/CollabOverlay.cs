// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// Divides a collaboration between the people making it, and says how much of the beatmap each of them has.
    /// </summary>
    /// <remarks>
    /// The number that matters is drain time, which is what a collaboration is agreed and argued in, and which nobody
    /// can work out by hand without adding up rests. Every stretch is measured the way the whole beatmap is, so the
    /// pieces add back up to the figure on the status panel rather than to something close to it.
    /// </remarks>
    public partial class CollabOverlay : VisibilityContainer, IKeyBindingHandler<GlobalAction>
    {
        private const float panel_width = 570;
        private const float row_height = 40;
        private const float list_height = 450;

        /// <summary>
        /// How solid the panel goes while the beatmap behind it is being looked at.
        /// </summary>
        /// <remarks>
        /// Only ever makes it thinner. Somebody who has already turned it below this would otherwise find that
        /// clicking away made it more solid, which is the opposite of what the click was for.
        /// </remarks>
        private const float unfocused_alpha = 0.2f;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private Clipboard clipboard { get; set; } = null!;

        [Resolved]
        private CollabHighlight highlight { get; set; } = null!;

        private readonly BindableFloat opacity = new BindableFloat(1);

        private CollabProjectStore store = null!;
        private CollabProject project = null!;

        private List<CollabBlock> blocks = new List<CollabBlock>();

        /// <summary>
        /// Whether the rows and totals are behind <see cref="blocks"/>, waiting for the panel to be looked at.
        /// </summary>
        private bool rowsStale = true;

        private CollabInputBlocker panel = null!;
        private FillFlowContainer rows = null!;
        private FillFlowContainer totals = null!;
        private OsuSpriteText emptyText = null!;
        private OsuSpriteText wholeText = null!;

        /// <summary>
        /// The panel itself, which is the part of this container that is drawn and that takes input.
        /// </summary>
        public Drawable Panel => panel;

        /// <summary>Whether the panel is what is being worked in, rather than the beatmap behind it.</summary>
        private bool focused = true;

        [BackgroundDependencyLoader]
        private void load(Storage storage, OsuConfigManager config)
        {
            config.BindWith(OsuSetting.CollabOverlayOpacity, opacity);

            store = new CollabProjectStore(storage);
            project = store.Load(editorBeatmap);

            RelativeSizeAxes = Axes.Both;

            // the panel is masked for its rounded corners, so a popover opened inside it would be cut off. This
            // container sits outside the panel and draws them as its siblings instead. The editor has none of
            // its own that reaches here.
            InternalChild = new PopoverContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = panel = new CollabInputBlocker
                {
                    Pressed = () => setFocused(true),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
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
                                        emptyText = new OsuSpriteText
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Text = CollabStrings.NoBookmarksYet,
                                            Colour = colourProvider.Foreground1,
                                        },
                                        new OsuScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Child = rows = new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Direction = FillDirection.Vertical,
                                                Padding = new MarginPadding(10),
                                                Spacing = new Vector2(0, 4),
                                            }
                                        },
                                    }
                                },
                                createTotals(),
                                createPlayheadToggle(),
                                createFooter(),
                            }
                        },
                    }
                },
            };
        }

        /// <summary>
        /// The bar the panel is dragged by, carrying the close button and the beatmap's own drain time.
        /// </summary>
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
                    Text = CollabStrings.Header,
                    Font = OsuFont.Default.With(size: 16, weight: FontWeight.SemiBold),
                },
                wholeText = new OsuSpriteText
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding { Right = 46 },
                    Colour = colourProvider.Foreground1,
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

        private Drawable createTotals() => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background3,
                },
                totals = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Full,
                    Padding = new MarginPadding(10),
                    Spacing = new Vector2(8, 6),
                },
            }
        };

        /// <summary>
        /// Keeps the strip along the bottom saying whose stretch the song is in, panel open or not.
        /// </summary>
        /// <remarks>
        /// Sits with the totals rather than among the buttons below, which act on the split: this changes nothing
        /// about the beatmap and is turned on to watch it play through.
        /// </remarks>
        private Drawable createPlayheadToggle() => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background3,
                },
                new OsuCheckbox
                {
                    RelativeSizeAxes = Axes.X,
                    Padding = new MarginPadding { Horizontal = 10, Vertical = 4 },
                    LabelText = CollabStrings.FollowPlayhead,
                    Current = highlight.FollowPlayhead,
                },
            }
        };

        private Drawable createFooter() => new Container
        {
            RelativeSizeAxes = Axes.X,
            Height = 48,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                },
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 10 },
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6, 0),
                    Children = new Drawable[]
                    {
                        new RoundedButton
                        {
                            Text = CollabStrings.AddCollaborator,
                            Width = 120,
                            Height = 30,
                            Action = addCollaborator,
                        },
                        new RoundedButton
                        {
                            Text = CollabStrings.CopySplit,
                            Width = 100,
                            Height = 30,
                            Action = copy,
                        },
                    }
                },
                new FillFlowContainer
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding { Right = 10 },
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6, 0),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Text = CollabStrings.PanelOpacity,
                            Font = OsuFont.Default.With(size: 12),
                            Colour = colourProvider.Content2,
                        },
                        new RoundedSliderBar<float>
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Current = opacity,
                            Width = 120,
                        },
                    }
                },
            }
        };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            editorBeatmap.Bookmarks.BindCollectionChanged((_, changed) =>
            {
                followBookmarksMovedTogether(changed);
                rebuild();
            });
            editorBeatmap.BeatmapReprocessed += rebuild;

            // moving the slider is working in the panel. It takes the press itself rather than letting it through, so
            // without this the panel would be adjusted against a solidity it is not being drawn at.
            opacity.BindValueChanged(_ => setFocused(true));

            rebuild();
        }

        /// <summary>
        /// Keeps who has which stretch when every bookmark moves by the same amount, as they do when the whole beatmap is
        /// moved in time and when that is undone.
        /// </summary>
        /// <remarks>
        /// Any other change to the bookmarks is a division nobody has agreed to yet, and what was given in the stretches it
        /// changes is dropped as it always was. Moved together, the stretches are the ones that were agreed, and dropping
        /// them emptied a whole collaboration for an offset put right by a few milliseconds.
        /// </remarks>
        private void followBookmarksMovedTogether(NotifyCollectionChangedEventArgs changed)
        {
            if (changed.Action != NotifyCollectionChangedAction.Replace || changed.OldItems == null || changed.NewItems == null)
                return;

            int[] before = changed.OldItems.Cast<int>().ToArray();
            int[] after = changed.NewItems.Cast<int>().ToArray();

            // every bookmark there is, and each as far as the first.
            if (before.Length == 0 || before.Length != after.Length || after.Length != editorBeatmap.Bookmarks.Count)
                return;

            int by = after[0] - before[0];

            if (by == 0 || before.Zip(after).Any(pair => pair.Second - pair.First != by))
                return;

            project.MoveAssignments(by);
            save();
        }

        /// <summary>
        /// Works the stretches out again from the beatmap as it now stands.
        /// </summary>
        /// <remarks>
        /// The stretches, what is assigned in them and the strip on the timeline are worked out here and now: they are
        /// cheap, and the strip is followed on the timeline whether the panel is open or not.
        ///
        /// The rows and totals are not. They are drawables, and throwing them away and making them again is where the
        /// time goes — 13.5ms a time on a real catch beatmap (measured 2026-09-11). A change to the beatmap arrives here
        /// once per mouse movement while an object is being dragged, since each movement is its own change, and a few
        /// thousand of them froze the editor for tens of seconds with the panel not even open. So nothing is made while
        /// it is closed, and while it is open the rows catch up once a frame rather than once per change.
        /// </remarks>
        private void rebuild()
        {
            blocks = CollabBlocks.For(editorBeatmap, editorBeatmap.Bookmarks);

            if (project.DropAssignmentsOutsideOf(blocks))
                save();

            updateBands();

            rowsStale = true;

            if (State.Value == Visibility.Visible)
                Scheduler.AddOnce(rebuildRows);
        }

        /// <summary>
        /// Makes the rows and totals again, if the stretches have changed since they were last made.
        /// </summary>
        private void rebuildRows()
        {
            if (!rowsStale)
                return;

            rowsStale = false;

            // the rows about to be thrown away are never told the pointer left them.
            highlight.Clear();

            // two bookmarks already make a stretch to give somebody, now that the stretches are only those between them.
            emptyText.Alpha = blocks.Count > 0 ? 0 : 1;

            rows.Clear();

            if (blocks.Count > 0)
            {
                foreach (var block in blocks)
                    rows.Add(new CollabBlockRow(block, project, colourProvider, onAssigned) { Height = row_height });
            }

            wholeText.Text = CollabStrings.WholeBeatmap(ComposeStatusDisplay.DrainTimeReadout(ComposeStatusDisplay.DrainTimeFor(editorBeatmap)));

            updateTotals();
        }

        /// <summary>
        /// Publishes every stretch that has somebody on it, in their colour, for the timeline's strip.
        /// </summary>
        private void updateBands()
        {
            highlight.Bands.Value = blocks
                                    .Where(b => project.Assignment.ContainsKey(b.Key) && project.Names.Contains(project.Assignment[b.Key]))
                                    .Select(b => new CollabBand(b.Start, b.End, CollabColours.For(project.Names.IndexOf(project.Assignment[b.Key]))))
                                    .ToArray();
        }

        private void updateTotals()
        {
            totals.Clear();

            var counted = project.TotalsFor(blocks);

            foreach ((string name, CollabTime time) in counted)
            {
                var theirs = blocks.Where(b => project.Assignment.TryGetValue(b.Key, out string? who) && who == name).ToArray();
                var colour = CollabColours.For(project.Names.IndexOf(name));

                totals.Add(new CollabTotal(name, time, colourProvider)
                {
                    Removed = () => remove(name),
                    Renamed = renamed => rename(name, renamed),
                    // the list of what they have is a different one per reading, so it doubles as who is asking.
                    Pointed = pointed => point(theirs, pointed ? theirs : null, colour),
                });
            }

            // here as well as in the rebuild: an assignment changes who owns what without changing where anything is,
            // and the strip has to follow that too.
            updateBands();

            var unclaimed = blocks.Where(b => !project.Assignment.ContainsKey(b.Key)).ToArray();

            // by whether anything is left rather than by how long it plays: before anything is mapped, nothing does.
            if (unclaimed.Length > 0 || counted.Count == 0)
            {
                totals.Add(new CollabTotal(CollabStrings.Unassigned, CollabTime.Of(unclaimed), colourProvider)
                {
                    Pointed = pointed => point(unclaimed, pointed ? unclaimed : null, colourProvider.Foreground1),
                });
            }
        }

        /// <summary>
        /// Lights a set of stretches on the timeline, or puts out the light this asker had.
        /// </summary>
        private void point(object asker, CollabBlock[]? lit, Color4 colour)
        {
            if (lit == null)
                highlight.Hide(asker);
            else
                highlight.Show(asker, lit, colour);
        }

        private void onAssigned()
        {
            save();
            updateTotals();
        }

        private void addCollaborator()
        {
            project.Names.Add(nextName());
            save();
            rebuild();
        }

        /// <summary>The next unused letter, which is what a split is agreed in before anyone is named.</summary>
        private string nextName()
        {
            for (char letter = 'A'; letter <= 'Z'; letter++)
            {
                if (!project.Names.Contains(letter.ToString()))
                    return letter.ToString();
            }

            return (project.Names.Count + 1).ToString();
        }

        private void rename(string from, string to)
        {
            if (!project.Rename(from, to))
                return;

            save();
            rebuild();
        }

        private void remove(string name)
        {
            project.Names.Remove(name);

            foreach (int key in project.Assignment.Where(a => a.Value == name).Select(a => a.Key).ToArray())
                project.Assignment.Remove(key);

            save();
            rebuild();
        }

        private void copy() => clipboard.SetText(
            CollabClipboard.Write(CollabProjectStore.BeatmapNameFor(editorBeatmap), blocks, project.Assignment));

        private void save() => store.Save(editorBeatmap, project);

        /// <summary>
        /// A click landing outside the panel is somebody looking at the beatmap, so the panel gets out of the way
        /// without closing: what has been decided so far is still there when they look back.
        /// </summary>
        /// <remarks>
        /// This container spans the screen, but only clicks the panel did not take reach it, so what arrives here is
        /// a click on the beatmap. The position is read anyway rather than assumed, so that the panel's solidity does
        /// not depend on it being the only thing that ever blocks.
        /// </remarks>
        protected override bool OnMouseDown(MouseDownEvent e)
        {
            setFocused(panel.ReceivePositionalInputAt(e.ScreenSpaceMouseDownPosition));

            // not handled: the click was meant for whatever is behind.
            return false;
        }

        private void setFocused(bool value)
        {
            focused = value;
            updateAlpha();
        }

        private void updateAlpha()
        {
            if (State.Value != Visibility.Visible)
                return;

            panel.FadeTo(focused ? opacity.Value : Math.Min(opacity.Value, unfocused_alpha), 200, Easing.OutQuint);
        }

        protected override void PopIn()
        {
            // the rows were left behind while the panel was closed; they catch up before anyone sees them.
            rebuildRows();

            focused = true;
            highlight.Open.Value = true;
            this.FadeIn(200, Easing.OutQuint);
            panel.Alpha = opacity.Value;
        }

        protected override void PopOut()
        {
            // the row the pointer was resting on is not told it was left, so the strip would stay lit.
            highlight.Clear();
            highlight.Open.Value = false;
            this.FadeOut(200, Easing.OutQuint);
        }

        /// <summary>
        /// Escape closes the panel rather than leaving the editor.
        /// </summary>
        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Action != GlobalAction.Back || State.Value != Visibility.Visible)
                return false;

            Hide();
            return true;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (editorBeatmap != null!)
                editorBeatmap.BeatmapReprocessed -= rebuild;
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
