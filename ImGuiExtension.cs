using System.Collections.Generic;
using ImGuiNET;

namespace PassiveSkillTreePlanter;

public class ImGuiExtension
{
    public static string ComboBox(string sideLabel, string currentSelectedItem, List<string> objectList, out bool didChange,
        ImGuiComboFlags comboFlags = ImGuiComboFlags.HeightRegular)
    {
        if (ImGui.BeginCombo(sideLabel, currentSelectedItem, comboFlags))
        {
            foreach (var obj in objectList)
            {
                var isSelected = currentSelectedItem == obj;

                if (ImGui.Selectable(obj, isSelected))
                {
                    didChange = true;
                    ImGui.EndCombo();
                    return obj;
                }

                if (isSelected) ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }

        didChange = false;
        return currentSelectedItem;
    }
}