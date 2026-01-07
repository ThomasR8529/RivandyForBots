using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using System;
using UnityEngine.Playables;

public class DamagePrefab : MonoBehaviour
{

    public TextMeshPro text;
    public PlayableDirector skeletonIcon;
    public RectTransform rectTransform;
    private Transform followTarget;
    private Vector3 worldOffset = new Vector3(0, 3f, 0);

    // Random offset settings so multiple prefabs don't overlap
    [SerializeField] private float verticalOffset = 3f;
    [SerializeField] private float randomRadius = 0.8f;
    [SerializeField] private float randomVerticalJitter = 0.5f;
    [SerializeField] private bool useRandomOffset = true;

    public Camera mainCamera;
    void Start()
    {
        mainCamera = Camera.main;
    }

    public void SetText(float value)
    {
        Destroy(gameObject, value != 666f ? 2.5f : 4f);
        if (value != 666f)
        {
            text.gameObject.SetActive(true);
            skeletonIcon.gameObject.SetActive(false);
            text.SetText(value >= 0 ? "+" + Math.Round(value, 1).ToString() : Math.Round(value, 1).ToString());
        }
        else
        {
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, 100f);
            text.gameObject.SetActive(false);
            skeletonIcon.gameObject.SetActive(true);
            skeletonIcon.Play();
        }
    }
    public void SetFollow(Transform target)
    {
        followTarget = target;
        if (useRandomOffset)
        {
            // Random point in a disk around the target to avoid stacking
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            float radius = randomRadius * Mathf.Sqrt(UnityEngine.Random.value);
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            float y = verticalOffset + UnityEngine.Random.Range(-randomVerticalJitter, randomVerticalJitter);
            worldOffset = new Vector3(x, y, z);
        }
        else
        {
            worldOffset = new Vector3(0f, verticalOffset, 0f);
        }
    }

    void LateUpdate()
    {
        if (mainCamera != null)
            transform.LookAt(transform.position + mainCamera.transform.forward);

        if (followTarget != null)
            transform.position = followTarget.position + worldOffset;
    }
}
