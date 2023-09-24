using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PassiveSkillTreePlanter.UrlDecoders;

public class MaxRollUrlDecoder
{
    private static readonly Regex MaxRollUrlRegex = new Regex(@"^https://maxroll.gg/poe/poe-tree/(?<groupId>[^/]+)/(?<treeId>\d+)", RegexOptions.Compiled);

    public static bool IsValidUrl(string url)
    {
        return MaxRollUrlRegex.IsMatch(url.Trim());
    }

    public static async Task<MaxRollFetchResult> FetchNodeList(string url, CancellationToken cancellationToken)
    {
        if (MaxRollUrlRegex.Match(url.Trim()) is not { Success: true } match)
        {
            throw new Exception($"url '{url}' does not match expected regex {MaxRollUrlRegex}");
        }

        var groupId = match.Groups["groupId"].Value;
        var treeId = int.Parse(match.Groups["treeId"].ValueSpan);
        var dataUrl = $"https://poeplanner.maxroll.gg/poeplanner-data/load/{groupId}";
        var dataString = await new HttpClient().GetStringAsync(dataUrl, cancellationToken);
        var data = JsonConvert.DeserializeObject<MaxRollDataRoot>(dataString);
        return new MaxRollFetchResult(url.Trim(),
            data?.Embeds?.FirstOrDefault(x => x?.Id == treeId) ?? throw new Exception($"Embed with id {treeId} was not found in the downloaded data"));
    }
}

public record MaxRollFetchResult(string Url, MaxRollEmbed Embed);

public class MaxRollDataRoot
{
    public MaxRollEmbed[] Embeds { get; set; }
}

public class MaxRollEmbed
{
    public string Type { get; set; }
    public string Name { get; set; }
    public MaxRollVariant[] Variants { get; set; }
    public int Id { get; set; }
}

public class MaxRollVariant
{
    public string Name { get; set; }
    public int[] History { get; set; }
}