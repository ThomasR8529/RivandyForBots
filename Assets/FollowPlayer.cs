using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField] private Transform playerObject;
    [SerializeField] bool detachFromParent = false;
    [SerializeField] private Vector3 offset;


    [SerializeField] private bool useLateUpdate = false;
    [SerializeField] private bool useFixedUpdate = false;
    [SerializeField] private bool useUpdate = false;

    [SerializeField] private bool changeRotationFixed;
    [SerializeField] private bool changeRotationLate;
    [SerializeField] private bool changeRotationUpdate;

    [SerializeField] private string levelNameToMultiplyScale;
    [SerializeField] private float scaleMultiplier = 1;

    private void Start()
    {
        if (detachFromParent)
        {
            transform.parent = null;
        }
        if (SceneManager.GetActiveScene().name == levelNameToMultiplyScale)
        {
            transform.localScale = new Vector3(transform.localScale.x * scaleMultiplier, transform.localScale.y * scaleMultiplier, transform.localScale.z * scaleMultiplier);
        }
    }
    private Vector3 GetTargetPosition()
    {
        return playerObject != null ? playerObject.TransformPoint(offset) : transform.position;
    }

    void Update()
    {
        if (playerObject != null && useUpdate)
            transform.position = GetTargetPosition();
        if (changeRotationUpdate)
            transform.rotation = playerObject.rotation;
    }

    private void LateUpdate()
    {
        if (playerObject != null && useLateUpdate)
        {
            transform.position = GetTargetPosition();
            if (changeRotationLate)
                transform.rotation = playerObject.rotation;
        }

    }

    private void FixedUpdate()
    {
        if (playerObject != null && useFixedUpdate)
        {
            transform.position = GetTargetPosition();
            if (changeRotationFixed)
                transform.rotation = playerObject.rotation;
        }
    }

}
