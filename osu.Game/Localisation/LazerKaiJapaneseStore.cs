// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Supplies Japanese text for the strings added by this fork, falling back to the game's own localisations
    /// for everything else.
    /// </summary>
    /// <remarks>
    /// osu!'s translations live in the separate <c>osu.Game.Resources</c> package, so strings added here have no
    /// entry there and would otherwise always show their English fallback. Wrapping the upstream store keeps the
    /// language setting in control: pick Japanese and these follow, pick anything else and they do not.
    ///
    /// Wording is kept short and noun-led, the way the rest of a settings list reads, and uses the words a mapper
    /// would use out loud — ノーツ for hit objects, アンカー for path control points.
    /// </remarks>
    public class LazerKaiJapaneseStore : ILocalisationStore
    {
        private readonly ILocalisationStore fallback;

        public CultureInfo EffectiveCulture => fallback.EffectiveCulture;

        private static readonly IReadOnlyDictionary<string, string> translations = new Dictionary<string, string>
        {
            // menus
            [$@"{editor_prefix}:menu_bar_test"] = @"テスト",
            [$@"{editor_prefix}:test_gameplay"] = @"テストプレイ",
            [$@"{editor_prefix}:test_play_mods"] = @"テストプレイのMod",
            [$@"{editor_prefix}:test_play_deselect_all_mods"] = @"Modをすべて解除",
            [$@"{editor_prefix}:export_beatmap_file"] = @"この難易度を.osu形式で書き出す（v14）",

            // test play
            [$@"{editor_prefix}:apply_test_play_mods_to_composer"] = @"編集画面にもModを反映",
            [$@"{editor_prefix}:live_mod_preview"] = @"編集中もModの表示を更新",

            // composing
            [$@"{editor_prefix}:advance_by_previous_spacing_on_placement"] = @"配置後に直前の間隔ぶん進む",
            [$@"{editor_prefix}:freehand_slider_placement"] = @"ドラッグでスライダーを描く",
            [$@"{editor_prefix}:arc_on_anchor_add"] = @"追加したアンカーをパーフェクトカーブに",
            [$@"{editor_prefix}:ctrl_click_cycles_curve_type"] = @"Ctrl+クリックでカーブ種別を変更",

            // display
            [$@"{editor_prefix}:show_offscreen_objects"] = @"プレイ範囲と画面外ノーツ",
            [$@"{editor_prefix}:show_difficulty_graph"] = @"難易度グラフ",
            [$@"{editor_prefix}:show_timeline"] = @"タイムライン",
            [$@"{editor_prefix}:always_expand_left_toolbox"] = @"左ツールボックスを常に展開",
            [$@"{editor_prefix}:always_expand_timeline_point_labels"] = @"タイムラインのラベルを常に展開",
            [$@"{editor_prefix}:seek_animation_duration"] = @"シークのアニメーション",
            [$@"{editor_prefix}:editor_colour_scheme"] = @"エディターの配色",

            // selection
            [$@"{editor_prefix}:selection_appearance"] = @"選択枠",
            [$@"{editor_prefix}:selection_colour"] = @"枠の色",
            [$@"{editor_prefix}:selection_thickness"] = @"枠の太さ",
            [$@"{editor_prefix}:selection_opacity"] = @"枠の不透明度",
            [$@"{editor_prefix}:selection_handle_colour"] = @"ハンドルの色",
            [$@"{editor_prefix}:selection_handle_size"] = @"ハンドルの大きさ",
            [$@"{editor_prefix}:slider_anchor_size"] = @"アンカーの大きさ",
            [$@"{editor_prefix}:show_selection_buttons"] = @"選択時のボタン",
            [$@"{editor_prefix}:selectable_after_end_time"] = @"通過後の選択可能時間",

            // input
            [$@"{editor_prefix}:drag_start_distance"] = @"ドラッグ開始距離",
            [$@"{editor_prefix}:swap_right_and_middle_mouse"] = @"右クリックとホイールを入れ替え",

            // backups
            [$@"{editor_prefix}:auto_backup"] = @"自動バックアップ",
            [$@"{editor_prefix}:auto_backup_enabled"] = @"編集中に自動バックアップ",
            [$@"{editor_prefix}:auto_backup_interval"] = @"バックアップの間隔",
            [$@"{editor_prefix}:auto_backup_keep_count"] = @"保持する世代数",
            [$@"{editor_prefix}:backup_now"] = @"いますぐバックアップ",
            [$@"{editor_prefix}:open_backup_folder"] = @"バックアップ先を開く",

            // colours
            [$@"{editor_prefix}:colour_default"] = @"既定",
            [$@"{editor_prefix}:colour_red"] = @"赤",
            [$@"{editor_prefix}:colour_orange"] = @"オレンジ",
            [$@"{editor_prefix}:colour_green"] = @"緑",
            [$@"{editor_prefix}:colour_cyan"] = @"シアン",
            [$@"{editor_prefix}:colour_blue"] = @"青",
            [$@"{editor_prefix}:colour_magenta"] = @"マゼンタ",
            [$@"{editor_prefix}:colour_white"] = @"白",

            // settings section
            [$@"{settings_prefix}:section_header"] = @"Lazer-Kai",
            [$@"{settings_prefix}:display_header"] = @"表示",
            [$@"{settings_prefix}:selection_header"] = @"選択",
            [$@"{settings_prefix}:composing_header"] = @"配置",
            [$@"{settings_prefix}:input_header"] = @"操作",
            [$@"{settings_prefix}:test_play_header"] = @"テストプレイ",
            [$@"{settings_prefix}:backup_header"] = @"バックアップ",
            [$@"{settings_prefix}:applies_game_wide"] = @"エディター外にも適用されます",
            [$@"{settings_prefix}:editor_only"] = @"エディターを開いている間だけ有効です",
            [$@"{settings_prefix}:takes_effect_on_reopen"] = @"次にエディターを開いたときに反映されます",
            [$@"{settings_prefix}:no_delay"] = @"なし",
            [$@"{settings_prefix}:minutes"] = @"{0}分",

            // key bindings
            [$@"{binding_prefix}:editor_toggle_composer_mod_preview"] = @"編集画面のModプレビューを切り替え",
            [$@"{binding_prefix}:editor_decrease_slider_velocity"] = @"SVを下げる",
            [$@"{binding_prefix}:editor_increase_slider_velocity"] = @"SVを上げる",
            [$@"{binding_prefix}:editor_decrease_slider_velocity_fine"] = @"SVを下げる（微調整）",
            [$@"{binding_prefix}:editor_increase_slider_velocity_fine"] = @"SVを上げる（微調整）",
        };

        private const string editor_prefix = @"osu.Game.Resources.Localisation.Editor";
        private const string settings_prefix = @"osu.Game.Resources.Localisation.LazerKaiSettings";
        private const string binding_prefix = @"osu.Game.Resources.Localisation.GlobalActionKeyBinding";

        public LazerKaiJapaneseStore(ILocalisationStore fallback)
        {
            this.fallback = fallback;
        }

        public string Get(string lookup) => translations.TryGetValue(lookup, out string? translated) ? translated : fallback.Get(lookup);

        public Task<string> GetAsync(string lookup, CancellationToken cancellationToken = default) =>
            translations.TryGetValue(lookup, out string? translated) ? Task.FromResult(translated) : fallback.GetAsync(lookup, cancellationToken);

        public Stream GetStream(string name) => fallback.GetStream(name);

        public IEnumerable<string> GetAvailableResources() => fallback.GetAvailableResources();

        public void Dispose()
        {
            fallback.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
