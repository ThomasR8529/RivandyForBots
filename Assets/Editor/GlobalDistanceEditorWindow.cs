using UnityEngine;
using UnityEditor;

public class GlobalDistanceEditorWindow : EditorWindow
{
    private float globalDistance;
    private string shaderName = "ZoneBasedWorld3"; // Replace with your shader name

    [MenuItem("Tools/Global Distance Editor")]
    public static void ShowWindow()
    {
        GetWindow<GlobalDistanceEditorWindow>("Global Distance Editor");
    }

    private void OnEnable()
    {
        // Load the saved value from EditorPrefs
        globalDistance = EditorPrefs.GetFloat("_GlobalDistance", 0f);
    }

    private void OnGUI()
    {
        GUILayout.Label("Global Distance Settings", EditorStyles.boldLabel);

        globalDistance = EditorGUILayout.FloatField("Global Distance", globalDistance);

        if (GUILayout.Button("Set Global Distance"))
        {
            // Save the value to EditorPrefs
            EditorPrefs.SetFloat("_GlobalDistance", globalDistance);

            // Set the global shader property
            Shader.SetGlobalFloat("_GlobalDistance", globalDistance);

            // Force the scene view to repaint so changes are visible immediately
            SceneView.RepaintAll();

            // Force materials to update
            UpdateMaterialsUsingShader(shaderName);
        }
    }

    private void UpdateMaterialsUsingShader(string shaderName)
    {
        // Find all materials in the project
        string[] guids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null && material.shader.name == shaderName)
            {
                EditorUtility.SetDirty(material);
            }
        }

        // Refresh the editor to make sure all changes are applied
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}