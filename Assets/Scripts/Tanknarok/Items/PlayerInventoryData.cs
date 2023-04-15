
using Fusion;

namespace FusionExamples.Tanknarok.Items
{
    /// <summary>
    /// Represents the information related with player's inventory data
    /// </summary>
    [System.Serializable]
    public struct PlayerInventoryData : INetworkStruct
    {
        [Networked, Capacity(16)] public NetworkArray<PlayerInventoryItemData> items => default;

        /// <summary>
        /// Check if the item id already exists in the inventory.
        /// In affirmative case, it returns the slot index.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool AlreadyExist(int id, out int index)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];

                if (item.id != id) continue;

                index = i;

                return true;
            }

            index = 0;

            return false;
        }

        /// <summary>
        /// Returns the first slot that is free.
        /// If there is not free slot it returns -1.
        /// </summary>
        /// <returns></returns>
        public int GetFreeSlotIndex()
        {
            var index = -1;

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];

                if (item.locked) continue;

                if (item.id > 0) continue;

                index = i;

                break;
            }

            return index;
        }
    }
}