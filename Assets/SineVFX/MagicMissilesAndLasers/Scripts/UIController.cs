using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class UIController : MonoBehaviour {

    public Light directionalLight;
    public ReflectionProbe reflectionProbe;
    public Material daySkyboxMaterial;
    public Material nightSkyboxMaterial;
    public Transform prefabHolder;
    public Text text;

    private Transform[] prefabs;
    private List<Transform> lt;
    private int activeNumber = 0;

    void Start()
    {
#if ENABLE_INPUT_SYSTEM
        // Check for Standalone Input Module and replace it with Input System UI Input Module
        var standaloneInputModule = FindFirstObjectByType<UnityEngine.EventSystems.StandaloneInputModule>();
        if (standaloneInputModule != null)
        {
            Debug.Log("Replacing Standalone Input Module with Input System UI Input Module.");
            var eventSystemGameObject = standaloneInputModule.gameObject;
            Destroy(standaloneInputModule);
            var inputSystemUIModule = eventSystemGameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
#endif

        if (prefabHolder == null)
        {
            Debug.LogError("PrefabHolder is not assigned.");
            return;
        }

        lt = new List<Transform>();
        prefabs = prefabHolder.GetComponentsInChildren<Transform>(true);

        foreach (Transform tran in prefabs)
        {
            if (tran.parent == prefabHolder)
            {
                lt.Add(tran);
            }
        }

        prefabs = lt.ToArray();
        EnableActive();
    }

    // Turn On active VFX Prefab
    public void EnableActive()
    {
        for (int i = 0; i < prefabs.Length; i++)
        {
            if (i == activeNumber)
            {
                prefabs[i].gameObject.SetActive(true);
                //prefabs[i].gameObject.active = true;

                text.text = prefabs[i].name;
            }
            else
            {
                prefabs[i].gameObject.SetActive(false);
                //prefabs[i].gameObject.active = false;
            }
        }
    }

    // Change active VFX
    public void ChangeEffect(bool bo)
    {
        if (bo == true)
        {
            activeNumber++;
            if (activeNumber == prefabs.Length)
            {
                activeNumber = 0;
            }
        }
        else
        {
            activeNumber--;
            if (activeNumber == -1)
            {
                activeNumber = prefabs.Length - 1;
            }
        }

        EnableActive();
    }

    public void SetDay()
    {
        if (directionalLight != null)
        {
            directionalLight.enabled = true;
        }
        if (reflectionProbe != null)
        {
            reflectionProbe.RenderProbe();
        }
        if (daySkyboxMaterial != null)
        {
            RenderSettings.skybox = daySkyboxMaterial;
        }
    }

    public void SetNight()
    {
        if (directionalLight != null)
        {
            directionalLight.enabled = false;
        }
        if (reflectionProbe != null)
        {
            reflectionProbe.RenderProbe();
        }
        if (nightSkyboxMaterial != null)
        {
            RenderSettings.skybox = nightSkyboxMaterial;
        }
    }

    // TEMP
    private void Update()
    {
        #if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.qKey.wasPressedThisFrame)
            {
                SetDay();
            }
            if (keyboard.eKey.wasPressedThisFrame)
            {
                SetNight();
            }
            if (keyboard.aKey.wasPressedThisFrame)
            {
                ChangeEffect(true);
            }
            if (keyboard.dKey.wasPressedThisFrame)
            {
                ChangeEffect(false);
            }
        }
        #else
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SetDay();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            SetNight();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            ChangeEffect(true);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            ChangeEffect(false);
        }
        #endif
    }
}
