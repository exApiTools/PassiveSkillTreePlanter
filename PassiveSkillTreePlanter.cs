using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared;
using ExileCore.Shared.AtlasHelper;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using PassiveSkillTreePlanter.SkillTreeJson;
using PassiveSkillTreePlanter.TreeGraph;
using PassiveSkillTreePlanter.UrlImporters;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vector2 = System.Numerics.Vector2;

namespace PassiveSkillTreePlanter;

public class PassiveSkillTreePlanter : BaseSettingsPlugin<PassiveSkillTreePlanterSettings>
{
    private const string AtlasTreeDataFile = "AtlasTreeData.json";
    private const string SkillTreeDataFile = "SkillTreeData.json";

    private const string OfficialSkillTreeDataJsonRawUrl = "https://raw.githubusercontent.com/grindinggear/skilltree-export/master/data.json";

    private const string OfficialAtlasTreeDataJsonRawUrl = "https://raw.githubusercontent.com/grindinggear/atlastree-export/master/data.json";

    private const string SkillTreeDir = "Builds";
    private readonly PoESkillTreeJsonDecoder _atlasTreeData = new();

    private readonly List<BaseUrlImporter> _importers = [new MaxrollTreeImporter(), new PobbinTreeImporter(), new PobCodeImporter()];

    private readonly Dictionary<uint, bool> _nodeMap = new();

    private readonly PoESkillTreeJsonDecoder _skillTreeData = new();
    private HashSet<ushort> _atlasUrlNodeIds = new();

    private HashSet<ushort> _characterUrlNodeIds = new();
    private SyncTask<bool> _currentTask;
    private bool _editorShown;

    private int _officialDownloadBusy;
    private Task<HashSet<uint>> _pathingNodes;
    private volatile bool _pendingReloadGameTreeData;
    private AtlasTexture _ringImage;

    private PassiveSkillTreePlanterSettingsMenu _settingsMenu;
    internal string BuildNameEditorValue = "";

    public PassiveSkillTreePlanter()
    {
        Name = "Passive Skill Tree Planner";
    }

    internal List<string> BuildFiles { get; private set; } = new();

    internal TreeConfig.SkillTreeData SelectedBuildData { get; private set; } = new();

    internal TreeConfig.SkillTreeData AtlasBuildData { get; private set; } = new();

    internal TreeConfig.SkillTreeData EditBuildData { get; private set; } = new();

    internal IReadOnlyList<BaseUrlImporter> UrlImporters => _importers;

    public string SkillTreeUrlFilesDir => Directory.CreateDirectory(Path.Join(ConfigDirectory, SkillTreeDir)).FullName;

    public override void OnLoad()
    {
        _settingsMenu = new PassiveSkillTreePlanterSettingsMenu(this);
        _ringImage = GetAtlasTexture("AtlasMapCircle");
        Graphics.InitImage("Icons.png");
        ReloadGameTreeData();
        ReloadBuildList();
        if (string.IsNullOrWhiteSpace(Settings.SelectedBuild))
        {
            Settings.SelectedBuild = "default";
        }

        LoadBuild(Settings.SelectedBuild);
        if (string.IsNullOrWhiteSpace(Settings.SelectedAtlasBuild))
        {
            Settings.SelectedAtlasBuild = Settings.SelectedBuild;
        }

        LoadAtlasBuild(Settings.SelectedAtlasBuild);
        if (string.IsNullOrWhiteSpace(Settings.SelectedEditBuild))
        {
            Settings.SelectedEditBuild = Settings.SelectedBuild;
        }

        LoadEditBuild(Settings.SelectedEditBuild);
        LoadUrl(Settings.LastSelectedAtlasUrl);
        LoadUrl(Settings.LastSelectedCharacterUrl);
    }

    internal void ReloadBuildList()
    {
        BuildFiles = TreeConfig.GetBuilds(SkillTreeUrlFilesDir);
    }

    internal void LoadBuild(string buildName)
    {
        Settings.SelectedBuild = buildName;
        SelectedBuildData = TreeConfig.LoadBuild(SkillTreeUrlFilesDir, Settings.SelectedBuild) ?? new TreeConfig.SkillTreeData();
        _characterUrlNodeIds = new HashSet<ushort>();
        SyncAtlasBuildDataIfSameFile();
    }

    internal void LoadEditBuild(string buildName)
    {
        if (BuildFiles.Count == 0)
        {
            ReloadBuildList();
        }

        if (BuildFiles.Count > 0 && (string.IsNullOrWhiteSpace(buildName) || !BuildFiles.Contains(buildName)))
        {
            buildName = BuildFiles.Contains(Settings.SelectedBuild) ? Settings.SelectedBuild : BuildFiles[0];
        }

        Settings.SelectedEditBuild = buildName ?? string.Empty;
        EditBuildData = string.IsNullOrWhiteSpace(buildName) ? new TreeConfig.SkillTreeData() : TreeConfig.LoadBuild(SkillTreeUrlFilesDir, buildName) ?? new TreeConfig.SkillTreeData();
        BuildNameEditorValue = Settings.SelectedEditBuild;
    }

    internal void LoadAtlasBuild(string buildName)
    {
        Settings.SelectedAtlasBuild = buildName;
        if (string.Equals(Settings.SelectedAtlasBuild, Settings.SelectedBuild, StringComparison.Ordinal))
        {
            AtlasBuildData = SelectedBuildData;
        }
        else
        {
            AtlasBuildData = TreeConfig.LoadBuild(SkillTreeUrlFilesDir, Settings.SelectedAtlasBuild) ?? new TreeConfig.SkillTreeData();
        }

        _atlasUrlNodeIds = new HashSet<ushort>();
    }

    private void SyncAtlasBuildDataIfSameFile()
    {
        if (string.Equals(Settings.SelectedAtlasBuild, Settings.SelectedBuild, StringComparison.Ordinal))
        {
            AtlasBuildData = SelectedBuildData;
        }
    }

    internal void LoadUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        var cleanedUrl = RemoveAccName(url).Trim();
        var (nodes, type) = TreeEncoder.DecodeUrl(cleanedUrl);
        if (nodes == null)
        {
            LogMessage($"PassiveSkillTree: Can't decode url {url}", 10);
            return;
        }

        if (type == ESkillTreeType.Character)
        {
            _characterUrlNodeIds = nodes;
            Settings.LastSelectedCharacterUrl = url;
            ValidateNodes(_characterUrlNodeIds, _skillTreeData.SkillNodes);
        }

        if (type == ESkillTreeType.Atlas)
        {
            _atlasUrlNodeIds = nodes;
            Settings.LastSelectedAtlasUrl = url;
            ValidateNodes(_atlasUrlNodeIds, _atlasTreeData.SkillNodes);
        }
    }

    public override bool Initialise() => true;

    internal void QueueRedownloadOfficialTreeData(bool isAtlas)
    {
        if (Interlocked.CompareExchange(ref _officialDownloadBusy, 1, 0) != 0) return;

        var destFile = isAtlas ? AtlasTreeDataFile : SkillTreeDataFile;
        var url = isAtlas ? OfficialAtlasTreeDataJsonRawUrl : OfficialSkillTreeDataJsonRawUrl;

        _ = Task.Run(async () =>
        {
            try
            {
                using var http = CreateRawGithubHttpClient();
                var json = await http.GetStringAsync(url).ConfigureAwait(false);

                var testDecoder = new PoESkillTreeJsonDecoder();
                testDecoder.Decode(json);
                if (testDecoder.SkillNodes.Count == 0) throw new InvalidOperationException("JSON did not decode to any skill nodes.");

                var path = Path.Join(DirectoryFullName, destFile);
                await Task.Run(() => File.WriteAllText(path, json)).ConfigureAwait(false);

                _pendingReloadGameTreeData = true;
            }
            catch (Exception ex)
            {
                LogError($"Official tree JSON download: {ex.Message}", 10);
            }
            finally
            {
                Interlocked.Exchange(ref _officialDownloadBusy, 0);
            }
        });
    }

    private static HttpClient CreateRawGithubHttpClient()
    {
        var http = new HttpClient {Timeout = TimeSpan.FromMinutes(10)};
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PassiveSkillTreePlanter (ExileCore plugin)");
        return http;
    }

    internal void ProcessPendingOfficialTreeReload()
    {
        if (!_pendingReloadGameTreeData) return;

        _pendingReloadGameTreeData = false;
        ReloadGameTreeData();
    }

    private void ReloadGameTreeData()
    {
        var atlasTreePath = Path.Join(DirectoryFullName, AtlasTreeDataFile);
        if (!File.Exists(atlasTreePath))
        {
            LogMessage($"Atlas passive skill tree: Can't find file {atlasTreePath} with atlas skill tree data.", 10);
        }
        else
        {
            _atlasTreeData.Decode(File.ReadAllText(atlasTreePath));
        }

        var skillTreeDataPath = Path.Join(DirectoryFullName, SkillTreeDataFile);
        if (!File.Exists(skillTreeDataPath))
        {
            LogMessage($"Passive skill tree: Can't find file {skillTreeDataPath} with skill tree data.", 10);
        }
        else
        {
            var skillTreeJson = File.ReadAllText(skillTreeDataPath);
            _skillTreeData.Decode(skillTreeJson);
        }
    }

    public override void Render()
    {
        DrawTreeOverlay(GameController.Game.IngameState.IngameUi.TreePanel.AsObject<TreePanel>(), _skillTreeData, _characterUrlNodeIds,
            () => GameController.Game.IngameState.ServerData.PassiveSkillIds.ToHashSet(), ESkillTreeType.Character);

        DrawTreeOverlay(GameController.Game.IngameState.IngameUi.AtlasTreePanel.AsObject<TreePanel>(), _atlasTreeData, _atlasUrlNodeIds,
            () => GameController.Game.IngameState.ServerData.AtlasPassiveSkillIds.ToHashSet(), ESkillTreeType.Atlas);

        TaskUtils.RunOrRestart(ref _currentTask, () => null);
    }

    private void DrawControlPanel(ESkillTreeType skillTreeType, TreePanel treePanel, IReadOnlySet<ushort> allocatedNodeIds, IReadOnlySet<ushort> targetNodeIds)
    {
        if (!Settings.ShowControlPanel) return;

        var isOpen = true;
        ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("#treeSwitcher", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            var source = skillTreeType == ESkillTreeType.Character ? SelectedBuildData : AtlasBuildData;
            var trees = source.Trees.Where(x => x.Type == skillTreeType).ToList();

            foreach (var tree in trees)
            {
                var lastSelectedUrl = skillTreeType switch
                {
                    ESkillTreeType.Character => Settings.LastSelectedCharacterUrl,
                    ESkillTreeType.Atlas => Settings.LastSelectedAtlasUrl
                };

                ImGui.BeginDisabled(lastSelectedUrl == tree.SkillTreeUrl);
                if (ImGui.Button($"Load {tree.Tag}"))
                {
                    LoadUrl(tree.SkillTreeUrl);
                }

                ImGui.EndDisabled();
            }

            if (ImGui.Button("Operate tree"))
            {
                _currentTask = ChangeTree(allocatedNodeIds, targetNodeIds, treePanel);
            }

            if (ImGui.Button("Show/hide editor"))
            {
                _editorShown = !_editorShown;
                _nodeMap.Clear();
                _pathingNodes = null;
            }

            ImGui.End();
        }
    }

    private static string CleanFileName(string fileName)
    {
        return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
    }

    internal void RenameFile(string fileName, string oldFileName)
    {
        fileName = CleanFileName(fileName);
        var oldFilePath = Path.Combine(SkillTreeUrlFilesDir, $"{oldFileName}.json");
        var newFilePath = Path.Combine(SkillTreeUrlFilesDir, $"{fileName}.json");

        File.Move(oldFilePath, newFilePath);
        if (string.Equals(Settings.SelectedAtlasBuild, oldFileName, StringComparison.Ordinal))
        {
            Settings.SelectedAtlasBuild = fileName;
        }

        if (string.Equals(Settings.SelectedBuild, oldFileName, StringComparison.Ordinal))
        {
            Settings.SelectedBuild = fileName;
        }

        if (string.Equals(Settings.SelectedEditBuild, oldFileName, StringComparison.Ordinal))
        {
            Settings.SelectedEditBuild = fileName;
        }

        ReloadBuildList();
        LoadBuild(Settings.SelectedBuild);
        LoadAtlasBuild(Settings.SelectedAtlasBuild);
        LoadEditBuild(Settings.SelectedEditBuild);
    }

    internal bool CanRename(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Intersect(Path.GetInvalidFileNameChars()).Any())
        {
            return false;
        }

        var newFilePath = Path.Combine(SkillTreeUrlFilesDir, $"{fileName}.json");
        return !File.Exists(newFilePath);
    }

    private static string RemoveAccName(string url)
    {
        // Aim is to remove the string content but keep the info inside the text file in case user wants to revisit that account/char in the future
        url = url.Split("?accountName")[0];
        url = url.Split("?characterName")[0];
        return url;
    }

    public override void DrawSettings()
    {
        _settingsMenu ??= new PassiveSkillTreePlanterSettingsMenu(this);
        _settingsMenu.Draw(() => base.DrawSettings());
    }

    private void ValidateNodes(HashSet<ushort> currentNodes, Dictionary<ushort, SkillNode> nodeDict)
    {
        var missingSourceNodeIds = new List<ushort>();
        var missingLinkNodeIds = new List<ushort>();
        foreach (var urlNodeId in currentNodes)
        {
            if (!nodeDict.TryGetValue(urlNodeId, out var node))
            {
                missingSourceNodeIds.Add(urlNodeId);
                continue;
            }

            foreach (var lNodeId in node.linkedNodes?.Where(currentNodes.Contains) ?? [])
            {
                if (!nodeDict.ContainsKey(lNodeId))
                {
                    missingLinkNodeIds.Add(lNodeId);
                }
            }
        }

        if (missingSourceNodeIds.Any())
        {
            LogError($"Can't find passive skill tree nodes with ids: {string.Join(", ", missingSourceNodeIds)}", 5);
        }

        if (missingLinkNodeIds.Any())
        {
            LogError($"Can't find passive skill tree nodes with ids {string.Join(", ", missingLinkNodeIds)} to draw the link", 5);
        }
    }

    private void DrawTreeOverlay(TreePanel treePanel, PoESkillTreeJsonDecoder treeData, IReadOnlySet<ushort> targetNodeIds, Func<IReadOnlySet<ushort>> allocatedNodeIdsFunc, ESkillTreeType type)
    {
        if (targetNodeIds is not {Count: > 0})
        {
            return;
        }

        if (!treePanel.IsVisible)
        {
            return;
        }

        var canvas = treePanel.CanvasElement;
        var baseOffset = new Vector2(canvas.Center.X, canvas.Center.Y);

        var allocatedNodeIds = allocatedNodeIdsFunc();
        DrawTreeOverlay(allocatedNodeIds, targetNodeIds, treeData, canvas.Scale, baseOffset);
        DrawTreeEditOverlay(treeData, canvas.Scale, baseOffset);
        DrawControlPanel(type, treePanel, allocatedNodeIds, targetNodeIds);
    }

    private async SyncTask<bool> ChangeTree(IReadOnlySet<ushort> allocatedNodeIds, IReadOnlySet<ushort> targetNodeIds, TreePanel panel)
    {
        var passivesById = panel.Passives.DistinctBy(x => x.PassiveSkill.PassiveId).ToDictionary(x => x.PassiveSkill.PassiveId);
        var wrongNodes = allocatedNodeIds.Except(targetNodeIds).ToHashSet();
        var nodesToTake = targetNodeIds.Except(allocatedNodeIds).ToHashSet();
        while (panel.IsVisible)
        {
            var nodeToRemove = wrongNodes.Select(arg => passivesById.GetValueOrDefault(arg)).FirstOrDefault(x => x is {IsAllocatedForPlan: true, CanDeallocate: true});
            if (nodeToRemove != null)
            {
                var windowRect = GameController.Window.GetWindowRectangleTimeCache.TopLeft.ToVector2Num();
                if (panel.RefundButton.IsVisible)
                {
                    if (panel.RefundButton.HasShinyHighlight)
                    {
                        DebugWindow.LogMsg("Clicking refund button");
                        Input.Click(MouseButtons.Left);
                    }
                    else
                    {
                        DebugWindow.LogMsg("Hovering refund button");
                        Input.SetCursorPos(windowRect + panel.RefundButton.GetClientRectCache.Center.ToVector2Num());
                    }

                    await Task.Delay(250);
                    continue;
                }

                if (GameController.IngameState.UIHover?.Address == nodeToRemove.Address)
                {
                    DebugWindow.LogMsg($"Clicking passive {nodeToRemove.PassiveSkill?.PassiveId} ({nodeToRemove.PassiveSkill?.Name})");
                    Input.Click(MouseButtons.Left);
                }
                else
                {
                    DebugWindow.LogMsg($"Hovering passive {nodeToRemove.PassiveSkill?.PassiveId} ({nodeToRemove.PassiveSkill?.Name})");
                    Input.SetCursorPos(windowRect + nodeToRemove.GetClientRectCache.Center.ToVector2Num());
                }

                await Task.Delay(250);
            }
            else if (panel.RefundButton.IsVisible)
            {
                var nodeToTake = nodesToTake.Select(arg => passivesById.GetValueOrDefault(arg)).FirstOrDefault(x => x is {IsAllocatedForPlan: false, CanAllocate: true});
                if (nodeToTake != null)
                {
                    var windowRect = GameController.Window.GetWindowRectangleTimeCache.TopLeft.ToVector2Num();

                    if (GameController.IngameState.UIHover?.Address == nodeToTake.Address)
                    {
                        DebugWindow.LogMsg($"Clicking passive {nodeToTake.PassiveSkill?.PassiveId} ({nodeToTake.PassiveSkill?.Name})");
                        Input.Click(MouseButtons.Left);
                    }
                    else
                    {
                        DebugWindow.LogMsg($"Hovering passive {nodeToTake.PassiveSkill?.PassiveId} ({nodeToTake.PassiveSkill?.Name})");
                        Input.SetCursorPos(windowRect + nodeToTake.GetClientRectCache.Center.ToVector2Num());
                    }

                    await Task.Delay(250);
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        return true;
    }

    private void DrawTreeEditOverlay(PoESkillTreeJsonDecoder treeData, float scale, Vector2 baseOffset)
    {
        if (!_editorShown)
        {
            return;
        }

        var nodes = treeData.SkillNodes.Where(x => x.Value.linkedNodes != null).Select(x => x.Value).ToList();
        var pathingNodes = _pathingNodes is {IsCompletedSuccessfully: true} ? _pathingNodes.Result : [];
        DebugWindow.LogMsg($"Solved optimization in {pathingNodes.Count} nodes");
        foreach (var node in nodes)
        {
            var drawSize = node.DrawSize * scale;
            var posX = baseOffset.X + node.DrawPosition.X * scale;
            var posY = baseOffset.Y + node.DrawPosition.Y * scale;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.SetNextWindowPos(new Vector2(posX, posY) - new Vector2(drawSize / 2));
            ImGui.SetNextWindowSize(new Vector2(drawSize));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, Color.Transparent.ToImgui());
            ImGui.PushStyleColor(ImGuiCol.Border, _nodeMap.GetValueOrDefault(node.Id, false) ? Color.Green.ToImgui() : pathingNodes.Contains(node.Id) ? Color.Blue.ToImgui() : Color.Red.ToImgui());
            foreach (var linkedNode in node.linkedNodes)
            {
                if (linkedNode < node.Id)
                {
                    continue;
                }

                if (pathingNodes.Contains(linkedNode) && pathingNodes.Contains(node.Id))
                {
                    Graphics.DrawLine(treeData.SkillNodes[linkedNode].DrawPosition * scale + baseOffset, new Vector2(posX, posY), 5, Color.Blue);
                }
            }

            if (ImGui.Begin($"planter_node_{node.Id}", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar))
            {
                ImGui.SetCursorPos(Vector2.Zero);
                if (ImGui.InvisibleButton("button", ImGui.GetContentRegionAvail()))
                {
                    _nodeMap[node.Id] = !_nodeMap.GetValueOrDefault(node.Id, false);
                    var nodesToPick = _nodeMap.Where(x => x.Value).Select(x => new Vertex((int)x.Key)).ToList();
                    _pathingNodes = Task.Run(() =>
                    {
                        var matrix = nodes.ToDictionary(x => new Vertex(x.Id), x => x.linkedNodes.Select(l => new Vertex(l)).ToList());
                        var graph = new Graph(matrix);
                        return GraphOptimizer.ReduceGraph(graph, nodesToPick).Select(x => (uint)x.Id).ToHashSet();
                    });
                }

                ImGui.End();
            }

            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
        }
    }

    private void DrawTreeOverlay(IReadOnlySet<ushort> allocatedNodeIds, IReadOnlySet<ushort> targetNodeIds, PoESkillTreeJsonDecoder treeData, float scale, Vector2 baseOffset)
    {
        if (_editorShown)
        {
            return;
        }

        var wrongNodes = allocatedNodeIds.Except(targetNodeIds).ToHashSet();
        var missingNodes = targetNodeIds.Except(allocatedNodeIds).ToHashSet();
        var allNodes = targetNodeIds.Union(allocatedNodeIds).Select(x => treeData.SkillNodes.GetValueOrDefault(x)).Where(x => x?.linkedNodes != null).ToList();
        var allConnections = allNodes
            .SelectMany(node => node.linkedNodes.Where(treeData.SkillNodes.ContainsKey).Where(id => targetNodeIds.Contains(id) || allocatedNodeIds.Contains(id))
                .Select(linkedNode => (Math.Min(node.Id, linkedNode), Math.Max(node.Id, linkedNode)))).Distinct().Select(pair => (ids: pair, type: pair switch
            {
                var (a, b) when wrongNodes.Contains(a) || wrongNodes.Contains(b) => ConnectionType.Deallocate,
                var (a, b) when missingNodes.Contains(a) || missingNodes.Contains(b) => ConnectionType.Allocate,
                _ => ConnectionType.Allocated
            })).ToList();

        foreach (var node in allNodes)
        {
            var drawSize = node.DrawSize * scale;
            var posX = baseOffset.X + node.DrawPosition.X * scale;
            var posY = baseOffset.Y + node.DrawPosition.Y * scale;

            var color = (allocatedNodeIds.Contains(node.Id), targetNodeIds.Contains(node.Id)) switch
            {
                (true, true) => Settings.PickedBorderColor.Value,
                (true, false) => Settings.WrongPickedBorderColor.Value,
                (false, true) => Settings.UnpickedBorderColor.Value,
                (false, false) => Color.Orange
            };

            Graphics.DrawImage(_ringImage, new RectangleF(posX - drawSize / 2, posY - drawSize / 2, drawSize, drawSize), color);
        }

        if (Settings.LineWidth > 0)
        {
            foreach (var link in allConnections)
            {
                if (NodeNameEndsWithGateway(link.ids.Item1) && NodeNameEndsWithGateway(link.ids.Item2))
                {
                    continue;
                }

                var node1 = treeData.SkillNodes[link.ids.Item1];
                var node2 = treeData.SkillNodes[link.ids.Item2];
                var node1Pos = baseOffset + node1.DrawPosition * scale;
                var node2Pos = baseOffset + node2.DrawPosition * scale;
                var diffVector = Vector2.Normalize(node2Pos - node1Pos);
                node1Pos += diffVector * node1.DrawSize * scale / 2;
                node2Pos -= diffVector * node2.DrawSize * scale / 2;

                Graphics.DrawLine(node1Pos, node2Pos, Settings.LineWidth, link.type switch
                {
                    ConnectionType.Deallocate => Settings.WrongPickedBorderColor,
                    ConnectionType.Allocate => Settings.UnpickedBorderColor,
                    ConnectionType.Allocated => Settings.PickedBorderColor,
                    _ => Color.Orange
                });

                bool NodeNameEndsWithGateway(ushort nodeId)
                {
                    return treeData.SkillNodes[nodeId].Name.EndsWith(" gateway", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        var textPos = new Vector2(50, 300);
        Graphics.DrawText($"Total Tree Nodes: {targetNodeIds.Count}", textPos, Color.White);
        textPos.Y += 20;
        Graphics.DrawText($"Picked Nodes: {allocatedNodeIds.Count}", textPos, Color.Green);
        textPos.Y += 20;
        Graphics.DrawText($"Wrong Picked Nodes: {wrongNodes.Count}", textPos, Color.Red);
    }

    private enum ConnectionType
    {
        Deallocate,
        Allocate,
        Allocated
    }
}