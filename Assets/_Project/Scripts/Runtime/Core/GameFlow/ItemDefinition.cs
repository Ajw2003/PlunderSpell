using UnityEngine;

namespace Plunderspell.Core
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Plunderspell/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        public string ItemName = "New Item";

        [TextArea]
        public string Description;

        public Sprite Icon;
        public int MaxStack = 1;
        public int GoldValue;
    }
}
