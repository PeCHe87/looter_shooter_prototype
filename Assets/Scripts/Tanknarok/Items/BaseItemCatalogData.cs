
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    [CreateAssetMenu(fileName = "ItemCatalog_", menuName = "ScriptableObjects/ItemCatalog")]
    public class BaseItemCatalogData : ScriptableObject
    {
        public int id;
        public ItemType type;
        public string displayName;
        public Sprite icon;
        public string description;
    }
}