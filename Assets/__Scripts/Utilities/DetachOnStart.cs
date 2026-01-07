using UnityEngine;

/// <summary>
/// Simple helper to drop this transform from its parent when Start runs.
/// </summary>
public class DetachOnStart : MonoBehaviour
{
    [SerializeField] private bool worldPositionStays = true;

    private void Start()
    {
        transform.SetParent(null, worldPositionStays);
    }
}
