using System.Collections.Generic;
using UnityEngine;

namespace PunchcardOperator
{
    [CreateAssetMenu(menuName = "Punchcard Operator/Level Definition", fileName = "Level_00")]
    public class PunchCardLevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id = "00";
        public string displayName = "OPERATOR TRAINING";
        public bool isTutorial;
        public ProgramBank bank = ProgramBank.None;
        [Tooltip("Campaign play order for non-tutorial levels (01 => 0, 12 => 11).")]
        public int orderIndex;

        [Header("Machine floor")]
        public GridDef grid = new GridDef();
        public MachineConfig machineA = new MachineConfig();
        public MachineConfig machineB = new MachineConfig { enabled = false };

        [Header("Punch card")]
        public PunchCard initialCard = new PunchCard();
        public List<CardTool> availableTools = new List<CardTool>();

        [Header("Solution / ratings (verified by the Level Validator)")]
        [Tooltip("Authored solved card. The validator runs the simulator on this and requires success.")]
        public PunchCard solutionCard = new PunchCard();
        [Tooltip("Edits needed to turn initialCard into solutionCard via the available tools (optimal path).")]
        public int solutionEditCount;
        public int compactEditTarget = 3;
        public int perfectEditTarget = 2;

        [Header("Briefing")]
        [TextArea(2, 4)] public string machineObjective;
        [Tooltip("Ordered coach-light prompts. Only used when isTutorial is true.")]
        [TextArea(1, 3)] public List<string> tutorialSteps = new List<string>();

        public bool DualChannel => machineB != null && machineB.enabled;
    }
}
