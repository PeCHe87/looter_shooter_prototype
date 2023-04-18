
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

        #endregion

        #region Unity events

        private void Start()
        {
            RefreshCollectableProgress(0);
        }

        #endregion

        #region Public methods

        public void SetDisplayName(string displayName)
        {
            _txtName.text = displayName.ToUpperInvariant();
        }

        public void UpdateHealth(int amount, int max)
        {
            var progress = Mathf.Clamp((float)amount / (float)max, 0, 1);

            RefreshHealthProgress(progress);
        }

        public void UpdateCollectables(int amount, int max)
        {
            var progress = Mathf.Clamp((float)amount / (float)max, 0, 1);

            RefreshCollectableProgress(progress);
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
        }

        #endregion
    }
}