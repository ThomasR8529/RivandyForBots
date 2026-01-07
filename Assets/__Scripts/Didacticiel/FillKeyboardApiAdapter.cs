using UnityEngine;

public class FillKeyboardApiAdapter : MonoBehaviour, IFillKeyboardApi
{
    private void Awake()
    {
        FillKeyboardApi.Instance = this;
    }

    public void UpdateKeys()
    {
        FillKeyboard.instance?.UpdateKeys();
    }
}
