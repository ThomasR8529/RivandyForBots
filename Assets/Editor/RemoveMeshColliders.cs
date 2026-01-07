using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Unity.VisualScripting;

public class RemoveMeshColliders : EditorWindow {
    private string folderPath = "Assets/";
    private string suffix = "LOD1";
    private List<string> removedColliders;

    [MenuItem("Tools/Remove Mesh Colliders")]
    public static void ShowWindow() {
        GetWindow<RemoveMeshColliders>("Remove Mesh Colliders");
    }

    private void OnGUI() {
        GUILayout.Label("Folder Path", EditorStyles.boldLabel);
        folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
        suffix = EditorGUILayout.TextField("Suffix", suffix);

        if (GUILayout.Button("Remove Mesh Colliders")) {
            removedColliders = new List<string>();
            ProcessPrefabs(RemoveAndRevertMeshColliders);
        }

        if (GUILayout.Button("Set Mesh Colliders Convex")) {
            ProcessPrefabs(SetMeshCollidersConvex);
        }
    }

    private void ProcessPrefabs(System.Action<GameObject> processPrefabAction) {
        string[] prefabFiles = Directory.GetFiles(folderPath, "*.prefab", SearchOption.AllDirectories);

        foreach (string prefabFile in prefabFiles) {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFile);
            if (prefab != null) {
                processPrefabAction(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void RemoveAndRevertMeshColliders(GameObject prefab) {
        MeshCollider[] meshColliders = prefab.GetComponentsInChildren<MeshCollider>(true);

        foreach (MeshCollider meshCollider in meshColliders) {
            if (!meshCollider.gameObject.name.EndsWith(suffix)) {
                DestroyImmediate(meshCollider, true);
            }
        }

        CheckAndAddMeshColliders(prefab.transform);
    }

    private void CheckAndAddMeshColliders(Transform parent) {
        foreach (Transform child in parent) {
            if (child.name.EndsWith(suffix) && parent.GetComponent<MeshCollider>() == null) {
                PrefabUtility.RevertRemovedComponent(child.gameObject, child.AddComponent<MeshCollider>(), InteractionMode.AutomatedAction);
                break;
            }

            CheckAndAddMeshColliders(child);
        }
    }

    private void SetMeshCollidersConvex(GameObject prefab) {
        MeshCollider[] meshColliders = prefab.GetComponentsInChildren<MeshCollider>(true);

        foreach (MeshCollider meshCollider in meshColliders) {
            if (meshCollider.enabled) {
                meshCollider.convex = true;
            }
        }
    }
}