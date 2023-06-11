
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionExamples.Tanknarok.UI
{
    public class UI_PlayerInfoPanel : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private TextMeshProUGUI _txtName = default;

        [Header("Health info")]
        [SerializeField] private Image _healthFill = default;

        [Header("Charger info")]
        [SerializeField] private Image _chargerFill = default;
        [SerializeField] private Image _chargerIcon = default;
        [SerializeField] private Sprite _chargerEmpty = default;
        [SerializeField] private Sprite _chargerQuarter = default;
        [SerializeField] private Sprite _chargerHalf = default;
        [SerializeField] private Sprite _chargerFull = default;

        [Header("Weapon info")]
        [SerializeField] private Image _weaponIcon = default;
        [SerializeField] private UI_PlayerInfoWeaponsPanel _weaponInfoPanel = default;

        #endregion

        #region Unity events

        private void Start()
        {
            RefreshCollectableProgress(0);
        }

        #endregion

        #region Public methods

        public void SetPlayer(Player player)
		{
            _weaponInfoPanel.SetPlayer(player);
		}

        public void SetDisplayName(string displayName)
        {
            _txtName.text = displayName.ToUpperInvariant();
        }

        public void UpdateHealth(int amount, int max, string origin)
        {
            var progress = Mathf.Clamp((float)amount / (float)max, 0, 1);

            RefreshHealthProgress(progress);
        }

        public void UpdateCollectables(int amount, int max, string origin)
        {
            var progress = Mathf.Clamp((float)amount / (float)max, 0, 1);

            RefreshCollectableProgress(progress);
        }

        public void RefreshWeapon(Items.ItemCatalogData itemCatalog)
        {
            _weaponInfoPanel.EquipWeapon(itemCatalog);
        }

        #endregion

        #region Private methods

        private void RefreshHealthProgress(float progress)
        {
            _healthFill.fillAmount = progress;
        }

        private void RefreshCollectableProgress(float progress)
        {
            _chargerFill.fillAmount = progress;

            var sprite = _chargerEmpty;

            if (progress == 1)
            {
                sprite = _chargerFull;
            }
            else if (progress >= 0.5f)
            {
                sprite = _chargerHalf;
            }
            else if (progress >= 0.25f)
            {
                sprite = _chargerQuarter;
            }

            _chargerIcon.sprite = sprite;
        }

        #endregion
    }
}