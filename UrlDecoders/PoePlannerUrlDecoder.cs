using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ExileCore;

namespace PassiveSkillTreePlanter.UrlDecoders;

public class PoePlannerUrlDecoder
{
    //Many thanks to https://github.com/EmmittJ/PoESkillTree
    private static readonly Regex UrlRegex = new Regex(@"^(http(|s):\/\/|)(\w*\.|)poeplanner\.com\/(?<atlastree>atlas-tree\/)?(?<build>[\w-=]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool UrlMatch(string buildUrl)
    {
        return UrlRegex.IsMatch(buildUrl);
    }

    public static bool TryMatch(string buildUrl, out ESkillTreeType type, out HashSet<ushort> passiveIds)
    {
        if (UrlRegex.Match(buildUrl) is { Success: true } match)
        {
            type = match.Groups["atlastree"].Success ? ESkillTreeType.Atlas : ESkillTreeType.Character;
            passiveIds = type == ESkillTreeType.Atlas ? DecodeAtlas(buildUrl) : Decode(buildUrl);
            return true;
        }

        type = default;
        passiveIds = default;
        return false;
    }

    public static HashSet<ushort> Decode(string url)
    {
        var buildSegment = url.Split('/').LastOrDefault();

        if (buildSegment == null)
        {
            Logger.Log.Error("Can't decode PoePlanner Url", 5);
            return [];
        }

        buildSegment = buildSegment
            .Replace("-", "+")
            .Replace("_", "/");

        var rawBytes = Convert.FromBase64String(buildSegment);
        var skillDataStart = rawBytes.AsSpan(
            sizeof(ushort) + sizeof(byte) + //common header
            sizeof(ushort) + sizeof(ushort) + sizeof(byte) + sizeof(byte) + sizeof(byte) //tree header
        );
        var nodeCount = MemoryMarshal.Read<ushort>(skillDataStart);
        var nodeIds = MemoryMarshal.Cast<byte, ushort>(skillDataStart.Slice(sizeof(ushort), sizeof(ushort) * nodeCount)).ToArray().ToHashSet();
        return nodeIds;
    }

    public static HashSet<ushort> DecodeAtlas(string url)
    {
        var buildSegment = url.Split('/').LastOrDefault();

        if (buildSegment == null)
        {
            Logger.Log.Error("Can't decode PoePlanner Url", 5);
            return [];
        }

        buildSegment = buildSegment
            .Replace("-", "+")
            .Replace("_", "/");

        var rawBytes = Convert.FromBase64String(buildSegment);

        var treeVersion = MemoryMarshal.Read<ushort>(rawBytes.AsSpan(0));
        var nodeOffset = treeVersion < 5 ? 4 : 5;
        var nodeCount = MemoryMarshal.Read<ushort>(rawBytes.AsSpan(nodeOffset));
        var nodeIds = MemoryMarshal.Cast<byte, ushort>(rawBytes.AsSpan(nodeOffset + sizeof(ushort), sizeof(ushort) * nodeCount)).ToArray().ToHashSet();
        return nodeIds;
    }
}