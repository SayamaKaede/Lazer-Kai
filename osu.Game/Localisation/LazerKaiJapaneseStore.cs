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
            [$@"{editor_prefix}:editing_locked_by_preview_mods"] = @"オブジェクトを変更するModを反映している間は、編集できません",
            [$@"{editor_prefix}:live_mod_preview"] = @"編集中もModの表示を更新",

            // composing
            [$@"{editor_prefix}:advance_by_previous_spacing_on_placement"] = @"オブジェクト配置後に自動で次へシーク",
            [$@"{editor_prefix}:freehand_slider_placement"] = @"ドラッグでスライダーを描く",
            [$@"{editor_prefix}:auto_new_combo"] = @"自動New Combo",
            [$@"{editor_prefix}:auto_new_combo_description"] = @"小節を開くノーツに、置いた時点でNew Comboを付けます。すでに置いてあるものは変更しません",
            [$@"{editor_prefix}:auto_new_combo_interval"] = @"頻度",
            [$@"{editor_prefix}:auto_new_combo_half_bar"] = @"半小節ごと",
            [$@"{editor_prefix}:auto_new_combo_one_bar"] = @"1小節ごと",
            [$@"{editor_prefix}:auto_new_combo_two_bars"] = @"2小節ごと",
            [$@"{editor_prefix}:default_addition_bank"] = @"Addition bankの初期値",
            [$@"{editor_prefix}:default_addition_bank_description"] = @"Addition bankを持たないノーツにWhistle・Finish・Clapを付けたとき、このbankになります",
            [$@"{editor_prefix}:hide_grid_from_points_tool"] = @"ToolsのGRIDボタンを非表示",
            [$@"{editor_prefix}:hide_grid_from_points_tool_description"] = @"割り当てられたキーでも選択されなくなります。グリッドは右のGrid欄から設定できます",
            [$@"{editor_prefix}:addition_bank_auto"] = @"Auto",
            [$@"{editor_prefix}:addition_bank_normal"] = @"Normal",
            [$@"{editor_prefix}:addition_bank_soft"] = @"Soft",
            [$@"{editor_prefix}:addition_bank_drum"] = @"Drum",
            [$@"{editor_prefix}:stable_slider_controls"] = @"スライダーの操作をStableに近づける",
            [$@"{editor_prefix}:stable_slider_controls_description"] = @"Ctrl+クリックで追加したアンカーは、区間を分けずにその区間の形を決めます（直線の区間では、追加した1本だけ）。既存のアンカーをCtrl+クリックすると、曲線の途中の点は角になります。続けてCtrl+クリックすると、前後の角までの形を順に切り替えます",
            [$@"{editor_prefix}:anchor_add_type"] = @"追加するアンカーの種類",
            [$@"{editor_prefix}:anchor_add_type_description"] = @"編集中は Ctrl+1〜4 で切り替えできます。通常はパーフェクトカーブ推奨",
            [$@"{editor_prefix}:timeline_inspector"] = @"拍設定の上部にインスペクターを表示",
            [$@"{editor_prefix}:timeline_inspector_description"] = @"ティックの図の代わりに、右のパネルのインスペクターと同じ内容を表示します",
            [$@"{editor_prefix}:integer_positions"] = @"小数点以下の座標に配置できないようにする",
            [$@"{editor_prefix}:integer_positions_description"] = @"配置とドラッグでの移動が、常に整数の座標になります。小数点以下は、Stableへの持ち込みやアップロードで丸められるため、意図しないスタックやスタックずれの原因になります",
            [$@"{editor_prefix}:placement_anchor_type"] = @"スライダーを置くときのアンカーの種類",
            [$@"{editor_prefix}:placement_anchor_type_description"] = @"スライダーを直接置くときの種類です。配置中の Alt+1〜4 は、その区間だけこれより優先されます",
            [$@"{editor_prefix}:placement_anchor_type_automatic"] = @"自動",
            [$@"{editor_prefix}:allow_outside_play_area"] = @"グリッド外への配置を許可する",
            [$@"{editor_prefix}:allow_outside_play_area_description"] = @"選択した範囲を、グリッドの外まで移動・拡大縮小できるようになります。オフのときは、ドラッグのたびにグリッド内へ押し戻されるため、すでに外にはみ出している選択は移動できません",
            [$@"{editor_prefix}:anchor_type_linear"] = @"直線",
            [$@"{editor_prefix}:anchor_type_bezier"] = @"ベジエ",
            [$@"{editor_prefix}:anchor_type_perfect_curve"] = @"パーフェクトカーブ",
            [$@"{editor_prefix}:anchor_type_catmull"] = @"カトマル（古い譜面用）",
            [$@"{editor_prefix}:anchor_add_type_changed"] = @"追加するアンカーの種類: {0}",
            [$@"{editor_prefix}:auto_generate_breaks"] = @"休憩時間を自動で挿入",
            [$@"{editor_prefix}:auto_generate_breaks_description"] = @"ノーツを触るたび休憩時間が挿入されるのが嫌な場合はオフ。オフなら、編集メニューから自分で置く形になります",
            [$@"{editor_prefix}:insert_break"] = @"休憩時間を挿入",

            // display
            [$@"{editor_prefix}:show_play_area_border"] = @"プレイ画面の枠を表示",
            [$@"{editor_prefix}:show_auto_cursor"] = @"Autoのカーソルを表示",
            [$@"{editor_prefix}:show_auto_cursor_description"] = @"再生中もシーク中も、Autoと同じ動きでカーソルを表示します",
            [$@"{editor_prefix}:detect_offscreen"] = @"Offscreen検知",
            // stream conversion
            [$@"{editor_prefix}:convert_to_stream"] = @"連打に変換",
            [$@"{editor_prefix}:stream_count"] = @"数",
            [$@"{editor_prefix}:stream_extended_limits"] = @"16個より多く置けるようにする",
            [$@"{editor_prefix}:stream_snap_spacing"] = @"スペーシングをティックに合わせる",
            [$@"{editor_prefix}:stream_snap_spacing_description"] = @"形の終わりを、ディスタンススナップの目盛りの整数倍に合わせます。譜面全体でスペーシングをそろえたいときに使います。オフにすると、終わりを自由に置けます。",
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
            [$@"{editor_prefix}:show_difficulty_graph"] = @"難易度グラフを表示",
            [$@"{editor_prefix}:difficulty_graph_colour"] = @"難易度グラフの色",
            [$@"{editor_prefix}:show_volume_graph"] = @"音量のグラフを表示",
            [$@"{editor_prefix}:show_volume_graph_description"] = @"曲全体の音量の移り変わりを下部タイムラインに線で表示します",
            [$@"{editor_prefix}:volume_graph_ignore_five_percent"] = @"5%は無視する",
            [$@"{editor_prefix}:volume_graph_ignore_five_percent_description"] = @"音量5%の箇所を、音量の設定が無いものとして扱います。線は直前の音量のまま続きます",
            [$@"{editor_prefix}:volume_graph_colour"] = @"音量グラフの色",
            [$@"{editor_prefix}:volume_graph_thickness"] = @"音量グラフの太さ",
            [$@"{editor_prefix}:volume_graph_opacity"] = @"音量グラフの不透明度",
            [$@"{editor_prefix}:difficulty_graph_opacity"] = @"難易度グラフの不透明度",
            [$@"{editor_prefix}:difficulty_graph_detail"] = @"難易度グラフの精密さ",
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
            [$@"{editor_prefix}:status_show_save_format"] = @"保存形式",
            [$@"{editor_prefix}:status_show_save_format_description"] = @"「Stableとの互換性を強化する」がオンのときだけ表示されます",
            [$@"{editor_prefix}:status_show_difficulty_settings"] = @"譜面ステータス（AR・CS・OD・HP）",

            // reference difficulty
            [$@"{editor_prefix}:reference_difficulty"] = @"ゴースト表示する難易度",
            [$@"{editor_prefix}:reference_difficulty_none"] = @"なし",
            [$@"{editor_prefix}:reference_difficulty_opacity"] = @"不透明度",
            [$@"{editor_prefix}:reference_difficulty_colour"] = @"色",
            [$@"{editor_prefix}:always_expand_timeline_point_labels"] = @"ラベルを常に展開",
            [$@"{editor_prefix}:hide_timeline_points"] = @"SV・音量のラベルを非表示",
            [$@"{editor_prefix}:hide_timeline_points_description"] = @"非表示にしたぶん、ノーツが大きくなり、操作しやすくなります",
            [$@"{editor_prefix}:show_nightcore_cymbals"] = @"NightCoreシンバルの位置を表示",
            [$@"{editor_prefix}:show_nightcore_cymbals_description"] = @"NightCoreのシンバルが鳴る位置を、上部タイムラインに表示します。赤線の位置と、そこから4小節ごとです",

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
            [$@"{settings_prefix}:mania_header"] = @"mania",
            [$@"{settings_prefix}:mania_hide_keys"] = @"エディター内での下部キーを非表示",
            [$@"{settings_prefix}:mania_hide_keys_description"] = @"判定ラインの下にもレーンを表示します。通過したノーツが表示され、そこにもノーツを配置できます",
            [$@"{settings_prefix}:mania_stage_spacing"] = @"エディター内でのステージ間の隙間",
            [$@"{settings_prefix}:mania_stage_spacing_description"] = @"2ステージ（Dual stages）の譜面で、ステージを中央に並べ、この幅だけ間を空けます",
            [$@"{settings_prefix}:mania_tick_thickness"] = @"エディター内でのティックの線の太さ",
            [$@"{settings_prefix}:mania_tick_opacity"] = @"エディター内でのティックの線の不透明度",
            [$@"{settings_prefix}:mania_tick_description"] = @"レーンに表示される線が対象です。タイムラインの線は変わりません",
            [$@"{export_prefix}:format"] = @"出力する形式",
            [$@"{export_prefix}:format_stable"] = @"Stable向け（v14 / .osz）",
            [$@"{export_prefix}:format_lazer"] = @"Lazer向け（変換しない / .olz）",
            [$@"{export_prefix}:export_difficulty"] = @"この難易度を出力",
            [$@"{export_prefix}:export_set"] = @"このセットを出力",

            // keeping stable's own files
            [$@"{stable_compatibility_prefix}:improve_compatibility"] = @"Stableとの互換性を強化する（試験的）",
            [$@"{stable_compatibility_prefix}:improve_compatibility_description"] = @"Stable形式（v14以前）の譜面を、編集していない行は元のまま保存し、Stable向けにはそのまま書き出します",
            [$@"{stable_compatibility_prefix}:notice_header"] = @"Stableとの互換性の強化について",
            [$@"{stable_compatibility_prefix}:notice_supported_modes"] = @"・現在はstandardモードのみしか対応していません",
            [$@"{stable_compatibility_prefix}:notice_backups"] = @"・このオプションが有効の時、Stable形式の譜面を開いたときに自動でバックアップが取られます。オプション > その他 > バックアップから、保持する個数を増やすことをおすすめします (例: 間隔15分 個数100個)",
            [$@"{stable_compatibility_prefix}:notice_warning"] = @"・もしStable形式で保存に失敗した場合は警告が出ます。エラーは自動で送信されるので、不具合の修正をお待ちください。",
            [$@"{stable_compatibility_prefix}:notice_ok"] = @"OK",
            [$@"{stable_compatibility_prefix}:saved_in_lazer_format"] = @"この編集はまだStable形式のまま保存できないため、Lazer形式で保存しました",
            [$@"{stable_compatibility_prefix}:dropped_file_saved_in_lazer_format"] = @"ドロップした「{0}」はStable形式のまま保存できなかったため、Lazer形式で保存しました",
            [$@"{stable_compatibility_prefix}:cannot_stay_stable_header"] = @"この編集はまだStable形式のまま保存できません",
            [$@"{stable_compatibility_prefix}:cannot_stay_stable_body"] = @"保存すると、この難易度はLazer形式になり、以後もLazer形式で扱われます。保存する前に、今のファイルを控えとしてバックアップのフォルダに残します。原因はログに記録しました。",
            [$@"{stable_compatibility_prefix}:save_in_lazer_format"] = @"Lazer形式で保存する",
            [$@"{stable_compatibility_prefix}:do_not_save"] = @"保存しない",
            [$@"{stable_compatibility_prefix}:convert"] = @"変換",
            [$@"{stable_compatibility_prefix}:convert_to_stable_format"] = @"v14形式に変換",
            [$@"{stable_compatibility_prefix}:convert_to_stable_format_header"] = @"この難易度をv14形式に変換しますか？",
            [$@"{stable_compatibility_prefix}:convert_to_stable_format_body"] = @"未保存の変更も含めて、Stable向けの出力と同じ形に変換して保存し、以後はStable形式として扱います（時刻と座標は整数に、複数の区間があるスライダーはベジエ曲線になります）。元に戻せません。",

            // the saved file's red and green lines on the timeline
            [$@"{saved_lines_prefix}:show_stable_green_lines"] = @"Stableの緑線を表示",
            [$@"{saved_lines_prefix}:show_stable_green_lines_description"] = @"上部タイムラインが、StableかLazer-Kaiモードの場合にのみ適用されます",
            [$@"{saved_lines_prefix}:red_line"] = @"赤線 {0}・BPM {1}・{2}/4・{3} #{4}・音量{5}%",
            [$@"{saved_lines_prefix}:green_line"] = @"緑線 {0}・SV x{1}・{2} #{3}・音量{4}%",
            [$@"{saved_lines_prefix}:kiai"] = @"・Kiai",
            [$@"{saved_lines_prefix}:put_in_on_save"] = @"（保存時に追加されます）",

            // the green line panel
            [$@"{green_line_prefix}:title"] = @"緑線",
            [$@"{green_line_prefix}:green_line"] = @"緑線",
            [$@"{green_line_prefix}:red_line"] = @"赤線",
            [$@"{green_line_prefix}:add"] = @"追加",
            [$@"{green_line_prefix}:remove"] = @"削除",
            [$@"{green_line_prefix}:cancel"] = @"キャンセル",
            [$@"{green_line_prefix}:confirm"] = @"確定",
            [$@"{green_line_prefix}:time"] = @"時刻",
            [$@"{green_line_prefix}:use_current_time"] = @"現在の時刻を使う",
            [$@"{green_line_prefix}:paste_at_current_time"] = @"選択した線を現在位置から貼り付け",
            [$@"{green_line_prefix}:use_green_line_velocity"] = @"緑線のSVを使う（{0}x）",
            [$@"{green_line_prefix}:using_green_line_velocity"] = @"緑線のSVを使用中",
            [$@"{green_line_prefix}:remove_unneeded"] = @"不要な緑線を削除",
            [$@"{green_line_prefix}:remove_unneeded_header"] = @"不要な緑線を削除しますか？",
            [$@"{green_line_prefix}:remove_unneeded_body"] = @"何も変えていない緑線を削除します（手前の線と同じ値の線、同じ時刻の別の線と同じ値の線）。同じ時刻に値の違う線が重なっている箇所は残します。Undoで元に戻せます。",
            [$@"{green_line_prefix}:removed"] = @"不要な緑線を{0}本削除しました",
            [$@"{green_line_prefix}:none_removed"] = @"不要な緑線はありませんでした",
            [$@"{green_line_prefix}:slider_velocity"] = @"SV",
            [$@"{green_line_prefix}:sample_set"] = @"サンプルセット",
            [$@"{green_line_prefix}:custom_index"] = @"カスタム番号",
            [$@"{green_line_prefix}:volume"] = @"音量",
            [$@"{green_line_prefix}:filter_all"] = @"すべて",
            [$@"{green_line_prefix}:filter_red_lines"] = @"赤線のみ",
            [$@"{green_line_prefix}:filter_green_lines"] = @"緑線のみ",
            [$@"{green_line_prefix}:whole_offset"] = @"全体のオフセットを調整...",
            [$@"{green_line_prefix}:whole_offset_header"] = @"全体のオフセットを調整",
            [$@"{green_line_prefix}:whole_offset_body"] = @"赤線・緑線・ノーツ・休憩時間・ブックマーク・プレビューポイントを、すべて同じ時間だけ移動します。そのあと、ティックから2ms未満ずれているノーツと緑線をティックに合わせます。元に戻すことができます。",
            [$@"{green_line_prefix}:whole_offset_amount"] = @"移動する時間（ms）",
            [$@"{green_line_prefix}:whole_offset_amount_hint"] = @"負の数を入力すると、前に移動します。0の場合は移動せず、ティックからずれているものだけを合わせます。",
            [$@"{green_line_prefix}:whole_offset_apply"] = @"適用",
            [$@"{green_line_prefix}:whole_offset_moved"] = @"全体を{0}ms移動しました。ティックに合わせた数: ノーツ{1}個、緑線{2}本",
            [$@"{green_line_prefix}:whole_offset_pending"] = @"先に、緑線の変更を確定するか取り消してください",
            [$@"{green_line_prefix}:copy_red_lines"] = @"タイミングをコピー...",
            [$@"{green_line_prefix}:copy_red_lines_header"] = @"タイミングをコピー",
            [$@"{green_line_prefix}:copy_red_lines_body"] = @"タイミング（赤線の位置・BPM・拍子）を、別の難易度のもので置き換えます。",
            [$@"{green_line_prefix}:copy_red_lines_direction"] = @"向き",
            [$@"{green_line_prefix}:copy_red_lines_from_another"] = @"他の難易度 → この難易度",
            [$@"{green_line_prefix}:copy_red_lines_to_others"] = @"この難易度 → 他の難易度",
            [$@"{green_line_prefix}:copy_red_lines_source"] = @"コピー元",
            [$@"{green_line_prefix}:copy_red_lines_targets"] = @"コピー先",
            [$@"{green_line_prefix}:copy_red_lines_to_others_warning"] = @"チェックした難易度を直接書き換えます。Undo はできません",
            [$@"{green_line_prefix}:copy_red_lines_keep_beats"] = @"ノーツもタイミングに合わせて移動する",
            [$@"{green_line_prefix}:copy_red_lines_keep_beats_hint"] = @"ノーツ・緑線・ブックマーク・休憩時間を、新しいタイミングでも同じ小節の同じティックに来るように移動します。オフセットが後ろにずれていればその分だけ後ろへ移動し、BPMが違えば間隔も変わります。オフの場合は、下の上限以内のものだけをティックに合わせます。",
            [$@"{green_line_prefix}:copy_red_lines_within"] = @"スナップするズレの上限（ms）",
            [$@"{green_line_prefix}:copy_red_lines_within_hint"] = @"タイミングを置き換えたあと、ティックからこの ms 以内にあるノーツ・緑線・ブックマークをティックに合わせます。0の場合はスナップしません。",
            [$@"{green_line_prefix}:copy_red_lines_apply"] = @"コピー",
            [$@"{green_line_prefix}:copy_red_lines_none"] = @"コピー元の難易度に赤線がありません",
            [$@"{green_line_prefix}:copy_red_lines_done"] = @"タイミングをコピーしました（赤線{0}本）。ティックに合わせた数: ノーツ{1}個、緑線{2}本。ティックから外れているノーツ: {3}個",
            [$@"{green_line_prefix}:copy_red_lines_done_to_others"] = @"{0}個の難易度にタイミングをコピーしました。ティックに合わせた数: ノーツ{1}個、緑線{2}本",
            [$@"{green_line_prefix}:copy_red_lines_off_ticks_in"] = @"ティックから外れているノーツがある難易度: {0}",
            [$@"{green_line_prefix}:copy_red_lines_not_kept"] = @"Stable形式のまま保存できないため、コピーしていない難易度: {0}",
            [$@"{green_line_prefix}:copy_red_lines_failed"] = @"コピーに失敗した難易度: {0}",
            [$@"{green_line_prefix}:not_editable"] = @"「Stableとの互換性を強化する」がオンのとき、Stable形式（v14以前）のosu!の譜面で緑線を編集できます",

            // HS モード
            [$@"{editor_prefix}:hitsound_screen"] = @"HS",
            [$@"{hitsound_prefix}:additions"] = @"ヒットサウンド",
            [$@"{hitsound_prefix}:sampleset"] = @"Sampleset",
            [$@"{hitsound_prefix}:addition_bank"] = @"Additions",
            [$@"{hitsound_prefix}:selected"] = @"選択中のノーツ {0}個",
            [$@"{hitsound_prefix}:circle"] = @"サークル",
            [$@"{hitsound_prefix}:slider_head"] = @"スライダーの頭",
            [$@"{hitsound_prefix}:slider_reverse"] = @"スライダーのリバース",
            [$@"{hitsound_prefix}:slider_tail"] = @"スライダーの尻尾",
            [$@"{hitsound_prefix}:spinner_end"] = @"スピナーの終わり",
            [$@"{hitsound_prefix}:copy_into_new_difficulty"] = @"ノーツをコピーして作成",
            [$@"{hitsound_prefix}:make_hitsound_difficulty"] = @"音の位置をサークルにして作成",
            [$@"{hitsound_prefix}:make_mania_hitsound_difficulty"] = @"maniaで作成（空の難易度）",
            [$@"{hitsound_prefix}:mania_difficulty_unavailable"] = @"osu!の譜面で使用できます",
            [$@"{hitsound_prefix}:lane_sounds_header"] = @"レーンの音",
            [$@"{hitsound_prefix}:lane_sounds_hint"] = @"ノーツは、置いたレーンの音になります。",
            [$@"{hitsound_prefix}:open_sound_folder"] = @"ヒットサウンドのフォルダーを開く",
            [$@"{hitsound_prefix}:reload_sound_folder"] = @"ヒットサウンドのフォルダーを再読み込み",
            [$@"{hitsound_prefix}:sound_folder_empty"] = @"ヒットサウンドのフォルダーが空です。wav・ogg・mp3のファイルを入れてください。",
            [$@"{hitsound_prefix}:lane"] = @"レーン {0}",
            [$@"{hitsound_prefix}:select_lane_notes"] = @"クリックでこのレーンのノーツをすべて選択",
            [$@"{hitsound_prefix}:lanes_mixed"] = @"レーン {0} は、同時に鳴る箇所で1つのファイルに混ざります",
            [$@"{hitsound_prefix}:lane_bank"] = @"サンプルセット",
            [$@"{hitsound_prefix}:lane_kind"] = @"ヒットサウンド",
            [$@"{hitsound_prefix}:play_lane_sound"] = @"再生",
            [$@"{hitsound_prefix}:convert_to_hitsound_difficulty"] = @"HS diffに変換",
            [$@"{hitsound_prefix}:convert_volumes"] = @"音量も含める",
            [$@"{hitsound_prefix}:convert_volumes_hint"] = @"オフの場合、HS diff側の音量をそのまま保ちます。",
            [$@"{hitsound_prefix}:mute_slider_bodies"] = @"スライダーボディの音を無音にする",
            [$@"{hitsound_prefix}:mute_slider_bodies_hint"] = @"HS diffのラインが使うサンプルセットとカスタム番号の組ごとに、無音のsliderslideを書き出します。同じ名前のファイルがすでにある場合は、そのまま残します。",
            [$@"{hitsound_prefix}:lane_volume"] = @"音量（再生位置から）",
            [$@"{hitsound_prefix}:fallback_sound"] = @"normalの音が無いサークルの音",
            [$@"{hitsound_prefix}:convert_mania_unavailable"] = @"「Stableとの互換性を強化する」がオンのとき、Stable形式（v14以前）のosu!maniaの譜面で使用できます",
            [$@"{hitsound_prefix}:nothing_to_convert"] = @"変換するノーツがありません",
            [$@"{hitsound_prefix}:cannot_mix_sounds"] = @"同じ枠に入る音を1つのファイルに混ぜる処理に失敗しました: {0}",
            [$@"{hitsound_prefix}:replace_sound_files"] = @"同じ名前で中身の違う音のファイルが譜面にあります: {0}。置き換えますか？",
            [$@"{hitsound_prefix}:converted_to_hitsound_difficulty"] = @"{0}に変換しました。サークル{1}個、音のファイル{2}個",
            [$@"{hitsound_prefix}:lane_presets"] = @"プリセット",
            [$@"{hitsound_prefix}:choose_a_lane_preset"] = @"プリセットから選択",
            [$@"{hitsound_prefix}:save_as_a_lane_preset"] = @"プリセットに登録",
            [$@"{hitsound_prefix}:no_lane_presets_yet"] = @"登録したプリセットはありません",
            [$@"{hitsound_prefix}:lane_preset_name"] = @"プリセットの名前",
            [$@"{hitsound_prefix}:save_lane_preset"] = @"登録",
            [$@"{hitsound_prefix}:overwrite_lane_preset"] = @"上書き",
            [$@"{hitsound_prefix}:no_lane_sounds_to_save"] = @"音を割り当てたレーンがありません",
            [$@"{hitsound_prefix}:lane_preset_sounds_missing"] = @"ヒットサウンドのフォルダーにない音は割り当てていません: {0}",
            [$@"{hitsound_prefix}:lane_sounds_missing"] = @"ヒットサウンドのフォルダーにない音があります: {0}",
            [$@"{hitsound_prefix}:new_difficulty_unavailable"] = @"「Stableとの互換性を強化する」がオンのとき、Stable形式（v14以前）のosu!の譜面で使用できます",
            [$@"{hitsound_prefix}:could_not_read_objects"] = @"読み取れないノーツがあったため、難易度を作成しませんでした",
            [$@"{hitsound_prefix}:selected_suffix"] = @"（選択中）",
            [$@"{hitsound_prefix}:slider_body"] = @"スライダーの本体",
            [$@"{hitsound_prefix}:outline_colour"] = @"選択枠の色",
            [$@"{hitsound_prefix}:outline_thickness"] = @"選択枠の太さ",
            [$@"{hitsound_prefix}:outline_opacity"] = @"選択枠の不透明度",
            [$@"{hitsound_prefix}:new_difficulty_header"] = @"新しい難易度",
            [$@"{hitsound_prefix}:display_header"] = @"設定",
            [$@"{hitsound_prefix}:panel_opacity"] = @"中央の不透明度",
            [$@"{hitsound_prefix}:change_sound"] = @"変更したときの音",
            [$@"{hitsound_prefix}:change_sound_menu"] = @"メニューの効果音",
            [$@"{hitsound_prefix}:change_sound_hitsound"] = @"ヒットサウンド",
            [$@"{hitsound_prefix}:same_as_previous"] = @"手前のHSをコピー",
            [$@"{hitsound_prefix}:copy_hitsounds"] = @"HSをコピー",
            [$@"{hitsound_prefix}:paste_hitsounds"] = @"HSを貼り付け",
            [$@"{hitsound_prefix}:copy_from_header"] = @"ほかの難易度からHSをコピー",
            [$@"{hitsound_prefix}:copy_from_source"] = @"コピー元",
            [$@"{hitsound_prefix}:copy_from_button"] = @"この難易度にコピー",
            [$@"{hitsound_prefix}:copy_from_unavailable"] = @"「Stableとの互換性を強化する」がオンのとき、Stable形式（v14以前）のosu!の譜面で、同じセットにほかのosu!の難易度があると使用できます",
            [$@"{hitsound_prefix}:could_not_read_difficulty"] = @"コピー元の難易度のファイルを読み取れませんでした",
            [$@"{hitsound_prefix}:near_lines_header"] = @"Unsnapかもしれない緑線",
            [$@"{hitsound_prefix}:show_harmless_unsnaps"] = @"影響がないUnsnapも表示",
            // a custom index of 0, as stable names it; the same in Japanese, as Sampleset and Additions are.
            [$@"{hitsound_prefix}:default_index"] = @"Default",
            [$@"{hitsound_prefix}:keep"] = @"固定",
            [$@"{hitsound_prefix}:keep_tooltip"] = @"置いたあとも残す",
            [$@"{hitsound_prefix}:no_near_lines"] = @"なし",
            [$@"{hitsound_prefix}:line_after_taken"] = @"{0}の{1}ms後（適用）",
            [$@"{hitsound_prefix}:line_after_not_taken"] = @"{0}の{1}ms後（適用されない）",
            [$@"{hitsound_prefix}:line_before_taken"] = @"{0}の{1}ms前（適用）",
            [$@"{hitsound_prefix}:line_before_not_taken"] = @"{0}の{1}ms前（適用されない）",
            [$@"{hitsound_prefix}:line_at"] = @"緑線 {0}",
            [$@"{hitsound_prefix}:line_to_add"] = @"緑線 {0}（新規）",
            [$@"{hitsound_prefix}:lines_of_selection"] = @"緑線（選択した箇所）",
            [$@"{hitsound_prefix}:advance_by_previous_spacing"] = @"サークル配置後に自動で次へシーク",
            [$@"{hitsound_prefix}:paste_hitsounds_only"] = @"HSだけ貼り付け",
            [$@"{hitsound_prefix}:place_circle"] = @"サークルを置く",
            [$@"{hitsound_prefix}:place_circle_with_previous"] = @"手前のHSで置く",
            [$@"{hitsound_prefix}:status_copied"] = @"HSのコピー ({0} {1}個)",
            [$@"{hitsound_prefix}:status_nothing_to_copy"] = @"HSのコピー (コピーする音なし)",
            [$@"{hitsound_prefix}:status_pasted"] = @"HSの貼り付け ({0} {1}個)",
            [$@"{hitsound_prefix}:status_nothing_to_paste_onto"] = @"HSの貼り付け (貼り付け先の音なし)",
            [$@"{hitsound_prefix}:status_nothing_copied"] = @"HSの貼り付け (コピーしたHSなし)",
            [$@"{hitsound_prefix}:status_copied_previous"] = @"手前のHSをコピー ({0} → {1} {2}個)",
            [$@"{hitsound_prefix}:status_nothing_to_copy_onto"] = @"手前のHSをコピー (コピー先の音なし)",
            [$@"{hitsound_prefix}:status_nothing_before"] = @"手前のHSをコピー (手前の音なし)",

            // 他のセットからインポート・ファイル名を編集
            [$@"{beatmap_files_prefix}:edit_file_names"] = @"ファイル名を編集",
            [$@"{beatmap_files_prefix}:drop_hint"] = @".wav・.mp3・.ogg のファイルをこの画面にドラッグ＆ドロップすると、このセットに追加できます（15秒未満）",
            [$@"{beatmap_files_prefix}:sounds_added"] = @"ヒットサウンドを{0}個追加しました",
            [$@"{beatmap_files_prefix}:too_long_to_add"] = @"15秒以上のファイルは追加できません: {0}",
            [$@"{beatmap_files_prefix}:not_read_as_sound"] = @"音として読み取れないファイルは追加していません: {0}",
            [$@"{beatmap_files_prefix}:name_of_the_song_not_added"] = @"曲のファイルと同じ名前のファイルは追加できません: {0}",
            [$@"{beatmap_files_prefix}:import_from_another_set"] = @"他のセットからインポート",
            [$@"{beatmap_files_prefix}:search_sets"] = @"セットを検索",
            [$@"{beatmap_files_prefix}:more_sets"] = @"ほかに{0}件あります。検索で絞り込んでください",
            [$@"{beatmap_files_prefix}:no_sets_found"] = @"該当するセットはありません",
            [$@"{beatmap_files_prefix}:choose_a_set"] = @"左の一覧からセットを選択してください",
            [$@"{beatmap_files_prefix}:set_summary"] = @"{0} · 難易度 {1}",
            [$@"{beatmap_files_prefix}:difficulties"] = @"難易度",
            [$@"{beatmap_files_prefix}:sounds"] = @"ヒットサウンド",
            [$@"{beatmap_files_prefix}:no_sounds"] = @"ヒットサウンドはありません",
            [$@"{beatmap_files_prefix}:select_all"] = @"すべて選択",
            [$@"{beatmap_files_prefix}:select_none"] = @"すべて解除",
            [$@"{beatmap_files_prefix}:play_sound"] = @"再生",
            [$@"{beatmap_files_prefix}:there_already"] = @"同じファイルがあります",
            [$@"{beatmap_files_prefix}:in_place_of_another"] = @"同じ名前の別のヒットサウンドがあります。チェックすると上書きします",
            [$@"{beatmap_files_prefix}:name_of_the_song"] = @"曲のファイルと同じ名前のためインポートできません",
            [$@"{beatmap_files_prefix}:import"] = @"インポート（難易度{0}・ヒットサウンド{1}）",
            [$@"{beatmap_files_prefix}:overwrite_sounds"] = @"このセットのヒットサウンドを上書きします: {0}。よろしいですか？",
            [$@"{beatmap_files_prefix}:imported"] = @"インポートしました（難易度{0}・ヒットサウンド{1}）",
            [$@"{beatmap_files_prefix}:shift_custom_index"] = @"カスタム番号をずらしてインポート",
            [$@"{beatmap_files_prefix}:shift_custom_index_hint"] = @"soft-hitclap2.wav のような名前のヒットサウンドを、カスタム番号を右の数だけ大きくした名前でインポートします。このセットにあるヒットサウンドを上書きせずにインポートできます。",
            [$@"{beatmap_files_prefix}:shift_difficulties_too"] = @"譜面内のカスタム番号もずらす",
            [$@"{beatmap_files_prefix}:shift_difficulties_too_hint"] = @"一緒にインポートする難易度の、緑線・赤線とノーツのカスタム番号も、同じ数だけ大きくします。インポートした難易度で、インポートしたヒットサウンドを鳴らす場合にオンにします。",
            [$@"{beatmap_files_prefix}:shift_by"] = @"ずらす数",
            [$@"{beatmap_files_prefix}:shifted_from"] = @"{0}（元の名前: {1}）",
            [$@"{beatmap_files_prefix}:choose_a_sound"] = @"左の一覧からヒットサウンドを選択してください",
            [$@"{beatmap_files_prefix}:sample_set"] = @"サンプルセット",
            [$@"{beatmap_files_prefix}:sound_kind"] = @"音の種類",
            [$@"{beatmap_files_prefix}:custom_index"] = @"カスタム番号",
            [$@"{beatmap_files_prefix}:custom_index_hint"] = @"1 は番号なしの名前になります",
            [$@"{beatmap_files_prefix}:new_name"] = @"変更後の名前: {0}",
            [$@"{beatmap_files_prefix}:name_taken"] = @"この名前は使用中です: {0}",
            [$@"{beatmap_files_prefix}:rename"] = @"名前を変更",

            // リプレイの解析設定
            [$@"{replay_prefix}:hide_hidden_effect"] = @"HDの効果を非表示",
            [$@"{replay_prefix}:hide_flashlight_effect"] = @"FLの効果を非表示",
            [$@"{replay_prefix}:misses"] = @"ミスの一覧",
            [$@"{replay_prefix}:judging"] = @"判定を計算しています...",
            [$@"{replay_prefix}:no_misses"] = @"ミス・スライダーブレイク・100・50 はありません。",
            [$@"{replay_prefix}:miss"] = @"ミス",
            [$@"{replay_prefix}:slider_break"] = @"スライダーブレイク",

            // 曲選択画面
            [$@"{song_select_prefix}:check_for_update"] = @"更新を確認",
            [$@"{song_select_prefix}:checking"] = @"「{0}」の更新を確認しています...",
            [$@"{song_select_prefix}:up_to_date"] = @"「{0}」は最新です",
            [$@"{song_select_prefix}:update_available"] = @"「{0}」に更新があります",
            [$@"{song_select_prefix}:check_failed"] = @"更新を確認できませんでした。オンラインに接続していますか？",
            [$@"{song_select_prefix}:delete_difficulty"] = @"この難易度を削除...",
            [$@"{song_select_prefix}:search_presets"] = @"検索プリセット",
            [$@"{song_select_prefix}:no_search_presets"] = @"保存したプリセットはありません。",
            [$@"{song_select_prefix}:search_preset_name"] = @"名前（空欄なら検索の文字列）",
            [$@"{song_select_prefix}:save_search_preset"] = @"今の検索を保存",
            [$@"{song_select_prefix}:user_tags"] = @"ユーザータグ",
            [$@"{song_select_prefix}:user_tag_counts"] = @"数字は、今表示している難易度のうち、そのタグが付いている数です。",
            [$@"{song_select_prefix}:no_user_tags"] = @"今表示している難易度には、ユーザータグがありません。",
            [$@"{song_select_prefix}:set_mods_on_difficulty"] = @"現在のModを難易度に設定",
            [$@"{song_select_prefix}:clear_difficulty_mods"] = @"難易度のModを解除",
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
            [$@"{editor_prefix}:backup_up_to_date"] = @"バックアップは最新です",

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
            // the fork's name is written the same way in Japanese; it is here so that it reads as decided rather
            // than as a string nobody got to.
            [$@"{settings_prefix}:section_header"] = @"Lazer-Kai",

            // the manual and the changelogs that ship beside the game
            [$@"{settings_prefix}:manual_header"] = @"説明書・Changelog",
            [$@"{settings_prefix}:open_manual"] = @"説明書を開く",
            [$@"{settings_prefix}:open_manual_description"] = @"このビルドに同梱されている説明書を、ブラウザで表示します",
            [$@"{settings_prefix}:open_guide"] = @"「はじめに」を開く",
            [$@"{settings_prefix}:open_guide_description"] = @"主な機能とよくある質問を、ブラウザで表示します",
            [$@"{settings_prefix}:show_changelog"] = @"Changelogを表示",
            [$@"{settings_prefix}:changelog_title"] = @"Changelog",

            // the look the game wears.
            [$@"{settings_prefix}:appearance_header"] = @"見た目",
            [$@"{settings_prefix}:ui_theme"] = @"テーマ",
            [$@"{settings_prefix}:ui_theme_description"] = @"次回の起動から適用されます",
            [$@"{settings_prefix}:ui_theme_lazer"] = @"osu!lazer",
            [$@"{settings_prefix}:ui_theme_lazer_kai"] = @"Lazer-Kai",

            [$@"{settings_prefix}:beatmap_section_header"] = @"譜面",
            [$@"{settings_prefix}:test_play_section_header"] = @"Mods",
            [$@"{settings_prefix}:other_section_header"] = @"その他",
            [$@"{settings_prefix}:display_header"] = @"表示",
            [$@"{settings_prefix}:difficulty_graph_header"] = @"下部タイムライン",
            [$@"{settings_prefix}:ghost_difficulty_header"] = @"ゴースト表示",
            [$@"{settings_prefix}:hitsound_header"] = @"ヒットサウンド",
            [$@"{settings_prefix}:other_customisation_header"] = @"その他のカスタマイズ",
            [$@"{settings_prefix}:toolbox_header"] = @"左右ツールボックス",
            [$@"{settings_prefix}:button_shape_header"] = @"左右ツールボックス - ボタン",
            [$@"{settings_prefix}:timeline_header"] = @"上部タイムライン",
            [$@"{settings_prefix}:selection_header"] = @"選択・範囲選択枠",
            [$@"{settings_prefix}:status_header"] = @"STATUS",
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
            // モードごとのスキン
            [$@"{settings_prefix}:ruleset_skin_header"] = @"モードごとのスキン",
            [$@"{settings_prefix}:ruleset_skin_not_set"] = @"指定なし",
            [$@"{settings_prefix}:ruleset_skin_description"] = @"このモードを選んでいる間に使用するスキン。指定なしなら、スキン設定で選んだスキンを使用します",
            [$@"{settings_prefix}:export_logs_description"] = @"直近のログを、Windowsのアカウント名を伏せた状態でコピーし、その場所を開きます",
            [$@"{settings_prefix}:exported_logs"] = @"ログを{0}件書き出しました。クリックすると場所を開きます",
            [$@"{settings_prefix}:export_logs_failed"] = @"ログを書き出せませんでした。",
            [$@"{settings_prefix}:error_reporting"] = @"エラーを自動で送信する",
            [$@"{settings_prefix}:error_reporting_description"] = @"エラーを、Lazer-Kaiの開発者に送信します。osu!のアカウント、Windowsのアカウント名、PCの名前は送信しません",
            [$@"{settings_prefix}:settings_preset_header"] = @"設定のプリセット",
            [$@"{settings_prefix}:settings_preset"] = @"プリセット",
            [$@"{settings_prefix}:settings_preset_description"] = @"Lazer-Kaiの設定だけが対象です。Stableとの互換性・エラーの自動送信・Mapping Toolsのパスは変わりません。おすすめ設定では、色とスキンも変わりません",
            [$@"{settings_prefix}:recommended_settings"] = @"おすすめ設定",
            [$@"{settings_prefix}:settings_before_applying"] = @"適用前の設定",
            [$@"{settings_prefix}:apply_settings_preset"] = @"このプリセットを適用する",
            [$@"{settings_prefix}:apply_settings_preset_header"] = @"「{0}」を適用しますか？",
            [$@"{settings_prefix}:apply_settings_preset_body"] = @"Lazer-Kaiの設定が上書きされます。色・スキン・Stableとの互換性・エラーの自動送信・Mapping Toolsのパスは変わりません。今の設定は「適用前の設定」として残します。",
            [$@"{settings_prefix}:apply_saved_settings_preset_body"] = @"色とスキンを含めて、Lazer-Kaiの設定が上書きされます。Stableとの互換性・エラーの自動送信・Mapping Toolsのパスは変わりません。今の設定は「適用前の設定」として残します。",
            [$@"{settings_prefix}:apply_settings_preset_confirm"] = @"適用する",
            [$@"{settings_prefix}:applied_settings_preset"] = @"「{0}」を適用しました",
            [$@"{settings_prefix}:settings_preset_name"] = @"名前",
            [$@"{settings_prefix}:save_settings_preset"] = @"今の設定をこの名前で保存する",
            [$@"{settings_prefix}:saved_settings_preset"] = @"「{0}」として保存しました",
            [$@"{settings_prefix}:delete_settings_preset"] = @"このプリセットを削除する",
            [$@"{settings_prefix}:delete_settings_preset_header"] = @"「{0}」を削除しますか？",
            [$@"{settings_prefix}:open_settings_preset_folder"] = @"プリセットのフォルダを開く",
            [$@"{settings_prefix}:show_mapping_tools_path"] = @"Mapping Tools のパスを表示",
            [$@"{settings_prefix}:hide_mapping_tools_path"] = @"Mapping Tools のパスを隠す",
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
            [$@"{binding_prefix}:editor_toggle_volume_graph"] = @"音量のグラフの表示を切り替え",
            [$@"{binding_prefix}:editor_hitsound_mode"] = @"HSモード",
            [$@"{binding_prefix}:editor_hitsound_same_as_previous"] = @"HS: 手前のHSをコピー",
            [$@"{binding_prefix}:editor_hitsound_place_circle"] = @"HS: サークルを置く",
            [$@"{binding_prefix}:editor_hitsound_place_circle_with_previous"] = @"HS: 手前のHSでサークルを置く",
            [$@"{binding_prefix}:editor_hitsound_paste_only"] = @"HS: HSだけ貼り付け",
            [$@"{binding_prefix}:toggle_replay_miss_timeline"] = @"ミスのタイムラインの表示を切り替え",
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
            [$@"{editor_prefix}:advance_by_previous_spacing_on_placement_description"] = @"ノーツを配置後、自動で直前のノーツとの間隔ぶん、自動で移動します。毎回のホイール操作が不要になります。maniaでは別の設定で、エディターの編集メニューから切り替えます（既定はオフ）",
            [$@"{editor_prefix}:freehand_slider_placement_description"] = @"スライダーを置くときに誤爆しがちな場合はオフに",
            // profile
            // named after the game's own listings on the same page, which read 「ゲスト難易度のビートマップ」,
            // 「Pendingのビートマップ」 and 「Graveyardのビートマップ」.
            [$@"{profile_prefix}:guest_pending_title"] = @"ゲスト難易度のPendingビートマップ",
            [$@"{profile_prefix}:guest_graveyard_title"] = @"ゲスト難易度のGraveyardビートマップ",
            // downloading a whole listing at once
            [$@"{profile_prefix}:download_all"] = @"すべてダウンロード",
            [$@"{profile_prefix}:download_all_tooltip"] = @"長押しで、この項目の未入手の譜面をすべてダウンロードします",
            [$@"{profile_prefix}:download_all_needs_login"] = @"ダウンロードにはログインが必要です",
            [$@"{profile_prefix}:download_all_reading"] = @"項目の一覧を取得しています…",
            [$@"{profile_prefix}:download_all_could_not_read"] = @"一覧を取得できませんでした",
            [$@"{profile_prefix}:download_all_nothing_left"] = @"この項目の譜面は、すべて入手済みです",
            [$@"{profile_prefix}:download_all_progress"] = @"ダウンロード中 {0}/{1}",
            [$@"{profile_prefix}:download_all_already_have"] = @"{0}件は入手済み",
            [$@"{profile_prefix}:download_all_unavailable"] = @"{0}件は配布停止",
            [$@"{profile_prefix}:download_all_done"] = @"{0}件の譜面をダウンロードしました",
            [$@"{profile_prefix}:download_all_failed"] = @"{0}件は失敗しました",

            // what a beatmap is made of, on the details tab at song select
            [$@"{mapping_prefix}:density"] = @"密度",
            [$@"{mapping_prefix}:density_value"] = @"{0}個/秒",
            // these four keep their English names in Japanese as well: they are what the editor calls them, and a
            // mapper reads them here as the same things.
            [$@"{mapping_prefix}:snap"] = @"Snap",
            [$@"{mapping_prefix}:unsnapped"] = @"Unsnapped",
            [$@"{mapping_prefix}:none_of_them"] = @"なし",
            [$@"{mapping_prefix}:bpm_changes"] = @"BPM変更",
            [$@"{mapping_prefix}:times_over"] = @"{0}回",
            [$@"{mapping_prefix}:volume"] = @"音量",
            [$@"{mapping_prefix}:slider_velocity"] = @"SV",
            [$@"{mapping_prefix}:kiai"] = @"Kiai",
            [$@"{mapping_prefix}:breaks"] = @"休憩時間",

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

            // combo colour presets, beside the combo colours on the setup screen
            [$@"{combo_colour_preset_prefix}:presets"] = @"プリセット",
            [$@"{combo_colour_preset_prefix}:choose_a_preset"] = @"プリセットから選択",
            [$@"{combo_colour_preset_prefix}:save_as_a_preset"] = @"プリセットに登録",
            [$@"{combo_colour_preset_prefix}:no_presets_yet"] = @"登録したプリセットはありません",
            [$@"{combo_colour_preset_prefix}:preset_name"] = @"プリセットの名前",
            [$@"{combo_colour_preset_prefix}:save"] = @"登録",
            [$@"{combo_colour_preset_prefix}:overwrite"] = @"上書き",
            [$@"{binding_prefix}:editor_toggle_auto_cursor"] = @"Autoのカーソルの表示を切り替え",

            // the volume row on its own
            [$@"{editor_prefix}:hide_timeline_volume"] = @"音量の行を非表示",
            [$@"{editor_prefix}:hide_timeline_volume_description"] = @"空いた分だけノーツが大きくなります。SVの行はそのままです",
        };

        private const string editor_prefix = @"osu.Game.Resources.Localisation.Editor";
        private const string editor_dialogs_prefix = @"osu.Game.Resources.Localisation.EditorDialogs";
        private const string settings_prefix = @"osu.Game.Resources.Localisation.LazerKaiSettings";
        private const string binding_prefix = @"osu.Game.Resources.Localisation.GlobalActionKeyBinding";
        private const string common_prefix = @"osu.Game.Resources.Localisation.Common";
        private const string profile_prefix = @"osu.Game.Resources.Localisation.LazerKaiProfile";
        private const string collab_prefix = @"osu.Game.Resources.Localisation.Collab";
        private const string export_prefix = @"osu.Game.Resources.Localisation.BeatmapExport";
        private const string stable_compatibility_prefix = @"osu.Game.Resources.Localisation.LazerKaiStableCompatibility";
        private const string saved_lines_prefix = @"osu.Game.Resources.Localisation.LazerKaiSavedLines";
        private const string green_line_prefix = @"osu.Game.Resources.Localisation.LazerKaiGreenLine";
        private const string hitsound_prefix = @"osu.Game.Resources.Localisation.LazerKaiHitsound";
        private const string beatmap_files_prefix = @"osu.Game.Resources.Localisation.LazerKaiBeatmapFiles";
        private const string song_select_prefix = @"osu.Game.Resources.Localisation.LazerKaiSongSelect";
        private const string preset_prefix = @"osu.Game.Resources.Localisation.PlacementPreset";
        private const string combo_colour_preset_prefix = @"osu.Game.Resources.Localisation.LazerKaiComboColourPreset";
        private const string mapping_prefix = @"osu.Game.Resources.Localisation.MappingSummary";
        private const string replay_prefix = @"osu.Game.Resources.Localisation.LazerKaiReplay";

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
