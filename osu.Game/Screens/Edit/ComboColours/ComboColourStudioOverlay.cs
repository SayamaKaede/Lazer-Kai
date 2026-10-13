// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Rulesets.Edit;
using osuTK;
using WebCommonStrings = osu.Game.Resources.Localisation.Web.CommonStrings;

namespace osu.Game.Screens.Edit.ComboColours
{
    /// <summary>
    /// Lays out which of the beatmap's combo colours the combos take, from one point in the song to the next.
    /// </summary>
    /// <remarks>
    /// A rebuild of Combo Colour Studio from OliBomby's Mapping Tools (MIT), working on the beatmap open in the editor
    /// rather than on a file. Deliberately not modal: a layout is built by listening to the song and watching the
    /// playfield, so the editor has to stay usable while this is open.
    /// </remarks>
    public partial class ComboColourStudioOverlay : VisibilityContainer, IKeyBindingHandler<GlobalAction>
    {
        public const float WIDTH = 460;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private IDialogOverlay? dialogOverlay { get; set; }

        /// <summary>
        /// The layout as it currently stands, in time order.
        /// </summary>
        public IReadOnlyList<ComboColourPoint> Points => project.Points;

        private ComboColourProjectStore store = null!;
        private ComboColourProject project = null!;

        private FillFlowContainer<PointRow> rows = null!;
        private OsuSpriteText emptyText = null!;

        [BackgroundDependencyLoader]
        private void load(Storage storage)
        {
            store = new ComboColourProjectStore(storage);
            project = store.Load(editorBeatmap);

            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;
            Margin = new MarginPadding { Left = 60 };
            Width = WIDTH;
            AutoSizeAxes = Axes.Y;

            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 10,
                EdgeEffect = new osu.Framework.Graphics.Effects.EdgeEffectParameters
                {
                    Type = osu.Framework.Graphics.Effects.EdgeEffectType.Shadow,
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
                                Height = 260,
                                Children = new Drawable[]
                                {
                                    emptyText = new OsuSpriteText
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Text = ComboColourStudioStrings.NoPointsYet,
                                        Colour = colourProvider.Foreground1,
                                    },
                                    new OsuScrollContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Child = rows = new FillFlowContainer<PointRow>
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
                            createFooter(),
                        }
                    },
                }
            };
        }

        private Drawable createHeader() => new HeaderBar(() => this)
        {
            RelativeSizeAxes = Axes.X,
            Height = 40,
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
                    Font = OsuFont.Default.With(size: 16, weight: FontWeight.SemiBold),
                    Text = ComboColourStudioStrings.ComboColourStudio,
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

        private Drawable createFooter()
        {
            FormNumberBox burstLength;

            var footer = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding(10),
                Spacing = new Vector2(0, 6),
                Children = new Drawable[]
                {
                    new RoundedButton
                    {
                        RelativeSizeAxes = Axes.X,
                        Text = ComboColourStudioStrings.AddPointAtCurrentTime,
                        Action = addPointAtCurrentTime,
                    },
                    burstLength = new FormNumberBox
                    {
                        Caption = ComboColourStudioStrings.MaxBurstLength,
                        HintText = ComboColourStudioStrings.MaxBurstLengthDescription,
                        Current = { Value = project.MaxBurstLength.ToString() },
                    },
                    new RoundedButton
                    {
                        RelativeSizeAxes = Axes.X,
                        Text = ComboColourStudioStrings.CopyFromAnotherDifficulty,
                        Action = showCopyFromMenu,
                    },
                    new RoundedButton
                    {
                        RelativeSizeAxes = Axes.X,
                        Text = ComboColourStudioStrings.Apply,
                        BackgroundColour = colourProvider.Highlight1,
                        Action = confirmApply,
                    },
                    new OsuSpriteText
                    {
                        Font = OsuFont.Default.With(size: 11),
                        Colour = colourProvider.Foreground1,
                        Text = ComboColourStudioStrings.BasedOnMappingTools,
                    },
                }
            };

            burstLength.Current.BindValueChanged(v =>
            {
                if (int.TryParse(v.NewValue, out int parsed) && parsed > 0)
                {
                    project.MaxBurstLength = parsed;
                    save();
                }
            });

            return footer;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            rebuildRows();
        }

        private void addPointAtCurrentTime()
        {
            double time = Math.Round(editorClock.CurrentTime);

            if (project.Points.Any(p => Math.Abs(p.Time - time) < 1))
                return;

            var point = new ComboColourPoint { Time = time };

            // starts on the first colour so the row shows something to work from.
            if (paletteSize > 0)
                point.Colours.Add(0);

            project.Points.Add(point);
            sortAndRebuild();
        }

        private void removePoint(ComboColourPoint point)
        {
            project.Points.Remove(point);
            sortAndRebuild();
        }

        /// <summary>
        /// Takes the layout along when the whole beatmap is moved in time.
        /// </summary>
        /// <remarks>
        /// The points are at times of their own and kept beside the beatmap, so nothing that moves the beatmap moves
        /// them. Left where they were, each would stand after the combo it was put on once the beatmap is moved
        /// earlier, and colour the next one in its place.
        /// </remarks>
        public void MoveInTime(int milliseconds)
        {
            if (milliseconds == 0 || project.Points.Count == 0)
                return;

            foreach (var point in project.Points)
                point.Time += milliseconds;

            rebuildRows();
            save();
        }

        private void sortAndRebuild()
        {
            project.Points.Sort((a, b) => a.Time.CompareTo(b.Time));
            rebuildRows();
            save();
        }

        private void rebuildRows()
        {
            rows.Clear();

            foreach (var point in project.Points)
                rows.Add(new PointRow(point, () => paletteSize, sortAndRebuild, () => removePoint(point)));

            emptyText.Alpha = project.Points.Count == 0 ? 1 : 0;
        }

        private int paletteSize => editorBeatmap.BeatmapSkin?.ComboColours.Count ?? 0;

        private void save() => store.Save(editorBeatmap, project);

        private void confirmApply()
        {
            if (dialogOverlay == null)
            {
                apply();
                return;
            }

            dialogOverlay.Push(new ApplyConfirmationDialog(apply));
        }

        private void apply()
        {
            try
            {
                ComboColourApplier.Apply(editorBeatmap, project, paletteSize);
            }
            catch (InvalidOperationException e)
            {
                dialogOverlay?.Push(new ColourMissingDialog(e.Message));
            }
        }

        private void showCopyFromMenu()
        {
            string mine = ComboColourProjectStore.FolderNameFor(editorBeatmap);
            string[] others = store.SavedLayouts().Where(name => name != mine).ToArray();

            if (others.Length == 0 || dialogOverlay == null)
                return;

            dialogOverlay.Push(new CopyFromDialog(others, name =>
            {
                project = store.Load(name);
                rebuildRows();
                save();
            }));
        }

        /// <summary>
        /// Swallows anything the panel's own controls did not take, so a click meant for the panel cannot fall through
        /// to the playfield or the timelines behind it.
        /// </summary>
        /// <remarks>
        /// Children are offered the event first, so this only catches what lands on the panel's background. Being
        /// non-modal, the panel deliberately blocks nothing outside its own bounds.
        /// </remarks>
        protected override bool OnMouseDown(MouseDownEvent e) => true;

        protected override bool OnClick(ClickEvent e) => true;

        protected override bool OnDoubleClick(DoubleClickEvent e) => true;

        protected override bool OnDragStart(DragStartEvent e) => true;

        protected override bool OnScroll(ScrollEvent e) => true;

        protected override bool OnHover(HoverEvent e) => true;

        protected override void PopIn() => this.FadeIn(200, Easing.OutQuint);

        protected override void PopOut() => this.FadeOut(200, Easing.OutQuint);

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

        /// <summary>
        /// The bar along the top: dragging it moves the panel.
        /// </summary>
        /// <remarks>
        /// The panel covers part of the playfield, and what a layout is being built against is what is underneath it.
        /// </remarks>
        private partial class HeaderBar : Container
        {
            private readonly Func<Drawable> panel;

            public HeaderBar(Func<Drawable> panel)
            {
                this.panel = panel;
            }

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void OnDrag(DragEvent e) => panel().Position += e.Delta;
        }

        public partial class ApplyConfirmationDialog : PopupDialog
        {
            public ApplyConfirmationDialog(Action apply)
            {
                HeaderText = ComboColourStudioStrings.ApplyConfirmationHeader;
                BodyText = ComboColourStudioStrings.ApplyConfirmationBody;
                Icon = FontAwesome.Solid.Palette;

                Buttons = new PopupDialogButton[]
                {
                    new PopupDialogDangerousButton
                    {
                        Text = ComboColourStudioStrings.ApplyConfirmationAccept,
                        Action = apply,
                    },
                    new PopupDialogCancelButton { Text = WebCommonStrings.ButtonsCancel },
                };
            }
        }

        private partial class ColourMissingDialog : PopupDialog
        {
            public ColourMissingDialog(string message)
            {
                HeaderText = ComboColourStudioStrings.CannotApply;
                BodyText = message;
                Icon = FontAwesome.Solid.ExclamationTriangle;

                Buttons = new PopupDialogButton[] { new PopupDialogOkButton { Text = DialogStrings.Confirm } };
            }
        }

        private partial class CopyFromDialog : PopupDialog
        {
            public CopyFromDialog(IEnumerable<string> layouts, Action<string> pick)
            {
                HeaderText = ComboColourStudioStrings.CopyFromAnotherDifficulty;
                Icon = FontAwesome.Solid.Copy;

                Buttons = layouts.Take(8)
                                 .Select(name => (PopupDialogButton)new PopupDialogOkButton
                                 {
                                     Text = name,
                                     Action = () => pick(name),
                                 })
                                 .Append(new PopupDialogCancelButton { Text = WebCommonStrings.ButtonsCancel })
                                 .ToArray();
            }
        }
    }
}
