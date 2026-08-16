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
    /// </remarks>
    public class LazerKaiJapaneseStore : ILocalisationStore
    {
        private readonly ILocalisationStore fallback;

        public CultureInfo EffectiveCulture => fallback.EffectiveCulture;

        private static readonly IReadOnlyDictionary<string, string> translations = new Dictionary<string, string>
        {
            // editor menus
            [$@"{editor_prefix}:menu_bar_test"] = @"テスト",
            [$@"{editor_prefix}:test_gameplay"] = @"テストプレイ",
            [$@"{editor_prefix}:test_play_mods"] = @"Mod",
            [$@"{editor_prefix}:test_play_deselect_all_mods"] = @"Modをすべて解除",
            [$@"{editor_prefix}:apply_test_play_mods_to_composer"] = @"編集画面にもModを反映",
            [$@"{editor_prefix}:live_mod_preview"] = @"編集中もModプレビューを更新",
            [$@"{editor_prefix}:advance_by_previous_spacing_on_placement"] = @"配置後に直前の間隔ぶん進む",
            [$@"{editor_prefix}:freehand_slider_placement"] = @"ドラッグでスライダーを描く",
            [$@"{editor_prefix}:always_expand_timeline_point_labels"] = @"タイムラインのラベルを常に展開",
            [$@"{editor_prefix}:selection_appearance"] = @"選択枠",
            [$@"{editor_prefix}:selection_colour"] = @"色",
            [$@"{editor_prefix}:selection_thickness"] = @"太さ",
            [$@"{editor_prefix}:selection_opacity"] = @"不透明度",
            [$@"{editor_prefix}:editor_colour_scheme"] = @"エディターの色（次回開いたときに反映）",
            [$@"{editor_prefix}:show_selection_buttons"] = @"選択時にボタンを表示",
            [$@"{editor_prefix}:selection_handle_size"] = @"ハンドルの大きさ",
            [$@"{editor_prefix}:selection_handle_colour"] = @"ハンドルの色",
            [$@"{editor_prefix}:slider_anchor_size"] = @"スライダーアンカーの大きさ",
            [$@"{editor_prefix}:show_timeline"] = @"タイムライン",
            [$@"{editor_prefix}:ctrl_click_cycles_curve_type"] = @"Ctrl+クリックでアンカーのカーブ種別を切り替え",
            [$@"{editor_prefix}:swap_right_and_middle_mouse"] = @"右クリックとホイールクリックを入れ替え（エディター内のみ）",
            [$@"{editor_prefix}:seek_animation_duration"] = @"シークのアニメーション",
            [$@"{editor_prefix}:always_expand_left_toolbox"] = @"左ツールボックスを常に展開",
            [$@"{editor_prefix}:drag_start_distance"] = @"ドラッグ開始距離（ゲーム全体に適用）",
            [$@"{editor_prefix}:show_offscreen_objects"] = @"プレイ範囲と画面外オブジェクトを表示",

            // key bindings
            [$@"{binding_prefix}:editor_toggle_composer_mod_preview"] = @"編集画面のModプレビューを切り替え",
            [$@"{binding_prefix}:editor_decrease_slider_velocity"] = @"SVを下げる",
            [$@"{binding_prefix}:editor_increase_slider_velocity"] = @"SVを上げる",
            [$@"{binding_prefix}:editor_decrease_slider_velocity_fine"] = @"SVを下げる（微調整）",
            [$@"{binding_prefix}:editor_increase_slider_velocity_fine"] = @"SVを上げる（微調整）",
        };

        private const string editor_prefix = @"osu.Game.Resources.Localisation.Editor";
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
