using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using PassiveSkillTreePlanter.UrlDecoders;

namespace PassiveSkillTreePlanter.UrlImporters;

public class PobCodeImporter : BaseUrlImporter
{
    protected override Task<List<FetchedTree>> FetchTrees(string url, CancellationToken cancellationToken)
    {
        var xml = PobHelpers.CodeToXml(url);
        var xmlDocument = new XmlDocument();
        //because loading xml securely is surprisingly hard
        xmlDocument.Load(XmlReader.Create(new StringReader(xml)));
        var root = xmlDocument.GetElementsByTagName("PathOfBuilding").Cast<XmlNode>().First();
        var tree = root.ChildNodes.Cast<XmlNode>().First(x => x.Name == "Tree");
        var urls = tree.ChildNodes.Cast<XmlNode>()
            .Where(x => x.Name == "Spec")
            .Select(spec => (name: spec.Attributes.GetNamedItem("title")?.Value,
                url: spec.ChildNodes.Cast<XmlNode>().FirstOrDefault(specChild => specChild.Name == "URL")?.InnerText.Trim()))
            .Where(x => x.url != null)
            .Select(x => PathOfExileUrlDecoder.TryMatch(x.url, out var treeType, out var passives) ? new FetchedTree(passives.ToList(), x.name, "", treeType, false) : null)
            .Where(x => x != null)
            .ToList();
        return Task.FromResult(urls);
    }

    protected override bool IsValidUrl(string url)
    {
        return Convert.TryFromBase64String(url.Replace('-', '+').Replace('_', '/'), new byte[url.Length], out _);
    }

    protected override string Name => "PoB";

    protected override uint UrlMaxLength => 100000;
}