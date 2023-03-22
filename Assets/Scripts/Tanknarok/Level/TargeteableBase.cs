
using UnityEngine;

public class TargeteableBase : MonoBehaviour
{
    [SerializeField] private int _id = default;
    [SerializeField] private GameObject _indicator = default;

    public int Id => _id;

    public void ShowIndicator()
    {
        _indicator.Toggle(true);

        //Debug.Log($"Target <color=yellow>{_id}</color> was <color=green>detected</color>");
    }

    public void HideIndicator()
    {
        _indicator.Toggle(false);

        //Debug.Log($"Target <color=yellow>{_id}</color> was <color=orange>undetected</color>");
    }

    private void Start()
    {
        HideIndicator();
    }
}
