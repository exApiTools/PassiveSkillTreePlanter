using ExileCore;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using PassiveSkillTreePlanter.UrlDecoders;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace PassiveSkillTreePlanter;

internal sealed class PassiveSkillTreePlanterSettingsMenu
{
    private const string TreeRowDragPayloadId = "PstpBuildEditTreeIndex";

    private readonly PassiveSkillTreePlanter _plugin;

    private string _addNewBuildFile = "";
    private bool _showAtlasBuildNotes = true;
    private bool _showCharacterBuildNotes = true;

    public PassiveSkillTreePlanterSettingsMenu(PassiveSkillTreePlanter plugin)
    {
        _plugin = plugin;
    }

    private static void CenterCellContentHorizontally(float itemWidth)
    {
        var cellAvail = ImGui.GetContentRegionAvail().X;
        var pad = Math.Max(0f, (cellAvail - itemWidth) * 0.5f);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + pad);
    }

    private static bool FullCellCenteredButton(string labelId)
    {
        var avail = ImGui.GetContentRegionAvail();
        var lineH = ImGui.GetFrameHeight();
        var size = new Vector2(avail.X, lineH);
        ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));
        var pressed = ImGui.Button(labelId, size);
        ImGui.PopStyleVar();
        return pressed;
    }

    public void Draw(Action drawCorePluginSettings)
    {
        _plugin.ProcessPendingOfficialTreeReload();

        var contentRegionArea = ImGui.GetContentRegionAvail();
        if (!ImGui.BeginTabBar("PassiveSkillTreePlanter_MainTabs", ImGuiTabBarFlags.None)) return;

        if (ImGui.BeginTabItem("Passive Tree"))
        {
            DrawBuildSelectionSharedMenu(ESkillTreeType.Character);
            var characterTrees = _plugin.SelectedBuildData.Trees.Where(t => t.Type == ESkillTreeType.Character).ToList();
            DrawTreeLoadTableSection(characterTrees, "CharLoad");
            DrawBuildNotesSubsection(_plugin.SelectedBuildData.Notes, ref _showCharacterBuildNotes, "CharSelNotes");
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Atlas Tree"))
        {
            DrawBuildSelectionSharedMenu(ESkillTreeType.Atlas);
            var atlasTrees = _plugin.AtlasBuildData.Trees.Where(t => t.Type == ESkillTreeType.Atlas).ToList();
            DrawTreeLoadTableSection(atlasTrees, "AtlasLoad");
            DrawBuildNotesSubsection(_plugin.AtlasBuildData.Notes, ref _showAtlasBuildNotes, "AtlasSelNotes");
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Build Edit"))
        {
            DrawBuildEdit(contentRegionArea);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("GGG tree JSON"))
        {
            DrawOfficialTreeExportTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Settings"))
        {
            drawCorePluginSettings();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawOfficialTreeExportTab()
    {
        if (ImGui.Button("Open skilltree-export on GitHub##ggg"))
        {
            Process.Start(new ProcessStartInfo("https://github.com/grindinggear/skilltree-export")
            {
                UseShellExecute = true
            });
        }

        if (ImGui.Button("Open atlastree-export on GitHub##ggg"))
        {
            Process.Start(new ProcessStartInfo("https://github.com/grindinggear/atlastree-export")
            {
                UseShellExecute = true
            });
        }

        ImGui.Spacing();
        if (ImGui.Button("Redownload SkillTreeData.json##gggPassive")) _plugin.QueueRedownloadOfficialTreeData(false);

        if (ImGui.Button("Redownload AtlasTreeData.json##gggAtlas")) _plugin.QueueRedownloadOfficialTreeData(true);
    }

    private void DrawBuildSelectionSharedMenu(ESkillTreeType sectionTreeKind)
    {
        if (ImGui.Button("Open Build Folder")) Process.Start("explorer.exe", Path.Join(_plugin.ConfigDirectory, "Builds"));

        ImGui.SameLine();
        if (ImGui.Button("(Re)Load List")) _plugin.ReloadBuildList();

        ImGui.Separator();

        switch (sectionTreeKind)
        {
            case ESkillTreeType.Character:
            {
                var newCharacterBuild = ImGuiExtension.ComboBox("Character build", _plugin.Settings.SelectedBuild, _plugin.BuildFiles, out var characterBuildSelected, ImGuiComboFlags.HeightLarge);
                if (characterBuildSelected)
                {
                    _plugin.LoadBuild(newCharacterBuild);
                }

                break;
            }
            case ESkillTreeType.Atlas:
            {
                var newAtlasBuild = ImGuiExtension.ComboBox("Atlas build", _plugin.Settings.SelectedAtlasBuild, _plugin.BuildFiles, out var atlasBuildSelected, ImGuiComboFlags.HeightLarge);
                if (atlasBuildSelected)
                {
                    _plugin.LoadAtlasBuild(newAtlasBuild);
                }

                break;
            }
            case ESkillTreeType.Unknown:
            default:
                break;
        }

        var forumLinkForSection = sectionTreeKind switch
        {
            ESkillTreeType.Character => _plugin.SelectedBuildData.BuildLink,
            ESkillTreeType.Atlas => _plugin.AtlasBuildData.BuildLink,
            _ => ""
        };

        var forumIdSuffix = sectionTreeKind switch
        {
            ESkillTreeType.Character => "char",
            ESkillTreeType.Atlas => "atlas",
            _ => "na"
        };

        ImGui.BeginDisabled(string.IsNullOrEmpty(forumLinkForSection));
        if (ImGui.Button($"Open forum thread##forumBtn_{forumIdSuffix}"))
        {
            Process.Start(new ProcessStartInfo(forumLinkForSection)
            {
                UseShellExecute = true
            });
        }

        ImGui.EndDisabled();

        ImGui.InputText("##CreationLabel", ref _addNewBuildFile, 1024, ImGuiInputTextFlags.EnterReturnsTrue);
        ImGui.BeginDisabled(!_plugin.CanRename(_addNewBuildFile));
        if (ImGui.Button($"Add new build {_addNewBuildFile}"))
        {
            TreeConfig.SaveSettingFile(Path.Join(_plugin.SkillTreeUrlFilesDir, _addNewBuildFile), new TreeConfig.SkillTreeData());
            _addNewBuildFile = string.Empty;
            _plugin.ReloadBuildList();
        }

        ImGui.EndDisabled();
    }

    private static void DrawBuildNotesSubsection(string notesText, ref bool showNotes, string subsectionId)
    {
        ImGui.Spacing();
        ImGui.Checkbox($"Show notes##{subsectionId}", ref showNotes);
        ImGui.Separator();
        if (!showNotes)
        {
            return;
        }

        ImGui.BeginChild("#notes");
        ImGuiNative.igPushTextWrapPos(0.0f);
        ImGui.TextUnformatted(string.IsNullOrEmpty(notesText) ? "" : notesText);
        ImGuiNative.igPopTextWrapPos();
        ImGui.EndChild();
    }

    private void DrawTreeLoadTableSection(List<TreeConfig.Tree> trees, string idPrefix)
    {
        if (!ImGui.BeginTable($"LoadTable_{idPrefix}", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchProp))
        {
            return;
        }

        ImGui.TableSetupColumn("Load", ImGuiTableColumnFlags.WidthFixed, 56f);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 42f);
        ImGui.TableSetupColumn("Tree Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        var iconSize = new Vector2(ImGui.CalcTextSize("A").Y);
        for (var j = 0; j < trees.Count; j++)
        {
            ImGui.TableNextRow();
            ImGui.PushID($"{idPrefix}_row_{j}");

            ImGui.TableSetColumnIndex(0);
            if (FullCellCenteredButton("LOAD"))
            {
                _plugin.LoadUrl(trees[j].SkillTreeUrl);
            }

            ImGui.TableSetColumnIndex(1);
            var iconsIndex = trees[j].Type switch
            {
                ESkillTreeType.Unknown => MapIconsIndex.QuestObject,
                ESkillTreeType.Character => MapIconsIndex.MyPlayer,
                ESkillTreeType.Atlas => MapIconsIndex.TangleAltar
            };

            var rect = SpriteHelper.GetUV(iconsIndex);
            CenterCellContentHorizontally(iconSize.X);
            ImGui.Image(_plugin.Graphics.GetTextureId("Icons.png"), iconSize, rect.TopLeft.ToVector2Num(), rect.BottomRight.ToVector2Num());

            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted(string.IsNullOrEmpty(trees[j].Tag) ? " " : $" {trees[j].Tag}");

            ImGui.PopID();
        }

        ImGui.EndTable();
    }

    private void DrawBuildEdit(Vector2 contentRegionArea)
    {
        if (ImGui.Button("Open Build Folder")) Process.Start("explorer.exe", Path.Join(_plugin.ConfigDirectory, "Builds"));

        ImGui.SameLine();
        if (ImGui.Button("(Re)Load List")) _plugin.ReloadBuildList();

        ImGui.Separator();

        var newEditBuild = ImGuiExtension.ComboBox("Build to edit", _plugin.Settings.SelectedEditBuild, _plugin.BuildFiles, out var editBuildSelected, ImGuiComboFlags.HeightLarge);
        if (editBuildSelected)
        {
            _plugin.LoadEditBuild(newEditBuild);
        }

        var renameValue = _plugin.BuildNameEditorValue;
        ImGui.InputText("##RenameLabel", ref renameValue, 200, ImGuiInputTextFlags.None);
        _plugin.BuildNameEditorValue = renameValue;
        ImGui.SameLine();
        ImGui.BeginDisabled(!_plugin.CanRename(_plugin.BuildNameEditorValue));
        if (ImGui.Button("Rename Build"))
        {
            _plugin.RenameFile(_plugin.BuildNameEditorValue, _plugin.Settings.SelectedEditBuild);
        }

        ImGui.EndDisabled();

        if (ImGui.Button("Save Build") || _plugin.EditBuildData.Modified && _plugin.Settings.SaveChangesAutomatically)
        {
            _plugin.EditBuildData.Modified = false;
            TreeConfig.SaveSettingFile(Path.Join(_plugin.SkillTreeUrlFilesDir, _plugin.Settings.SelectedEditBuild), _plugin.EditBuildData);
            _plugin.ReloadBuildList();
            if (string.Equals(_plugin.Settings.SelectedEditBuild, _plugin.Settings.SelectedBuild, StringComparison.Ordinal))
            {
                _plugin.LoadBuild(_plugin.Settings.SelectedBuild);
            }

            if (string.Equals(_plugin.Settings.SelectedEditBuild, _plugin.Settings.SelectedAtlasBuild, StringComparison.Ordinal))
            {
                _plugin.LoadAtlasBuild(_plugin.Settings.SelectedAtlasBuild);
            }
        }

        if (_plugin.EditBuildData.Modified)
        {
            ImGui.TextColored(Color.Red.ToImguiVec4(), "Unsaved changes detected");
        }

        ImGui.Separator();

        var trees = _plugin.EditBuildData.Trees;
        if (trees.Count > 0)
        {
            var buildLink = _plugin.EditBuildData.BuildLink;
            if (ImGui.InputText("Forum Thread", ref buildLink, 1024, ImGuiInputTextFlags.None))
            {
                _plugin.EditBuildData.BuildLink = buildLink.Replace("\u0000", null);
                _plugin.EditBuildData.Modified = true;
            }

            ImGui.Text("Notes");
            var notes = _plugin.EditBuildData.Notes;
            if (ImGui.InputTextMultiline("##Notes", ref notes, 150000, new Vector2(contentRegionArea.X - 20, 200)))
            {
                _plugin.EditBuildData.Notes = notes.Replace("\u0000", null);
                _plugin.EditBuildData.Modified = true;
            }

            ImGui.Separator();
            if (ImGui.BeginTable("EditTreesTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Drag", ImGuiTableColumnFlags.WidthFixed, 40f);
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 38f);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 42f);
                ImGui.TableSetupColumn("Tree Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Skill Tree", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                for (var j = 0; j < trees.Count; j++)
                {
                    ImGui.TableNextRow();
                    ImGui.PushID($"{j}");
                    DrawTreeEdit(trees, j);
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            ImGui.Separator();
        }
        else
        {
            ImGui.Text("No Data Selected");
        }

        if (ImGui.Button("+##AN"))
        {
            trees.Add(new TreeConfig.Tree());
            _plugin.EditBuildData.Modified = true;
        }

        ImGui.Text("Export current build");
        ImGui.SameLine();
        var rectMyPlayer = SpriteHelper.GetUV(MapIconsIndex.MyPlayer);
        if (ImGui.ImageButton("charBtn", _plugin.Graphics.GetTextureId("Icons.png"), new Vector2(ImGui.CalcTextSize("A").Y), rectMyPlayer.TopLeft.ToVector2Num(),
                rectMyPlayer.BottomRight.ToVector2Num()))
        {
            trees.Add(new TreeConfig.Tree
            {
                Tag = "Current character tree",
                SkillTreeUrl = PathOfExileUrlDecoder.Encode(_plugin.GameController.Game.IngameState.ServerData.PassiveSkillIds.ToHashSet(), ESkillTreeType.Character)
            });

            _plugin.EditBuildData.Modified = true;
        }

        ImGui.SameLine();
        var rectTangle = SpriteHelper.GetUV(MapIconsIndex.TangleAltar);
        if (ImGui.ImageButton("atlasBtn", _plugin.Graphics.GetTextureId("Icons.png"), new Vector2(ImGui.CalcTextSize("A").Y), rectTangle.TopLeft.ToVector2Num(), rectTangle.BottomRight.ToVector2Num()))
        {
            trees.Add(new TreeConfig.Tree
            {
                Tag = "Current atlas tree",
                SkillTreeUrl = PathOfExileUrlDecoder.Encode(_plugin.GameController.Game.IngameState.ServerData.AtlasPassiveSkillIds.ToHashSet(), ESkillTreeType.Atlas)
            });

            _plugin.EditBuildData.Modified = true;
        }

        foreach (var importer in _plugin.UrlImporters)
        {
            if (importer.DrawAddInterface() is { } newTree)
            {
                trees.Add(newTree);
                _plugin.EditBuildData.Modified = true;
            }
        }
    }

    private void DrawTreeEdit(List<TreeConfig.Tree> trees, int treeIndex)
    {
        ImGui.TableSetColumnIndex(0);
        var rowLabel = string.IsNullOrEmpty(trees[treeIndex].Tag) ? $"row_{treeIndex}" : trees[treeIndex].Tag;
        ImGui.PushID($"drag_{treeIndex}_{rowLabel.GetHashCode()}");

        var dropTargetStart = ImGui.GetCursorScreenPos();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
        ImGui.Button("=", new Vector2(30, 20));
        ImGui.PopStyleColor();

        if (ImGui.BeginDragDropSource())
        {
            ImGuiHelpers.SetDragDropPayload(TreeRowDragPayloadId, treeIndex);
            ImGui.TextUnformatted(string.IsNullOrEmpty(trees[treeIndex].Tag) ? $"(tree {treeIndex})" : trees[treeIndex].Tag);
            ImGui.EndDragDropSource();
        }
        else if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Drag to reorder");
        }

        ImGui.SetCursorScreenPos(dropTargetStart);
        ImGui.InvisibleButton($"dropTreeRow##{treeIndex}", new Vector2(30, 20));

        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGuiHelpers.AcceptDragDropPayload<int>(TreeRowDragPayloadId);
            if (payload != null && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                var from = payload.Value;
                if (from >= 0 && from < trees.Count && treeIndex >= 0 && treeIndex < trees.Count)
                {
                    var moved = trees[from];
                    trees.RemoveAt(from);
                    trees.Insert(treeIndex, moved);
                    _plugin.EditBuildData.Modified = true;
                }
            }

            ImGui.EndDragDropTarget();
        }

        ImGui.PopID();

        ImGui.TableSetColumnIndex(1);
        if (FullCellCenteredButton("X##REMOVERULE"))
        {
            trees.RemoveAt(treeIndex);
            _plugin.EditBuildData.Modified = true;
            return;
        }

        ImGui.TableSetColumnIndex(2);
        var iconsIndex = trees[treeIndex].Type switch
        {
            ESkillTreeType.Unknown => MapIconsIndex.QuestObject,
            ESkillTreeType.Character => MapIconsIndex.MyPlayer,
            ESkillTreeType.Atlas => MapIconsIndex.TangleAltar
        };

        var rect = SpriteHelper.GetUV(iconsIndex);
        var iconSize = new Vector2(ImGui.CalcTextSize("A").Y);
        CenterCellContentHorizontally(iconSize.X);
        ImGui.Image(_plugin.Graphics.GetTextureId("Icons.png"), iconSize, rect.TopLeft.ToVector2Num(), rect.BottomRight.ToVector2Num());

        ImGui.TableSetColumnIndex(3);
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.InputText("##TAG", ref trees[treeIndex].Tag, 1024, ImGuiInputTextFlags.AutoSelectAll);
        ImGui.PopItemWidth();

        ImGui.TableSetColumnIndex(4);
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputText("##GN", ref trees[treeIndex].SkillTreeUrl, 1024, ImGuiInputTextFlags.AutoSelectAll))
        {
            trees[treeIndex].ResetType();
            _plugin.EditBuildData.Modified = true;
        }

        ImGui.PopItemWidth();
    }
}