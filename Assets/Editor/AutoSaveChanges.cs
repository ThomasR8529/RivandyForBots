using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


[InitializeOnLoad]
public class AutoSaveChanges : EditorWindow
{

    private static Dictionary<string, Dictionary<string, object>> saveDict = new Dictionary<string, Dictionary<string, object>>();
    private static bool autoSaveEnabled = false;


/*    static AutoSaveChanges()
    {
        autoSaveEnabled = EditorPrefs.GetBool("AutoSaveEnabled", false);
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        SceneView.duringSceneGui += OnSceneGUI;
    }*/


    /// <summary>
    /// Fonction permettant de traiter les différents états de l'interface.
    /// </summary>
    /// <param name="state">Etat dans lequel nous nous dirigons</param>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Debug.Log(autoSaveEnabled);
        // Si la sauvegarde est activée...
        if (autoSaveEnabled)
        {
            // Lorsque l'on quitte le PlayMode, on sauvegarde les modifications.
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Debug.Log("SaveModification()");
                SaveModification();
            }

            // Lorsque l'on entre dans l'EditMode, on applique les modifications.
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log("ApplyModification()");
                ApplyModification();
            }

        }

        // Même si on désactive la sauvegarde, il faut clear le dictionnaire pour être sûr de ne pas récupérer
        // les anciennes modifications pour un usage ultérieur.
        if (state == PlayModeStateChange.EnteredEditMode) saveDict.Clear();
    }


    /// <summary>
    /// Creation de la fenêtre UI
    /// </summary>
    private static void OnSceneGUI(SceneView sceneView)
    {
        Handles.BeginGUI();

        // Creation du background
        Rect backgroundRect = new Rect(0, Screen.height - 80, 125, 50);
        Texture2D backgroundTexture = Texture2D.whiteTexture;
        backgroundTexture.Apply();
        GUI.DrawTexture(backgroundRect, backgroundTexture);

        // Creation de la toggle
        Rect rect = new Rect(10, Screen.height - 70, 125, 15); // Définit la zone pour votre GUI
        GUI.contentColor = Color.black;
        autoSaveEnabled = EditorGUI.ToggleLeft(rect, "Save play mode", EditorPrefs.GetBool("AutoSaveEnabled", false));

        Handles.EndGUI();

        // Sauvegarde de la variable dans les préférences utilisateurs, utile car le changement vers le playmode reset la variable.
        EditorPrefs.SetBool("AutoSaveEnabled", autoSaveEnabled);
    }


    /// <summary>
    /// Fonction permettant de sauvegarder les composants, et les transforms des gameObjects de la Scene
    /// </summary>
    private static void SaveModification()
    {
        GameObject[] gameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject gameObject in gameObjects)
        {
            string gameObjectName = gameObject.name;
            if (!saveDict.ContainsKey(gameObjectName))
            {
                saveDict[gameObjectName] = new Dictionary<string, object>();
            }

            // On sauvegarde les transforms: position, rotation , scale
            saveDict[gameObjectName]["position"] = gameObject.transform.position;
            saveDict[gameObjectName]["rotation"] = gameObject.transform.rotation;
            saveDict[gameObjectName]["scale"] = gameObject.transform.localScale;

            // Composants
            Component[] components = gameObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component != null && component != gameObject.transform && !saveDict[gameObjectName].ContainsKey(component.transform.name))
                {
                    PropertyInfo[] properties = component.GetType().GetProperties();
                    Dictionary<string, object> componentData = new Dictionary<string, object>();
                    foreach (PropertyInfo property in properties)
                    {
                        if (property.CanRead && property.CanWrite)
                        {
                            componentData[property.Name] = property.GetValue(component);
                        }
                    }

                    saveDict[gameObjectName][component.transform.name] = componentData;
                }
            }
        }
    }

    /// <summary>
    /// Fonction permettant d'appliquer les modifications récupérées
    /// </summary>
    private static void ApplyModification()
    {
        foreach (string gameObjectName in saveDict.Keys)
        {
            // Restore the saved values of the transform's properties
            GameObject obj = GameObject.Find(gameObjectName);
            if (obj == null) continue;
            UnityEngine.Transform transform = obj.transform;
            transform.position = (Vector3)saveDict[gameObjectName]["position"];
            transform.rotation = (Quaternion)saveDict[gameObjectName]["rotation"];
            transform.localScale = (Vector3)saveDict[gameObjectName]["scale"];

            // Restore the saved values of other components' properties
            Component[] components = transform.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component != null && component != transform && saveDict[gameObjectName].ContainsKey(component.transform.name))
                {
                    Dictionary<string, object> componentData = (Dictionary<string, object>)saveDict[transform.gameObject.name][component.transform.name];
                    PropertyInfo[] properties = component.GetType().GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        if (property.Name == "current") continue;
                        if (property.CanRead && property.CanWrite && componentData.ContainsKey(property.Name))
                        {
                            property.SetValue(component, componentData[property.Name]);
                        }
                    }
                }
            }
        }

        saveDict.Clear();
    }
}