
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    public enum ItemType { NONE, RESOURCE, CONSUMABLE, EQUIPABLE }

    /// <summary>
    /// Class that represents the base item catalog information that is showable on UI
    /// </summary>
    [System.Serializable]
    public class ItemCatalogData
    {
        public int id;
        public BaseItemCatalogData data;
    }
}