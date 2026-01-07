using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using TMPro;

namespace SurvivorMode.Editor
{
    public static class SurvivorEventUIPrefabCreator
    {
        private const string RootFolder = "Assets/SurvivorEventUI";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string TimelinesFolder = RootFolder + "/Timelines";

        [MenuItem("Tools/Survivor/Create Event UI Prefab")]
        public static void CreatePrefab()
        {
            EnsureFolders();

            // Create timelines
            var startTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var successTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var failureTimeline = ScriptableObject.CreateInstance<TimelineAsset>();

            AssetDatabase.CreateAsset(startTimeline, TimelinesFolder + "/EV_Start.playable" );
            AssetDatabase.CreateAsset(successTimeline, TimelinesFolder + "/EV_Success.playable" );
            AssetDatabase.CreateAsset(failureTimeline, TimelinesFolder + "/EV_Failure.playable" );

            // Root GO
            var root = new GameObject("SurvivorEventUI");

            // Canvas
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            root.AddComponent<GraphicRaycaster>();

            // Panel
            var panelGO = new GameObject("Panel", typeof(RectTransform));
            panelGO.transform.SetParent(root.transform, false);
            var panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.25f, 0.2f);
            panelRT.anchorMax = new Vector2(0.75f, 0.8f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;
            var panelImage = panelGO.AddComponent<Image>();
            var col = new Color(0f, 0f, 0f, 0.6f);
            panelImage.color = col;

            // Title TMP + LocalizedText
            var titleGO = new GameObject("Title", typeof(RectTransform));
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.05f, 0.7f);
            titleRT.anchorMax = new Vector2(0.95f, 0.95f);
            titleRT.offsetMin = Vector2.zero;
            titleRT.offsetMax = Vector2.zero;
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "Event Title";
            titleTMP.fontSize = 56f;
            titleTMP.alignment = TextAlignmentOptions.Center;
            var titleLoc = titleGO.AddComponent<LocalizedText>();

            // Description TMP + LocalizedText
            var descGO = new GameObject("Description", typeof(RectTransform));
            descGO.transform.SetParent(panelGO.transform, false);
            var descRT = descGO.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.05f, 0.25f);
            descRT.anchorMax = new Vector2(0.6f, 0.7f);
            descRT.offsetMin = Vector2.zero;
            descRT.offsetMax = Vector2.zero;
            var descTMP = descGO.AddComponent<TextMeshProUGUI>();
            descTMP.text = "Event Description";
            descTMP.fontSize = 32f;
            descTMP.alignment = TextAlignmentOptions.TopLeft;
            descTMP.enableWordWrapping = true;
            var descLoc = descGO.AddComponent<LocalizedText>();

            // Image
            var imageGO = new GameObject("Image", typeof(RectTransform));
            imageGO.transform.SetParent(panelGO.transform, false);
            var imageRT = imageGO.GetComponent<RectTransform>();
            imageRT.anchorMin = new Vector2(0.62f, 0.25f);
            imageRT.anchorMax = new Vector2(0.95f, 0.7f);
            imageRT.offsetMin = Vector2.zero;
            imageRT.offsetMax = Vector2.zero;
            var displayImage = imageGO.AddComponent<Image>();
            displayImage.color = Color.white;

            // PlayableDirector + UI bridge
            var director = root.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.playableAsset = startTimeline;

            var ui = root.AddComponent<SurvivorEventTimelineUI>();
            // Bind fields via SerializedObject for safety
            var so = new SerializedObject(ui);
            so.FindProperty("titleLocalized").objectReferenceValue = titleLoc;
            so.FindProperty("descriptionLocalized").objectReferenceValue = descLoc;
            so.FindProperty("titleTMPFallback").objectReferenceValue = titleTMP;
            so.FindProperty("descriptionTMPFallback").objectReferenceValue = descTMP;
            so.FindProperty("eventImage").objectReferenceValue = displayImage;
            so.FindProperty("startDirector").objectReferenceValue = director;
            so.FindProperty("startTimeline").objectReferenceValue = startTimeline;
            so.FindProperty("successTimeline").objectReferenceValue = successTimeline;
            so.FindProperty("failureTimeline").objectReferenceValue = failureTimeline;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            var prefabPath = PrefabsFolder + "/SurvivorEventUI.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
            GameObject.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!success)
            {
                Debug.LogWarning("[Survivor] Failed to create prefab.");
            }
            else
            {
                Debug.Log("[Survivor] Prefab created at: " + prefabPath);
            }
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.CreateFolder("Assets", "SurvivorEventUI");
            }
            if (!AssetDatabase.IsValidFolder(PrefabsFolder))
            {
                AssetDatabase.CreateFolder(RootFolder, "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(TimelinesFolder))
            {
                AssetDatabase.CreateFolder(RootFolder, "Timelines");
            }
        }
    }
}

