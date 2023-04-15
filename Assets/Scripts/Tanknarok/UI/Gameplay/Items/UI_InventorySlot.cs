using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FusionExamples.Tanknarok.Items;

namespace FusionExamples.Tanknarok.UI
{
    public class UI_InventorySlot : MonoBehaviour
    {
        #region Inspector

        [Header("Regular panel")]
        [SerializeField] private GameObject _regular = default;
        [SerializeField] private Image _icon = default;
        [SerializeField] private GameObject _stack = default;
        [SerializeField] private TextMeshProUGUI _stackAmount = default;

        [Header("Empty panel")]
        [SerializeField] private GameObject _empty = default;

        [Header("Lock panel")]
        [SerializeField] private GameObject _lock = default;

        #endregion

        #region Private properties

        private LevelManager _levelManager = default;

        #endregion

        #region Public methods

        public void Init(LevelManager levelManager)
        {
            _levelManager = levelManager;
        }

        public void SetupLocked()
        {
            _regular.Toggle(false);
            _empty.Toggle(false);
            _lock.Toggle(true);
        }

        public void SetupEmpty()
        {
            _regular.Toggle(false);
            _empty.Toggle(true);
            _lock.Toggle(false);
        }

        public void SetupRegular(PlayerInventoryItemData item)
        {
            _empty.Toggle(false);
            _lock.Toggle(false);

            _stack.Toggle(item.amount > 0);
            _stackAmount.text = $"{item.amount}";

            Sprite icon = null;

            if (_levelManager.Catalog.TryGetItem(item.id, out var itemData))
            {
                icon = itemData.icon;
            }

            _icon.sprite = icon;

            _regular.Toggle(true);
        }

        #endregion
    }
}