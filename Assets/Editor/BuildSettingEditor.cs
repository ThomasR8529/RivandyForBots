using UnityEngine;
using UnityEditor;

public class BuildSettingsEditor : EditorWindow {
    [MenuItem("Build Settings/Configure Build Options")]
    public static void ShowWindow() {
        EditorWindow.GetWindow(typeof(BuildSettingsEditor), true, "Build Settings");
    }

    void OnGUI() {
        GUILayout.Label("Build Configuration", EditorStyles.boldLabel);

        if (GUILayout.Button("Set to Linux Dedicated Server")) {
            SetToLinuxDedicatedServer();
        }

        if (GUILayout.Button("Set to Windows (Non-Server)")) {
            SetToWindowsNonServer();
        }
    }

    static void SetToLinuxDedicatedServer() {
        // Set to Linux build target
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);

        // Enable dedicated server build
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;

        Debug.Log("Build settings changed: Now targeting Linux with Dedicated Server mode enabled.");
    }

    static void SetToWindowsNonServer() {
        // Set to Windows build target
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        // Disable dedicated server build
        EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;

        Debug.Log("Build settings changed: Now targeting Windows with Non-Server mode.");
    }
}
