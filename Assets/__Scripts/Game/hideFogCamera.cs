using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class hideFogCamera : MonoBehaviour
{
    bool fog;

    private void Start()
    {
        fog = RenderSettings.fog;
        RenderPipelineManager.beginCameraRendering += RenderPipelineManager_beginCameraRendering;
        RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
    }

    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering -= RenderPipelineManager_beginCameraRendering;
        RenderPipelineManager.endCameraRendering -= RenderPipelineManager_endCameraRendering;
    }

    private void RenderPipelineManager_beginCameraRendering(ScriptableRenderContext arg1, Camera arg2)
    {
        if (arg2.gameObject == gameObject)
        {
            RenderSettings.fog = false;
        }
    }

    private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext arg1, Camera arg2)
    {
        if (arg2.gameObject == gameObject)
        {
            RenderSettings.fog = fog;
        }
    }

}
