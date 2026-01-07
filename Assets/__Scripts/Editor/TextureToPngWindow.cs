using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TextureToPngWindow : EditorWindow
{
    private const string MenuPath = "Tools/Utilities/Texture2D To PNG";

    private readonly List<string> _messages = new List<string>();
    private Vector2 _logScroll;
    private GUIStyle _dropAreaStyle;

    [MenuItem(MenuPath)]
    private static void ShowWindow()
    {
        var window = GetWindow<TextureToPngWindow>();
        window.titleContent = new GUIContent("Texture2PNG");
        window.minSize = new Vector2(320f, 200f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Glissez / deposez des Texture2D du Project ici pour creer des PNG.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        Rect dropArea = GUILayoutUtility.GetRect(0f, 120f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drop Texture2D assets here", GetDropAreaStyle());
        HandleDragAndDrop(dropArea);

        EditorGUILayout.Space();
        DrawLog();
    }

    private GUIStyle GetDropAreaStyle()
    {
        if (_dropAreaStyle == null)
        {
            _dropAreaStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
        }

        return _dropAreaStyle;
    }

    private void HandleDragAndDrop(Rect dropArea)
    {
        Event evt = Event.current;
        if (!dropArea.Contains(evt.mousePosition))
        {
            return;
        }

        if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
        {
            return;
        }

        bool hasTextures = DragAndDrop.objectReferences.Any(o => o is Texture2D);
        if (!hasTextures)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            return;
        }

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

        if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            ConvertDraggedTextures();
        }

        evt.Use();
    }

    private void ConvertDraggedTextures()
    {
        bool wroteFile = false;

        foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
        {
            if (obj is Texture2D texture2D)
            {
                try
                {
                    if (ConvertTextureToPng(texture2D, out string assetPath))
                    {
                        wroteFile = true;
                        _messages.Insert(0, $"Saved: {assetPath}");
                    }
                }
                catch (Exception ex)
                {
                    _messages.Insert(0, $"Error on {obj.name}: {ex.Message}");
                }
            }
        }

        if (wroteFile)
        {
            AssetDatabase.Refresh();
        }
    }

    private bool ConvertTextureToPng(Texture2D texture, out string projectRelativePath)
    {
        projectRelativePath = null;

        string assetPath = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(assetPath))
        {
            _messages.Insert(0, $"Ignoring {texture.name}: asset path not found.");
            return false;
        }

        string absolutePath = GetAbsolutePath(assetPath);
        string directory = Path.GetDirectoryName(absolutePath);
        if (directory == null)
        {
            _messages.Insert(0, $"Ignoring {texture.name}: cannot resolve directory.");
            return false;
        }

        string fileName = Path.GetFileNameWithoutExtension(absolutePath);
        string currentExtension = Path.GetExtension(absolutePath);
        bool alreadyPng = string.Equals(currentExtension, ".png", StringComparison.OrdinalIgnoreCase);
        string targetFileName = alreadyPng ? $"{fileName}_Copy.png" : $"{fileName}.png";
        string targetAbsolutePath = Path.Combine(directory, targetFileName);

        Texture2D readableCopy = CreateReadableCopy(texture);
        byte[] pngData = readableCopy.EncodeToPNG();
        DestroyImmediate(readableCopy);

        File.WriteAllBytes(targetAbsolutePath, pngData);
        projectRelativePath = ToProjectRelativePath(targetAbsolutePath);
        AssetDatabase.ImportAsset(projectRelativePath);
        return true;
    }

    private static Texture2D CreateReadableCopy(Texture2D source)
    {
        RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, temp);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = temp;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
        readable.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temp);

        return readable;
    }

    private void DrawLog()
    {
        if (_messages.Count == 0)
        {
            EditorGUILayout.HelpBox("Aucun fichier converti pour le moment.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Historique :", EditorStyles.boldLabel);
        _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(120f));
        foreach (string message in _messages)
        {
            EditorGUILayout.LabelField($"- {message}");
        }

        EditorGUILayout.EndScrollView();
    }

    private static string GetAbsolutePath(string assetPath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
    }

    private static string ToProjectRelativePath(string absolutePath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace("\\", "/");
        string normalized = absolutePath.Replace("\\", "/");
        if (normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Substring(projectRoot.Length + 1);
        }

        return normalized;
    }
}
