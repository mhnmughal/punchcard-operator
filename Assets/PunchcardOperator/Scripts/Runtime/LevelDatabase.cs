using System.Collections.Generic;
using UnityEngine;

namespace PunchcardOperator
{
    [CreateAssetMenu(menuName = "Punchcard Operator/Level Database", fileName = "LevelDatabase")]
    public class LevelDatabase : ScriptableObject
    {
        public List<PunchCardLevelDefinition> levels = new List<PunchCardLevelDefinition>();

        public PunchCardLevelDefinition ById(string id) => levels.Find(l => l != null && l.id == id);

        public PunchCardLevelDefinition Tutorial => levels.Find(l => l != null && l.isTutorial);

        public List<PunchCardLevelDefinition> Campaign
        {
            get
            {
                var list = levels.FindAll(l => l != null && !l.isTutorial);
                list.Sort((x, y) => x.orderIndex.CompareTo(y.orderIndex));
                return list;
            }
        }
    }
}
