using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace PassiveSkillTreePlanter;

public class PassiveSkillTreePlanterSettings : ISettings
{
    public string SelectedBuild { get; set; } = string.Empty;
    public string LastSelectedCharacterUrl { get; set; }
    public string LastSelectedAtlasUrl { get; set; }

    public RangeNode<int> LineWidth { get; set; } = new(3, 0, 5);

    public ColorNode PickedBorderColor { get; set; } = new ColorNode();
    public ColorNode UnpickedBorderColor { get; set; } = new ColorNode(Color.Green);
    public ColorNode WrongPickedBorderColor { get; set; } = new ColorNode(Color.Red);

    public ToggleNode ShowControlPanel { get; set; } = new ToggleNode(true);
    public ToggleNode SaveChangesAutomatically { get; set; } = new ToggleNode(true);
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
}