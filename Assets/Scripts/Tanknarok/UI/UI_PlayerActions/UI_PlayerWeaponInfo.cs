
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

        #endregion

        #region Private properties

        private bool _isReloading = false;
        private NetworkRunner _runner = default;
        private TickTimer _cooldown = default;
        private float _delayTime = default;

        #endregion

        #region Public methods

        public void Init()
        {
            _txtReloading.enabled = false;
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
            _cooldown = cooldown;
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
                    break;

                case Items.ItemWeaponType.MELEE:
                    SetupMelee();
                    break;
            }
        }

        #endregion

        #region Private methods

        private void Update()
        {
            if (!_isReloading) return;

            var remaining = _cooldown.RemainingTime(_runner);

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

        #endregion
    }
}