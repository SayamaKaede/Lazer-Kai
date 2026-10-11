// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Audio;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// The hitsound screen of a difficulty made to hitsound in: what each lane sounds as, and what it is to be once
    /// turned into an osu! hitsound difficulty.
    /// </summary>
    /// <remarks>
    /// It lies over the playfield as <see cref="HitsoundPanel"/> does on any other difficulty. The notes are put down on
    /// the compose screen; this is where the lanes are set up, which is done once and seldom after.
    /// </remarks>
    public partial class LaneSoundPanel : CompositeDrawable
    {
        private readonly IBindable<bool> shown;

        [Resolved]
        private LaneSounds laneSounds { get; set; } = null!;

        private FillFlowContainer<LaneRow> rows = null!;
        private OsuSpriteText emptyText = null!;

        /// <param name="shown">Whether the hitsound screen is the one showing.</param>
        public LaneSoundPanel(IBindable<bool> shown)
        {
            this.shown = shown.GetBoundCopy();

            // loaded and ready while hidden, so that it is there the moment the screen is switched to.
            Alpha = 0;
            AlwaysPresent = true;
        }

        public override bool PropagatePositionalInputSubTree => shown.Value && base.PropagatePositionalInputSubTree;

        public override bool PropagateNonPositionalInputSubTree => shown.Value && base.PropagateNonPositionalInputSubTree;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = 820,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Padding = new MarginPadding { Horizontal = 20, Vertical = 15 },
                    Child = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        RowDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(GridSizeMode.Absolute, 10),
                            new Dimension(),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 8),
                                    Children = new Drawable[]
                                    {
                                        new OsuSpriteText
                                        {
                                            Text = LazerKaiHitsoundStrings.LaneSoundsHeader,
                                            Font = OsuFont.GetFont(size: 17, weight: FontWeight.SemiBold),
                                        },
                                        new OsuSpriteText
                                        {
                                            Text = LazerKaiHitsoundStrings.LaneSoundsHint,
                                            Font = OsuFont.GetFont(size: 14),
                                            Colour = colourProvider.Content2,
                                        },
                                        new FillFlowContainer
                                        {
                                            AutoSizeAxes = Axes.Both,
                                            Direction = FillDirection.Horizontal,
                                            Spacing = new Vector2(8, 0),
                                            Children = new Drawable[]
                                            {
                                                new RoundedButton
                                                {
                                                    Text = LazerKaiHitsoundStrings.OpenSoundFolder,
                                                    Width = 240,
                                                    Height = 36,
                                                    Action = () => laneSounds.Library.PresentExternally(),
                                                },
                                                new RoundedButton
                                                {
                                                    Text = LazerKaiHitsoundStrings.ReloadSoundFolder,
                                                    Width = 240,
                                                    Height = 36,
                                                    Action = rebuild,
                                                },
                                            }
                                        },
                                        emptyText = new OsuSpriteText
                                        {
                                            Text = LazerKaiHitsoundStrings.SoundFolderEmpty,
                                            Font = OsuFont.GetFont(size: 14),
                                            Colour = colourProvider.Colour1,
                                        },
                                    }
                                },
                            },
                            new[] { Empty() },
                            new Drawable[]
                            {
                                new OsuScrollContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = rows = new FillFlowContainer<LaneRow>
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 6),
                                        Padding = new MarginPadding { Right = 14 },
                                    },
                                },
                            },
                        }
                    }
                },
            };

            rebuild();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            shown.BindValueChanged(s => Alpha = s.NewValue ? 1 : 0, true);
        }

        /// <summary>
        /// Makes the rows anew, from the sound folder as it is now.
        /// </summary>
        private void rebuild()
        {
            var files = laneSounds.Library.Files();

            emptyText.Alpha = files.Count == 0 ? 1 : 0;

            rows.Clear();

            // a dropdown opens over the rows under it.
            for (int lane = 0; lane < laneSounds.LaneCount; lane++)
                rows.Add(new LaneRow(lane, files) { Depth = lane });
        }

        public enum LaneBank
        {
            Normal,
            Soft,
            Drum,
        }

        public enum LaneKind
        {
            Normal,
            Whistle,
            Finish,
            Clap,
        }

        private static readonly string[] bank_names = { HitSampleInfo.BANK_NORMAL, HitSampleInfo.BANK_SOFT, HitSampleInfo.BANK_DRUM };

        private static readonly string[] kind_names = { HitSampleInfo.HIT_NORMAL, HitSampleInfo.HIT_WHISTLE, HitSampleInfo.HIT_FINISH, HitSampleInfo.HIT_CLAP };

        /// <summary>
        /// A sound of the folder to give a lane, or none.
        /// </summary>
        public record SoundChoice(string? Path)
        {
            public override string ToString() => Path ?? @"-";
        }

        /// <summary>
        /// One lane: its sound, what it is to be in an osu! difficulty, and a button to hear it.
        /// </summary>
        public partial class LaneRow : CompositeDrawable
        {
            public readonly int Lane;

            private readonly IReadOnlyList<string> files;

            [Resolved]
            private LaneSounds laneSounds { get; set; } = null!;

            public FormDropdown<SoundChoice> Sound { get; private set; } = null!;
            public FormEnumDropdown<LaneBank> Bank { get; private set; } = null!;
            public FormEnumDropdown<LaneKind> Kind { get; private set; } = null!;

            public LaneRow(int lane, IReadOnlyList<string> files)
            {
                Lane = lane;
                this.files = files;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                var sound = laneSounds[Lane];

                var none = new SoundChoice((string?)null);
                var choices = files.Select(f => new SoundChoice(f)).Prepend(none).ToList();

                // a sound no longer in the folder is still the lane's, having been copied into the beatmap.
                var current = !sound.HasFile ? none : choices.FirstOrDefault(c => c.Path == sound.Source);

                if (current == null)
                    choices.Add(current = new SoundChoice(sound.Source.Length > 0 ? sound.Source : sound.File));

                InternalChild = new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    ColumnDimensions = new[]
                    {
                        new Dimension(),
                        new Dimension(GridSizeMode.Absolute, 6),
                        new Dimension(GridSizeMode.Absolute, 170),
                        new Dimension(GridSizeMode.Absolute, 6),
                        new Dimension(GridSizeMode.Absolute, 170),
                        new Dimension(GridSizeMode.Absolute, 6),
                        new Dimension(GridSizeMode.Absolute, 40),
                    },
                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                    Content = new[]
                    {
                        new[]
                        {
                            Sound = new FormDropdown<SoundChoice>
                            {
                                Caption = LazerKaiHitsoundStrings.Lane(Lane + 1),
                                Items = choices,
                                Current = { Value = current },
                            },
                            Empty(),
                            Bank = new FormEnumDropdown<LaneBank>
                            {
                                Caption = LazerKaiHitsoundStrings.LaneBank,
                                Current = { Value = (LaneBank)System.Math.Max(0, System.Array.IndexOf(bank_names, sound.Bank)) },
                            },
                            Empty(),
                            Kind = new FormEnumDropdown<LaneKind>
                            {
                                Caption = LazerKaiHitsoundStrings.LaneKind,
                                Current = { Value = (LaneKind)System.Math.Max(0, System.Array.IndexOf(kind_names, sound.Sound)) },
                            },
                            Empty(),
                            new IconButton
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Icon = FontAwesome.Solid.Play,
                                TooltipText = LazerKaiHitsoundStrings.PlayLaneSound,
                                Action = () => laneSounds.Play(Lane),
                            },
                        },
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Sound.Current.BindValueChanged(choice =>
                {
                    // heard as it is picked, as a note put in the lane will sound.
                    if (laneSounds.Give(Lane, choice.NewValue.Path) && choice.NewValue.Path != null)
                        laneSounds.Play(Lane);
                });

                Bank.Current.BindValueChanged(_ => setSlot());
                Kind.Current.BindValueChanged(_ => setSlot());
            }

            private void setSlot() => laneSounds.SetSlot(Lane, bank_names[(int)Bank.Current.Value], kind_names[(int)Kind.Current.Value]);
        }
    }
}
