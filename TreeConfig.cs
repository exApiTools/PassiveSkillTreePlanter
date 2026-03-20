using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PassiveSkillTreePlanter;

public class TreeConfig
{
    public static List<string> GetBuilds(string buildDirectory)
    {
        var dirInfo = new DirectoryInfo(buildDirectory);
        var files = dirInfo.GetFiles("*.json").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
        return files;
    }

    public static SkillTreeData LoadBuild(string buildDirectory, string buildName)
    {
        var buildFile = Path.Join(buildDirectory, $"{buildName}.json");
        return LoadSettingFile<SkillTreeData>(buildFile);
    }

    public static TSettingType LoadSettingFile<TSettingType>(string fileName)
    {
        if (!File.Exists(fileName)) return default;

        return JsonConvert.DeserializeObject<TSettingType>(File.ReadAllText(fileName));
    }

    public static void SaveSettingFile<TSettingType>(string fileName, TSettingType setting)
    {
        var buildFile = $"{fileName}.json";
        var serialized = JsonConvert.SerializeObject(setting, Formatting.Indented);

        File.WriteAllText(buildFile, serialized);
    }

    public class Tree
    {
        private ESkillTreeType? _type;
        public string SkillTreeUrl = "";
        public string Tag = "";

        [JsonIgnore]
        public ESkillTreeType Type => _type ??= TreeEncoder.DecodeUrl(SkillTreeUrl) switch
        {
            (not null, var type) => type,
            _ => ESkillTreeType.Unknown
        };

        public void ResetType() => _type = null;
    }

    public class SkillTreeData
    {
        public string Notes { get; set; } = "";
        public List<Tree> Trees { get; set; } = new();
        public string BuildLink { get; set; } = "";
        internal bool Modified { get; set; }
    }
}