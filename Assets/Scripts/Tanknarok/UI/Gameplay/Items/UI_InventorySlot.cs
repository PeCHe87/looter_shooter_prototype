using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FusionExamples.Tanknarok.Items;

namespace FusionExamples.Tanknarok.UI
{
    public enum InventorySlotStatus { NONE, REGULAR, EMPTY, LOCKED }

    public class UI_InventorySlot : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private int _index = default;
        [SerializeField] private Button _btn = default;

        [Header("Regular panel")]
        [SerializeField] private GameObject _regular = default;
        [SerializeField] private Image _icon = default;
        [SerializeField] private GameObject _stack = default;
        [SerializeField] private TextMeshProUGUI _stackAmount = default;
        [SerializeField] private Image _selectionBorder = default;

        [Header("Empty panel")]
        [SerializeField] private GameObject _empty = default;

        [Header("Lock panel")]
        [SerializeField] private GameObject _lock = default;

        #endregion

        #region Private properties

        private LevelManager _levelManager = default;
        private InventorySlotStatus _status = InventorySlotStatus.NONE;
        private PlayerInventoryItemData _data = default;
        private readonly PlayerInventoryItemData _emptyData = new PlayerInventoryItemData();

        #endregion

        #region Public properties

        public int Index => _index;

        #endregion

        #region Public methods

        public void Init(LevelManager levelManager, System.Action<int, bool, InventorySlotStatus, PlayerInventoryItemData> callbackSelection)
        {
            _levelManager = levelManager;

            _callbackSelection = callbackSelection;

            _btn.onClick.AddListener(Select);
        }

        public void Teardown()
        {
            _btn.onClick.RemoveAllListeners();
        }

        public void SetupLocked()
        {
            _regular.Toggle(false);
            _empty.Toggle(false);
            _lock.Toggle(true);

            _status = InventorySlotStatus.LOCKED;

            _data = _emptyData;
        }

        public void SetupEmpty()
        {
            _regular.Toggle(false);
            _empty.Toggle(true);
            _lock.Toggle(false);

            _status = InventorySlotStatus.EMPTY;

            _data = _emptyData;
        }

        public void SetupRegular(PlayerInventoryItemData item)
        {
            _empty.Toggle(false);
            _lock.Toggle(false);

            if (item.isStackable)
            {
                _stack.Toggle(item.amount > 0);
                _stackAmount.text = $"{item.amount}";
            }
            else
            {
                _stack.Toggle(false);
            }

            Sprite icon = null;

            if (_levelManager.Catalog.TryGetItem(item.id, out var itemCatalog))
            {
                icon = itemCatalog.data.icon;
            }

            _icon.sprite = icon;

            _regular.Toggle(true);

            _status = InventorySlotStatus.REGULAR;

            _data = item;
        }

        public void Deselect()
        {
            _isSelected = false;

            MarkAsUnselected();
        }

        #endregion

        #region Selection

        private bool _isSelected = false;
        private System.Action<int, bool, InventorySlotStatus, PlayerInventoryItemData> _callbackSelection = default;

        private void Select()
        {
            if (!_isSelected)
            {
                MarkAsSelected();
                _isSelected = true;
            }
            else
            {
                MarkAsUnselected();
                _isSelected = false;
            }

            _callbackSelection?.Invoke(_index, _isSelected, _status, _data);
        }

        private void MarkAsSelected()
        {
            _selectionBorder.enabled = true;
        }

        private void MarkAsUnselected()
        {
            _selectionBorder.enabled = false;
        }

        #endregion
    }
}