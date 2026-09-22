// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osuTK;
using osuTK.Input;
using WebCommonStrings = osu.Game.Resources.Localisation.Web.CommonStrings;

namespace osu.Game.Screens.Edit.Setup
{
    /// <summary>
    /// The combo colour presets: one tab to put a preset on the beatmap, one to keep the beatmap's colours as one.
    /// </summary>
    public partial class ComboColourPresetPopover : OsuPopover
    {
        private const float width = 300;

        /// <summary>
        /// How tall the list may grow before it scrolls instead.
        /// </summary>
        private const float max_list_height = 280;

        private readonly BindableList<Colour4> colours = new BindableList<Colour4>();
        private readonly ComboColourPresetStore store;
        private readonly Action<IEnumerable<Colour4>> apply;

        private readonly Bindable<Tab> tab = new Bindable<Tab>();

        private ComboColourPresetList list = null!;

        private Container chooseTab = null!;
        private OsuScrollContainer scroll = null!;
        private FillFlowContainer<PresetRow> rows = null!;
        private OsuSpriteText nothingSaved = null!;

        private Container saveTab = null!;
        private Swatches currentColours = null!;
        private OsuTextBox nameBox = null!;
        private RoundedButton saveButton = null!;

        public ComboColourPresetPopover(BindableList<Colour4> colours, ComboColourPresetStore store, Action<IEnumerable<Colour4>> apply)
        {
            this.colours.BindTo(colours);
            this.store = store;
            this.apply = apply;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            // read afresh each time, so that a preset saved while another difficulty was open is here too.
            list = store.Load();

            Child = new FillFlowContainer
            {
                Width = width,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(10),
                Children = new Drawable[]
                {
                    new OsuTabControl<Tab>
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 30,
                        Current = tab,
                    },
                    chooseTab = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            nothingSaved = new OsuSpriteText
                            {
                                Text = LazerKaiComboColourPresetStrings.NoPresetsYet,
                                Colour = colourProvider.Foreground1,
                                Margin = new MarginPadding { Vertical = 10 },
                            },
                            scroll = new OsuScrollContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                Child = rows = new FillFlowContainer<PresetRow>
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(4),
                                },
                            },
                        },
                    },
                    saveTab = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Child = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(10),
                            Children = new Drawable[]
                            {
                                currentColours = new Swatches(),
                                nameBox = new OsuTextBox
                                {
                                    RelativeSizeAxes = Axes.X,
                                    PlaceholderText = LazerKaiComboColourPresetStrings.PresetName,
                                    CommitOnFocusLost = false,
                                },
                                saveButton = new RoundedButton
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Action = save,
                                },
                            },
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            colours.BindCollectionChanged((_, _) =>
            {
                currentColours.SetColours(colours);
                updateSaveButton();
            }, true);

            nameBox.Current.BindValueChanged(_ => updateSaveButton(), true);
            nameBox.OnCommit += (_, _) => save();

            tab.BindValueChanged(t =>
            {
                chooseTab.FadeTo(t.NewValue == Tab.Choose ? 1 : 0);
                saveTab.FadeTo(t.NewValue == Tab.Save ? 1 : 0);

                // the tab is opened to type a name in, so that is where the keyboard goes without another click.
                if (t.NewValue == Tab.Save)
                    ScheduleAfterChildren(() => GetContainingFocusManager()?.ChangeFocus(nameBox));
            }, true);

            refreshRows();
        }

        protected override void Update()
        {
            base.Update();

            // a scroll container will not take its height from what it holds, so it is given it here, up to a limit.
            scroll.Height = Math.Min(rows.DrawHeight, max_list_height);
        }

        private string enteredName => nameBox.Current.Value.Trim();

        private void updateSaveButton()
        {
            saveButton.Enabled.Value = enteredName.Length > 0 && colours.Count > 0;
            saveButton.Text = list.Find(enteredName) != null ? LazerKaiComboColourPresetStrings.Overwrite : LazerKaiComboColourPresetStrings.Save;
        }

        private void save()
        {
            if (!saveButton.Enabled.Value)
                return;

            list.Save(enteredName, colours);
            store.Save(list);

            nameBox.Current.Value = string.Empty;
            refreshRows();

            // straight to the list, where what was just saved can be seen among the rest.
            tab.Value = Tab.Choose;
        }

        private void choose(ComboColourPreset preset)
        {
            apply(preset.ToColours());
            this.HidePopover();
        }

        private void delete(ComboColourPreset preset)
        {
            list.Presets.Remove(preset);
            store.Save(list);

            refreshRows();
            updateSaveButton();
        }

        private void refreshRows()
        {
            rows.ChildrenEnumerable = list.Presets.Select(p => new PresetRow(p)
            {
                Chosen = () => choose(p),
                DeleteRequested = () => delete(p),
            });

            nothingSaved.Alpha = list.Presets.Count == 0 ? 1 : 0;
            scroll.Alpha = list.Presets.Count == 0 ? 0 : 1;
        }

        private enum Tab
        {
            [LocalisableDescription(typeof(LazerKaiComboColourPresetStrings), nameof(LazerKaiComboColourPresetStrings.ChooseAPreset))]
            Choose,

            [LocalisableDescription(typeof(LazerKaiComboColourPresetStrings), nameof(LazerKaiComboColourPresetStrings.SaveAsAPreset))]
            Save,
        }

        /// <summary>
        /// One preset in the list: its name and its colours. Clicked to be put on the beatmap, middle clicked to offer
        /// deleting it.
        /// </summary>
        /// <remarks>
        /// Not a context menu: the popover takes every mouse press made inside it, so the menu container around the
        /// editor never hears the right click. The button is offered in the row instead.
        ///
        /// The middle button rather than the right, so that swapping the two in the settings puts this on the right
        /// one: the swap is made where input enters the game, and the physical right button arrives here as the middle.
        /// </remarks>
        public partial class PresetRow : OsuClickableContainer
        {
            public Action? Chosen { get; init; }
            public Action? DeleteRequested { get; init; }

            public readonly ComboColourPreset Preset;

            public DangerousRoundedButton DeleteButton { get; private set; } = null!;

            private Box background = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public PresetRow(ComboColourPreset preset)
            {
                Preset = preset;

                // given here rather than in the loader: the clickable container sizes its content from these in a
                // loader of its own, which runs before this class's.
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Masking = true;
                CornerRadius = 5;
                Action = () => Chosen?.Invoke();

                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background3,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(8),
                        Spacing = new Vector2(6),
                        Children = new Drawable[]
                        {
                            new TruncatingSpriteText
                            {
                                RelativeSizeAxes = Axes.X,
                                Text = Preset.Name,
                                Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold),
                            },
                            new Swatches(Preset.ToColours()),
                        },
                    },
                    DeleteButton = new DangerousRoundedButton
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        Margin = new MarginPadding { Right = 8 },
                        Size = new Vector2(60, 26),
                        Text = WebCommonStrings.ButtonsDelete,
                        Alpha = 0,
                        Action = () => DeleteRequested?.Invoke(),
                    },
                };
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                if (e.Button != MouseButton.Middle)
                    return base.OnMouseDown(e);

                DeleteButton.FadeTo(DeleteButton.Alpha > 0 ? 0 : 1);
                return true;
            }

            protected override bool OnHover(HoverEvent e)
            {
                background.FadeColour(colourProvider.Background2, 100, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                background.FadeColour(colourProvider.Background3, 100, Easing.OutQuint);

                // offered for the row under the mouse only, so that moving on to another one takes it back.
                DeleteButton.FadeOut();
                base.OnHoverLost(e);
            }
        }

        /// <summary>
        /// A row of small squares, one for each colour.
        /// </summary>
        private partial class Swatches : FillFlowContainer
        {
            public Swatches(IEnumerable<Colour4>? colours = null)
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Direction = FillDirection.Horizontal;
                Spacing = new Vector2(4);

                if (colours != null)
                    SetColours(colours);
            }

            public void SetColours(IEnumerable<Colour4> colours)
            {
                ChildrenEnumerable = colours.Select(c => new Container
                {
                    Size = new Vector2(20),
                    Masking = true,
                    CornerRadius = 4,
                    CornerExponent = 2.5f,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = c,
                    },
                });
            }
        }
    }
}
