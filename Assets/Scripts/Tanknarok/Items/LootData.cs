
using Fusion;

namespace FusionExamples.Tanknarok.Items
{
    /// <summary>
    /// Network struct that represents the information related with a loot and its items
    /// </summary>
    [System.Serializable]
    public struct LootData : INetworkStruct
    {
        public int id;
        [Networked, Capacity(16)] public NetworkArray<ItemLootData> items => default;
    }
}