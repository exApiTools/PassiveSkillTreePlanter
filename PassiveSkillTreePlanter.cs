using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.AtlasHelper;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using PassiveSkillTreePlanter.SkillTreeJson;
using PassiveSkillTreePlanter.UrlDecoders;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace PassiveSkillTreePlanter;

public class PassiveSkillTreePlanter : BaseSettingsPlugin<PassiveSkillTreePlanterSettings>
{
    private const string AtlasTreeDataFile = "AtlasTreeData.json";
    private const string SkillTreeDataFile = "SkillTreeData.json";
    private const string SkillTreeDir = "Builds";
    private readonly PoESkillTreeJsonDecoder _skillTreeData = new PoESkillTreeJsonDecoder();
    private readonly PoESkillTreeJsonDecoder _atlasTreeData = new PoESkillTreeJsonDecoder();

    //List of nodes decoded from URL
    private HashSet<ushort> _characterUrlNodeIds = new HashSet<ushort>();
    private HashSet<ushort> _atlasUrlNodeIds = new HashSet<ushort>();

    private int _selectedSettingsTab;
    private string _addNewBuildFile = "";
    private string _buildNameEditorValue;

    private string _maxrollUrlInput = "";
    private Task<MaxRollFetchResult> _maxRollDataFetchTask;
    private CancellationTokenSource _maxRollDataFetchCancellation;
    private int _selectedMaxrollVariant;
    private int _selectedMaxrollProgress = -1;

    private AtlasTexture _ringImage;

    private List<string> BuildFiles { get; set; } = new List<string>();

    public string SkillTreeUrlFilesDir => Directory.CreateDirectory(Path.Join(ConfigDirectory, SkillTreeDir)).FullName;

    private TreeConfig.SkillTreeData _selectedBuildData = new TreeConfig.SkillTreeData();

    public override void OnLoad()
    {
        _ringImage = GetAtlasTexture("AtlasMapCircle");
        Graphics.InitImage("Icons.png");
        ReloadGameTreeData();
        ReloadBuildList();
        if (string.IsNullOrWhiteSpace(Settings.SelectedBuild))
        {
            Settings.SelectedBuild = "default";
        }

        LoadBuild(Settings.SelectedBuild);
        LoadUrl(Settings.LastSelectedAtlasUrl);
        LoadUrl(Settings.LastSelectedCharacterUrl);
    }

    private void ReloadBuildList()
    {
        BuildFiles = TreeConfig.GetBuilds(SkillTreeUrlFilesDir);
    }

    private void LoadBuild(string buildName)
    {
        Settings.SelectedBuild = buildName;
        _selectedBuildData = TreeConfig.LoadBuild(SkillTreeUrlFilesDir, Settings.SelectedBuild) ?? new TreeConfig.SkillTreeData();
        _characterUrlNodeIds = new HashSet<ushort>();
        _atlasUrlNodeIds = new HashSet<ushort>();
        _buildNameEditorValue = buildName;
    }

    private void LoadUrl(string url)
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

    public override bool Initialise()
    {
        return true;
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
        DrawOverlays();
        DrawTreeSwitcher();
    }

    private void DrawTreeSwitcher()
    {
        if (!Settings.EnableEzTreeChanger)
            return;

        var skillTreeElement = GameController.Game.IngameState.IngameUi.TreePanel;
        var atlasTreeElement = GameController.Game.IngameState.IngameUi.AtlasTreePanel;
        if (!skillTreeElement.IsVisible && !atlasTreeElement.IsVisible)
            return;

        var isOpen = true;
        ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("#treeSwitcher", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            if (skillTreeElement.IsVisible)
            {
                var characterTrees = _selectedBuildData.Trees.Where(x => x.Type == ESkillTreeType.Character).ToList();

                for (var j = 0; j < characterTrees.Count; j++)
                {
                    ImGui.BeginDisabled(Settings.LastSelectedCharacterUrl == characterTrees[j].SkillTreeUrl);
                    if (ImGui.Button($"Load {characterTrees[j].Tag}"))
                    {
                        _selectedBuildData.SelectedIndex = j;
                        LoadUrl(characterTrees[j].SkillTreeUrl);
                    }
                    ImGui.EndDisabled();
                }
            }
            else
            {
                var atlasTrees = _selectedBuildData.Trees.Where(x => x.Type == ESkillTreeType.Atlas).ToList();

                for (var j = 0; j < atlasTrees.Count; j++)
                {
                    ImGui.BeginDisabled(Settings.LastSelectedAtlasUrl == atlasTrees[j].SkillTreeUrl);
                    if (ImGui.Button($"Load {atlasTrees[j].Tag}"))
                    {
                        _selectedBuildData.SelectedIndex = j;
                        LoadUrl(atlasTrees[j].SkillTreeUrl);
                    }
                    ImGui.EndDisabled();
                }
            }
            ImGui.EndMenu();
        }
    }


    private static string CleanFileName(string fileName)
    {
        return Path.GetInvalidFileNameChars()
            .Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
    }

    private void RenameFile(string fileName, string oldFileName)
    {
        fileName = CleanFileName(fileName);
        var oldFilePath = Path.Combine(SkillTreeUrlFilesDir, $"{oldFileName}.json");
        var newFilePath = Path.Combine(SkillTreeUrlFilesDir, $"{fileName}.json");

        File.Move(oldFilePath, newFilePath);
        Settings.SelectedBuild = fileName;
        ReloadBuildList();
        LoadBuild(Settings.SelectedBuild);
    }

    private bool CanRename(string fileName)
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
        string[] settingName =
        {
            "Build Selection",
            "Build Edit",
            "Settings",
        };
        if (ImGui.BeginChild("LeftSettings", new Vector2(150, ImGui.GetContentRegionAvail().Y), false, ImGuiWindowFlags.None))
            for (var i = 0; i < settingName.Length; i++)
                if (ImGui.Selectable(settingName[i], _selectedSettingsTab == i))
                    _selectedSettingsTab = i;

        ImGui.EndChild();
        ImGui.SameLine();
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
        var contentRegionArea = ImGui.GetContentRegionAvail();
        if (ImGui.BeginChild("RightSettings", contentRegionArea, true, ImGuiWindowFlags.None))
        {
            var trees = _selectedBuildData.Trees;
            switch (settingName[_selectedSettingsTab])
            {
                case "Build Selection":
                    if (ImGui.Button("Open Build Folder"))
                        Process.Start(SkillTreeUrlFilesDir);

                    ImGui.SameLine();
                    if (ImGui.Button("(Re)Load List"))
                        ReloadBuildList();

                    ImGui.SameLine();
                    if (ImGui.Button("Open Forum Thread"))
                        Process.Start(_selectedBuildData.BuildLink);

                    var newBuildName = ImGuiExtension.ComboBox("Builds", Settings.SelectedBuild,
                        BuildFiles, out var buildSelected, ImGuiComboFlags.HeightLarge);
                    if (buildSelected)
                    {
                        LoadBuild(newBuildName);
                    }

                    ImGui.Separator();
                    ImGui.Text($"Currently Selected: {Settings.SelectedBuild}");
                    ImGui.InputText("##CreationLabel", ref _addNewBuildFile, 1024, ImGuiInputTextFlags.EnterReturnsTrue);
                    ImGui.BeginDisabled(!CanRename(_addNewBuildFile));
                    if (ImGui.Button($"Add new build {_addNewBuildFile}"))
                    {
                        TreeConfig.SaveSettingFile(Path.Join(SkillTreeUrlFilesDir, _addNewBuildFile), new TreeConfig.SkillTreeData());
                        _addNewBuildFile = string.Empty;
                        ReloadBuildList();
                    }

                    ImGui.EndDisabled();

                    ImGui.Separator();
                    ImGui.Columns(3, "LoadColums", true);
                    ImGui.SetColumnWidth(0, 51f);
                    ImGui.SetColumnWidth(1, 38f);
                    ImGui.Text("");
                    ImGui.NextColumn();
                    ImGui.Text("Type");
                    ImGui.NextColumn();
                    ImGui.Text("Tree Name");
                    ImGui.NextColumn();
                    if (trees.Count != 0)
                        ImGui.Separator();

                    for (var j = 0; j < trees.Count; j++)
                    {
                        if (ImGui.Button($"LOAD##LOADRULE{j}"))
                        {
                            LoadUrl(trees[j].SkillTreeUrl);
                        }

                        ImGui.NextColumn();
                        var iconsIndex = trees[j].Type switch
                        {
                            ESkillTreeType.Unknown => MapIconsIndex.QuestObject,
                            ESkillTreeType.Character => MapIconsIndex.MyPlayer,
                            ESkillTreeType.Atlas => MapIconsIndex.TangleAltar,
                        };
                        var rect = SpriteHelper.GetUV(iconsIndex);
                        ImGui.Image(Graphics.GetTextureId("Icons.png"), new Vector2(ImGui.CalcTextSize("A").Y), rect.TopLeft.ToVector2Num(),
                            rect.BottomRight.ToVector2Num());
                        ImGui.NextColumn();
                        ImGui.Text(trees[j].Tag);
                        ImGui.NextColumn();
                        ImGui.PopItemWidth();
                    }

                    ImGui.Columns(1, "", false);
                    ImGui.Separator();

                    ImGui.Text("NOTES:");

                    // only way to wrap text with imgui.net without a limit on TextWrap function
                    ImGuiNative.igPushTextWrapPos(0.0f);
                    ImGui.TextUnformatted(_selectedBuildData.Notes);
                    ImGuiNative.igPopTextWrapPos();
                    break;
                case "Build Edit":
                    DrawBuildEdit(trees, contentRegionArea);
                    break;
                case "Settings":
                    base.DrawSettings();
                    break;
            }
        }

        ImGui.PopStyleVar();
        ImGui.EndChild();
    }

    private void DrawBuildEdit(List<TreeConfig.Tree> trees, Vector2 contentRegionArea)
    {
        if (trees.Count > 0)
        {
            ImGui.Separator();
            var buildLink = _selectedBuildData.BuildLink;
            if (ImGui.InputText("Forum Thread", ref buildLink, 1024, ImGuiInputTextFlags.None))
            {
                _selectedBuildData.BuildLink = buildLink.Replace("\u0000", null);
                _selectedBuildData.Modified = true;
            }

            ImGui.Text("Notes");
            // Keep at max 4k byte size not sure why it crashes when upped, not going to bother dealing with this either.
            var notes = _selectedBuildData.Notes;
            if (ImGui.InputTextMultiline("##Notes", ref notes, 150000, new Vector2(contentRegionArea.X - 20, 200)))
            {
                _selectedBuildData.Notes = notes.Replace("\u0000", null);
                _selectedBuildData.Modified = true;
            }

            ImGui.Separator();
            ImGui.Columns(5, "EditColums", true);
            ImGui.SetColumnWidth(0, 30f);
            ImGui.SetColumnWidth(1, 50f);
            ImGui.SetColumnWidth(3, 38f);
            ImGui.Text("");
            ImGui.NextColumn();
            ImGui.Text("Move");
            ImGui.NextColumn();
            ImGui.Text("Tree Name");
            ImGui.NextColumn();
            ImGui.Text("Type");
            ImGui.NextColumn();
            ImGui.Text("Skill Tree");
            ImGui.NextColumn();
            if (trees.Count != 0)
                ImGui.Separator();
            for (var j = 0; j < trees.Count; j++)
            {
                ImGui.PushID($"{j}");
                DrawTreeEdit(trees, j);
                ImGui.PopID();
            }

            ImGui.Separator();
            ImGui.Columns(1, "", false);
        }
        else
        {
            ImGui.Text("No Data Selected");
        }

        if (ImGui.Button("+##AN"))
        {
            trees.Add(new TreeConfig.Tree());
            _selectedBuildData.Modified = true;
        }

        ImGui.Text("Export current build");
        ImGui.SameLine();
        var rectMyPlayer = SpriteHelper.GetUV(MapIconsIndex.MyPlayer);
        ImGui.PushID("charBtn");
        if (ImGui.ImageButton(Graphics.GetTextureId("Icons.png"), new Vector2(ImGui.CalcTextSize("A").Y),
                rectMyPlayer.TopLeft.ToVector2Num(), rectMyPlayer.BottomRight.ToVector2Num()))
        {
            trees.Add(new TreeConfig.Tree
            {
                Tag = "Current character tree",
                SkillTreeUrl = PathOfExileUrlDecoder.Encode(GameController.Game.IngameState.ServerData.PassiveSkillIds.ToHashSet(), ESkillTreeType.Character)
            });
            _selectedBuildData.Modified = true;
        }

        ImGui.PopID();

        ImGui.SameLine();
        var rectTangle = SpriteHelper.GetUV(MapIconsIndex.TangleAltar);
        ImGui.PushID("atlasBtn");
        if (ImGui.ImageButton(Graphics.GetTextureId("Icons.png"), new Vector2(ImGui.CalcTextSize("A").Y),
                rectTangle.TopLeft.ToVector2Num(), rectTangle.BottomRight.ToVector2Num()))
        {
            trees.Add(new TreeConfig.Tree
            {
                Tag = "Current atlas tree",
                SkillTreeUrl = PathOfExileUrlDecoder.Encode(GameController.Game.IngameState.ServerData.AtlasPassiveSkillIds.ToHashSet(), ESkillTreeType.Atlas)
            });
            _selectedBuildData.Modified = true;
        }

        ImGui.PopID();

        DrawMaxrollImport(trees);

        ImGui.Separator();

        ImGui.InputText("##RenameLabel", ref _buildNameEditorValue, 200, ImGuiInputTextFlags.None);
        ImGui.SameLine();
        ImGui.BeginDisabled(!CanRename(_buildNameEditorValue));
        if (ImGui.Button("Rename Build"))
        {
            RenameFile(_buildNameEditorValue, Settings.SelectedBuild);
        }

        ImGui.EndDisabled();

        if (ImGui.Button($"Save Build to File: {Settings.SelectedBuild}") ||
            _selectedBuildData.Modified && Settings.SaveChangesAutomatically)
        {
            _selectedBuildData.Modified = false;
            TreeConfig.SaveSettingFile(Path.Join(SkillTreeUrlFilesDir, Settings.SelectedBuild), _selectedBuildData);
            ReloadBuildList();
        }

        if (_selectedBuildData.Modified)
        {
            ImGui.TextColored(Color.Red.ToImguiVec4(), "Unsaved changes detected");
        }
    }

    private void DrawMaxrollImport(List<TreeConfig.Tree> trees)
    {
        if (ImGui.InputText("Import maxroll build", ref _maxrollUrlInput, 200))
        {
            _selectedMaxrollProgress = -1;
            _selectedMaxrollVariant = 0;
            _maxRollDataFetchCancellation?.Cancel();
            _maxRollDataFetchCancellation = null;
            _maxRollDataFetchTask = null;
            if (MaxRollUrlDecoder.IsValidUrl(_maxrollUrlInput))
            {
                _maxRollDataFetchCancellation = new CancellationTokenSource();
                _maxRollDataFetchTask = MaxRollUrlDecoder.FetchNodeList(_maxrollUrlInput, _maxRollDataFetchCancellation.Token);
            }
        }

        if (!string.IsNullOrWhiteSpace(_maxrollUrlInput) && !MaxRollUrlDecoder.IsValidUrl(_maxrollUrlInput))
        {
            ImGui.TextColored(Color.Red.ToImguiVec4(), "Not a valid maxroll url");
        }

        if (_maxRollDataFetchTask is { IsCompletedSuccessfully: true })
        {
            var data = _maxRollDataFetchTask.Result;
            if (data.Embed.Variants?.Length is 0 or null)
            {
                ImGui.TextColored(Color.Red.ToImguiVec4(), "No variants in the requested build");
            }

            else
            {
                if (data.Embed.Variants.Length != 1)
                {
                    if (ImGui.SliderInt("Variant", ref _selectedMaxrollVariant, 0, data.Embed.Variants.Length - 1, null, ImGuiSliderFlags.AlwaysClamp))
                    {
                        _selectedMaxrollProgress = data.Embed.Variants[_selectedMaxrollVariant]?.History?.Length ?? 0;
                    }
                }
                else
                {
                    _selectedMaxrollVariant = 0;
                }

                if (_selectedMaxrollProgress == -1)
                {
                    _selectedMaxrollProgress = data.Embed.Variants[_selectedMaxrollVariant]?.History?.Length ?? 0;
                }

                if (data.Embed.Variants[_selectedMaxrollVariant]?.History == null)
                {
                    ImGui.TextColored(Color.Red.ToImguiVec4(), "Selected variant does not contain valid build data");
                }
                else
                {
                    ImGui.SliderInt("Progress", ref _selectedMaxrollProgress, 0, data.Embed.Variants[_selectedMaxrollVariant].History.Length, null, ImGuiSliderFlags.AlwaysClamp);
                    if (ImGui.Button("Import"))
                    {
                        trees.Add(new TreeConfig.Tree
                        {
                            Tag = $"Maxroll import ({data.Url}), {_selectedMaxrollProgress} pts",
                            SkillTreeUrl = PathOfExileUrlDecoder.Encode(
                                data.Embed.Variants[_selectedMaxrollVariant].History.Take(_selectedMaxrollProgress).Select(x => (ushort)x).ToHashSet(),
                                data.Embed.Type == "atlas" ? ESkillTreeType.Atlas : ESkillTreeType.Character),
                        });
                        _selectedBuildData.Modified = true;
                    }
                }
            }
        }
        else if (_maxRollDataFetchTask is { IsCompleted: false })
        {
            ImGui.Text("Loading the build data...");
        }
        else if (_maxRollDataFetchTask is { IsCompleted: true, IsCompletedSuccessfully: false })
        {
            ImGui.TextColored(Color.Red.ToImguiVec4(), $"Data fetch failed: {_maxRollDataFetchTask.Exception}");
        }
    }

    private void DrawTreeEdit(List<TreeConfig.Tree> trees, int treeIndex)
    {
        if (ImGui.Button("X##REMOVERULE"))
        {
            trees.RemoveAt(treeIndex);
            _selectedBuildData.Modified = true;
            return;
        }

        ImGui.NextColumn();

        ImGui.BeginDisabled(treeIndex == 0);
        if (ImGui.Button("^##MOVERULEUPEDIT"))
        {
            MoveElement(trees, treeIndex, true);
            _selectedBuildData.Modified = true;
        }

        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.BeginDisabled(treeIndex == trees.Count - 1);
        if (ImGui.Button("v##MOVERULEDOWNEDIT"))
        {
            MoveElement(trees, treeIndex, false);
            _selectedBuildData.Modified = true;
        }

        ImGui.EndDisabled();
        ImGui.NextColumn();
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputText("##TAG", ref trees[treeIndex].Tag, 1024, ImGuiInputTextFlags.AutoSelectAll);
        ImGui.PopItemWidth();
        //ImGui.SameLine();
        ImGui.NextColumn();
        var iconsIndex = trees[treeIndex].Type switch
        {
            ESkillTreeType.Unknown => MapIconsIndex.QuestObject,
            ESkillTreeType.Character => MapIconsIndex.MyPlayer,
            ESkillTreeType.Atlas => MapIconsIndex.TangleAltar,
        };
        var rect = SpriteHelper.GetUV(iconsIndex);
        ImGui.Image(Graphics.GetTextureId("Icons.png"), new Vector2(ImGui.CalcTextSize("A").Y), rect.TopLeft.ToVector2Num(),
            rect.BottomRight.ToVector2Num());
        ImGui.NextColumn();
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputText("##GN", ref trees[treeIndex].SkillTreeUrl, 1024, ImGuiInputTextFlags.AutoSelectAll))
        {
            trees[treeIndex].ResetType();
            _selectedBuildData.Modified = true;
        }

        ImGui.PopItemWidth();
        ImGui.NextColumn();
    }

    private static void MoveElement<T>(List<T> list, int changeIndex, bool moveUp)
    {
        if (moveUp)
        {
            // Move Up
            if (changeIndex > 0)
            {
                (list[changeIndex], list[changeIndex - 1]) = (list[changeIndex - 1], list[changeIndex]);
            }
        }
        else
        {
            // Move Down                               
            if (changeIndex < list.Count - 1)
            {
                (list[changeIndex], list[changeIndex + 1]) = (list[changeIndex + 1], list[changeIndex]);
            }
        }
    }

    private void ValidateNodes(HashSet<ushort> currentNodes, Dictionary<ushort, SkillNode> nodeDict)
    {
        foreach (var urlNodeId in currentNodes)
        {
            if (!nodeDict.TryGetValue(urlNodeId, out var node))
            {
                LogError($"PassiveSkillTree: Can't find passive skill tree node with id: {urlNodeId}", 5);
                continue;
            }

            foreach (var lNodeId in node.linkedNodes.Where(currentNodes.Contains))
            {
                if (!nodeDict.ContainsKey(lNodeId))
                {
                    LogError($"PassiveSkillTree: Can't find passive skill tree node with id: {lNodeId} to draw the link", 5);
                }
            }
        }
    }

    private void DrawOverlays()
    {
        DrawTreeOverlay(GameController.Game.IngameState.IngameUi.TreePanel.AsObject<TreePanel>(),
            _skillTreeData, _characterUrlNodeIds,
            () => GameController.Game.IngameState.ServerData.PassiveSkillIds.ToHashSet());
        DrawTreeOverlay(GameController.Game.IngameState.IngameUi.AtlasTreePanel.AsObject<TreePanel>(),
            _atlasTreeData, _atlasUrlNodeIds,
            () => GameController.Game.IngameState.ServerData.AtlasPassiveSkillIds.ToHashSet());
    }

    private void DrawTreeOverlay(TreePanel treePanel, PoESkillTreeJsonDecoder treeData, IReadOnlySet<ushort> targetNodeIds, Func<IReadOnlySet<ushort>> allocatedNodeIds)
    {
        if (targetNodeIds is not { Count: > 0 })
        {
            return;
        }

        if (!treePanel.IsVisible)
        {
            return;
        }

        var canvas = treePanel.CanvasElement;
        var baseOffset = new Vector2(canvas.Center.X, canvas.Center.Y);

        DrawTreeOverlay(allocatedNodeIds(), targetNodeIds, treeData, canvas.Scale, baseOffset);
    }

    private enum ConnectionType
    {
        Deallocate,
        Allocate,
        Allocated,
    }

    private void DrawTreeOverlay(IReadOnlySet<ushort> allocatedNodeIds, IReadOnlySet<ushort> targetNodeIds, PoESkillTreeJsonDecoder treeData, float scale, Vector2 baseOffset)
    {
        var wrongNodes = allocatedNodeIds.Except(targetNodeIds).ToHashSet();
        var missingNodes = targetNodeIds.Except(allocatedNodeIds).ToHashSet();
        var allNodes = targetNodeIds.Union(allocatedNodeIds).Select(x => treeData.SkillNodes.GetValueOrDefault(x)).Where(x => x != null).ToList();
        var allConnections = allNodes
            .SelectMany(node => node.linkedNodes
                .Where(treeData.SkillNodes.ContainsKey)
                .Where(id => targetNodeIds.Contains(id) || allocatedNodeIds.Contains(id))
                .Select(linkedNode => (Math.Min(node.Id, linkedNode), Math.Max(node.Id, linkedNode))))
            .Distinct()
            .Select(pair => (ids: pair, type: pair switch
            {
                var (a, b) when wrongNodes.Contains(a) || wrongNodes.Contains(b) => ConnectionType.Deallocate,
                var (a, b) when missingNodes.Contains(a) || missingNodes.Contains(b) => ConnectionType.Allocate,
                _ => ConnectionType.Allocated,
            }))
            .ToList();
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
                (false, false) => Color.Orange,
            };

            Graphics.DrawImage(_ringImage, new RectangleF(posX - drawSize / 2, posY - drawSize / 2, drawSize, drawSize), color);
        }

        if (Settings.LineWidth > 0)
        {
            foreach (var link in allConnections)
            {
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
                    _ => Color.Orange,
                });
            }
        }

        var textPos = new Vector2(50, 300);
        Graphics.DrawText($"Total Tree Nodes: {targetNodeIds.Count}", textPos, Color.White, 15);
        textPos.Y += 20;
        Graphics.DrawText($"Picked Nodes: {allocatedNodeIds.Count}", textPos, Color.Green, 15);
        textPos.Y += 20;
        Graphics.DrawText($"Wrong Picked Nodes: {wrongNodes.Count}", textPos, Color.Red, 15);
    }
}