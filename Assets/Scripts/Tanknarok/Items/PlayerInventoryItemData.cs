
using Fusion;

namespace FusionExamples.Tanknarok.Items
{
    /// <summary>
    /// Represents the information about a player's item data
    /// </summary>
    [System.Serializable]
    public struct PlayerInventoryItemData : INetworkStruct
    {
        public int id;
        public int amount;
        public bool locked;

        public bool IsEmpty()
        {
            return amount == 0;
        }
    }
}