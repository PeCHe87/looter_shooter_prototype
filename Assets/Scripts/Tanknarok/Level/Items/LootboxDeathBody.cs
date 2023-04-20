
using UnityEngine;

namespace FusionExamples.Tanknarok
{
    public class LootboxDeathBody : LootboxBase
    {
        [SerializeField] private GameObject _art = default;
        [SerializeField] private SpriteRenderer _mapIndicator = default;

        protected override void Empty()
        {
            _isEmpty = true;

            _art.Toggle(false);
            _mapIndicator.gameObject.Toggle(false);
        }
    }
}