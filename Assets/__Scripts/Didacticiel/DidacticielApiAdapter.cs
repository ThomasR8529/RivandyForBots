using UnityEngine;

public class DidacticielApiAdapter : MonoBehaviour, IDidacticielApi
{
    private void Awake()
    {
        DidacticielApi.Instance = this;
    }

    public void StartIt()
    {
        DidacticielManager.instance?.StartIt();
    }
}
