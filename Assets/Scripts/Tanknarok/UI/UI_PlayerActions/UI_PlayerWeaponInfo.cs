
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerWeaponInfo : MonoBehaviour
{
    [SerializeField] private Image _progressBar = default;
    [SerializeField] private TextMeshProUGUI _txtAmmo = default;
    [SerializeField] private TextMeshProUGUI _txtReloading = default;

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

    public void StartReloading()
    {
        _progressBar.fillAmount = 0;

        _txtAmmo.enabled = false;
        _txtReloading.enabled = true;
    }

    public void StopReloading(int ammo, int magazine)
    {
        _txtAmmo.enabled = true;
        _txtReloading.enabled = false;

        Refresh(ammo, magazine);
    }
}
