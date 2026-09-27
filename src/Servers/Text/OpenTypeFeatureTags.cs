using System.Text;

namespace Electron2D;

// Registered human-readable feature and variation aliases; tag spellings remain case-sensitive.
internal static class OpenTypeFeatureTags
{
    private static readonly Dictionary<string, uint> Aliases = new(StringComparer.Ordinal)
    {
        ["access_all_alternates"] = 0x61616C74,
        ["above_base_forms"] = 0x61627666,
        ["above_base_mark_positioning"] = 0x6162766D,
        ["above_base_substitutions"] = 0x61627673,
        ["alternative_fractions"] = 0x61667263,
        ["akhands"] = 0x616B686E,
        ["below_base_forms"] = 0x626C7766,
        ["below_base_mark_positioning"] = 0x626C776D,
        ["below_base_substitutions"] = 0x626C7773,
        ["contextual_alternates"] = 0x63616C74,
        ["case_sensitive_forms"] = 0x63617365,
        ["glyph_composition"] = 0x63636D70,
        ["conjunct_form_after_ro"] = 0x63666172,
        ["contextual_half_width_spacing"] = 0x63687773,
        ["conjunct_forms"] = 0x636A6374,
        ["contextual_ligatures"] = 0x636C6967,
        ["centered_cjk_punctuation"] = 0x63706374,
        ["capital_spacing"] = 0x63707370,
        ["contextual_swash"] = 0x63737768,
        ["cursive_positioning"] = 0x63757273,
        ["petite_capitals_from_capitals"] = 0x63327063,
        ["small_capitals_from_capitals"] = 0x63327363,
        ["distances"] = 0x64697374,
        ["discretionary_ligatures"] = 0x646C6967,
        ["denominators"] = 0x646E6F6D,
        ["dotless_forms"] = 0x64746C73,
        ["expert_forms"] = 0x65787074,
        ["final_glyph_on_line_alternates"] = 0x66616C74,
        ["terminal_forms_2"] = 0x66696E32,
        ["terminal_forms_3"] = 0x66696E33,
        ["terminal_forms"] = 0x66696E61,
        ["flattened_accent_forms"] = 0x666C6163,
        ["fractions"] = 0x66726163,
        ["full_widths"] = 0x66776964,
        ["half_forms"] = 0x68616C66,
        ["halant_forms"] = 0x68616C6E,
        ["alternate_half_widths"] = 0x68616C74,
        ["historical_forms"] = 0x68697374,
        ["horizontal_kana_alternates"] = 0x686B6E61,
        ["historical_ligatures"] = 0x686C6967,
        ["hangul"] = 0x686E676C,
        ["hojo_kanji_forms"] = 0x686F6A6F,
        ["half_widths"] = 0x68776964,
        ["initial_forms"] = 0x696E6974,
        ["isolated_forms"] = 0x69736F6C,
        ["italics"] = 0x6974616C,
        ["justification_alternates"] = 0x6A616C74,
        ["jis78_forms"] = 0x6A703738,
        ["jis83_forms"] = 0x6A703833,
        ["jis90_forms"] = 0x6A703930,
        ["jis2004_forms"] = 0x6A703034,
        ["kerning"] = 0x6B65726E,
        ["left_bounds"] = 0x6C666264,
        ["standard_ligatures"] = 0x6C696761,
        ["leading_jamo_forms"] = 0x6C6A6D6F,
        ["lining_figures"] = 0x6C6E756D,
        ["localized_forms"] = 0x6C6F636C,
        ["left_to_right_alternates"] = 0x6C747261,
        ["left_to_right_mirrored_forms"] = 0x6C74726D,
        ["mark_positioning"] = 0x6D61726B,
        ["medial_forms_2"] = 0x6D656432,
        ["medial_forms"] = 0x6D656469,
        ["mathematical_greek"] = 0x6D67726B,
        ["mark_to_mark_positioning"] = 0x6D6B6D6B,
        ["mark_positioning_via_substitution"] = 0x6D736574,
        ["alternate_annotation_forms"] = 0x6E616C74,
        ["nlc_kanji_forms"] = 0x6E6C636B,
        ["nukta_forms"] = 0x6E756B74,
        ["numerators"] = 0x6E756D72,
        ["oldstyle_figures"] = 0x6F6E756D,
        ["optical_bounds"] = 0x6F706264,
        ["ordinals"] = 0x6F72646E,
        ["ornaments"] = 0x6F726E6D,
        ["proportional_alternate_widths"] = 0x70616C74,
        ["petite_capitals"] = 0x70636170,
        ["proportional_kana"] = 0x706B6E61,
        ["proportional_figures"] = 0x706E756D,
        ["pre_base_forms"] = 0x70726566,
        ["pre_base_substitutions"] = 0x70726573,
        ["post_base_forms"] = 0x70737466,
        ["post_base_substitutions"] = 0x70737473,
        ["proportional_widths"] = 0x70776964,
        ["quarter_widths"] = 0x71776964,
        ["randomize"] = 0x72616E64,
        ["required_contextual_alternates"] = 0x72636C74,
        ["rakar_forms"] = 0x726B7266,
        ["required_ligatures"] = 0x726C6967,
        ["reph_forms"] = 0x72706866,
        ["right_bounds"] = 0x72746264,
        ["right_to_left_alternates"] = 0x72746C61,
        ["right_to_left_mirrored_forms"] = 0x72746C6D,
        ["ruby_notation_forms"] = 0x72756279,
        ["required_variation_alternates"] = 0x7276726E,
        ["stylistic_alternates"] = 0x73616C74,
        ["scientific_inferiors"] = 0x73696E66,
        ["optical_size"] = 0x73697A65,
        ["small_capitals"] = 0x736D6370,
        ["simplified_forms"] = 0x736D706C,
        ["math_script_style_alternates"] = 0x73737479,
        ["stretching_glyph_decomposition"] = 0x73746368,
        ["subscript"] = 0x73756273,
        ["superscript"] = 0x73757073,
        ["swash"] = 0x73777368,
        ["titling"] = 0x7469746C,
        ["trailing_jamo_forms"] = 0x746A6D6F,
        ["traditional_name_forms"] = 0x746E616D,
        ["tabular_figures"] = 0x746E756D,
        ["traditional_forms"] = 0x74726164,
        ["third_widths"] = 0x74776964,
        ["unicase"] = 0x756E6963,
        ["alternate_vertical_metrics"] = 0x76616C74,
        ["vattu_variants"] = 0x76617475,
        ["vertical_contextual_half_width_spacing"] = 0x76636877,
        ["vertical_alternates"] = 0x76657274,
        ["alternate_vertical_half_metrics"] = 0x7668616C,
        ["vowel_jamo_forms"] = 0x766A6D6F,
        ["vertical_kana_alternates"] = 0x766B6E61,
        ["vertical_kerning"] = 0x766B726E,
        ["proportional_alternate_vertical_metrics"] = 0x7670616C,
        ["vertical_alternates_and_rotation"] = 0x76727432,
        ["vertical_alternates_for_rotation"] = 0x76727472,
        ["slashed_zero"] = 0x7A65726F,
        ["italic"] = 0x6974616C,
        ["optical_size"] = 0x6F70737A,
        ["slant"] = 0x736C6E74,
        ["width"] = 0x77647468,
        ["weight"] = 0x77676874,
    };

    internal static uint Resolve(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Aliases.TryGetValue(name, out var tag)) return tag;
        if (TryNumbered(name, "character_variant_", 99, out var number))
            return 0x63760000u | ((uint)('0' + number / 10) << 8) | (uint)('0' + number % 10);
        if (TryNumbered(name, "stylistic_set_", 20, out number))
            return 0x73730000u | ((uint)('0' + number / 10) << 8) | (uint)('0' + number % 10);
        name = name.Replace("custom_", string.Empty, StringComparison.Ordinal);
        tag = 0x20202020; var count = 0;
        foreach (var rune in name.EnumerateRunes())
        {
            if (rune.Value == 0 || count == 4) break;
            var shift = (3 - count++) * 8;
            tag = (tag & ~(255u << shift)) | ((uint)(rune.Value <= 127 ? rune.Value : 32) << shift);
        }
        return count == 0 ? 0 : tag;
    }

    private static bool TryNumbered(string name, string prefix, int maximum, out int number)
    {
        number = 0;
        if (name.Length != prefix.Length + 2 || !name.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var first = name[^2]; var second = name[^1];
        if (first is < '0' or > '9' || second is < '0' or > '9') return false;
        number = (first - '0') * 10 + second - '0';
        return number > 0 && number <= maximum;
    }
}
