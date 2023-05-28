
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionExamples.Tanknarok.UI
{
    public class UI_PlayerWeaponInfo : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Image _icon = default;
        [SerializeField] private Image _progressBar = default;
        [SerializeField] private Image _reloadingBar = default;
        [SerializeField] private TextMeshProUGUI _txtAmmo = default;
        [SerializeField] private TextMeshProUGUI _txtReloading = default;
        [SerializeField] private Sprite _iconAssault = default;
        [SerializeField] private Sprite _iconMelee = default;
        [SerializeField] private Image _cooldownAvailable = default;

        [Header("Reloading button")]
        [SerializeField] private Button _btnReloading = default;

        #endregion

        #region Private properties

        private bool _isReloading = false;
        private NetworkRunner _runner = default;
        private TickTimer _cooldownReloadingTimer = default;
        private float _delayTime = default;
        private TickTimer _cooldownAvailableTimer = default;
        private float _availableDelayTime = default;
        private bool _isAvailable = true;

        #endregion

        #region Public methods

        public void Init()
        {
            _txtReloading.enabled = false;

            _cooldownAvailable.enabled = false;
        }

        public void Refresh(int ammo, int magazine)
        {
            var progress = (float)ammo / magazine;

            _progressBar.fillAmount = progress;

            _txtAmmo.text = $"<color=#F8AC08>{ammo}</color>/<size=20>{magazine}</size>";
        }

        public void StartReloading(float delay, TickTimer cooldown, NetworkRunner runner)
        {
            _runner = runner;
            _cooldownReloadingTimer = cooldown;
            _delayTime = delay;

            _isReloading = true;

            _progressBar.fillAmount = 0;

            _txtAmmo.enabled = false;
            _txtReloading.enabled = true;

            _reloadingBar.fillAmount = 0;
            _reloadingBar.enabled = true;
        }

        public void StopReloading(int ammo, int magazine)
        {
            _txtAmmo.enabled = true;
            _txtReloading.enabled = false;

            _reloadingBar.enabled = false;

            Refresh(ammo, magazine);
        }

        public void RefreshWeaponType(Items.ItemWeaponType weaponType)
        {
            switch (weaponType)
            {
                case Items.ItemWeaponType.ASSAULT:
                    SetupAssault();
                    ShowReloadingButton();
                    break;

                case Items.ItemWeaponType.MELEE:
                    SetupMelee();
                    HideReloadingButton();
                    break;
            }
        }

        public void StartUsing(float delay, TickTimer cooldown, NetworkRunner runner)
        {
            _cooldownAvailableTimer = cooldown;
            _availableDelayTime = delay;
            _runner = runner;

            _cooldownAvailable.fillAmount = 1;
            _cooldownAvailable.enabled = true;

            _isAvailable = false;
        }

        #endregion

        #region Private methods

        private void Update()
        {
            UpdateAvailable();

            if (!_isReloading) return;

            var remaining = _cooldownReloadingTimer.RemainingTime(_runner);

            var progress = (remaining / _delayTime);

            var fillAmount = 1 - progress;

            _reloadingBar.fillAmount = fillAmount ?? 0;
        }

        private void SetupAssault()
        {
            _icon.sprite = _iconAssault;

            _txtAmmo.enabled = true;

            _progressBar.enabled = true;
        }

        private void SetupMelee()
        {
            _icon.sprite = _iconMelee;

            _txtAmmo.enabled = false;

            _progressBar.enabled = false;
        }

        private void UpdateAvailable()
        {
            if (_isAvailable) return;

            var remaining = _cooldownAvailableTimer.RemainingTime(_runner);

            var progress = (remaining / _availableDelayTime);

            _cooldownAvailable.fillAmount = progress ?? 0;

            if (progress > 0) return;

            _isAvailable = true;

            _cooldownAvailable.enabled = false;
        }

        private void ShowReloadingButton()
        {
            _btnReloading.gameObject.Toggle(true);
        }

        private void HideReloadingButton()
        {
            _btnReloading.gameObject.Toggle(false);
        }

        #endregion
    }
}