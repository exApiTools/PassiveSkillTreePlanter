using System;
using System.Collections.Generic;

namespace PassiveSkillTreePlanterNew
{

    public partial class PoESkillTree
    {
        public List<Class> Classes { get; set; }
        public Dictionary<string, Group> Groups { get; set; }
        public Dictionary<string, Node> Nodes { get; set; }
        public Dictionary<string, ExtraImage> ExtraImages { get; set; }
        public List<long> JewelSlots { get; set; }
        public long Min_X { get; set; }
        public long Min_Y { get; set; }
        public long Max_X { get; set; }
        public long Max_Y { get; set; }
        public Assets Assets { get; set; }
        public Constants Constants { get; set; }
        public SkillSprites SkillSprites { get; set; }
        public List<double> ImageZoomLevels { get; set; }
    }

    public partial class Assets
    {
        public AscendancyButton PsSkillFrame { get; set; }
        public AscendancyButton PsSkillFrameHighlighted { get; set; }
        public AscendancyButton PsSkillFrameActive { get; set; }
        public AscendancyButton KeystoneFrameUnallocated { get; set; }
        public AscendancyButton KeystoneFrameCanAllocate { get; set; }
        public AscendancyButton KeystoneFrameAllocated { get; set; }
        public AscendancyButton PsGroupBackground1 { get; set; }
        public AscendancyButton PsGroupBackground2 { get; set; }
        public AscendancyButton PsGroupBackground3 { get; set; }
        public AscendancyButton GroupBackgroundSmallAlt { get; set; }
        public AscendancyButton GroupBackgroundMediumAlt { get; set; }
        public AscendancyButton GroupBackgroundLargeHalfAlt { get; set; }
        public AscendancyButton Orbit1Normal { get; set; }
        public AscendancyButton Orbit1Intermediate { get; set; }
        public AscendancyButton Orbit1Active { get; set; }
        public AscendancyButton Orbit2Normal { get; set; }
        public AscendancyButton Orbit2Intermediate { get; set; }
        public AscendancyButton Orbit2Active { get; set; }
        public AscendancyButton Orbit3Normal { get; set; }
        public AscendancyButton Orbit3Intermediate { get; set; }
        public AscendancyButton Orbit3Active { get; set; }
        public AscendancyButton Orbit4Normal { get; set; }
        public AscendancyButton Orbit4Intermediate { get; set; }
        public AscendancyButton Orbit4Active { get; set; }
        public AscendancyButton LineConnectorNormal { get; set; }
        public AscendancyButton LineConnectorIntermediate { get; set; }
        public AscendancyButton LineConnectorActive { get; set; }
        public AscendancyButton PsLineDeco { get; set; }
        public AscendancyButton PsLineDecoHighlighted { get; set; }
        public AscendancyButton PsStartNodeBackgroundInactive { get; set; }
        public AscendancyButton Centerduelist { get; set; }
        public AscendancyButton Centermarauder { get; set; }
        public AscendancyButton Centerranger { get; set; }
        public AscendancyButton Centershadow { get; set; }
        public AscendancyButton Centertemplar { get; set; }
        public AscendancyButton Centerwitch { get; set; }
        public AscendancyButton Centerscion { get; set; }
        public EternalEmpireJewelCircle1 PsPointsFrame { get; set; }
        public AscendancyButton NotableFrameUnallocated { get; set; }
        public AscendancyButton NotableFrameCanAllocate { get; set; }
        public AscendancyButton NotableFrameAllocated { get; set; }
        public AscendancyButton BlightedNotableFrameUnallocated { get; set; }
        public AscendancyButton BlightedNotableFrameCanAllocate { get; set; }
        public AscendancyButton BlightedNotableFrameAllocated { get; set; }
        public AscendancyButton JewelFrameUnallocated { get; set; }
        public AscendancyButton JewelFrameCanAllocate { get; set; }
        public AscendancyButton JewelFrameAllocated { get; set; }
        public AscendancyButton JewelSocketActiveBlue { get; set; }
        public AscendancyButton JewelSocketActiveGreen { get; set; }
        public AscendancyButton JewelSocketActiveRed { get; set; }
        public AscendancyButton JewelSocketActivePrismatic { get; set; }
        public AscendancyButton JewelSocketActiveAbyss { get; set; }
        public AscendancyButton JewelSocketActiveLegion { get; set; }
        public EternalEmpireJewelCircle1 JewelCircle1 { get; set; }
        public EternalEmpireJewelCircle1 JewelCircle1Inverse { get; set; }
        public EternalEmpireJewelCircle1 VaalJewelCircle1 { get; set; }
        public EternalEmpireJewelCircle1 VaalJewelCircle2 { get; set; }
        public EternalEmpireJewelCircle1 KaruiJewelCircle1 { get; set; }
        public EternalEmpireJewelCircle1 KaruiJewelCircle2 { get; set; }
        public EternalEmpireJewelCircle1 MarakethJewelCircle1 { get; set; }
        public EternalEmpireJewelCircle1 MarakethJewelCircle2 { get; set; }
        public EternalEmpireJewelCircle1 TemplarJewelCircle1 { get; set; }
        public EternalEmpireJewelCircle1 TemplarJewelCircle2 { get; set; }
        public EternalEmpireJewelCircle1 EternalEmpireJewelCircle1 { get; set; }
        public EternalEmpireJewelCircle1 EternalEmpireJewelCircle2 { get; set; }
        public AscendancyButton JewelSocketAltNormal { get; set; }
        public AscendancyButton JewelSocketAltCanAllocate { get; set; }
        public AscendancyButton JewelSocketAltActive { get; set; }
        public AscendancyButton JewelSocketActiveBlueAlt { get; set; }
        public AscendancyButton JewelSocketActiveGreenAlt { get; set; }
        public AscendancyButton JewelSocketActiveRedAlt { get; set; }
        public AscendancyButton JewelSocketActivePrismaticAlt { get; set; }
        public AscendancyButton JewelSocketActiveAbyssAlt { get; set; }
        public AscendancyButton JewelSocketActiveLegionAlt { get; set; }
        public AscendancyButton JewelSocketClusterAltNormal1Small { get; set; }
        public AscendancyButton JewelSocketClusterAltCanAllocate1Small { get; set; }
        public AscendancyButton JewelSocketClusterAltNormal1Medium { get; set; }
        public AscendancyButton JewelSocketClusterAltCanAllocate1Medium { get; set; }
        public AscendancyButton JewelSocketClusterAltNormal1Large { get; set; }
        public AscendancyButton JewelSocketClusterAltCanAllocate1Large { get; set; }
        public AscendancyButton AscendancyButton { get; set; }
        public AscendancyButton AscendancyButtonHighlight { get; set; }
        public AscendancyButton AscendancyButtonPressed { get; set; }
        public AscendancyButton AscendancyFrameLargeAllocated { get; set; }
        public AscendancyButton AscendancyFrameLargeCanAllocate { get; set; }
        public AscendancyButton AscendancyFrameLargeNormal { get; set; }
        public AscendancyButton AscendancyFrameSmallAllocated { get; set; }
        public AscendancyButton AscendancyFrameSmallCanAllocate { get; set; }
        public AscendancyButton AscendancyFrameSmallNormal { get; set; }
        public AscendancyButton AscendancyMiddle { get; set; }
        public AscendancyButton ClassesAscendant { get; set; }
        public AscendancyButton ClassesJuggernaut { get; set; }
        public AscendancyButton ClassesBerserker { get; set; }
        public AscendancyButton ClassesChieftain { get; set; }
        public AscendancyButton ClassesRaider { get; set; }
        public AscendancyButton ClassesDeadeye { get; set; }
        public AscendancyButton ClassesPathfinder { get; set; }
        public AscendancyButton ClassesOccultist { get; set; }
        public AscendancyButton ClassesElementalist { get; set; }
        public AscendancyButton ClassesNecromancer { get; set; }
        public AscendancyButton ClassesSlayer { get; set; }
        public AscendancyButton ClassesGladiator { get; set; }
        public AscendancyButton ClassesChampion { get; set; }
        public AscendancyButton ClassesInquisitor { get; set; }
        public AscendancyButton ClassesHierophant { get; set; }
        public AscendancyButton ClassesGuardian { get; set; }
        public AscendancyButton ClassesAssassin { get; set; }
        public AscendancyButton ClassesTrickster { get; set; }
        public AscendancyButton ClassesSaboteur { get; set; }
        public AscendancyButton Background1 { get; set; }
        public AscendancyButton BackgroundDex { get; set; }
        public AscendancyButton BackgroundDexInt { get; set; }
        public AscendancyButton BackgroundInt { get; set; }
        public AscendancyButton BackgroundStr { get; set; }
        public AscendancyButton BackgroundStrDex { get; set; }
        public AscendancyButton BackgroundStrInt { get; set; }
        public EternalEmpireJewelCircle1 ImgPsFadeCorner { get; set; }
        public EternalEmpireJewelCircle1 ImgPsFadeSide { get; set; }
    }

    public partial class AscendancyButton
    {
        public Uri The01246 { get; set; }
        public Uri The02109 { get; set; }
        public Uri The02972 { get; set; }
        public Uri The03835 { get; set; }
    }

    public partial class EternalEmpireJewelCircle1
    {
        public Uri The1 { get; set; }
    }

    public partial class Class
    {
        public string Name { get; set; }
        public long BaseStr { get; set; }
        public long BaseDex { get; set; }
        public long BaseInt { get; set; }
        public List<Ascendancy> Ascendancies { get; set; }
    }

    public partial class Ascendancy
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FlavourText { get; set; }
        public string FlavourTextColour { get; set; }
        public FlavourTextRect FlavourTextRect { get; set; }
    }

    public partial class FlavourTextRect
    {
        public long X { get; set; }
        public long Y { get; set; }
        public long Width { get; set; }
        public long Height { get; set; }
    }

    public partial class Constants
    {
        public Classes Classes { get; set; }
        public CharacterAttributes CharacterAttributes { get; set; }
        public long PssCentreInnerRadius { get; set; }
        public List<int> SkillsPerOrbit { get; set; }
        public List<int> OrbitRadii { get; set; }
    }

    public partial class CharacterAttributes
    {
        public long Strength { get; set; }
        public long Dexterity { get; set; }
        public long Intelligence { get; set; }
    }

    public partial class Classes
    {
        public long StrDexIntClass { get; set; }
        public long StrClass { get; set; }
        public long DexClass { get; set; }
        public long IntClass { get; set; }
        public long StrDexClass { get; set; }
        public long StrIntClass { get; set; }
        public long DexIntClass { get; set; }
    }

    public partial class ExtraImage
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Image { get; set; }
    }

    public partial class Group
    {
        public double X { get; set; }
        public double Y { get; set; }
        public List<long> Orbits { get; set; }
        public List<ushort> Nodes { get; set; }
        public bool IsProxy { get; set; }
    }

    public partial class Node
    {
        public long Skill { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public bool IsNotable { get; set; }
        public List<Recipe> Recipe { get; set; }
        public List<string> Stats { get; set; }
        public long Group { get; set; }
        public long Orbit { get; set; }
        public long OrbitIndex { get; set; }
        public List<long> Out { get; set; }
        public List<long> In { get; set; }
        public List<string> ReminderText { get; set; }
        public bool IsMastery { get; set; }
        public long GrantedStrength { get; set; }
        public string AscendancyName { get; set; }
        public long GrantedDexterity { get; set; }
        public bool IsAscendancyStart { get; set; }
        public bool IsMultipleChoice { get; set; }
        public long GrantedIntelligence { get; set; }
        public bool IsJewelSocket { get; set; }
        public ExpansionJewel ExpansionJewel { get; set; }
        public long GrantedPassivePoints { get; set; }
        public bool IsKeystone { get; set; }
        public List<string> FlavourText { get; set; }
        public bool IsProxy { get; set; }
        public bool IsMultipleChoiceOption { get; set; }
        public bool IsBlighted { get; set; }
        public int? ClassStartIndex { get; set; }
    }

    public partial class ExpansionJewel
    {
        public long Size { get; set; }
        public long Index { get; set; }
        public long Proxy { get; set; }
        public long Parent { get; set; }
    }

    public partial class SkillSprites
    {
        public List<KeystoneActive> NormalActive { get; set; }
        public List<KeystoneActive> NotableActive { get; set; }
        public List<KeystoneActive> KeystoneActive { get; set; }
        public List<KeystoneActive> NormalInactive { get; set; }
        public List<KeystoneActive> NotableInactive { get; set; }
        public List<KeystoneActive> KeystoneInactive { get; set; }
        public List<KeystoneActive> Mastery { get; set; }
    }

    public partial class KeystoneActive
    {
        public string Filename { get; set; }
        public Dictionary<string, Coord> Coords { get; set; }
    }

    public partial class Coord
    {
        public long X { get; set; }
        public long Y { get; set; }
        public long W { get; set; }
        public long H { get; set; }
    }

    public enum Recipe { AmberOil, AzureOil, BlackOil, ClearOil, CrimsonOil, GoldenOil, IndigoOil, OpalescentOil, SepiaOil, SilverOil, TealOil, VerdantOil, VioletOil };
}
