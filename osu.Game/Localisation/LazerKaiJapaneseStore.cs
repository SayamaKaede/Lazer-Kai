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
            [$@"{editor_prefix}:menu_bar_test"] = @"Mods",
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
            // stream conversion
            [$@"{editor_prefix}:convert_to_stream"] = @"連打に変換",
            [$@"{editor_prefix}:stream_count"] = @"数",
            [$@"{editor_prefix}:stream_extended_limits"] = @"16個より多く置けるようにする",
            [$@"{editor_prefix}:stream_new_combo_every"] = @"New Comboの間隔",
            [$@"{editor_prefix}:stream_new_combo_none"] = @"無し",
            [$@"{editor_prefix}:stream_acceleration"] = @"加速",
            [$@"{editor_prefix}:stream_easing"] = @"イージング",
            [$@"{editor_prefix}:stream_zigzag"] = @"ジグザグ",
            [$@"{editor_prefix}:stream_zigzag_period"] = @"何個ごとに折り返すか",
            [$@"{editor_prefix}:stream_zigzag_shape"] = @"ジグザグの変化",
            [$@"{editor_prefix}:stream_draw_the_curve"] = @"カーブを自分で描く",

            [$@"{editor_prefix}:grid_default_shape"] = @"グリッドの形",
            [$@"{editor_prefix}:grid_default_spacing"] = @"グリッドの間隔",
            [$@"{editor_prefix}:grid_default_rotation"] = @"グリッドの角度",
            [$@"{editor_prefix}:grid_default_x"] = @"グリッドの原点 X",
            [$@"{editor_prefix}:grid_default_y"] = @"グリッドの原点 Y",
            [$@"{editor_prefix}:grid_defaults_description"] = @"エディターを開いたときのグリッドの初期値です。間隔は、譜面に保存されている値より優先されます",
            [$@"{editor_prefix}:show_unsnap_markers"] = @"スナップから外れたノーツに印を付ける",
            [$@"{editor_prefix}:show_unsnap_markers_description"] = @"2msより大きいズレの場合にのみ機能します",
            [$@"{editor_prefix}:show_difficulty_graph"] = @"難易度グラフ",
            [$@"{editor_prefix}:difficulty_graph_colour"] = @"色",
            [$@"{editor_prefix}:difficulty_graph_opacity"] = @"不透明度",
            [$@"{editor_prefix}:difficulty_graph_detail"] = @"精密さ",
            [$@"{editor_prefix}:difficulty_graph_detail_screen"] = @"難易度グラフの詳細",
            [$@"{editor_prefix}:difficulty_graph_per_skill"] = @"スキル別",
            [$@"{editor_prefix}:difficulty_graph_total"] = @"合計",
            [$@"{editor_prefix}:difficulty_graph_window_opacity"] = @"透明度",
            [$@"{editor_prefix}:difficulty_graph_apply_current_mods"] = @"現在のModを適用",
            [$@"{editor_prefix}:difficulty_graph_detail_description"] = @"精密さを上げ過ぎると読み込みが遅くなる可能性があります",
            [$@"{editor_prefix}:loop_playback"] = @"ループ再生",
            [$@"{editor_prefix}:show_timeline"] = @"タイムライン",
            [$@"{editor_prefix}:hitsounds"] = @"効果音",
            [$@"{editor_prefix}:music"] = @"音楽",
            [$@"{editor_prefix}:always_expand_left_toolbox"] = @"左ツールボックスを常に展開",
            [$@"{editor_prefix}:left_toolbox_width"] = @"左バー展開時の幅",
            [$@"{editor_prefix}:right_toolbox_width"] = @"右バー展開時の幅",
            [$@"{editor_prefix}:contract_sidebars"] = @"カーソルが乗っていない間はバーを縮める",
            [$@"{editor_prefix}:tool_button_squareness"] = @"(左)Toolboxのボタンの高さ",
            [$@"{editor_prefix}:toggle_button_squareness"] = @"(左)Togglesのボタンの高さ",
            [$@"{editor_prefix}:sample_bank_button_squareness"] = @"(左)Bankのボタンの高さ",
            [$@"{editor_prefix}:grid_button_squareness"] = @"(右)Gridのボタンの高さ",
            [$@"{editor_prefix}:action_button_squareness"] = @"(右)Transform・Generateのボタンの高さ",
            [$@"{editor_prefix}:slider_velocity_preset_squareness"] = @"(右)SVプリセットのボタンの高さ",

            // status panel rows
            [$@"{editor_prefix}:status_show_star_rating"] = @"SR",
            [$@"{editor_prefix}:status_show_max_performance"] = @"MAX PP",
            [$@"{editor_prefix}:status_show_score"] = @"SCORE（Lazer）",
            [$@"{editor_prefix}:status_show_legacy_score"] = @"SCORE V1（Stable）",
            [$@"{editor_prefix}:status_show_slider_velocity"] = @"SV",
            [$@"{editor_prefix}:status_show_normal_bank"] = @"Normal bank (Sampleset)",
            [$@"{editor_prefix}:status_show_addition_bank"] = @"Addition bank (Additions)",
            [$@"{editor_prefix}:status_show_sample_volume"] = @"ボリューム",
            [$@"{editor_prefix}:status_show_kiai"] = @"Kiai",
            [$@"{editor_prefix}:status_show_mods"] = @"Mod",
            [$@"{editor_prefix}:status_show_combo"] = @"コンボ（現在の再生位置でのコンボ数）",
            [$@"{editor_prefix}:status_show_object_count"] = @"オブジェクト数",
            [$@"{editor_prefix}:status_show_drain_time"] = @"ドレインタイム",
            [$@"{editor_prefix}:status_show_difficulty_settings"] = @"譜面ステータス（AR・CS・OD・HP）",

            // reference difficulty
            [$@"{editor_prefix}:reference_difficulty"] = @"ゴースト表示する難易度",
            [$@"{editor_prefix}:reference_difficulty_none"] = @"なし",
            [$@"{editor_prefix}:reference_difficulty_opacity"] = @"不透明度",
            [$@"{editor_prefix}:reference_difficulty_colour"] = @"色",
            [$@"{editor_prefix}:always_expand_timeline_point_labels"] = @"ラベルを常に展開",
            [$@"{editor_prefix}:hide_timeline_points"] = @"SV・音量のラベルを非表示",
            [$@"{editor_prefix}:hide_timeline_points_description"] = @"非表示にしたぶん、ノーツが大きくなり、操作しやすくなります",
            [$@"{editor_prefix}:timeline_style"] = @"表示形式",
            [$@"{editor_prefix}:timeline_style_lazer"] = @"Lazer",
            [$@"{editor_prefix}:timeline_style_stable"] = @"Stable",
            [$@"{editor_prefix}:timeline_style_lazer_kai"] = @"Lazer-Kai",
            [$@"{editor_prefix}:timeline_style_description"] = @"Stableはノーツを下端に揃え、ティックも下だけに表示します。SVと音量はノーツの上に並びます。Lazer-Kaiはそれに加えて、上端からもティックが伸びます",
            [$@"{editor_prefix}:timeline_drag_to_seek"] = @"上部を掴んでドラッグで再生位置を移動",
            [$@"{editor_prefix}:timeline_drag_to_seek_description"] = @"手のひらツールのように掴めますが、誤爆が多い場合はオフに",
            [$@"{editor_prefix}:timeline_drag_area_scale"] = @"リバースの大きさ",
            [$@"{editor_prefix}:timeline_hide_points"] = @"SV・音量の帯を非表示",
            [$@"{editor_prefix}:timeline_point_rows"] = @"タイムラインのSV・音量の帯",
            [$@"{editor_prefix}:shown"] = @"表示",
            [$@"{editor_prefix}:hidden"] = @"非表示",
            [$@"{editor_prefix}:timeline_repeat_drag_threshold"] = @"リバーススライダーの感度",
            [$@"{editor_prefix}:timeline_repeat_drag_threshold_description"] = @"スライダーの尻尾を掴んでリバースを作るときに、どのくらいでリバースが増えるかの感度です",
            [$@"{editor_prefix}:seek_animation_duration"] = @"シークのアニメーション",
            [$@"{editor_prefix}:editor_colour_scheme"] = @"エディターのテーマ色",

            // named after what the hue actually looks like, which the stock names do not always do.
            [$@"{editor_prefix}:colour_scheme_red"] = @"赤",
            [$@"{editor_prefix}:colour_scheme_orange"] = @"黄色",
            [$@"{editor_prefix}:colour_scheme_lime"] = @"黄緑",
            [$@"{editor_prefix}:colour_scheme_green"] = @"緑",
            [$@"{editor_prefix}:colour_scheme_aquamarine"] = @"青緑",
            [$@"{editor_prefix}:colour_scheme_blue"] = @"水色",
            [$@"{editor_prefix}:colour_scheme_purple"] = @"紫",
            [$@"{editor_prefix}:colour_scheme_plum"] = @"マゼンタ",
            [$@"{editor_prefix}:colour_scheme_pink"] = @"ピンク",
            [$@"{editor_prefix}:hide_kiai_flash"] = @"Kiaiのフラッシュを無効化",
            [$@"{editor_prefix}:hide_kiai_flash_description"] = @"エディター内でのみ非表示になります。テストプレイでは今までどおり光ります",

            // selection
            [$@"{editor_prefix}:selection_appearance"] = @"選択枠",
            [$@"{editor_prefix}:selection_colour"] = @"四角の枠の色",
            [$@"{editor_prefix}:selection_thickness"] = @"四角の枠の太さ",
            [$@"{editor_prefix}:selection_opacity"] = @"四角の枠の不透明度",
            [$@"{editor_prefix}:object_outline_colour"] = @"ノーツの枠の色",
            [$@"{editor_prefix}:object_outline_thickness"] = @"ノーツの枠の太さ",
            [$@"{editor_prefix}:object_outline_opacity"] = @"ノーツの枠の不透明度",

            // export
            [$@"{settings_prefix}:export_header"] = @"出力",
            [$@"{export_prefix}:format"] = @"出力する形式",
            [$@"{export_prefix}:format_stable"] = @"Stable向け（v14 / .osz）",
            [$@"{export_prefix}:format_lazer"] = @"Lazer向け（変換しない / .olz）",
            [$@"{export_prefix}:export_difficulty"] = @"この難易度を出力",
            [$@"{export_prefix}:export_set"] = @"このセットを出力",
            [$@"{editor_prefix}:selection_handle_colour"] = @"8点ハンドルの色",
            [$@"{editor_prefix}:selection_handle_size"] = @"8点ハンドルの大きさ",
            [$@"{editor_prefix}:slider_anchor_size"] = @"スライダーアンカーの大きさ",
            [$@"{editor_prefix}:slider_end_drag_marker_colour"] = @"スライダー伸縮ハンドルの色",
            [$@"{editor_prefix}:slider_end_drag_marker_scale"] = @"スライダー伸縮ハンドルの太さ",
            [$@"{editor_prefix}:slider_end_drag_marker_opacity"] = @"スライダー伸縮ハンドルの不透明度",
            [$@"{editor_prefix}:hide_selection_buttons"] = @"ショートカットボタンの非表示",
            [$@"{editor_prefix}:hide_rotation_handles"] = @"回転ハンドルの非表示",
            [$@"{editor_prefix}:hide_rotation_handles_description"] = @"選択時の四隅に出る回転させられるハンドルを非表示",
            [$@"{editor_prefix}:selectable_after_end_time"] = @"ノーツ通過後の選択可能時間",

            // input
            [$@"{editor_prefix}:beatmap_upload_unavailable"] = @"このビルドは譜面をアップロードしません",
            [$@"{editor_prefix}:remove_drag_threshold"] = @"ドラッグ時の引っかかりを無くす",
            [$@"{editor_prefix}:remove_drag_threshold_description"] = @"本家ではクリックしたあと、10px以内の移動だとドラッグしたことにならないので、オンにすると微調整や細かい操作がしやすくなります",
            [$@"{editor_prefix}:swap_right_and_middle_mouse"] = @"右クリックとホイールクリックを入れ替え",

            // backups
            [$@"{editor_prefix}:auto_backup"] = @"自動バックアップ",
            [$@"{editor_prefix}:auto_backup_enabled"] = @"編集中に自動バックアップ",
            [$@"{editor_prefix}:auto_backup_interval"] = @"バックアップの間隔",
            [$@"{editor_prefix}:auto_backup_keep_count"] = @"保持する個数",
            [$@"{editor_prefix}:backup_now"] = @"いますぐバックアップ",
            [$@"{editor_prefix}:open_backup_folder"] = @"この譜面のバックアップフォルダを開く",
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

            // dropping a .osu onto the open editor
            [$@"{editor_prefix}:dropped_difficulty_added"] = @"「{0}」を新しい難易度として追加しました。",
            [$@"{editor_prefix}:dropped_difficulty_replaced"] = @"難易度「{0}」を上書きしました。",
            [$@"{editor_prefix}:dropped_difficulty_unreadable"] = @"「{0}」を難易度として読み込めませんでした。",
            [$@"{editor_prefix}:dropped_difficulty_name_taken"] = @"このセットには別モードの「{0}」が既にあるため、ドロップされたファイルは取り込みませんでした。",
            [$@"{editor_dialogs_prefix}:overwrite_current_difficulty_header"] = @"編集中の難易度を上書きしますか?",
            [$@"{editor_dialogs_prefix}:overwrite_current_difficulty_details"] = @"ドロップされたファイルは「{0}」で、いま開いている難易度です。上書きすると保存していない変更は破棄され、新しい内容でエディターを開き直します。",

            // Mapping Tools への受け渡し
            [$@"{editor_prefix}:click_to_export_logs"] = @"クリックすると、送信用のログを書き出します（Windowsのアカウント名は伏せられます）",
            [$@"{editor_prefix}:open_in_mapping_tools"] = @"Mapping Tools で開く",
            [$@"{editor_prefix}:mapping_tools_not_found"] = @"Mapping Tools が見つかりませんでした。インストール先をオプションの Lazer-Kai から指定してください。",
            [$@"{editor_prefix}:mapping_tools_already_running"] = @"Mapping Tools が既に起動しています。どの譜面を扱うかは起動時にしか読み込まれないので、一度閉じてからやり直してください。",
            [$@"{editor_prefix}:mapping_tools_path_is_something_else"] = @"オプションで指定されているファイルは Mapping Tools ではありません。Mapping Tools.exe を指定してください。",
            [$@"{editor_prefix}:mapping_tools_failed_to_start"] = @"Mapping Tools を起動できませんでした。",

            // settings section
            [$@"{settings_prefix}:section_header"] = @"Lazer-Kai",
            // the fork's name is written the same way in Japanese; it is here so that it reads as decided rather
            // than as a string nobody got to.
            [$@"{settings_prefix}:section_header"] = @"Lazer-Kai",
            [$@"{settings_prefix}:beatmap_section_header"] = @"譜面",
            [$@"{settings_prefix}:test_play_section_header"] = @"Mods",
            [$@"{settings_prefix}:other_section_header"] = @"その他",
            [$@"{settings_prefix}:display_header"] = @"表示",
            [$@"{settings_prefix}:difficulty_graph_header"] = @"難易度グラフ",
            [$@"{settings_prefix}:ghost_difficulty_header"] = @"ゴースト表示",
            [$@"{settings_prefix}:hitsound_header"] = @"ヒットサウンド",
            [$@"{settings_prefix}:other_customisation_header"] = @"その他のカスタマイズ",
            [$@"{settings_prefix}:toolbox_header"] = @"左右ツールボックス",
            [$@"{settings_prefix}:button_shape_header"] = @"左右ツールボックス - ボタン",
            [$@"{settings_prefix}:timeline_header"] = @"上部タイムライン",
            [$@"{settings_prefix}:selection_header"] = @"選択・範囲選択枠",
            [$@"{settings_prefix}:status_header"] = @"STATUS",
            [$@"{settings_prefix}:composing_header"] = @"配置",
            [$@"{settings_prefix}:input_header"] = @"操作",
            [$@"{settings_prefix}:test_play_header"] = @"Mod",
            [$@"{settings_prefix}:backup_header"] = @"バックアップ",
            [$@"{settings_prefix}:beatmap_skin_header"] = @"譜面のスキン",
            [$@"{settings_prefix}:editor_beatmap_colours"] = @"譜面のコンボカラー",
            [$@"{settings_prefix}:editor_beatmap_colours_description"] = @"現在の譜面のコンボカラーを強制します",
            [$@"{settings_prefix}:editor_beatmap_hitsounds"] = @"譜面のヒットサウンド",
            [$@"{settings_prefix}:editor_beatmap_hitsounds_description"] = @"現在の譜面のヒットサウンドを強制します",
            [$@"{settings_prefix}:external_tool_header"] = @"外部ツール",
            [$@"{settings_prefix}:mapping_tools_path"] = @"Mapping Tools",
            [$@"{settings_prefix}:mapping_tools_path_description"] = @"Mapping Tools.exe の場所。空欄なら通常のインストール先を使用します",
            [$@"{settings_prefix}:mapping_tools_not_detected"] = @"Mapping Tools が見つかりません",
            [$@"{settings_prefix}:export_logs"] = @"送信用にログを書き出す",
            [$@"{settings_prefix}:export_logs_description"] = @"直近のログを、Windowsのアカウント名を伏せた状態でコピーし、その場所を開きます",
            [$@"{settings_prefix}:exported_logs"] = @"ログを{0}件書き出しました。クリックすると場所を開きます",
            [$@"{settings_prefix}:export_logs_failed"] = @"ログを書き出せませんでした。",
            [$@"{settings_prefix}:show_mapping_tools_path"] = @"Mapping Tools のパスを表示",
            [$@"{settings_prefix}:hide_mapping_tools_path"] = @"Mapping Tools のパスを隠す",
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
            [$@"{binding_prefix}:editor_toggle_reference_difficulty"] = @"ゴースト表示のオン・オフ",
            [$@"{binding_prefix}:editor_next_reference_difficulty"] = @"ゴーストを一つ上の難易度に",
            [$@"{binding_prefix}:editor_previous_reference_difficulty"] = @"ゴーストを一つ下の難易度に",
            [$@"{binding_prefix}:editor_toggle_difficulty_graph"] = @"難易度グラフの表示を切り替え",
            [$@"{binding_prefix}:editor_toggle_play_area_border"] = @"プレイ画面の枠の表示を切り替え",
            [$@"{binding_prefix}:editor_toggle_offscreen_detection"] = @"Offscreen検知を切り替え",
            [$@"{binding_prefix}:editor_toggle_left_toolbox_expansion"] = @"左ツールボックスを常に展開するかを切り替え",
            [$@"{binding_prefix}:editor_toggle_timeline_point_labels"] = @"タイムラインのラベルを常に展開するかを切り替え",
            [$@"{binding_prefix}:editor_backup_now"] = @"いますぐバックアップ",
            [$@"{binding_prefix}:editor_decrease_slider_velocity"] = @"SVを下げる",
            [$@"{binding_prefix}:editor_increase_slider_velocity"] = @"SVを上げる",
            [$@"{binding_prefix}:editor_decrease_slider_velocity_fine"] = @"SVを下げる（微調整）",
            [$@"{binding_prefix}:editor_increase_slider_velocity_fine"] = @"SVを上げる（微調整）",

            // timeline skin
            [$@"{editor_prefix}:timeline_use_skin"] = @"現在のスキンを適用",
            [$@"{editor_prefix}:timeline_use_skin_description"] = @"よりStableの見た目と操作感に近付きます",

            // collab manager
            // the feature keeps its English name, which is what it is called between the people using it. Everything
            // said about it is in Japanese, and 「担当」 throughout: it is the word a collaboration is divided in.
            // the name stays as it is: it is what the people using it call the feature.
            [$@"{collab_prefix}:header"] = @"Collab Manager",
            [$@"{collab_prefix}:menu_item"] = @"Collab Manager...",
            [$@"{collab_prefix}:no_bookmarks_yet"] = @"区切りたい位置にブックマークを追加すると、そこが担当範囲になります",
            [$@"{collab_prefix}:unassigned"] = @"未割り当て",
            [$@"{collab_prefix}:follow_playhead"] = @"常に下部タイムラインの現在位置で色分けを表示",
            [$@"{collab_prefix}:add_collaborator"] = @"マッパーを追加",
            [$@"{collab_prefix}:copy_split"] = @"リストをコピー",
            [$@"{collab_prefix}:whole_beatmap"] = @"譜面全体 {0}",
            [$@"{collab_prefix}:block_summary"] = @"{0} / {1}ノーツ",
            [$@"{collab_prefix}:rename"] = @"名前を変更",
            [$@"{collab_prefix}:collaborator_name"] = @"マッパーの名前",
            [$@"{collab_prefix}:panel_opacity"] = @"透明度",

            [$@"{editor_prefix}:detect_offscreen_description"] = @"はみ出すと該当のノーツが赤くなります",
            [$@"{editor_prefix}:seek_animation_duration_description"] = @"ホイールなどでタイムラインが動く時のアニメーションの長さ",
            [$@"{editor_prefix}:always_expand_timeline_point_labels_description"] = @"オフだとボリュームのラベルがランダムに閉じたり開いたりします",
            [$@"{editor_prefix}:timeline_drag_area_scale_description"] = @"スライダーの尻尾の掴める部分の大きさです。スキンが有効になっている場合は変わりません",
            [$@"{editor_prefix}:hide_selection_buttons_description"] = @"選択時の左右反転や上下反転など、5つのボタンを非表示",
            [$@"{editor_prefix}:selectable_after_end_time_description"] = @"本家だと明らかに見えていないノーツが選択可能になって巻き込みやすくなっています。200ms辺りから調整してみてください",
            [$@"{editor_prefix}:selection_colour_description"] = @"選択時の四角い範囲を表している枠の色",
            [$@"{editor_prefix}:selection_thickness_description"] = @"選択時の四角い範囲を表している枠の太さ",
            [$@"{editor_prefix}:selection_opacity_description"] = @"選択時の四角い範囲を表している不透明度",
            [$@"{editor_prefix}:object_outline_colour_description"] = @"選択したノーツ自体に表示される枠の色",
            [$@"{editor_prefix}:object_outline_thickness_description"] = @"選択したノーツ自体に表示される枠の太さ",
            [$@"{editor_prefix}:object_outline_opacity_description"] = @"選択したノーツ自体に表示される枠の不透明度",
            [$@"{editor_prefix}:selection_handle_colour_description"] = @"選択時の四角い範囲のサイズを変更するための、8つの点の色",
            [$@"{editor_prefix}:selection_handle_size_description"] = @"選択時の四角い範囲のサイズを変更するための、8つの点の大きさ",
            [$@"{editor_prefix}:slider_end_drag_marker_colour_description"] = @"スライダーの尻尾に付く、掴んで縮めたりできる半円の色",
            [$@"{editor_prefix}:slider_end_drag_marker_scale_description"] = @"スライダーの尻尾に付く、掴んで縮めたりできる半円の太さ",
            [$@"{editor_prefix}:slider_end_drag_marker_opacity_description"] = @"スライダーの尻尾に付く、掴んで縮めたりできる半円の不透明度",
            [$@"{editor_prefix}:advance_by_previous_spacing_on_placement_description"] = @"ノーツを配置後、自動で直前のノーツとの間隔ぶん、自動で移動します。毎回のホイール操作が不要になります",
            [$@"{editor_prefix}:freehand_slider_placement_description"] = @"スライダーを置くときに誤爆しがちな場合はオフに",
            [$@"{editor_prefix}:ctrl_click_cycles_curve_type_description"] = @"例えば、Stableでいう白点にCtrl+クリックすると、赤点になります",
            [$@"{editor_prefix}:swap_right_and_middle_mouse_description"] = @"右クリックで削除、ホイールクリックでメニューを表示する方式になります。エディターを開いている間だけ有効です",
            // profile
            // named after the game's own listings on the same page, which read 「ゲスト難易度のビートマップ」,
            // 「Pendingのビートマップ」 and 「Graveyardのビートマップ」.
            [$@"{profile_prefix}:guest_pending_title"] = @"ゲスト難易度のPendingビートマップ",
            [$@"{profile_prefix}:guest_graveyard_title"] = @"ゲスト難易度のGraveyardビートマップ",

            // placement presets
            [$@"{preset_prefix}:header"] = @"配置プリセット",
            [$@"{preset_prefix}:menu_item"] = @"配置プリセット...",
            [$@"{preset_prefix}:every_beatmap"] = @"全譜面共通",
            [$@"{preset_prefix}:this_beatmap"] = @"この譜面",
            [$@"{preset_prefix}:keep_the_selection"] = @"選択範囲を登録",
            [$@"{preset_prefix}:nothing_kept_yet"] = @"登録したいものを選択して、下のボタンを押してください",
            [$@"{preset_prefix}:press_a_digit"] = @"数字キーを押すと割り当てます。Escを押すと割り当てを解除します",
            [$@"{preset_prefix}:assign_a_key"] = @"数字キーを割り当てる",
            [$@"{preset_prefix}:remove"] = @"削除",
            [$@"{binding_prefix}:editor_toggle_placement_presets"] = @"配置プリセットを開く",
        };

        private const string editor_prefix = @"osu.Game.Resources.Localisation.Editor";
        private const string editor_dialogs_prefix = @"osu.Game.Resources.Localisation.EditorDialogs";
        private const string settings_prefix = @"osu.Game.Resources.Localisation.LazerKaiSettings";
        private const string binding_prefix = @"osu.Game.Resources.Localisation.GlobalActionKeyBinding";
        private const string common_prefix = @"osu.Game.Resources.Localisation.Common";
        private const string profile_prefix = @"osu.Game.Resources.Localisation.LazerKaiProfile";
        private const string collab_prefix = @"osu.Game.Resources.Localisation.Collab";
        private const string export_prefix = @"osu.Game.Resources.Localisation.BeatmapExport";
        private const string preset_prefix = @"osu.Game.Resources.Localisation.PlacementPreset";

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
