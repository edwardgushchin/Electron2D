using System.Text;

namespace Electron2D;

public static partial class TranslationServer
{
    private readonly record struct NumeralSystem(string[] Digits, string Percent, string LowerExponent, string UpperExponent);
    private static readonly Dictionary<string, NumeralSystem> NumeralSystems = CreateNumeralSystems();
    private static Dictionary<string, NumeralSystem> CreateNumeralSystems()
    {
        var systems = new Dictionary<string, NumeralSystem>(StringComparer.Ordinal);
        Add("ar ar_AE ar_BH ar_DJ ar_EG ar_ER ar_IL ar_IQ ar_JO ar_KM ar_KW ar_LB ar_MR ar_OM ar_PS ar_QA ar_SA ar_SD ar_SO ar_SS ar_SY ar_TD ar_YE ckb ckb_IQ ckb_IR sd sd_PK sd_Arab sd_Arab_PK", "٠١٢٣٤٥٦٧٨٩٫", "٪", "اس", "اس");
        Add("fa fa_AF fa_IR ks ks_IN ks_Arab ks_Arab_IN lrc lrc_IQ lrc_IR mzn mzn_IR pa_PK pa_Arab pa_Arab_PK ps ps_AF ps_PK ur_IN uz_AF uz_Arab uz_Arab_AF", "۰۱۲۳۴۵۶۷۸۹٫", "٪", "اس", "اس");
        Add("as as_IN bn bn_BD bn_IN mni mni_IN mni_Beng mni_Beng_IN", "০১২৩৪৫৬৭৮৯.", "%", "e", "E");
        Add("mr mr_IN ne ne_IN ne_NP sa sa_IN", "०१२३४५६७८९.", "%", "e", "E");
        Add("dz dz_BT", "༠༡༢༣༤༥༦༧༨༩.", "%", "e", "E");
        Add("sat sat_IN sat_Olck sat_Olck_IN", "᱐᱑᱒᱓᱔᱕᱖᱗᱘᱙.", "%", "e", "E");
        Add("my my_MM", "၀၁၂၃၄၅၆၇၈၉.", "%", "e", "E");
        Add("ccp ccp_BD ccp_IN", "𑄶𑄷𑄸𑄹𑄺𑄻𑄼𑄽𑄾𑄿.", "%", "e", "E");
        Add("ff ff_Adlm_BF ff_Adlm_CM ff_Adlm_GH ff_Adlm_GM ff_Adlm_GN ff_Adlm_GW ff_Adlm_LR ff_Adlm_MR ff_Adlm_NE ff_Adlm_NG ff_Adlm_SL ff_Adlm_SN", "𞥐𞥑𞥒𞥓𞥔𞥕𞥖𞥗𞥘𞥙.", "%", "𞤉", "𞤉");
        return systems;
        void Add(string locales, string digits, string percent, string lower, string upper)
        {
            var system = new NumeralSystem(digits.EnumerateRunes().Select(r => r.ToString()).ToArray(), percent, lower, upper);
            foreach (var locale in locales.Split(' ')) systems[locale] = system;
        }
    }
    /// <summary>Converts Western digits, decimal punctuation and exponent markers to a locale's numeral system.</summary>
    /// <param name="number">The nonnull input text.</param><param name="locale">A nonempty locale identifier; hyphens and underscores are accepted.</param>
    /// <returns>Localized text, or the original string for an unlisted locale.</returns>
    /// <exception cref="ArgumentException">The locale is empty.</exception>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static string FormatNumber(string number, string locale)
    {
        ArgumentNullException.ThrowIfNull(number); ArgumentException.ThrowIfNullOrEmpty(locale);
        if (!NumeralSystems.TryGetValue(locale.Replace('-', '_'), out var system)) return number;
        number = number.Replace("e", system.LowerExponent, StringComparison.Ordinal).Replace("E", system.UpperExponent, StringComparison.Ordinal);
        var result = new StringBuilder(); foreach (var rune in number.EnumerateRunes()) { if (rune.Value is >= '0' and <= '9') result.Append(system.Digits[rune.Value - '0']); else if (rune.Value is '.' or ',') result.Append(system.Digits[10]); else result.Append(rune.ToString()); }
        return result.ToString();
    }
    /// <summary>Converts the selected locale's digits, decimal marker and exponent markers to Western text.</summary>
    /// <param name="number">The nonnull input text.</param><param name="locale">A nonempty locale identifier; hyphens and underscores are accepted.</param>
    /// <returns>Parsed text, or the original string for an unlisted locale.</returns>
    /// <exception cref="ArgumentException">The locale is empty.</exception>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static string ParseNumber(string number, string locale)
    {
        ArgumentNullException.ThrowIfNull(number); ArgumentException.ThrowIfNullOrEmpty(locale);
        if (!NumeralSystems.TryGetValue(locale.Replace('-', '_'), out var system)) return number;
        number = number.Replace(system.LowerExponent, "e", StringComparison.Ordinal).Replace(system.UpperExponent, "E", StringComparison.Ordinal);
        var result = new StringBuilder(); foreach (var rune in number.EnumerateRunes()) { var text = rune.ToString(); var index = Array.IndexOf(system.Digits, text); if (index == 10) result.Append('.'); else if (index >= 0) result.Append((char)('0' + index)); else result.Append(text); }
        return result.ToString();
    }
    /// <summary>Returns the percent sign used by the selected numeral system.</summary>
    /// <param name="locale">A nonempty locale identifier.</param><returns>The locale's sign, or % for an unlisted locale.</returns>
    /// <exception cref="ArgumentException">The locale is empty.</exception>
    public static string GetPercentSign(string locale) { ArgumentException.ThrowIfNullOrEmpty(locale); return NumeralSystems.TryGetValue(locale.Replace('-', '_'), out var system) ? system.Percent : "%"; }

}
