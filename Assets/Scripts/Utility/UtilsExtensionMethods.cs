
using UnityEngine;

public static class UtilsExtensionMethods
{
    public static void Toggle(this GameObject go, bool value)
    {
        if (go.activeSelf && value) return;

        if (!go.activeSelf && !value) return;

        go.SetActive(value);
    }
}
