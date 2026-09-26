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
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Select
{
    public sealed partial class FilterControl
    {
        /// <summary>
        /// The searches kept under a name: one puts its search into the box, and the one in the box can be kept.
        /// </summary>
        public partial class SearchPresetPopover : OsuPopover
        {
            private const float width = 320;
            private const float max_list_height = 360;

            private readonly FilterControl filterControl;
            private readonly IBindable<string> searchText = new Bindable<string>();

            private SearchPresetStore store = null!;

            private OsuScrollContainer scroll = null!;
            private FillFlowContainer<PresetRow> list = null!;
            private OsuSpriteText noPresets = null!;
            private OsuTextBox nameTextBox = null!;
            private RoundedButton saveButton = null!;

            public SearchPresetPopover(FilterControl filterControl)
            {
                this.filterControl = filterControl;
            }

            [BackgroundDependencyLoader]
            private void load(Storage storage, OverlayColourProvider colourProvider)
            {
                store = new SearchPresetStore(storage);

                Child = new FillFlowContainer
                {
                    Width = width,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = LazerKaiSongSelectStrings.SearchPresets,
                            Font = OsuFont.Default.With(weight: FontWeight.SemiBold),
                        },
                        noPresets = new OsuSpriteText
                        {
                            Text = LazerKaiSongSelectStrings.NoSearchPresets,
                            Font = OsuFont.Default.With(size: 14),
                            Colour = colourProvider.Content2,
                        },
                        scroll = new OsuScrollContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            Child = list = new FillFlowContainer<PresetRow>
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(2),
                            },
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 1,
                            Colour = colourProvider.Background2,
                            Margin = new MarginPadding { Vertical = 3 },
                        },
                        nameTextBox = new OsuTextBox
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 32,
                            PlaceholderText = LazerKaiSongSelectStrings.SearchPresetName,
                            CommitOnFocusLost = false,
                        },
                        saveButton = new RoundedButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 32,
                            Text = LazerKaiSongSelectStrings.SaveSearchPreset,
                            Action = save,
                        },
                    },
                };

                nameTextBox.OnCommit += (_, _) => save();
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                searchText.BindTo(filterControl.SearchText);
                searchText.BindValueChanged(_ => updateState());

                show(store.Load());

                ScheduleAfterChildren(() => GetContainingFocusManager()?.ChangeFocus(nameTextBox));
            }

            protected override void Update()
            {
                base.Update();
                scroll.Height = Math.Min(list.DrawHeight, max_list_height);
            }

            private void save()
            {
                if (string.IsNullOrWhiteSpace(searchText.Value))
                    return;

                show(store.Add(nameTextBox.Text, searchText.Value));
                nameTextBox.Text = string.Empty;
            }

            private void show(IReadOnlyList<SearchPreset> presets)
            {
                list.ChildrenEnumerable = presets.Select(p => new PresetRow(p)
                {
                    Action = () =>
                    {
                        filterControl.Search(p.Query);
                        this.HidePopover();
                    },
                    Delete = () => show(store.Remove(p.Name)),
                });

                noPresets.Alpha = presets.Any() ? 0 : 1;
                updateState();
            }

            private void updateState()
            {
                saveButton.Enabled.Value = !string.IsNullOrWhiteSpace(searchText.Value);

                foreach (var row in list)
                    row.Current = row.Preset.Query == searchText.Value.Trim();
            }

            public partial class PresetRow : OsuClickableContainer
            {
                public readonly SearchPreset Preset;

                public Action? Delete { get; init; }

                private readonly Box background;

                private bool current;

                /// <summary>
                /// Whether this is what the search box has in it now.
                /// </summary>
                public bool Current
                {
                    get => current;
                    set
                    {
                        current = value;

                        if (IsLoaded)
                            updateState();
                    }
                }

                [Resolved]
                private OverlayColourProvider colourProvider { get; set; } = null!;

                public PresetRow(SearchPreset preset)
                {
                    Preset = preset;

                    RelativeSizeAxes = Axes.X;
                    Height = 40;
                    Masking = true;
                    CornerRadius = 5;
                    TooltipText = preset.Query;

                    Children = new Drawable[]
                    {
                        background = new Box { RelativeSizeAxes = Axes.Both },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Direction = FillDirection.Vertical,
                            Padding = new MarginPadding { Left = 10, Right = 36 },
                            Children = new Drawable[]
                            {
                                new TruncatingSpriteText
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Text = preset.Name,
                                    Font = OsuFont.Default.With(size: 15, weight: FontWeight.SemiBold),
                                },
                                new TruncatingSpriteText
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Text = preset.Query,
                                    Font = OsuFont.Default.With(size: 12),
                                    Alpha = 0.6f,
                                },
                            }
                        },
                        new IconButton
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Icon = FontAwesome.Solid.Trash,
                            IconScale = new Vector2(0.8f),
                            TooltipText = Resources.Localisation.Web.CommonStrings.ButtonsDelete,
                            Action = () => Delete?.Invoke(),
                        },
                    };
                }

                protected override void LoadComplete()
                {
                    base.LoadComplete();
                    updateState();
                }

                protected override bool OnHover(HoverEvent e)
                {
                    updateState();
                    return base.OnHover(e);
                }

                protected override void OnHoverLost(HoverLostEvent e)
                {
                    updateState();
                    base.OnHoverLost(e);
                }

                private void updateState()
                {
                    if (Current)
                        background.Colour = IsHovered ? colourProvider.Background1 : colourProvider.Background2;
                    else
                        background.Colour = IsHovered ? colourProvider.Background3 : colourProvider.Background5;
                }
            }
        }
    }
}
