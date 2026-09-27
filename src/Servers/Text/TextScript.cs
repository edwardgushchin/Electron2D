using Electron2D.TextFormatting.Unicode;

namespace Electron2D;

/// <summary>Connects Unicode script properties to shaping run selection and ISO 15924 tags.</summary>
internal static class TextScript
{
    // ISO 15924 tags in the pinned Unicode 17 Script enum order. The compiler stores this span as data.
    private static ReadOnlySpan<uint> Tags =>
    [
        0x5A7A7A7Au, 0x5A797979u, 0x5A696E68u, 0x41646C6Du, 0x41676862u, 0x41686F6Du,
        0x41726162u, 0x41726D69u, 0x41726D6Eu, 0x41767374u, 0x42616C69u, 0x42616D75u,
        0x42617373u, 0x4261746Bu, 0x42656E67u, 0x42657266u, 0x42686B73u, 0x426F706Fu,
        0x42726168u, 0x42726169u, 0x42756769u, 0x42756864u, 0x43616B6Du, 0x43616E73u,
        0x43617269u, 0x4368616Du, 0x43686572u, 0x43687273u, 0x436F7074u, 0x43706D6Eu,
        0x43707274u, 0x4379726Cu, 0x44657661u, 0x4469616Bu, 0x446F6772u, 0x44737274u,
        0x4475706Cu, 0x45677970u, 0x456C6261u, 0x456C796Du, 0x45746869u, 0x47617261u,
        0x47656F72u, 0x476C6167u, 0x476F6E67u, 0x476F6E6Du, 0x476F7468u, 0x4772616Eu,
        0x4772656Bu, 0x47756A72u, 0x47756B68u, 0x47757275u, 0x48616E67u, 0x48616E69u,
        0x48616E6Fu, 0x48617472u, 0x48656272u, 0x48697261u, 0x486C7577u, 0x486D6E67u,
        0x486D6E70u, 0x48726B74u, 0x48756E67u, 0x4974616Cu, 0x4A617661u, 0x4B616C69u,
        0x4B616E61u, 0x4B617769u, 0x4B686172u, 0x4B686D72u, 0x4B686F6Au, 0x4B697473u,
        0x4B6E6461u, 0x4B726169u, 0x4B746869u, 0x4C616E61u, 0x4C616F6Fu, 0x4C61746Eu,
        0x4C657063u, 0x4C696D62u, 0x4C696E61u, 0x4C696E62u, 0x4C697375u, 0x4C796369u,
        0x4C796469u, 0x4D61686Au, 0x4D616B61u, 0x4D616E64u, 0x4D616E69u, 0x4D617263u,
        0x4D656466u, 0x4D656E64u, 0x4D657263u, 0x4D65726Fu, 0x4D6C796Du, 0x4D6F6469u,
        0x4D6F6E67u, 0x4D726F6Fu, 0x4D746569u, 0x4D756C74u, 0x4D796D72u, 0x4E61676Du,
        0x4E616E64u, 0x4E617262u, 0x4E626174u, 0x4E657761u, 0x4E6B6F6Fu, 0x4E736875u,
        0x4F67616Du, 0x4F6C636Bu, 0x4F6E616Fu, 0x4F726B68u, 0x4F727961u, 0x4F736765u,
        0x4F736D61u, 0x4F756772u, 0x50616C6Du, 0x50617563u, 0x5065726Du, 0x50686167u,
        0x50686C69u, 0x50686C70u, 0x50686E78u, 0x506C7264u, 0x50727469u, 0x526A6E67u,
        0x526F6867u, 0x52756E72u, 0x53616D72u, 0x53617262u, 0x53617572u, 0x53676E77u,
        0x53686177u, 0x53687264u, 0x53696464u, 0x53696474u, 0x53696E64u, 0x53696E68u,
        0x536F6764u, 0x536F676Fu, 0x536F7261u, 0x536F796Fu, 0x53756E64u, 0x53756E75u,
        0x53796C6Fu, 0x53797263u, 0x54616762u, 0x54616B72u, 0x54616C65u, 0x54616C75u,
        0x54616D6Cu, 0x54616E67u, 0x54617674u, 0x5461796Fu, 0x54656C75u, 0x54666E67u,
        0x54676C67u, 0x54686161u, 0x54686169u, 0x54696274u, 0x54697268u, 0x546E7361u,
        0x546F6472u, 0x546F6C73u, 0x546F746Fu, 0x54757467u, 0x55676172u, 0x56616969u,
        0x56697468u, 0x57617261u, 0x5763686Fu, 0x5870656Fu, 0x58737578u, 0x59657A69u,
        0x59696969u, 0x5A616E62u,
    ];

    /// <summary>Gets the four-byte ISO 15924 tag expected by the shaping backend.</summary>
    internal static uint ToTag(Script script)
    {
        var tags = Tags;
        if ((uint)script >= (uint)tags.Length) throw new ArgumentOutOfRangeException(nameof(script));
        return tags[(int)script];
    }

    /// <summary>Reports whether a script names a specific writing system.</summary>
    internal static bool IsStrong(Script script) => script is not (Script.Unknown or Script.Common or Script.Inherited);

    /// <summary>Reports whether a scalar can join a run of the supplied script.</summary>
    /// <remarks>Explicit Script_Extensions restrict even Common and Inherited characters. Without
    /// such a restriction, Common and Inherited can adopt the surrounding run's script. Choosing
    /// that run and preserving grapheme boundaries belong to the layout itemizer.</remarks>
    internal static bool IsCompatible(uint scalar, Script script)
    {
        var codepoint = new Codepoint(scalar);
        if (codepoint.HasScriptExtension(script)) return true;
        var primary = codepoint.Script;
        return primary is Script.Common or Script.Inherited && codepoint.HasScriptExtension(primary);
    }
}
