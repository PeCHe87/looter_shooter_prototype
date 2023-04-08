
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class UI_PlayerDashInfo : MonoBehaviour
{
    [SerializeField] private Image _progressBarBackground = default;
    [SerializeField] private Image _progressBar = default;

    private bool _isReloading = false;
    private NetworkRunner _runner = default;
    private TickTimer _cooldown = default;
    private float _delayTime = default;

    public void StartReloading(float delay, TickTimer cooldown, NetworkRunner runner)
    {
        _runner = runner;
        _cooldown = cooldown;
        _delayTime = delay;

        _progressBar.fillAmount = 0;
        _progressBarBackground.enabled = true;
        _progressBar.enabled = true;

        _isReloading = true;
    }

    private void StopReloading()
    {
        _progressBar.fillAmount = 1;
        _progressBarBackground.enabled = false;
        _progressBar.enabled = false;

        _isReloading = false;
    }

    private void Update()
    {
        if (!_isReloading) return;

        var remaining = _cooldown.RemainingTime(_runner);

        var progress = (remaining / _delayTime);

        var fillAmount = 1 - progress;

        _progressBar.fillAmount = fillAmount ?? 0;

        if (progress > 0) return;

        StopReloading();
    }
}
