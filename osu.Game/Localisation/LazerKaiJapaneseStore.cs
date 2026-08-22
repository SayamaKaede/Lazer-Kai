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
    /// says out loud rather than literal translations of the code's terms.
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
            [$@"{editor_prefix}:carry_over_song_select_mods"] = @"曲選択画面とエディターでのMod情報を共有する",
            [$@"{editor_prefix}:carry_over_song_select_mods_description"] = @"テストプレイが曲選択画面で選んだModから始まり、ここで選んだModは出たあとも選ばれたままになります",
            [$@"{editor_prefix}:apply_test_play_mods_to_composer"] = @"編集画面にもModを反映",
            [$@"{editor_prefix}:live_mod_preview"] = @"編集中もModの表示を更新",

            // composing
            [$@"{editor_prefix}:advance_by_previous_spacing_on_placement"] = @"オブジェクト配置後に自動で次へシーク",
            [$@"{editor_prefix}:freehand_slider_placement"] = @"ドラッグでスライダーを描く",
            [$@"{editor_prefix}:default_addition_bank"] = @"Addition bankの初期値",
            [$@"{editor_prefix}:default_addition_bank_description"] = @"Addition bankを持たないノーツにWhistle・Finish・Clapを付けたとき、このbankになります",
            [$@"{editor_prefix}:hide_grid_from_points_tool"] = @"ToolsのGRIDボタンを非表示",
            [$@"{editor_prefix}:hide_grid_from_points_tool_description"] = @"割り当てられたキーでも選択されなくなります。グリッドは右のGrid欄から設定できます",
            [$@"{editor_prefix}:addition_bank_auto"] = @"Auto",
            [$@"{editor_prefix}:addition_bank_normal"] = @"Normal",
            [$@"{editor_prefix}:addition_bank_soft"] = @"Soft",
            [$@"{editor_prefix}:addition_bank_drum"] = @"Drum",
            [$@"{editor_prefix}:arc_on_anchor_add"] = @"追加したアンカーをパーフェクトカーブに",
            [$@"{editor_prefix}:arc_on_anchor_add_description"] = @"よりStableの操作感に近付きます",
            [$@"{editor_prefix}:ctrl_click_cycles_curve_type"] = @"Ctrl+クリックでカーブ種別を変更",
            [$@"{editor_prefix}:auto_generate_breaks"] = @"休憩時間を自動で挿入",
            [$@"{editor_prefix}:auto_generate_breaks_description"] = @"ノーツを触るたび休憩時間が挿入されるのが嫌な場合はオフ。オフなら、編集メニューから自分で置く形になります",
            [$@"{editor_prefix}:insert_break"] = @"休憩時間を挿入",

            // display
            [$@"{editor_prefix}:show_play_area_border"] = @"プレイ画面の枠を表示",
            [$@"{editor_prefix}:detect_offscreen"] = @"Offscreen検知",
            [$@"{editor_prefix}:show_difficulty_graph"] = @"難易度グラフ",
            [$@"{editor_prefix}:difficulty_graph_colour"] = @"難易度グラフの色",
            [$@"{editor_prefix}:difficulty_graph_opacity"] = @"難易度グラフの不透明度",
            [$@"{editor_prefix}:loop_playback"] = @"ループ再生",
            [$@"{editor_prefix}:show_timeline"] = @"タイムライン",
            [$@"{editor_prefix}:hitsounds"] = @"効果音",
            [$@"{editor_prefix}:music"] = @"音楽",
            [$@"{editor_prefix}:always_expand_left_toolbox"] = @"左ツールボックスを常に展開",
            [$@"{editor_prefix}:tool_button_squareness"] = @"Toolboxのボタンの高さ",
            [$@"{editor_prefix}:toggle_button_squareness"] = @"Togglesのボタンの高さ",
            [$@"{editor_prefix}:always_expand_timeline_point_labels"] = @"タイムラインのラベルを常に展開",
            [$@"{editor_prefix}:hide_timeline_points"] = @"タイムラインのSV・音量の帯を非表示",
            [$@"{editor_prefix}:hide_timeline_points_description"] = @"空いた分だけノーツが大きくなり、掴みやすくなります。SVは右のバーから変更できます",
            [$@"{editor_prefix}:timeline_drag_area_scale"] = @"タイムラインのリバースの大きさ",
            [$@"{editor_prefix}:timeline_hide_points"] = @"SV・音量の帯を非表示",
            [$@"{editor_prefix}:timeline_point_rows"] = @"タイムラインのSV・音量の帯",
            [$@"{editor_prefix}:shown"] = @"表示",
            [$@"{editor_prefix}:hidden"] = @"非表示",
            [$@"{editor_prefix}:timeline_repeat_drag_threshold"] = @"リバーススライダーの感度",
            [$@"{editor_prefix}:timeline_repeat_drag_threshold_description"] = @"次のリバースまでの何割まで引っ張ったらリバースが増えるか",
            [$@"{editor_prefix}:pinned_group_order"] = @"ピン留めの並び順",
            [$@"{editor_prefix}:pinned_group_order_toolbox"] = @"バー本来の順",
            [$@"{editor_prefix}:pinned_group_order_pinned"] = @"ピン留めした順",
            [$@"{editor_prefix}:seek_animation_duration"] = @"シークのアニメーション",
            [$@"{editor_prefix}:editor_colour_scheme"] = @"エディターのテーマ色",
            [$@"{editor_prefix}:hide_kiai_flash"] = @"Kiaiのフラッシュを非表示",
            [$@"{editor_prefix}:hide_kiai_flash_description"] = @"エディター内でのみ非表示になります。テストプレイでは今までどおり光ります",

            // selection
            [$@"{editor_prefix}:selection_appearance"] = @"選択枠",
            [$@"{editor_prefix}:selection_colour"] = @"枠の色",
            [$@"{editor_prefix}:selection_thickness"] = @"枠の太さ",
            [$@"{editor_prefix}:selection_opacity"] = @"枠の不透明度",
            [$@"{editor_prefix}:selection_handle_colour"] = @"ハンドルの色",
            [$@"{editor_prefix}:selection_handle_size"] = @"ハンドルの大きさ",
            [$@"{editor_prefix}:slider_anchor_size"] = @"アンカーの大きさ",
            [$@"{editor_prefix}:slider_end_drag_marker_colour"] = @"スライダー伸縮ハンドルの色",
            [$@"{editor_prefix}:slider_end_drag_marker_scale"] = @"スライダー伸縮ハンドルの太さ",
            [$@"{editor_prefix}:slider_end_drag_marker_opacity"] = @"スライダー伸縮ハンドルの不透明度",
            [$@"{editor_prefix}:hide_selection_buttons"] = @"選択時のショートカットボタンの非表示",
            [$@"{editor_prefix}:hide_rotation_handles"] = @"選択時の回転ハンドルの非表示",
            [$@"{editor_prefix}:hide_rotation_handles_description"] = @"Ctrl+Shift+ホイールで回転、Altキーも加えるとさらに微調整ができます",
            [$@"{editor_prefix}:selectable_after_end_time"] = @"通過後の選択可能時間",

            // input
            [$@"{editor_prefix}:beatmap_upload_unavailable"] = @"このビルドは譜面をアップロードしません",
            [$@"{editor_prefix}:remove_drag_threshold"] = @"ドラッグ時の引っかかりを無くす",
            [$@"{editor_prefix}:remove_drag_threshold_description"] = @"細かい微調整がやりやすくなります。エディター外にも適用されます",
            [$@"{editor_prefix}:swap_right_and_middle_mouse"] = @"右クリックとホイールを入れ替え",

            // backups
            [$@"{editor_prefix}:auto_backup"] = @"自動バックアップ",
            [$@"{editor_prefix}:auto_backup_enabled"] = @"編集中に自動バックアップ",
            [$@"{editor_prefix}:auto_backup_interval"] = @"バックアップの間隔",
            [$@"{editor_prefix}:auto_backup_keep_count"] = @"保持する個数",
            [$@"{editor_prefix}:backup_now"] = @"いますぐバックアップ",
            [$@"{editor_prefix}:open_backup_folder"] = @"バックアップフォルダを開く",
            [$@"{editor_prefix}:notify_on_backup"] = @"バックアップ完了時に通知を出す",
            [$@"{editor_prefix}:backup_written"] = @"バックアップしました",

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
            [$@"{settings_prefix}:no_delay"] = @"無し",
            [$@"{settings_prefix}:minutes"] = @"{0}分",

            // key bindings
            // shown as the value on the toasts the shortcuts raise.
            [$@"{common_prefix}:enabled"] = @"オン",
            [$@"{common_prefix}:disabled"] = @"オフ",

            [$@"{binding_prefix}:editor_toggle_composer_mod_preview"] = @"編集画面のModプレビューを切り替え",
            [$@"{binding_prefix}:editor_toggle_timeline"] = @"タイムラインの表示を切り替え",
            [$@"{binding_prefix}:editor_toggle_hitsounds"] = @"効果音のミュート切り替え",
            [$@"{binding_prefix}:editor_toggle_music"] = @"音楽のミュート切り替え",
            [$@"{binding_prefix}:editor_toggle_timeline_points"] = @"タイムラインのSV・音量の帯を切り替え",
            [$@"{binding_prefix}:editor_backup_now"] = @"いますぐバックアップ",
            [$@"{binding_prefix}:editor_decrease_slider_velocity"] = @"SVを下げる",
            [$@"{binding_prefix}:editor_increase_slider_velocity"] = @"SVを上げる",
            [$@"{binding_prefix}:editor_decrease_slider_velocity_fine"] = @"SVを下げる（微調整）",
            [$@"{binding_prefix}:editor_increase_slider_velocity_fine"] = @"SVを上げる（微調整）",
        };

        private const string editor_prefix = @"osu.Game.Resources.Localisation.Editor";
        private const string settings_prefix = @"osu.Game.Resources.Localisation.LazerKaiSettings";
        private const string binding_prefix = @"osu.Game.Resources.Localisation.GlobalActionKeyBinding";
        private const string common_prefix = @"osu.Game.Resources.Localisation.Common";

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
