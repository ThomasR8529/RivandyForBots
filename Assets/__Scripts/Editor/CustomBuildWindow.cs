using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class CustomBuildWindow : EditorWindow
{
    private enum BuildPlatform
    {
        Windows,
        DedicatedServer
    }

    private class SceneEntry
    {
        public string path;
        public bool selected;
    }

    private readonly List<SceneEntry> _scenes = new List<SceneEntry>();
    private Vector2 _scroll;
    private BuildPlatform _platform = BuildPlatform.Windows;
    private string _outputPath = string.Empty;
    private string _productName = "Build";

    [MenuItem("Tools/Build/Custom Build...")]
    public static void Open()
    {
        var win = GetWindow<CustomBuildWindow>(true, "Custom Build", true);
        win.minSize = new Vector2(520, 480);
        win.PopulateScenes();
        win._productName = PlayerSettings.productName;
    }

    private void PopulateScenes()
    {
        _scenes.Clear();

        // Scenes included by default: those enabled in Build Settings
        var included = new HashSet<string>(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path));

        // All scenes in project
        foreach (var guid in AssetDatabase.FindAssets("t:Scene"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            _scenes.Add(new SceneEntry
            {
                path = path,
                selected = included.Contains(path)
            });
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scenes to include", EditorStyles.boldLabel);

        if (_scenes.Count == 0)
        {
            if (GUILayout.Button("Refresh Scenes")) PopulateScenes();
        }
        else
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.Height(260)))
            {
                _scroll = scroll.scrollPosition;
                foreach (var s in _scenes)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        s.selected = EditorGUILayout.ToggleLeft(s.path, s.selected);
                        if (GUILayout.Button("Ping", GUILayout.Width(60)))
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<Object>(s.path);
                            EditorGUIUtility.PingObject(obj);
                        }
                    }
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
        _platform = (BuildPlatform)EditorGUILayout.EnumPopup("Platform", _platform);
        _productName = EditorGUILayout.TextField("Product Name", _productName);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Output Path", GUILayout.Width(80));
            EditorGUILayout.SelectableLabel(string.IsNullOrEmpty(_outputPath) ? "<Choose...>" : _outputPath, GUILayout.Height(16));
            if (GUILayout.Button("Choose...", GUILayout.Width(90)))
            {
                string defaultName = _platform == BuildPlatform.DedicatedServer ? _productName + "_Server" : _productName;
                string ext = Application.platform == RuntimePlatform.WindowsEditor ? "exe" : "";
                string defaultPath = System.IO.Path.Combine("Builds", _platform == BuildPlatform.DedicatedServer ? "Server" : "Windows", defaultName + (string.IsNullOrEmpty(ext) ? string.Empty : "." + ext));
                var file = EditorUtility.SaveFilePanel("Select output executable", System.IO.Path.GetDirectoryName(defaultPath), System.IO.Path.GetFileName(defaultPath), ext);
                if (!string.IsNullOrEmpty(file))
                {
                    _outputPath = file;
                }
            }
        }

        GUILayout.FlexibleSpace();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh Scenes")) PopulateScenes();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_outputPath)))
            {
                if (GUILayout.Button("Build", GUILayout.Height(28), GUILayout.Width(140)))
                {
                    Build();
                }
            }
        }
        EditorGUILayout.Space();
    }

    private void Build()
    {
        var scenePaths = _scenes.Where(s => s.selected).Select(s => s.path).ToArray();
        if (scenePaths.Length == 0)
        {
            EditorUtility.DisplayDialog("Build", "Select at least one scene.", "OK");
            return;
        }
        if (string.IsNullOrEmpty(_outputPath))
        {
            EditorUtility.DisplayDialog("Build", "Choose an output path.", "OK");
            return;
        }

        // Determine build target
        var target = BuildTarget.StandaloneWindows64;
        var group = BuildTargetGroup.Standalone;

        // Cache current subtarget to restore later
        var prevSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;

        // Configure dedicated server vs windows client
        var options = BuildOptions.None;

        if (_platform == BuildPlatform.DedicatedServer)
        {
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
            options |= BuildOptions.EnableHeadlessMode;
        }
        else
        {
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
        }

        // Build options
        var buildOptions = new BuildPlayerOptions
        {
            scenes = scenePaths,
            locationPathName = _outputPath,
            target = target,
            targetGroup = group,
            options = options
        };

        try
        {
            var report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog("Build", "Build succeeded:\n" + report.summary.outputPath, "OK");
                EditorUtility.RevealInFinder(report.summary.outputPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Build", "Build failed: " + report.summary.result, "OK");
            }
        }
        finally
        {
            // Restore
            EditorUserBuildSettings.standaloneBuildSubtarget = prevSubtarget;
        }
    }
}

