
using UnityEngine;
using UnityEngine.UI;

public class UI_ButtonPlayerAction_Dash : MonoBehaviour
{
    [SerializeField] private Button _btn = default;

    private void Awake()
    {
        _btn.onClick.AddListener(ApplyAction);
    }

    private void OnDestroy()
    {
        _btn.onClick.RemoveAllListeners();
    }

    private void ApplyAction()
    {
        PlayerActionEvents.OnStartDash?.Invoke();
    }
}
