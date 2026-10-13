// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Notifications;
using osuTK;

namespace osu.Game.Screens.Edit.BeatmapFiles
{
    /// <summary>
    /// Where difficulties and sounds of another beatmap set are brought into the one being edited: the sets of the game
    /// on the left, to be searched, and what the chosen one has on the right, to be ticked (2026-10-13).
    /// </summary>
    public partial class ImportFromAnotherSet : CompositeDrawable
    {
        /// <summary>
        /// How many sets are listed at once. A mapper has thousands, and is looking for one by its name.
        /// </summary>
        public const int MOST_LISTED = 100;

        /// <summary>
        /// How long the list waits after a key before it is searched, for a word to be typed whole.
        /// </summary>
        private const double search_delay = 150;

        /// <summary>
        /// Whether anything has been brought in: the editor then has to open the set again to have it.
        /// </summary>
        public bool Changed { get; private set; }

        public SearchTextBox SearchBox { get; private set; } = null!;

        public FillFlowContainer<SetRow> SetRows { get; private set; } = null!;

        public RoundedButton ImportButton { get; private set; } = null!;

        /// <summary>
        /// Ticks every sound of the chosen set that can be brought in, those that would be put in place of another too.
        /// </summary>
        public RoundedButton? TickAllButton { get; private set; }

        public RoundedButton? TickNoneButton { get; private set; }

        public IEnumerable<DifficultyRow> DifficultyRows => contents.OfType<DifficultyRow>();

        public IEnumerable<SoundRow> SoundRows => contents.OfType<SoundRow>();

        /// <summary>
        /// Whether the sets of the game have been read.
        /// </summary>
        public bool SetsListed => sets != null;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved]
        private AudioManager audio { get; set; } = null!;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private IDialogOverlay? dialogOverlay { get; set; }

        [Resolved(canBeNull: true)]
        private INotificationOverlay? notifications { get; set; }

        private IReadOnlyList<ListedSet>? sets;

        /// <summary>
        /// The set chosen on the left, with its difficulties and its files.
        /// </summary>
        private BeatmapSetInfo? source;

        /// <summary>
        /// The sounds of <see cref="source"/>, to be heard before they are brought in.
        /// </summary>
        private ISampleStore? previews;

        private LoadingSpinner loading = null!;
        private OsuSpriteText nothingFound = null!;
        private OsuSpriteText listNote = null!;
        private FillFlowContainer contents = null!;
        private ScheduledDelegate? search;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChild = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Relative, 0.4f),
                    new Dimension(GridSizeMode.Absolute, 20),
                    new Dimension(),
                },
                Content = new[]
                {
                    new[]
                    {
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            RowDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension(GridSizeMode.Absolute, 10),
                                new Dimension(),
                                new Dimension(GridSizeMode.AutoSize),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    SearchBox = new SearchTextBox
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        PlaceholderText = LazerKaiBeatmapFilesStrings.SearchSets,
                                    },
                                },
                                new[] { Empty() },
                                new Drawable[]
                                {
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new Drawable[]
                                        {
                                            new OsuScrollContainer
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Child = SetRows = new FillFlowContainer<SetRow>
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(4),
                                                },
                                            },
                                            nothingFound = new OsuSpriteText
                                            {
                                                Text = LazerKaiBeatmapFilesStrings.NoSetsFound,
                                                Colour = colourProvider.Foreground1,
                                                Alpha = 0,
                                            },
                                            loading = new LoadingSpinner
                                            {
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre,
                                                State = { Value = Visibility.Visible },
                                            },
                                        },
                                    },
                                },
                                new Drawable[]
                                {
                                    listNote = new OsuSpriteText
                                    {
                                        Colour = colourProvider.Foreground1,
                                        Font = OsuFont.Default.With(size: 12),
                                        Margin = new MarginPadding { Top = 6 },
                                    },
                                },
                            },
                        },
                        Empty(),
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            RowDimensions = new[]
                            {
                                new Dimension(),
                                new Dimension(GridSizeMode.Absolute, 10),
                                new Dimension(GridSizeMode.AutoSize),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new OsuScrollContainer
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Child = contents = new FillFlowContainer
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
                                new[] { Empty() },
                                new Drawable[]
                                {
                                    ImportButton = new RoundedButton
                                    {
                                        Anchor = Anchor.TopRight,
                                        Origin = Anchor.TopRight,
                                        Width = 300,
                                        Height = 36,
                                        Action = import,
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

            SearchBox.Current.BindValueChanged(_ =>
            {
                search?.Cancel();
                search = Scheduler.AddDelayed(listSets, search_delay);
            });

            showContents();
            readSets();
        }

        /// <summary>
        /// Reads the sets of the game for the list. Away from the screen: there may be thousands.
        /// </summary>
        private void readSets()
        {
            Guid edited = editorBeatmap.BeatmapInfo.BeatmapSet!.ID;

            Task.Run(() => realm.Run(r => r.All<BeatmapSetInfo>()
                                           .Where(s => !s.DeletePending && !s.Protected)
                                           .AsEnumerable()
                                           // a set with no difficulty has nothing to read its files by, nor a name to be found by.
                                           .Where(s => s.ID != edited && s.Beatmaps.Count > 0)
                                           .Select(ListedSet.Of)
                                           .OrderByDescending(s => s.Added)
                                           .ToArray()))
                .ContinueWith(read =>
                {
                    if (read.IsFaulted)
                    {
                        Logger.Error(read.Exception, @"Failed to read the beatmap sets to import from.");
                        return;
                    }

                    Schedule(() =>
                    {
                        sets = read.GetResultSafely();
                        loading.Hide();
                        listSets();
                    });
                });
        }

        private void listSets()
        {
            if (sets == null)
                return;

            string[] terms = SearchBox.Current.Value.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var found = sets.Where(s => terms.All(t => s.Terms.Contains(t, StringComparison.Ordinal))).ToArray();

            SetRows.ChildrenEnumerable = found.Take(MOST_LISTED).Select(s => new SetRow(s)
            {
                Chosen = () => choose(s.ID),
                IsChosen = s.ID == source?.ID,
            });

            nothingFound.Alpha = found.Length == 0 ? 1 : 0;
            listNote.Text = found.Length > MOST_LISTED ? LazerKaiBeatmapFilesStrings.MoreSets(found.Length - MOST_LISTED) : default;
        }

        private void choose(Guid id)
        {
            source = BeatmapSetFiles.Read(realm, id);

            previews?.Dispose();
            previews = source == null ? null : audio.GetSampleStore(new BeatmapSetFiles(beatmaps, source));

            foreach (var row in SetRows)
                row.IsChosen = row.Set.ID == id;

            showContents();
        }

        /// <summary>
        /// Shows what the chosen set has again: what bringing a sound in would do is another thing once the sounds of
        /// the set being edited have been given other names.
        /// </summary>
        public void Refresh() => showContents();

        /// <summary>
        /// Shows what the chosen set has: its difficulties, to be ticked, and its sounds, ticked already where the set
        /// being edited has none of their names.
        /// </summary>
        private void showContents()
        {
            contents.Clear();

            if (source == null)
            {
                contents.Add(new OsuSpriteText
                {
                    Text = LazerKaiBeatmapFilesStrings.ChooseASet,
                    Colour = colourProvider.Foreground1,
                });

                updateImportButton();
                return;
            }

            // read afresh: what was brought in a moment ago is there already now.
            var edited = BeatmapSetFiles.Read(realm, editorBeatmap.BeatmapInfo.BeatmapSet!.ID)!;

            contents.Add(new OsuSpriteText
            {
                Text = source.Metadata.GetDisplayTitleRomanisable(),
                Font = OsuFont.Default.With(size: 18, weight: FontWeight.SemiBold),
                Margin = new MarginPadding { Bottom = 6 },
            });

            contents.Add(heading(LazerKaiBeatmapFilesStrings.Difficulties));

            foreach (var difficulty in source.Beatmaps.OrderBy(b => b.Ruleset.OnlineID).ThenBy(b => b.StarRating))
                contents.Add(new DifficultyRow(difficulty) { Ticked = updateImportButton });

            var sounds = BeatmapSetSounds.ToBringInto(edited, source);

            contents.Add(new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Margin = new MarginPadding { Top = 10 },
                Children = new Drawable[]
                {
                    heading(LazerKaiBeatmapFilesStrings.Sounds).With(d =>
                    {
                        d.Anchor = Anchor.CentreLeft;
                        d.Origin = Anchor.CentreLeft;
                    }),
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(6, 0),
                        Alpha = sounds.Any(s => s.CanBeBroughtIn) ? 1 : 0,
                        Children = new Drawable[]
                        {
                            TickAllButton = new RoundedButton
                            {
                                Size = new Vector2(100, 24),
                                Text = LazerKaiBeatmapFilesStrings.SelectAll,
                                Action = () => tickSounds(true),
                            },
                            TickNoneButton = new RoundedButton
                            {
                                Size = new Vector2(100, 24),
                                Text = LazerKaiBeatmapFilesStrings.SelectNone,
                                Action = () => tickSounds(false),
                            },
                        },
                    },
                },
            });

            if (sounds.Count == 0)
            {
                contents.Add(new OsuSpriteText
                {
                    Text = LazerKaiBeatmapFilesStrings.NoSounds,
                    Colour = colourProvider.Foreground1,
                });
            }

            foreach (var sound in sounds)
            {
                contents.Add(new SoundRow(sound)
                {
                    Ticked = updateImportButton,
                    Played = () => previews?.Get(sound.Name)?.Play(),
                });
            }

            updateImportButton();
        }

        private static OsuSpriteText heading(LocalisableString text) => new OsuSpriteText
        {
            Text = text,
            Font = OsuFont.Default.With(size: 16, weight: FontWeight.SemiBold),
        };

        private void tickSounds(bool ticked)
        {
            foreach (var row in SoundRows.Where(r => r.Sound.CanBeBroughtIn))
                row.Box.Current.Value = ticked;
        }

        private BeatmapInfo[] tickedDifficulties => DifficultyRows.Where(r => r.Box.Current.Value).Select(r => r.Difficulty).ToArray();

        private ImportableSound[] tickedSounds => SoundRows.Where(r => r.Box.Current.Value).Select(r => r.Sound).ToArray();

        private void updateImportButton()
        {
            int difficulties = tickedDifficulties.Length;
            int sounds = tickedSounds.Length;

            ImportButton.Text = LazerKaiBeatmapFilesStrings.Import(difficulties, sounds);
            ImportButton.Enabled.Value = difficulties + sounds > 0;
        }

        private void import()
        {
            var difficulties = tickedDifficulties;
            var sounds = tickedSounds;

            if (source == null || difficulties.Length + sounds.Length == 0)
                return;

            string[] overwritten = sounds.Where(s => s.Standing == ImportStanding.InPlaceOfAnother).Select(s => s.Name).ToArray();

            // a sound of the set being edited is not put anything in place of without saying which: it cannot be had back.
            if (overwritten.Length > 0 && dialogOverlay != null)
            {
                dialogOverlay.Push(new ConfirmDialog(LazerKaiBeatmapFilesStrings.OverwriteSounds(string.Join(@", ", overwritten)), () => bringIn(difficulties, sounds)));
                return;
            }

            bringIn(difficulties, sounds);
        }

        private void bringIn(BeatmapInfo[] difficulties, ImportableSound[] sounds)
        {
            if (source == null)
                return;

            var imported = new BeatmapSetImporter(beatmaps).Import(editorBeatmap.BeatmapInfo, source, difficulties, sounds);

            Changed = true;

            notifications?.Post(new SimpleNotification
            {
                Icon = FontAwesome.Solid.FileImport,
                Text = LazerKaiBeatmapFilesStrings.Imported(imported.Difficulties.Count, imported.Sounds.Count),
            });

            showContents();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            previews?.Dispose();
        }

        /// <summary>
        /// A set of the game as the list has it: what it is called, and what it is found by.
        /// </summary>
        public record ListedSet(Guid ID, RomanisableString Title, string Creator, int Difficulties, string Terms, DateTimeOffset Added)
        {
            public static ListedSet Of(BeatmapSetInfo set)
            {
                var metadata = set.Metadata;

                string terms = string.Join(' ', metadata.Artist, metadata.ArtistUnicode, metadata.Title, metadata.TitleUnicode, metadata.Author.Username, metadata.Source, metadata.Tags);

                return new ListedSet(set.ID, metadata.GetDisplayTitleRomanisable(false), metadata.Author.Username, set.Beatmaps.Count, terms.ToLowerInvariant(), set.DateAdded);
            }
        }

        /// <summary>
        /// One set in the list, clicked to see what it has.
        /// </summary>
        public partial class SetRow : OsuClickableContainer
        {
            public readonly ListedSet Set;

            public Action? Chosen { get; init; }

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

            public SetRow(ListedSet set)
            {
                Set = set;

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
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding { Horizontal = 8, Vertical = 6 },
                        Spacing = new Vector2(2),
                        Children = new Drawable[]
                        {
                            new TruncatingSpriteText
                            {
                                RelativeSizeAxes = Axes.X,
                                Text = Set.Title,
                                Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold),
                            },
                            new TruncatingSpriteText
                            {
                                RelativeSizeAxes = Axes.X,
                                Text = LazerKaiBeatmapFilesStrings.SetSummary(Set.Creator, Set.Difficulties),
                                Font = OsuFont.Default.With(size: 12),
                                Colour = colourProvider.Foreground1,
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

        /// <summary>
        /// One difficulty of the chosen set, ticked to be brought in.
        /// </summary>
        public partial class DifficultyRow : CompositeDrawable
        {
            public readonly BeatmapInfo Difficulty;

            public Action? Ticked { get; init; }

            public readonly OsuCheckbox Box;

            public DifficultyRow(BeatmapInfo difficulty)
            {
                Difficulty = difficulty;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                InternalChild = Box = new OsuCheckbox(nubOnRight: false)
                {
                    // the ruleset as well: a set hitsounded in osu!mania has a difficulty of each for the one name.
                    LabelText = $@"{Difficulty.DifficultyName}  ({Difficulty.Ruleset.Name})",
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                Box.Current.BindValueChanged(_ => Ticked?.Invoke());
            }
        }

        /// <summary>
        /// One sound of the chosen set: ticked to be brought in, and played to be heard first.
        /// </summary>
        public partial class SoundRow : CompositeDrawable
        {
            public readonly ImportableSound Sound;

            public Action? Ticked { get; init; }

            public Action? Played { get; init; }

            public readonly OsuCheckbox Box;

            public IconButton PlayButton { get; private set; } = null!;

            public SoundRow(ImportableSound sound)
            {
                Sound = sound;

                Box = new OsuCheckbox(nubOnRight: false)
                {
                    LabelText = Sound.Name,
                    Current = { Value = Sound.Standing == ImportStanding.New },
                };

                // one that is there already has nothing to bring, and is listed for it to be seen that it is there.
                if (!Sound.CanBeBroughtIn)
                    Box.Current.Disabled = true;
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider, OsuColour colours)
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                InternalChild = new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute, 26),
                        new Dimension(),
                    },
                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            // beside the name, which is the first line of a row that has a note under it.
                            PlayButton = new IconButton
                            {
                                Size = new Vector2(20),
                                IconScale = new Vector2(0.55f),
                                Icon = FontAwesome.Solid.Play,
                                TooltipText = LazerKaiBeatmapFilesStrings.PlaySound,
                                Action = () => Played?.Invoke(),
                            },
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    Box,
                                    new OsuSpriteText
                                    {
                                        Text = noteOf(Sound.Standing),
                                        Font = OsuFont.Default.With(size: 12),
                                        Colour = Sound.Standing == ImportStanding.InPlaceOfAnother ? colours.Orange1 : colourProvider.Foreground1,
                                        Alpha = Sound.Standing == ImportStanding.New ? 0 : 1,
                                        // under the name, from where the name starts.
                                        Margin = new MarginPadding { Left = 62, Top = 1, Bottom = 5 },
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

                Box.Current.BindValueChanged(_ => Ticked?.Invoke());
            }

            private static LocalisableString noteOf(ImportStanding standing)
            {
                switch (standing)
                {
                    case ImportStanding.ThereAlready:
                        return LazerKaiBeatmapFilesStrings.ThereAlready;

                    case ImportStanding.InPlaceOfAnother:
                        return LazerKaiBeatmapFilesStrings.InPlaceOfAnother;

                    case ImportStanding.NameOfTheSong:
                        return LazerKaiBeatmapFilesStrings.NameOfTheSong;

                    default:
                        return default;
                }
            }
        }
    }
}
