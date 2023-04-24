
using System.Linq;
using UnityEngine;

namespace FusionExamples.Tanknarok.Items
{
    public class CatalogData : MonoBehaviour
    {
        [SerializeField] private ItemCatalogData[] _items;

        public bool TryGetItem(int id, out ItemCatalogData item)
        {
            item = _items.FirstOrDefault( x => x.data.id == id );

            return item.data.id != 0;
        }
    }
}