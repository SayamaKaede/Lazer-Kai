// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Play;
using osu.Game.Users;
using osu.Game.Utils;
using WebCommonStrings = osu.Game.Resources.Localisation.Web.CommonStrings;

namespace osu.Game.Screens.Select
{
    public partial class SoloSongSelect : SongSelect
    {
        protected override UserActivity InitialActivity => new UserActivity.ChoosingBeatmap();

        private PlayerLoader? playerLoader;
        private IReadOnlyList<Mod>? modsAtGameplayStart;

        [Resolved]
        private BeatmapSetOverlay? beatmapOverlay { get; set; }

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }

        [Resolved]
        private OsuGame? game { get; set; }

        private Sample? sampleConfirmSelection { get; set; }

        private DifficultyModStore difficultyMods = null!;

        /// <summary>
        /// The difficulty whose mods were last looked up, so that coming back to the same one (from gameplay, say) leaves
        /// alone whatever was changed on top of them.
        /// </summary>
        private BeatmapInfo? difficultyModsLookedUpFor;

        /// <summary>
        /// The mods selected before a difficulty's own were put on, to go back to on one without any.
        /// </summary>
        private IReadOnlyList<Mod>? modsBeforeDifficultyMods;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.Cache(difficultyMods = new DifficultyModStore(parent.Get<Storage>()));
            return dependencies;
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            sampleConfirmSelection = audio.Samples.Get(@"SongSelect/confirm-selection");

            AddInternal(new SongSelectTouchInputDetector());
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Beatmap.BindValueChanged(_ => putOnDifficultyMods());
            Ruleset.BindValueChanged(_ =>
            {
                // what was selected before is of the other ruleset, and the game has already carried the mods across.
                modsBeforeDifficultyMods = null;
                difficultyModsLookedUpFor = null;
                Schedule(putOnDifficultyMods);
            });
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            putOnDifficultyMods();
        }

        public override IEnumerable<OsuMenuItem> GetDifficultyModActions(BeatmapInfo beatmap)
        {
            yield return new OsuMenuItemSpacer();

            yield return new OsuMenuItem(LazerKaiSongSelectStrings.SetModsOnDifficulty, MenuItemType.Standard, () => setDifficultyMods(beatmap))
            {
                Icon = FontAwesome.Solid.Thumbtack,
                Action = { Disabled = Mods.Value.Count == 0 },
            };

            if (difficultyMods.Get(beatmap, Ruleset.Value.CreateInstance()) != null)
                yield return new OsuMenuItem(LazerKaiSongSelectStrings.ClearDifficultyMods, MenuItemType.Standard, () => difficultyMods.Clear(beatmap));
        }

        private void setDifficultyMods(BeatmapInfo beatmap)
        {
            difficultyMods.Set(beatmap, Ruleset.Value.CreateInstance(), Mods.Value);

            // they are already on, and what was selected before them is what they are.
            if (beatmap.Equals(Beatmap.Value.BeatmapInfo))
                difficultyModsLookedUpFor = beatmap;
        }

        /// <summary>
        /// Puts on the mods set on the selected difficulty, or puts back what was selected before on one without any.
        /// </summary>
        private void putOnDifficultyMods()
        {
            if (!this.IsCurrentScreen() || Beatmap.IsDefault)
                return;

            var beatmap = Beatmap.Value.BeatmapInfo;

            if (beatmap.Equals(difficultyModsLookedUpFor))
                return;

            difficultyModsLookedUpFor = beatmap;

            var mods = difficultyMods.Get(beatmap, Ruleset.Value.CreateInstance());

            if (mods != null)
            {
                modsBeforeDifficultyMods ??= Mods.Value;
                Mods.Value = mods;
            }
            else if (modsBeforeDifficultyMods != null)
            {
                Mods.Value = modsBeforeDifficultyMods;
                modsBeforeDifficultyMods = null;
            }
        }

        public override IEnumerable<OsuMenuItem> GetForwardActions(BeatmapInfo beatmap)
        {
            yield return new OsuMenuItem(ButtonSystemStrings.Play.ToSentence(), MenuItemType.Highlighted, () => SelectAndRun(beatmap, OnStart)) { Icon = FontAwesome.Solid.Check };
            yield return new OsuMenuItem(ButtonSystemStrings.Edit.ToSentence(), MenuItemType.Standard, () => Edit(beatmap)) { Icon = FontAwesome.Solid.PencilAlt };

            yield return new OsuMenuItemSpacer();

            if (beatmap.OnlineID > 0)
            {
                yield return new OsuMenuItem(CommonStrings.Details, MenuItemType.Standard, () => beatmapOverlay?.FetchAndShowBeatmap(beatmap.OnlineID));

                if (beatmap.GetOnlineURL(api, Ruleset.Value) is string url)
                    yield return new OsuMenuItem(CommonStrings.CopyLink, MenuItemType.Standard, () => game?.CopyToClipboard(url));

                yield return new OsuMenuItemSpacer();
            }

            foreach (var i in CreateCollectionMenuActions(beatmap))
                yield return i;

            if (beatmap.LastPlayed == null)
                yield return new OsuMenuItem(SongSelectStrings.MarkAsPlayed, MenuItemType.Standard, () => beatmaps.MarkPlayed(beatmap)) { Icon = FontAwesome.Solid.TimesCircle };
            else
                yield return new OsuMenuItem(SongSelectStrings.RemoveFromPlayed, MenuItemType.Standard, () => beatmaps.MarkNotPlayed(beatmap)) { Icon = FontAwesome.Solid.TimesCircle };

            yield return new OsuMenuItem(SongSelectStrings.ClearAllLocalScores, MenuItemType.Standard, () => dialogOverlay?.Push(new BeatmapClearScoresDialog(beatmap)))
            {
                Icon = FontAwesome.Solid.Eraser
            };

            if (beatmaps.CanHide(beatmap))
                yield return new OsuMenuItem(WebCommonStrings.ButtonsHide.ToSentence(), MenuItemType.Destructive, () => beatmaps.Hide(beatmap));
        }

        protected override void OnStart()
        {
            if (playerLoader != null) return;

            modsAtGameplayStart = Mods.Value.Select(m => m.DeepClone()).ToArray();

            // Ctrl+Enter should start map with autoplay enabled.
            if (GetContainingInputManager()?.CurrentState?.Keyboard.ControlPressed == true)
            {
                var autoInstance = getAutoplayMod();

                if (autoInstance == null)
                {
                    notifications?.Post(new SimpleNotification
                    {
                        Text = NotificationsStrings.NoAutoplayMod
                    });
                    return;
                }

                var mods = Mods.Value.Append(autoInstance).ToArray();

                if (!ModUtils.CheckCompatibleSet(mods, out var invalid))
                    mods = mods.Except(invalid).Append(autoInstance).ToArray();

                Mods.Value = mods;
            }

            sampleConfirmSelection?.Play();

            this.Push(playerLoader = new PlayerLoader(createPlayer));

            Player createPlayer()
            {
                Player player;

                var replayGeneratingMod = Mods.Value.OfType<ICreateReplayData>().FirstOrDefault();

                if (replayGeneratingMod != null)
                {
                    player = new ReplayPlayer(replayGeneratingMod.CreateScoreFromReplayData);
                }
                else
                {
                    player = new SoloPlayer();
                }

                return player;
            }
        }

        public void Edit(BeatmapInfo beatmap)
        {
            if (!this.IsCurrentScreen())
                return;

            SelectAndRun(beatmap, () => this.Push(new EditorLoader()));
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);
            revertMods();

            // the editor can have moved to another difficulty.
            putOnDifficultyMods();
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            if (base.OnExiting(e))
                return true;

            revertMods();
            return false;
        }

        private ModAutoplay? getAutoplayMod() => Ruleset.Value.CreateInstance().GetAutoplayMod();

        private void revertMods()
        {
            if (playerLoader == null) return;

            Mods.Value = modsAtGameplayStart;
            playerLoader = null;
        }

        private partial class PlayerLoader : Play.PlayerLoader
        {
            public override bool ShowFooter => !QuickRestart;

            public PlayerLoader(Func<Player> createPlayer)
                : base(createPlayer)
            {
            }
        }
    }
}
