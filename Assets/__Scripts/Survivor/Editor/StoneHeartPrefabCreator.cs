using UnityEditor;
using UnityEngine;
using Unity.Netcode;
using SurvivorMode;

namespace SurvivorMode.Editor
{
    public static class StoneHeartPrefabCreator
    {
        [MenuItem("Tools/Survivor/Create Stone Heart Prefab")]
        public static void CreateStoneHeart()
        {
            // Ensure Resources/Prefabs exists
            string folder = "Assets/Resources/Prefabs";
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

            // Build GO
            var go = new GameObject("StoneHeart");
            go.layer = 12; // Heart layer used by damage filters

            var col = go.AddComponent<CapsuleCollider>();
            col.isTrigger = false;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            go.AddComponent<StoneHeartTarget>();
            go.AddComponent<NetworkObject>();
            go.AddComponent<PlayerReference>();
            var stats = go.AddComponent<PlayerStatistics>();
            stats.playerStatData.maxHealth = 500f;
            stats.playerStatData.health = 500f;

            // Save prefab
            string path = folder + "/StoneHeart.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            AssetDatabase.SaveAssets();
            Debug.Log("Stone Heart prefab created at: " + path);
            Selection.activeObject = prefab;
        }
    }
}

