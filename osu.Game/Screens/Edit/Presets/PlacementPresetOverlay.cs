// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
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
using osu.Game.Rulesets.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Screens.Edit.Presets
{
    /// <summary>
    /// The shapes kept aside to be placed again, and the way back to putting one down.
    /// </summary>
    /// <remarks>
    /// Deliberately not modal. The digits belong to this list while it is open, but everything else — playing the
    /// song, moving along the timeline, undoing — carries on underneath, because deciding which shape to reach for
    /// is done while listening to the part it is going into.
    /// </remarks>
    public partial class PlacementPresetOverlay : VisibilityContainer, IKeyBindingHandler<GlobalAction>
    {
        public const float WIDTH = 460;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        [Resolved]
        private IBeatSnapProvider beatSnapProvider { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private Editor? editor { get; set; }

        private PlacementPresetStore store = null!;
        private PlacementPresetList globalList = null!;
        private PlacementPresetList beatmapList = null!;

        private readonly Bindable<PlacementPresetScope> scope = new Bindable<PlacementPresetScope>();

        private FillFlowContainer<PresetPanel> panels = null!;
        private OsuSpriteText emptyText = null!;
        private OsuSpriteText keyPrompt = null!;
        private RoundedButton keepButton = null!;

        /// <summary>
        /// The preset waiting for a key to be pressed, if one is.
        /// </summary>
        /// <remarks>
        /// While this is set every key is taken, including the ones the editor would otherwise answer: a digit is
        /// being chosen, and space would start the song playing on its way past.
        /// </remarks>
        private PlacementPreset? awaitingKeyFor;

        private PlacementPresetList currentList => scope.Value == PlacementPresetScope.Global ? globalList : beatmapList;

        [BackgroundDependencyLoader]
        private void load(Storage storage)
        {
            store = new PlacementPresetStore(storage);
            globalList = store.LoadGlobal();
            beatmapList = store.LoadForBeatmap(editorBeatmap);

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
                        Children = new[]
                        {
                            createHeader(),
                            createTabs(),
                            createList(),
                            createFooter(),
                        }
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            scope.BindValueChanged(_ => rebuild());
            editorBeatmap.SelectedHitObjects.BindCollectionChanged((_, _) => updateKeepButton(), true);

            rebuild();
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
                    Text = PlacementPresetStrings.Header,
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

        private Drawable createTabs() => new Container
        {
            RelativeSizeAxes = Axes.X,
            Height = 30,
            Padding = new MarginPadding { Horizontal = 10 },
            Child = new OsuTabControl<PlacementPresetScope>
            {
                RelativeSizeAxes = Axes.Both,
                Current = scope,
            }
        };

        private Drawable createList() => new Container
        {
            RelativeSizeAxes = Axes.X,
            Height = 260,
            Children = new Drawable[]
            {
                emptyText = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = PlacementPresetStrings.NothingKeptYet,
                    Colour = colourProvider.Foreground1,
                },
                new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = panels = new FillFlowContainer<PresetPanel>
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Full,
                        Padding = new MarginPadding(10),
                        Spacing = new Vector2(8),
                    }
                },
            }
        };

        private Drawable createFooter() => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Padding = new MarginPadding(10),
            Spacing = new Vector2(0, 6),
            Children = new Drawable[]
            {
                keyPrompt = new OsuSpriteText
                {
                    Alpha = 0,
                    Font = OsuFont.Default.With(size: 12),
                    Colour = colourProvider.Content2,
                    Text = PlacementPresetStrings.PressADigit,
                },
                keepButton = new RoundedButton
                {
                    RelativeSizeAxes = Axes.X,
                    Text = PlacementPresetStrings.KeepTheSelection,
                    Action = KeepSelection,
                },
            }
        };

        private void rebuild()
        {
            panels.Clear();

            float radius = editor?.CurrentComposer?.PlacementPresetObjectRadius ?? 0;

            foreach (var preset in currentList.Presets)
            {
                var captured = preset;

                panels.Add(new PresetPanel(captured, radius)
                {
                    Place = () => place(captured),
                    AssignKey = () => beginAssign(captured),
                    Remove = () => remove(captured),
                });
            }

            emptyText.Alpha = currentList.Presets.Count == 0 ? 1 : 0;
        }

        private void updateKeepButton() => keepButton.Enabled.Value = editorBeatmap.SelectedHitObjects.Count > 0;

        /// <summary>
        /// The list the tabs are currently showing.
        /// </summary>
        public PlacementPresetList VisibleList => currentList;

        public void KeepSelection()
        {
            var selected = editorBeatmap.SelectedHitObjects.OrderBy(h => h.StartTime).ToList();

            if (selected.Count == 0)
                return;

            currentList.Presets.Add(PlacementPreset.From(selected));
            save();
            rebuild();
        }

        private void remove(PlacementPreset preset)
        {
            currentList.Presets.Remove(preset);
            save();
            rebuild();
        }

        private void beginAssign(PlacementPreset preset)
        {
            awaitingKeyFor = preset;
            keyPrompt.Alpha = 1;
        }

        private void endAssign()
        {
            awaitingKeyFor = null;
            keyPrompt.Alpha = 0;
        }

        private void save()
        {
            if (scope.Value == PlacementPresetScope.Global)
                store.SaveGlobal(globalList);
            else
                store.SaveForBeatmap(editorBeatmap, beatmapList);
        }

        /// <summary>
        /// Puts a kept shape into the beatmap, in the middle of the playfield at the current time.
        /// </summary>
        /// <remarks>
        /// The middle rather than under the cursor, which is over this list at the moment the choice is made. What
        /// arrives is left selected, so the next thing done to it is done to the shape that was just placed.
        /// </remarks>
        private void place(PlacementPreset preset)
        {
            var composer = editor?.CurrentComposer;

            if (composer == null || !composer.SupportsPlacementPresets)
                return;

            var objects = preset.GetObjects();

            if (objects.Count == 0)
                return;

            PlacementPresetPlacement.StripReading(objects);

            foreach (var hitObject in objects)
                composer.ApplyPlacementDefaults(hitObject);

            PlacementPresetPlacement.LayOut(objects, beatSnapProvider.SnapTime(editorClock.CurrentTime), composer.PlacementPresetCentre);

            editorBeatmap.BeginChange();
            editorBeatmap.SelectedHitObjects.Clear();
            editorBeatmap.AddRange(objects);
            editorBeatmap.SelectedHitObjects.AddRange(objects);
            editorBeatmap.EndChange();
        }

        /// <summary>
        /// Takes the number row off the toolbox while the list is open, and gives it back on the way out.
        /// </summary>
        /// <remarks>
        /// The key binding container puts itself at the head of the input queue so that bindings beat plain key
        /// handlers, which means overriding <see cref="OnKeyDown"/> alone never sees a digit: the tool switches
        /// instead — measured, not guessed.
        ///
        /// Holding focus does get in front of it, but only while focus is actually held, which in the editor it
        /// often is not: 「数字キーはやっぱ後ろのTOOLSが反応しちゃった」 (2026-09-05), after a build where focus
        /// was the whole mechanism. Turning the tool shortcuts off instead leaves the binding unclaimed, and an
        /// unclaimed binding lets the plain key carry on down the queue to here, wherever focus happens to be.
        /// </remarks>
        private void setToolShortcuts(bool enabled)
        {
            var composer = editor?.CurrentComposer;

            if (composer != null)
                composer.ToolShortcutsEnabled.Value = enabled;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (State.Value != Visibility.Visible)
                return false;

            if (awaitingKeyFor != null)
            {
                if (e.Repeat)
                    return true;

                int? chosen = digitFor(e.Key);

                // a digit takes it, escape clears whatever it had, and anything else was not an answer to the
                // question being asked, so it leaves the preset as it was.
                if (chosen != null || e.Key == Key.Escape)
                {
                    currentList.Assign(awaitingKeyFor, chosen);
                    save();
                }

                endAssign();
                rebuild();

                // taken whatever it was: a key was being chosen, and the editor answering it as well would start
                // the song playing or switch the tool on the way past.
                return true;
            }

            if (e.Repeat || e.ControlPressed || e.ShiftPressed || e.AltPressed || e.SuperPressed)
                return false;

            int? digit = digitFor(e.Key);

            if (digit == null)
                return false;

            var preset = currentList.ForDigit(digit.Value);

            if (preset != null)
                place(preset);

            // taken even with nothing on it: the digits are this list's while it is open, and letting one through
            // would change the tool underneath instead, which is not what pressing a number here is for.
            return true;
        }

        /// <summary>
        /// Escape closes the panel rather than leaving the editor.
        /// </summary>
        /// <remarks>
        /// Read as the action rather than as the key, so it follows whatever the user has bound going back to. The
        /// digits above are read as keys because a digit is what is being offered, not an action.
        /// </remarks>
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
        /// The list covers part of the playfield, and what is being decided while it is open is which shape suits
        /// the part of the beatmap underneath it.
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

        private static int? digitFor(Key key)
        {
            if (key >= Key.Number0 && key <= Key.Number9)
                return key - Key.Number0;

            if (key >= Key.Keypad0 && key <= Key.Keypad9)
                return key - Key.Keypad0;

            return null;
        }

        /// <summary>
        /// Swallows anything the panel's own controls did not take, so a click meant for the panel cannot fall
        /// through to the playfield or the timelines behind it.
        /// </summary>
        /// <remarks>
        /// Children are offered the event first, so this only catches what lands on the panel's background. Being
        /// non-modal, the panel deliberately blocks nothing outside its own bounds. The same as the two panels the
        /// menu opens next to this one.
        /// </remarks>
        protected override bool OnMouseDown(MouseDownEvent e) => true;

        protected override bool OnClick(ClickEvent e) => true;

        protected override bool OnDoubleClick(DoubleClickEvent e) => true;

        protected override bool OnDragStart(DragStartEvent e) => true;

        protected override bool OnScroll(ScrollEvent e) => true;

        protected override bool OnHover(HoverEvent e) => true;

        protected override void PopIn()
        {
            rebuild();
            this.FadeIn(200, Easing.OutQuint);

            setToolShortcuts(false);
        }

        protected override void PopOut()
        {
            endAssign();
            this.FadeOut(200, Easing.OutQuint);

            setToolShortcuts(true);
        }

        protected override void Dispose(bool isDisposing)
        {
            // the editor outlives this on a screen change, and the toolbox would be left without its keys.
            if (State.Value == Visibility.Visible)
                setToolShortcuts(true);

            base.Dispose(isDisposing);
        }
    }
}
