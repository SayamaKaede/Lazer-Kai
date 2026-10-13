// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Models;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Notifications;
using osu.Game.Screens.Edit.Hitsounds;
using osuTK;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace osu.Game.Screens.Edit.BeatmapFiles
{
    /// <summary>
    /// The sounds of the beatmap set being edited, to be heard and to be given the names they are played as hitsounds
    /// by, without the folder of the set being opened (2026-10-13).
    /// </summary>
    /// <remarks>
    /// A name is not typed: the names stable plays are of one form (see <see cref="HitsoundFileName"/>), so the sample
    /// set, the sound and the number are chosen and the name follows. What plays a sound by its old name, as a note
    /// of an osu!mania difficulty or a storyboard may, is not told of the new one.
    /// </remarks>
    public partial class SoundsOfTheSet : CompositeDrawable
    {
        /// <summary>
        /// Whether a sound has been given another name: the editor then has to open the set again to find it by it.
        /// </summary>
        public bool Changed { get; private set; }

        public FillFlowContainer<SoundRow> Rows { get; private set; } = null!;

        public FormEnumDropdown<NamedBank> Bank { get; private set; } = null!;

        public FormEnumDropdown<NamedSound> Sound { get; private set; } = null!;

        public FormNumberBox Number { get; private set; } = null!;

        public IconButton NumberUp { get; private set; } = null!;

        public IconButton NumberDown { get; private set; } = null!;

        /// <summary>
        /// How wide the number's box is: as the custom index of the hitsound screen.
        /// </summary>
        private const float number_width = 165;

        public RoundedButton RenameButton { get; private set; } = null!;

        /// <summary>
        /// The sound chosen on the left, by its name in the set.
        /// </summary>
        public string? Chosen { get; private set; }

        /// <summary>
        /// The name what is chosen on the right comes to, if a number is said.
        /// </summary>
        public string? NewName { get; private set; }

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved]
        private AudioManager audio { get; set; } = null!;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private IDialogOverlay? dialogOverlay { get; set; }

        [Resolved(canBeNull: true)]
        private INotificationOverlay? notifications { get; set; }

        /// <summary>
        /// How long a file is from which it is not taken for a hitsound, in milliseconds. A song dropped here, where
        /// it would have been dropped on the editor to be the beatmap's, is not made a file of the set beside it.
        /// </summary>
        public const double TOO_LONG = 15000;

        /// <summary>
        /// How the length of a file dropped here is found, in milliseconds: null for one that is no sound.
        /// </summary>
        public Func<byte[], double?> LengthOf { get; set; } = SoundLength.Of;

        private BeatmapSetInfo set = null!;

        private ISampleStore? samples;

        private OsuSpriteText nothing = null!;
        private OsuSpriteText chooseOne = null!;
        private FillFlowContainer form = null!;
        private OsuSpriteText title = null!;
        private OsuSpriteText result = null!;
        private OsuSpriteText taken = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, OsuColour colours)
        {
            RelativeSizeAxes = Axes.Both;

            InternalChild = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.5f),
                    new Dimension(GridSizeMode.Absolute, 20),
                    new Dimension(),
                },
                Content = new[]
                {
                    new[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    // over what is said under the list.
                                    Padding = new MarginPadding { Bottom = 44 },
                                    Child = new OsuScrollContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Child = Rows = new FillFlowContainer<SoundRow>
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(4),
                                            // clear of the scrollbar.
                                            Padding = new MarginPadding { Right = 14 },
                                        },
                                    },
                                },
                                nothing = new OsuSpriteText
                                {
                                    Text = LazerKaiBeatmapFilesStrings.NoSounds,
                                    Colour = colourProvider.Foreground1,
                                    Alpha = 0,
                                },
                                // what is not to be guessed of a list: that files are dropped on it.
                                new OsuTextFlowContainer(t =>
                                {
                                    t.Font = OsuFont.Default.With(size: 12);
                                    t.Colour = colourProvider.Foreground1;
                                })
                                {
                                    Anchor = Anchor.BottomLeft,
                                    Origin = Anchor.BottomLeft,
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Text = LazerKaiBeatmapFilesStrings.DropHint,
                                },
                            },
                        },
                        Empty(),
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                chooseOne = new OsuSpriteText
                                {
                                    Text = LazerKaiBeatmapFilesStrings.ChooseASound,
                                    Colour = colourProvider.Foreground1,
                                },
                                form = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(8),
                                    Children = new Drawable[]
                                    {
                                        title = new TruncatingSpriteText
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Font = OsuFont.Default.With(size: 18, weight: FontWeight.SemiBold),
                                            Margin = new MarginPadding { Bottom = 6 },
                                        },
                                        Bank = new FormEnumDropdown<NamedBank>
                                        {
                                            Caption = LazerKaiBeatmapFilesStrings.SampleSet,
                                        },
                                        Sound = new FormEnumDropdown<NamedSound>
                                        {
                                            Caption = LazerKaiBeatmapFilesStrings.SoundKind,
                                        },
                                        // as the custom index of the hitsound screen is: a number to say, no wider than
                                        // a number, with arrows at its end that take it one up or down.
                                        new Container
                                        {
                                            Width = number_width,
                                            AutoSizeAxes = Axes.Y,
                                            Children = new Drawable[]
                                            {
                                                Number = new FormNumberBox
                                                {
                                                    Caption = LazerKaiBeatmapFilesStrings.CustomIndex,
                                                    HintText = LazerKaiBeatmapFilesStrings.CustomIndexHint,
                                                    Current = { Value = @"1" },
                                                },
                                                new FillFlowContainer
                                                {
                                                    Anchor = Anchor.CentreRight,
                                                    Origin = Anchor.CentreRight,
                                                    AutoSizeAxes = Axes.Both,
                                                    Direction = FillDirection.Vertical,
                                                    Margin = new MarginPadding { Right = 6 },
                                                    Children = new Drawable[]
                                                    {
                                                        NumberUp = new HitsoundPanel.IndexStepButton(1) { Action = () => stepNumber(1) },
                                                        NumberDown = new HitsoundPanel.IndexStepButton(-1) { Action = () => stepNumber(-1) },
                                                    },
                                                },
                                            },
                                        },
                                        result = new OsuSpriteText
                                        {
                                            Margin = new MarginPadding { Top = 6 },
                                        },
                                        taken = new OsuSpriteText
                                        {
                                            Font = OsuFont.Default.With(size: 14),
                                            Colour = colours.Orange1,
                                        },
                                        RenameButton = new RoundedButton
                                        {
                                            Width = 200,
                                            Height = 36,
                                            Margin = new MarginPadding { Top = 6 },
                                            Text = LazerKaiBeatmapFilesStrings.Rename,
                                            Action = rename,
                                        },
                                    },
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

            Bank.Current.BindValueChanged(_ => updateNewName());
            Sound.Current.BindValueChanged(_ => updateNewName());
            Number.Current.BindValueChanged(_ => updateNewName());

            Refresh();
        }

        /// <summary>
        /// Reads the sounds of the set again: they are other than they were once some have been brought in.
        /// </summary>
        public void Refresh()
        {
            set = BeatmapSetFiles.Read(realm, editorBeatmap.BeatmapInfo.BeatmapSet!.ID)!;

            // made again each time: a sound once played is kept by its name, which may be another sound's by now.
            samples?.Dispose();
            samples = audio.GetSampleStore(new BeatmapSetFiles(beatmaps, set));

            string[] sounds = BeatmapSetSounds.Of(set).Select(f => f.Filename).ToArray();

            if (Chosen != null && !sounds.Contains(Chosen))
                Chosen = null;

            Rows.ChildrenEnumerable = sounds.Select(name => new SoundRow(name)
            {
                Chosen = () => choose(name),
                Played = () => samples?.Get(name)?.Play(),
                IsChosen = name == Chosen,
            });

            nothing.Alpha = sounds.Length == 0 ? 1 : 0;

            showChosen();
        }

        private void choose(string name)
        {
            Chosen = name;

            foreach (var row in Rows)
                row.IsChosen = row.Filename == name;

            // one that is a hitsound already is shown as what it is. Any other leaves what was chosen for the one
            // before it, which the next of a set of sounds is most often one step from.
            if (HitsoundFileName.Of(name) is HitsoundFileName named)
            {
                Bank.Current.Value = (NamedBank)Array.IndexOf(HitsoundFileName.BANKS, named.Bank);
                Sound.Current.Value = (NamedSound)Array.IndexOf(HitsoundFileName.SOUNDS, named.Sound);
                Number.Current.Value = named.Index.ToString(CultureInfo.InvariantCulture);
            }

            showChosen();
        }

        private void showChosen()
        {
            chooseOne.Alpha = Chosen == null ? 1 : 0;
            form.Alpha = Chosen == null ? 0 : 1;

            title.Text = Chosen ?? string.Empty;

            updateNewName();
        }

        /// <summary>
        /// The number said, or null if none is: nothing said is the first, as the name with no number is.
        /// </summary>
        private int? number
        {
            get
            {
                string text = Number.Current.Value.Trim();

                if (text.Length == 0)
                    return 1;

                return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int said) && said >= 1 ? said : null;
            }
        }

        /// <summary>
        /// Takes the number one up or down, from the first if none is said. There is no custom set before the first.
        /// </summary>
        private void stepNumber(int by) => Number.Current.Value = Math.Max(1, (number ?? 1) + by).ToString(CultureInfo.InvariantCulture);

        private void updateNewName()
        {
            NewName = null;

            if (Chosen != null && number is int index)
                NewName = new HitsoundFileName(HitsoundFileName.BANKS[(int)Bank.Current.Value], HitsoundFileName.SOUNDS[(int)Sound.Current.Value], index).WithExtension(Path.GetExtension(Chosen));

            // the name it has already is no other name to give it.
            bool another = NewName != null && !string.Equals(NewName, Chosen, StringComparison.OrdinalIgnoreCase);

            var by = another ? BeatmapSetSounds.TakenBy(set, NewName!, Chosen!) : null;

            result.Text = NewName == null ? default : LazerKaiBeatmapFilesStrings.NewName(NewName);

            taken.Text = by == null ? default : LazerKaiBeatmapFilesStrings.NameTaken(by.Filename);
            taken.Alpha = by == null ? 0 : 1;

            RenameButton.Enabled.Value = another && by == null;
        }

        private void rename()
        {
            if (!RenameButton.Enabled.Value || Chosen == null || NewName == null)
                return;

            if (set.GetFile(Chosen) is not RealmNamedFileUsage file)
                return;

            string name = NewName;

            using (var stream = new BeatmapSetFiles(beatmaps, set).GetStream(Chosen))
            {
                if (stream == null)
                    return;

                beatmaps.AddFile(set, stream, name);
            }

            beatmaps.DeleteFile(set, file);

            Changed = true;
            Chosen = name;

            Refresh();
        }

        /// <summary>
        /// Makes files of sound dropped on the screen sounds of the set, under the names they have.
        /// </summary>
        /// <remarks>
        /// One the set has another sound under the name of is asked about first, as one brought in from another set is.
        /// One as long as a song is not taken, nor one that cannot be read as sound, nor one named as the set's song.
        /// </remarks>
        /// <param name="paths">Where the files are.</param>
        public void AddFiles(IReadOnlyList<string> paths)
        {
            var free = new List<(string name, byte[] contents)>();
            var taken = new List<(string name, byte[] contents)>();

            var tooLong = new List<string>();
            var notSound = new List<string>();
            var songs = new List<string>();

            string? thereAlready = null;

            foreach (string path in paths)
            {
                string name = Path.GetFileName(path);

                if (!BeatmapSetSounds.IsSound(name))
                    continue;

                byte[] contents;

                try
                {
                    contents = File.ReadAllBytes(path);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    notSound.Add(name);
                    continue;
                }

                double? length = LengthOf(contents);

                if (length == null)
                {
                    notSound.Add(name);
                    continue;
                }

                if (length >= TOO_LONG)
                {
                    tooLong.Add(name);
                    continue;
                }

                string hash;

                using (var stream = new MemoryStream(contents))
                    hash = stream.ComputeSHA2Hash();

                switch (BeatmapSetSounds.StandingOf(set, name, hash))
                {
                    case ImportStanding.New:
                        free.Add((name, contents));
                        break;

                    case ImportStanding.InPlaceOfAnother:
                        taken.Add((name, contents));
                        break;

                    case ImportStanding.NameOfTheSong:
                        songs.Add(name);
                        break;

                    case ImportStanding.ThereAlready:
                        thereAlready = name;
                        break;
                }
            }

            sayNotAdded(tooLong, LazerKaiBeatmapFilesStrings.TooLongToAdd);
            sayNotAdded(notSound, LazerKaiBeatmapFilesStrings.NotReadAsSound);
            sayNotAdded(songs, LazerKaiBeatmapFilesStrings.NameOfTheSongNotAdded);

            // the very file is not put in again, and is shown to be there.
            if (thereAlready != null && free.Count == 0 && taken.Count == 0 && set.GetFile(thereAlready) is RealmNamedFileUsage there)
                choose(there.Filename);

            put(free);

            if (taken.Count == 0)
                return;

            // a sound of the set is not put anything in place of without saying which: it cannot be had back.
            if (dialogOverlay != null)
                dialogOverlay.Push(new ConfirmDialog(LazerKaiBeatmapFilesStrings.OverwriteSounds(string.Join(@", ", taken.Select(t => t.name))), () => put(taken)));
            else
                put(taken);
        }

        private void sayNotAdded(IReadOnlyList<string> names, Func<string, LocalisableString> text)
        {
            if (names.Count == 0)
                return;

            notifications?.Post(new SimpleErrorNotification { Text = text(string.Join(@", ", names)) });
        }

        private void put(IReadOnlyList<(string name, byte[] contents)> sounds)
        {
            if (sounds.Count == 0)
                return;

            foreach (var (name, contents) in sounds)
            {
                using (var stream = new MemoryStream(contents))
                    BeatmapSetSounds.Put(beatmaps, set, name, stream);
            }

            Changed = true;

            notifications?.Post(new SimpleNotification
            {
                Icon = FontAwesome.Solid.FileImport,
                Text = LazerKaiBeatmapFilesStrings.SoundsAdded(sounds.Count),
            });

            Refresh();

            // the last of them is chosen, to be given its name next: by the name the set has it under, which for one
            // put in place of another is as that one's was written.
            if (set.GetFile(sounds[^1].name) is RealmNamedFileUsage last && Rows.Any(r => r.Filename == last.Filename))
                choose(last.Filename);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            samples?.Dispose();
        }

        /// <summary>
        /// The sample sets, in the order of <see cref="HitsoundFileName.BANKS"/> and written as they are in a name.
        /// </summary>
        public enum NamedBank
        {
            [Description(@"normal")]
            Normal,

            [Description(@"soft")]
            Soft,

            [Description(@"drum")]
            Drum,
        }

        /// <summary>
        /// The sounds of a sample set, in the order of <see cref="HitsoundFileName.SOUNDS"/> and written as they are
        /// in a name.
        /// </summary>
        public enum NamedSound
        {
            [Description(@"hitnormal")]
            HitNormal,

            [Description(@"hitwhistle")]
            HitWhistle,

            [Description(@"hitfinish")]
            HitFinish,

            [Description(@"hitclap")]
            HitClap,

            [Description(@"slidertick")]
            SliderTick,

            [Description(@"sliderslide")]
            SliderSlide,

            [Description(@"sliderwhistle")]
            SliderWhistle,
        }

        /// <summary>
        /// One sound of the set: clicked to be given a name, and played to be heard.
        /// </summary>
        public partial class SoundRow : OsuClickableContainer
        {
            public readonly string Filename;

            public Action? Chosen { get; init; }

            public Action? Played { get; init; }

            public IconButton PlayButton { get; private set; } = null!;

            private bool isChosen;

            public bool IsChosen
            {
                get => isChosen;
                set
                {
                    isChosen = value;

                    if (IsLoaded)
                        updateColour();
                }
            }

            private Box background = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public SoundRow(string filename)
            {
                Filename = filename;

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
                    background = new Box { RelativeSizeAxes = Axes.Both },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding { Horizontal = 8, Vertical = 6 },
                        Child = new GridContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.Absolute, 28),
                                new Dimension(),
                            },
                            RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    PlayButton = new IconButton
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(20),
                                        IconScale = new Vector2(0.55f),
                                        Icon = FontAwesome.Solid.Play,
                                        TooltipText = LazerKaiBeatmapFilesStrings.PlaySound,
                                        Action = () => Played?.Invoke(),
                                    },
                                    new TruncatingSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        RelativeSizeAxes = Axes.X,
                                        Text = Filename,
                                    },
                                },
                            },
                        },
                    },
                };

                updateColour();
            }

            private void updateColour() =>
                background.FadeColour(isChosen ? colourProvider.Background1 : IsHovered ? colourProvider.Background2 : colourProvider.Background3, 100, Easing.OutQuint);

            protected override bool OnHover(HoverEvent e)
            {
                updateColour();
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                updateColour();
                base.OnHoverLost(e);
            }
        }
    }
}
