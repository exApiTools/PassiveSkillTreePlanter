using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PassiveSkillTreePlanter.UrlDecoders;

namespace PassiveSkillTreePlanter.UrlImporters;

public class MaxrollTreeImporter : BaseUrlImporter
{
    protected override async Task<List<FetchedTree>> FetchTrees(string url, CancellationToken cancellationToken)
    {
        var nodeList = await MaxRollUrlDecoder.FetchNodeList(url, cancellationToken);
        return nodeList.TreeCollection.Variants.Select(x => new FetchedTree(
            x.History.Select(n => (ushort)n).ToList(),
            x.Name,
            nodeList.Url,
            nodeList.TreeCollection.Type == "atlas" ? ESkillTreeType.Atlas : ESkillTreeType.Character,
            true)).ToList();
    }

    protected override bool IsValidUrl(string url)
    {
        return MaxRollUrlDecoder.IsValidUrl(url);
    }

    protected override string Name => "Maxroll";
}