using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PassiveSkillTreePlanter.UrlImporters;

public class PobbinTreeImporter : PobCodeImporter
{
    private static readonly Regex MaxRollUrlRegex = new Regex(@"^https://pobb\.in/(?<pobId>[^/]+)/?$", RegexOptions.Compiled);

    protected override async Task<List<FetchedTree>> FetchTrees(string url, CancellationToken cancellationToken)
    {
        if (MaxRollUrlRegex.Match(url.Trim()) is not { Success: true } match)
        {
            throw new Exception($"url '{url}' does not match expected regex {MaxRollUrlRegex}");
        }

        var groupId = match.Groups["pobId"].Value;
        var dataUrl = $"https://pobb.in/{groupId}/raw";
        var dataString = await new HttpClient().GetStringAsync(dataUrl, cancellationToken);
        return (await base.FetchTrees(dataString, cancellationToken)).Select(x => x with { Url = url.Trim() }).ToList();
    }

    protected override bool IsValidUrl(string url)
    {
        return MaxRollUrlRegex.IsMatch(url);
    }

    protected override string Name => "Pobbin";
    protected override uint UrlMaxLength => 200;
}