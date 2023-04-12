
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    /// <summary>
    /// Class that represents the base item catalog information that is showable on UI
    /// </summary>
    [System.Serializable]
    public class ItemCatalogData
    {
        public int id;
        public string displayName;
        public Sprite icon;
    }
}