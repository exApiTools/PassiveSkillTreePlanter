using System;
using System.Collections.Generic;
using ExileCore;
using PassiveSkillTreePlanter.UrlDecoders;

namespace PassiveSkillTreePlanter;

public class TreeEncoder
{
    public static (HashSet<ushort> Nodes, ESkillTreeType Type) DecodeUrl(string url)
    {
        try
        {
            if (PoePlannerUrlDecoder.TryMatch(url, out var type, out var passiveIds) ||
                PathOfExileUrlDecoder.TryMatch(url, out type, out passiveIds))
            {
                return (passiveIds, type);
            }
        }
        catch (Exception ex)
        {
            DebugWindow.LogError($"Failed to decode url {url}:\n{ex}");
            return default;
        }

        return default;
    }
}