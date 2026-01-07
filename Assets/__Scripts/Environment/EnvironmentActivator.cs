using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentActivator : MonoBehaviour
{

    public bool isFogActive;
    public Color32 colorFog;
    public float densityFog;
    public bool isCameraSkyColor;
    public Color32 skyColor;

    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.layer == 9) {
            RenderSettings.fog = isFogActive;
            RenderSettings.fogColor = colorFog;
            RenderSettings.fogDensity = densityFog;
            Camera.main.clearFlags = isCameraSkyColor ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
            if(isCameraSkyColor) Camera.main.backgroundColor = skyColor;
        }
    }
}
