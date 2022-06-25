using System;
using System.Collections.Generic;
using PassiveSkillTreePlanterNew;
using SharpDX;

namespace PassiveSkillTreePlanter
{
    public class SkillNode
    {
        public Constants Constants { private get; init; }
        public List<int> OrbitRadii => Constants.OrbitRadii;
        public List<int> SkillsPerOrbit => Constants.SkillsPerOrbit;
        public bool bJevel;
        public bool bKeyStone;
        public bool bMastery;
        public bool bMult;
        public bool bNotable;
        public List<Vector2> DrawNodeLinks = new List<Vector2>();
        public Vector2 DrawPosition;

        //Cached for drawing
        public float DrawSize = 100;
        public ushort Id; // "id": -28194677,
        public List<long> linkedNodes = new List<long>();
        public string Name; //"dn": "Block Recovery",
        public long Orbit; //  "o": 1,
        public long OrbitIndex; // "oidx": 3,
        public SkillNodeGroup SkillNodeGroup;

        public Vector2 Position
        {
            get
            {
                if (SkillNodeGroup == null) return new Vector2();
                double d = OrbitRadii[(int) Orbit];
                return SkillNodeGroup.Position - new Vector2((float)(d * Math.Sin(-Arc)), (float)(d * Math.Cos(-Arc)));
            }
        }

        public double Arc => GetOrbitAngle(OrbitIndex, SkillsPerOrbit[(int)Orbit]);

        public void Init()
        {
            DrawPosition = Position;

            if (bJevel)
                DrawSize = 160;

            if (bNotable)
                DrawSize = 170;

            if (bKeyStone)
                DrawSize = 250;
        }

        private static readonly int[] Angles16 = { 0, 30, 45, 60, 90, 120, 135, 150, 180, 210, 225, 240, 270, 300, 315, 330 };
        private static readonly int[] Angles40 = { 0, 10, 20, 30, 40, 45, 50, 60, 70, 80, 90, 100, 110, 120, 130, 135, 140, 150, 160, 170, 180, 190, 200, 210, 220, 225, 230, 240, 250, 260, 270, 280, 290, 300, 310, 315, 320, 330, 340, 350 };

        private static double GetOrbitAngle(long orbitIndex, long maxNodePositions)
        {
            return orbitIndex switch
            {
                16 => Angles16[orbitIndex],
                40 => Angles40[orbitIndex],
                _ => 2 * Math.PI * orbitIndex / maxNodePositions
            };
        }
    }

    public class SkillNodeGroup
    {
        public List<SkillNode> Nodes = new List<SkillNode>();
        public List<long> OcpOrb = new List<long>();
        public Vector2 Position;
    }
}