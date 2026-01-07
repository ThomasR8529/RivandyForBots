using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ScrollRect))]
public class GlobalWheelToScrollRect : MonoBehaviour
{
    [SerializeField] float step = 0.05f;   // quantité de scroll par cran
    [SerializeField] bool invert = false;  // inverser le sens

    private ScrollRect sr;

    void Awake() => sr = GetComponent<ScrollRect>();

    void Update()
    {
        if (sr == null || !sr.enabled) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        float y = mouse.scroll.ReadValue().y; // la molette est toujours sur Y
        if (Mathf.Approximately(y, 0f)) return;

        float dir = (invert ? -1f : 1f) * Mathf.Sign(y);
        float next = sr.horizontalNormalizedPosition - dir * step;
        sr.horizontalNormalizedPosition = Mathf.Clamp01(next);
    }
}