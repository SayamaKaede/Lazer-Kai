// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.IO.Serialization;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Screens.Edit.Hitsounds;

namespace osu.Game.Screens.Edit.Compose
{
    public partial class ComposeScreen : EditorScreenWithTimeline, IGameplaySettings
    {
        [Resolved]
        private Clipboard hostClipboard { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; }

        [Resolved]
        private IGameplaySettings globalGameplaySettings { get; set; }

        [Resolved]
        private IBeatSnapProvider beatSnapProvider { get; set; }

        private Bindable<string> clipboard { get; set; }

        private HitObjectComposer composer;

        public ComposeScreen()
            : base(EditorScreenMode.Compose)
        {
        }

        private Ruleset ruleset;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

            ruleset = parent.Get<IBindable<WorkingBeatmap>>().Value.BeatmapInfo.Ruleset.CreateInstance();
            composer = ruleset?.CreateHitObjectComposer();
            laneSounds = parent.Get<LaneSounds>();

            // make the composer available to the timeline and other components in this screen.
            if (composer != null)
                dependencies.CacheAs(composer);

            return dependencies;
        }

        protected override Drawable CreateMainContent()
        {
            if (ruleset == null || composer == null)
                return new ScreenWhiteBox.UnderConstructionMessage(ruleset == null ? "This beatmap" : $"{ruleset.Description}'s composer");

            return wrapSkinnableContent(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new ComposerHolder(showingHitsounds)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = composer,
                    },
                    // a difficulty made to hitsound in has its lanes to set up there instead.
                    laneSounds != null ? new LaneSoundPanel(showingHitsounds) : (Drawable)(HitsoundPanel = new HitsoundPanel(showingHitsounds)),
                }
            });
        }

        /// <summary>
        /// What takes the playfield's place on the hitsound screen. Null on a difficulty made to hitsound in, which has
        /// a <see cref="LaneSoundPanel"/> there.
        /// </summary>
        public HitsoundPanel HitsoundPanel { get; private set; }

        /// <summary>
        /// The sounds of the lanes, on a difficulty made to hitsound in. Taken as the dependencies are made: the content
        /// is made by the class this is of, before anything of this one's is resolved.
        /// </summary>
        private LaneSounds laneSounds;

        /// <summary>
        /// Whether this screen is standing in for the hitsound screen, which it is the compose screen of.
        /// </summary>
        private readonly BindableBool showingHitsounds = new BindableBool();

        [Resolved(canBeNull: true)]
        private Editor editor { get; set; }

        private IBindable<EditorScreenMode> editorMode;

        /// <summary>
        /// Keeps the composer from being clicked or keyed while the hitsound screen is over it. It goes on running all the
        /// same, for the timeline and the selection.
        /// </summary>
        private partial class ComposerHolder : Container
        {
            private readonly IBindable<bool> blocked;

            public ComposerHolder(IBindable<bool> blocked)
            {
                this.blocked = blocked.GetBoundCopy();
            }

            public override bool PropagatePositionalInputSubTree => !blocked.Value && base.PropagatePositionalInputSubTree;

            public override bool PropagateNonPositionalInputSubTree => !blocked.Value && base.PropagateNonPositionalInputSubTree;
        }

        protected override Drawable CreateTimelineContent()
        {
            if (ruleset == null || composer == null)
                return base.CreateTimelineContent();

            TimelineBreakDisplay breakDisplay = new TimelineBreakDisplay
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Height = 0.75f,
            };

            return wrapSkinnableContent(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[]
                {
                    // We want to display this below hitobjects to better expose placement objects visually.
                    // It needs to be above the blueprint container to handle drags on breaks though.
                    breakDisplay.CreateProxy(),
                    createTimelineBlueprints(),
                    breakDisplay
                }
            });
        }

        /// <summary>
        /// The timeline's objects, whose selection of sound points the hitsound screen changes: a slider's tail or a
        /// reverse can be selected on its own there.
        /// </summary>
        private TimelineBlueprintContainer createTimelineBlueprints()
        {
            var blueprints = new TimelineBlueprintContainer(composer);

            if (HitsoundPanel != null)
                HitsoundPanel.SamplePoints = blueprints.SamplePoints;

            return blueprints;
        }

        private Drawable wrapSkinnableContent(Drawable content)
        {
            Debug.Assert(ruleset != null);

            // the lanes of a difficulty made to hitsound in are files of the hitsound folder, which its notes are to find
            // before anything the beatmap or a skin has.
            if (laneSounds != null)
                content = new LaneSoundSkinProvidingContainer(laneSounds).WithChild(content);

            return new EditorSkinProvidingContainer(EditorBeatmap).WithChild(content);
        }

        [BackgroundDependencyLoader]
        private void load(EditorClipboard clipboard)
        {
            this.clipboard = clipboard.Content.GetBoundCopy();
            copiedHitsounds = clipboard.Hitsounds.GetBoundCopy();
        }

        private IBindable<IReadOnlyList<CopiedHitsound>> copiedHitsounds;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // May be null in the case of a ruleset that doesn't have editor support, see CreateMainContent().
            if (composer == null)
                return;

            if (editor != null)
            {
                editorMode = editor.Mode.GetBoundCopy();
                editorMode.BindValueChanged(mode => showingHitsounds.Value = mode.NewValue == EditorScreenMode.Hitsounds, true);
            }

            showingHitsounds.BindValueChanged(s => composer.ToolboxesShown.Value = !s.NewValue, true);

            EditorBeatmap.SelectedHitObjects.CollectionChanged += selectionChanged;
            clipboard.BindValueChanged(_ => updateClipboardActionAvailability());
            copiedHitsounds.BindValueChanged(_ => updateClipboardActionAvailability());
            showingHitsounds.BindValueChanged(_ => updateClipboardActionAvailability());
            composer.OnLoadComplete += _ => updateClipboardActionAvailability();
            updateClipboardActionAvailability();
        }

        private void selectionChanged(object sender, NotifyCollectionChangedEventArgs e) => updateClipboardActionAvailability();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            // the editor rebuilds this screen while keeping its beatmap; a subscription left behind would hold the
            // whole screen for the rest of the session.
            if (EditorBeatmap.IsNotNull())
                EditorBeatmap.SelectedHitObjects.CollectionChanged -= selectionChanged;
        }

        #region Clipboard operations

        // on the hitsound screen, the hitsounds are what is copied and pasted, and nothing is cut (2026-10-08).

        public override void Cut()
        {
            if (!CanCut.Value || showingHitsounds.Value)
                return;

            Copy();
            EditorBeatmap.RemoveRange(EditorBeatmap.SelectedHitObjects.ToArray());
        }

        public override void Copy()
        {
            if (showingHitsounds.Value)
            {
                hostClipboard.SetText(getTimestamp());
                HitsoundPanel?.CopyHitsounds();
                return;
            }

            // on stable, pressing Ctrl-C would copy the current timestamp to system clipboard
            // regardless of whether anything was even selected at all.
            // UX-wise this is generally strange and unexpected, but make it work anyways to preserve muscle memory.
            // note that this means that `getTimestamp()` must handle no-selection case, too.
            // additionally, note we're intentionally not using `OsuGame.CopyToClipboard()`
            // because we do not want toasts to pop up on every Ctrl-C press - it'd be disruptive to mappers.
            hostClipboard.SetText(getTimestamp());

            if (CanCopy.Value)
                clipboard.Value = new ClipboardContent(EditorBeatmap).Serialize();
        }

        public override void Paste()
        {
            // with nothing copied too, which the hitsound screen says.
            if (showingHitsounds.Value)
            {
                HitsoundPanel?.PasteHitsounds(true);
                return;
            }

            if (!CanPaste.Value)
                return;

            var objects = clipboard.Value.Deserialize<ClipboardContent>().HitObjects;

            Debug.Assert(objects.Any());

            double timeOffset = beatSnapProvider.SnapTime(clock.CurrentTime) - objects.Min(o => o.StartTime);

            foreach (var h in objects)
                h.StartTime += timeOffset;

            EditorBeatmap.BeginChange();

            EditorBeatmap.SelectedHitObjects.Clear();

            EditorBeatmap.AddRange(objects);
            EditorBeatmap.SelectedHitObjects.AddRange(objects);

            EditorBeatmap.EndChange();
        }

        private void updateClipboardActionAvailability()
        {
            if (showingHitsounds.Value)
            {
                // what is at the playhead is copied with nothing selected.
                CanCut.Value = false;
                CanCopy.Value = true;
                CanPaste.Value = copiedHitsounds.Value.Count > 0;
                return;
            }

            CanCut.Value = CanCopy.Value = EditorBeatmap.SelectedHitObjects.Any();
            CanPaste.Value = composer.IsLoaded && !composer.EditingLocked && !string.IsNullOrEmpty(clipboard.Value);
        }

        private string getTimestamp()
        {
            if (composer == null)
                return string.Empty;

            double displayTime = EditorBeatmap.SelectedHitObjects.MinBy(h => h.StartTime)?.StartTime ?? clock.CurrentTime;
            string selectionAsString = composer.ConvertSelectionToString();

            return !string.IsNullOrEmpty(selectionAsString)
                ? $"{displayTime.ToEditorFormattedString()} ({selectionAsString}) - "
                : $"{displayTime.ToEditorFormattedString()} - ";
        }

        #endregion

        // Combo colour normalisation should not be applied in the editor.
        // Note this doesn't affect editor test mode.
        IBindable<float> IGameplaySettings.ComboColourNormalisationAmount => new Bindable<float>();

        // Arguable.
        IBindable<float> IGameplaySettings.PositionalHitsoundsLevel => globalGameplaySettings.PositionalHitsoundsLevel;
    }
}
