using UnityEditor;
using UnityEngine;
using Unity.Netcode;

namespace SurvivorMode.Editor
{
    public static class DangerZonePrefabCreator
    {
        [MenuItem("Tools/Survivor/Create Danger Zone Prefab")]
        public static void CreateDangerZone()
        {
            string folder = "Assets/Resources/Prefabs";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

            var go = new GameObject("DangerZone");
            go.layer = 0;

            var col = go.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.radius = 4f;
            col.height = 2f;

            go.AddComponent<DangerZoneArea>();
            go.AddComponent<DangerZoneController>();
            go.AddComponent<NetworkObject>();

            string path = folder + "/DangerZone.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            Debug.Log("Danger Zone prefab created at: " + path);
            Selection.activeObject = prefab;
        }
    }
}

