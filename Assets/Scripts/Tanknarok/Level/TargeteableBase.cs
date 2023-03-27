
using UnityEngine;

public class TargeteableBase : MonoBehaviour
{
    [SerializeField] private string _id = default;
    [SerializeField] private GameObject _indicator = default;

    private bool _isLocal = false;

    public string Id => _id;

    public void ShowIndicator()
    {
        if (_isLocal) return;

        _indicator.Toggle(true);
    }

    public void HideIndicator()
    {
        _indicator.Toggle(false);
    }

    public void SetId(string id, bool isLocal)
    {
        _id = id;
        _isLocal = isLocal;
    }

    private void Start()
    {
        HideIndicator();
    }
}
