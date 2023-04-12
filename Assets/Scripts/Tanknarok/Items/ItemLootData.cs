
using Fusion;

namespace FusionExamples.Tanknarok.Items
{
    /// <summary>
    /// Network struct that represents the information from an item that is part of a Loot
    /// </summary>
    [System.Serializable]
    public struct ItemLootData : INetworkStruct
    {
        public int id;
        public int amount;
    }
}