using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PassiveSkillTreePlanter.UrlDecoders;

public class PathOfExileUrlDecoder
{
    //Many thanks to https://github.com/EmmittJ/PoESkillTree
    private static readonly Regex UrlRegex =
        new Regex(@"^(http(|s):\/\/|)(\w*\.|)pathofexile\.com\/(fullscreen-|)(?<type>atlas|passive)-skill-tree\/(\d+(\.\d+)+\/)?(?<build>[\w-=]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool TryMatch(string buildUrl, out ESkillTreeType type, out HashSet<ushort> passiveIds)
    {
        if (UrlRegex.Match(buildUrl) is { Success: true } match)
        {
            type = match.Groups["type"].Value switch
            {
                "atlas" => ESkillTreeType.Atlas,
                "passive" => ESkillTreeType.Character
            };
            passiveIds = Decode(match.Groups["build"].Value);
            return true;
        }

        type = default;
        passiveIds = default;
        return false;
    }

    private static HashSet<ushort> Decode(string buildCode)
    {
        var nodeIds = new HashSet<ushort>();

        var textToDecode = buildCode.Replace('-', '+').Replace('_', '/');

        var data = Convert.FromBase64String(textToDecode);

        var version = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];

        for (var k = version > 3 ? 7 : 6; k < data.Length; k += 2)
        {
            var nodeId = (ushort)((data[k] << 8) | data[k + 1]);
            nodeIds.Add(nodeId);
        }

        return nodeIds;
    }

    public static string Encode(HashSet<ushort> nodes, ESkillTreeType type)
    {
        var prefix = type switch
        {
            ESkillTreeType.Atlas => "https://www.pathofexile.com/fullscreen-atlas-skill-tree/",
            ESkillTreeType.Character => "https://www.pathofexile.com/fullscreen-passive-skill-tree/",
        };
        var versionBytes = new byte[] { 0, 0, 0, 6 };
        var classBytes = new byte[] { 0 };
        var ascendancyBytes = new byte[] { 0 };
        var nodeCountBytes = new byte[] { (byte)nodes.Count };
        var nodeBytes = nodes.OrderBy(x => x).SelectMany(x => new byte[] { (byte)(x >> 8), (byte)x });
        var tailBytes = new byte[] { 0, 0 };
        var allBytes = versionBytes.Concat(classBytes).Concat(ascendancyBytes).Concat(nodeCountBytes).Concat(nodeBytes).Concat(tailBytes).ToArray();
        var encodedStr = Convert.ToBase64String(allBytes).Replace('+', '-').Replace('/', '_');
        return prefix + encodedStr;
    }
}