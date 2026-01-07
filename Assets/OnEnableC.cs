using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnEnableC : MonoBehaviour
{
    public UnityEvent OnEnableEvent;

    public UnityEvent OnStartEvent;

    public UnityEvent OnDisableEvent;
    public UnityEvent OnDestroyEvent;

    public void OnEnable() {
        OnEnableEvent.Invoke();
    }

    public void OnDisable()
    {
        OnDisableEvent.Invoke();
    }

    public void OnDestroy() {
        OnDestroyEvent.Invoke();
    }

    public void Start()
    {
        OnStartEvent.Invoke();
    }
}
