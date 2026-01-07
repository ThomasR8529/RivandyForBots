using UnityEngine;
using Cinemachine;

public class CameraZoom : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    public float zoomSpeed = 15f;
    public float minFOV = 30f;
    public float maxFOV = 65;

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            float fov = virtualCamera.m_Lens.FieldOfView;
            fov -= scroll * zoomSpeed;
            fov = Mathf.Clamp(fov, minFOV, maxFOV);
            virtualCamera.m_Lens.FieldOfView = fov;
        }
    }
}