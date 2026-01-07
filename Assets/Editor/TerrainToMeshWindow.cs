using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class TerrainToMeshWindow : EditorWindow
{
    private Terrain _terrain;
    private int _meshResolution = 256;
    private bool _createGameObject = true;
    private bool _addCollider = true;
    private bool _copyTerrainMaterial = true;
    private bool _bakePaintToTexture = false;
    private int _textureResolution = 1024;
    private bool _applyColorCorrection = true;
    private float _colorMultiplier = 1f;
    private bool _exportPng = false;
    private int _textureTilesX = 1;
    private int _textureTilesY = 1;
    private bool _convertTrees = true;
    private bool _convertDetails = false;

    [MenuItem("Tools/Terrain/Convert Terrain To Mesh...", priority = 205)]
    private static void ShowWindow()
    {
        var window = GetWindow<TerrainToMeshWindow>("Terrain -> Mesh");
        window.minSize = new Vector2(340f, 180f);
    }

    private void OnEnable()
    {
        TryAssignTerrainFromSelection();
    }

    private void OnSelectionChange()
    {
        if (_terrain == null)
        {
            TryAssignTerrainFromSelection();
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        _terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", _terrain, typeof(Terrain), true);

        if (_terrain == null)
        {
            EditorGUILayout.HelpBox("Selectionnez un Terrain dans la scene ou assignez-en un.", MessageType.Info);
        }
        else
        {
            var data = _terrain.terrainData;
            EditorGUILayout.LabelField("Heightmap", $"{data.heightmapResolution} echantillons");
            _meshResolution = EditorGUILayout.IntSlider(
                new GUIContent("Resolution du mesh", "Nombre de sommets par axe dans le mesh genere."),
                _meshResolution,
                2,
                data.heightmapResolution);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sortie", EditorStyles.boldLabel);
        _createGameObject = EditorGUILayout.Toggle(new GUIContent("Creer un GameObject"), _createGameObject);
        using (new EditorGUI.DisabledScope(!_createGameObject))
        {
            _addCollider = EditorGUILayout.Toggle(new GUIContent("Ajouter un MeshCollider"), _addCollider);
            _copyTerrainMaterial = EditorGUILayout.Toggle(new GUIContent("Copier le materiau du Terrain"), _copyTerrainMaterial);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);
        _bakePaintToTexture = EditorGUILayout.Toggle(new GUIContent("Baker la peinture terrain", "Combine les Terrain Layers dans une texture unique et cree un materiau pour le mesh."), _bakePaintToTexture);
        using (new EditorGUI.DisabledScope(!_bakePaintToTexture))
        {
            _textureResolution = EditorGUILayout.IntSlider(new GUIContent("Resolution texture"), _textureResolution, 256, 4096);
            _applyColorCorrection = EditorGUILayout.Toggle(new GUIContent("Correction luminosite"), _applyColorCorrection);
            if (_applyColorCorrection)
            {
                _colorMultiplier = EditorGUILayout.Slider(new GUIContent("Multiplicateur", "Facteur applique sur les couleurs pour les eclaircir ou assombrir."), _colorMultiplier, 0.25f, 2.5f);
            }
            _exportPng = EditorGUILayout.Toggle(new GUIContent("Exporter en PNG", "Cree aussi un fichier PNG a partir de la texture bakee."), _exportPng);
            _textureTilesX = EditorGUILayout.IntSlider(new GUIContent("Tuiles horizontales", "Nombre de sous-textures generees sur l'axe X."), _textureTilesX, 1, 8);
            _textureTilesY = EditorGUILayout.IntSlider(new GUIContent("Tuiles verticales", "Nombre de sous-textures generees sur l'axe Z."), _textureTilesY, 1, 8);
        }
        if (_bakePaintToTexture)
        {
            EditorGUILayout.HelpBox("Necessite des Terrain Layers avec textures diffuse. Peut etre long sur des resolutions elevees.", MessageType.None);
            if (_textureTilesX > 1 || _textureTilesY > 1)
            {
                EditorGUILayout.HelpBox($"La texture sera decoupee en {_textureTilesX * _textureTilesY} images pour conserver une haute definition.", MessageType.Info);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Vegetation", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(!_createGameObject))
        {
            _convertTrees = EditorGUILayout.Toggle(new GUIContent("Convertir les arbres"), _convertTrees);
            _convertDetails = EditorGUILayout.Toggle(new GUIContent("Convertir les details", "Instancie les prefabs utilises par les Details (mesh uniquement). Peut generer un tres grand nombre de GameObjects."), _convertDetails);
            if (!_createGameObject)
            {
                EditorGUILayout.HelpBox("Activez \"Creer un GameObject\" pour convertir la vegetation.", MessageType.Warning);
            }
            else if (_convertDetails)
            {
                EditorGUILayout.HelpBox("Attention : convertir les details peut creer des milliers de GameObjects et ralentir l'editeur.", MessageType.Warning);
            }
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(_terrain == null))
        {
            if (GUILayout.Button("Convertir en Mesh"))
            {
                ConvertTerrain();
            }
        }
    }

    private void ConvertTerrain()
    {
        if (_terrain == null)
        {
            return;
        }

        var terrainData = _terrain.terrainData;
        int clampedResolution = Mathf.Clamp(_meshResolution, 2, terrainData.heightmapResolution);

        string defaultName = $"{_terrain.name}_Mesh.asset";
        string assetPath = EditorUtility.SaveFilePanelInProject(
            "Enregistrer le mesh genere",
            defaultName,
            "asset",
            "Choisissez ou sauvegarder le mesh genere.");

        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        int textureTilesX = _bakePaintToTexture ? Mathf.Max(1, _textureTilesX) : 1;
        int textureTilesY = _bakePaintToTexture ? Mathf.Max(1, _textureTilesY) : 1;

        Mesh mesh = BuildMesh(terrainData, clampedResolution, textureTilesX, textureTilesY);
        if (mesh == null)
        {
            EditorUtility.DisplayDialog("Terrain -> Mesh", "Echec de la generation du mesh.", "Compris");
            return;
        }

        string directory = Path.GetDirectoryName(assetPath) ?? "Assets";
        string meshName = Path.GetFileNameWithoutExtension(assetPath);

        List<Texture2D> bakedTextures = null;
        List<Material> bakedMaterials = null;

        if (_bakePaintToTexture)
        {
            bakedTextures = BakeTerrainTextures(_terrain, _textureResolution, _applyColorCorrection, _colorMultiplier, textureTilesX, textureTilesY);
            if (bakedTextures != null && bakedTextures.Count > 0)
            {
                bakedMaterials = new List<Material>(bakedTextures.Count);
                for (int i = 0; i < bakedTextures.Count; i++)
                {
                    Texture2D texture = bakedTextures[i];
                    if (texture == null)
                    {
                        bakedMaterials.Add(null);
                        continue;
                    }

                    string textureName = $"{meshName}_Texture_{i:D2}";
                    texture.name = textureName;
                    string texturePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, $"{textureName}.asset"));
                    AssetDatabase.CreateAsset(texture, texturePath);

                    var material = CreateBakedMaterial(texture, $"{meshName}_Material_{i:D2}");
                    string materialPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, $"{material.name}.mat"));
                    AssetDatabase.CreateAsset(material, materialPath);
                    bakedMaterials.Add(material);

                    if (_exportPng)
                    {
                        string pngPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, $"{textureName}.png"));
                        SaveTextureAsPng(texture, pngPath);
                        AssetDatabase.ImportAsset(pngPath);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Impossible de baker la texture du terrain. Verifiez que le terrain utilise des Terrain Layers avec textures diffuses.");
            }
        }

        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssets();

        GameObject meshRoot = null;
        if (_createGameObject)
        {
            meshRoot = CreateMeshGameObject(mesh, bakedMaterials);
            if (meshRoot != null && (_convertTrees || _convertDetails))
            {
                ConvertVegetation(meshRoot);
            }
        }

        Selection.activeObject = mesh;
        EditorGUIUtility.PingObject(mesh);
        EditorUtility.DisplayDialog("Terrain -> Mesh", "Mesh genere avec succes.", "OK");
    }

    private GameObject CreateMeshGameObject(Mesh mesh, List<Material> bakedMaterials)
    {
        var go = new GameObject($"{_terrain.name}_Mesh");
        Undo.RegisterCreatedObjectUndo(go, "Create Terrain Mesh");

        var terrainTransform = _terrain.transform;
        go.transform.SetParent(terrainTransform.parent, false);
        go.transform.localPosition = terrainTransform.localPosition;
        go.transform.localRotation = terrainTransform.localRotation;
        go.transform.localScale = terrainTransform.localScale;

        var meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        var meshRenderer = go.AddComponent<MeshRenderer>();
        ApplyMaterialsToRenderer(meshRenderer, mesh, bakedMaterials);

        if (_addCollider)
        {
            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        Selection.activeGameObject = go;
        return go;
    }

    private void ApplyMaterialsToRenderer(MeshRenderer renderer, Mesh mesh, List<Material> bakedMaterials)
    {
        if (renderer == null || mesh == null)
        {
            return;
        }

        IList<Material> sourceMaterials = bakedMaterials;
        if ((sourceMaterials == null || sourceMaterials.Count == 0) && _copyTerrainMaterial && _terrain.materialTemplate != null)
        {
            sourceMaterials = new List<Material> { _terrain.materialTemplate };
        }

        int subMeshCount = Mathf.Max(1, mesh.subMeshCount);
        if (subMeshCount <= 1)
        {
            renderer.sharedMaterial = (sourceMaterials != null && sourceMaterials.Count > 0) ? sourceMaterials[0] : null;
            return;
        }

        var assigned = new Material[subMeshCount];
        for (int i = 0; i < subMeshCount; i++)
        {
            if (sourceMaterials != null && sourceMaterials.Count > 0)
            {
                assigned[i] = sourceMaterials[Mathf.Min(i, sourceMaterials.Count - 1)];
            }
            else
            {
                assigned[i] = null;
            }
        }

        renderer.sharedMaterials = assigned;
    }

    private void ConvertVegetation(GameObject meshRoot)
    {
        if (meshRoot == null || _terrain == null)
        {
            return;
        }

        var terrainData = _terrain.terrainData;
        if (terrainData == null)
        {
            return;
        }

        if (_convertTrees)
        {
            InstantiateTreePrefabs(meshRoot.transform, terrainData);
        }

        if (_convertDetails)
        {
            InstantiateDetailPrefabs(meshRoot.transform, terrainData);
        }

        EditorUtility.ClearProgressBar();
    }

    private void InstantiateTreePrefabs(Transform parent, TerrainData data)
    {
        var instances = data.treeInstances;
        var prototypes = data.treePrototypes;
        if (instances == null || instances.Length == 0 || prototypes == null || prototypes.Length == 0)
        {
            return;
        }

        GameObject treesRoot = null;

        for (int i = 0; i < instances.Length; i++)
        {
            if ((i & 31) == 0)
            {
                EditorUtility.DisplayProgressBar("Terrain -> Mesh", $"Conversion des arbres ({i}/{instances.Length})", i / (float)instances.Length);
            }

            var tree = instances[i];
            if (tree.prototypeIndex < 0 || tree.prototypeIndex >= prototypes.Length)
            {
                continue;
            }

            var prefab = prototypes[tree.prototypeIndex].prefab;
            if (prefab == null)
            {
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
            {
                continue;
            }

            if (treesRoot == null)
            {
                treesRoot = new GameObject("Trees");
                Undo.RegisterCreatedObjectUndo(treesRoot, "Create Terrain Trees");
                treesRoot.transform.SetParent(parent, false);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Create Terrain Trees");

            Vector3 worldPosition = GetWorldPositionFromNormalized(tree.position);
            instance.transform.position = worldPosition;

            Quaternion rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * tree.rotation, Vector3.up) * _terrain.transform.rotation;
            instance.transform.rotation = rotation;

            Vector3 baseScale = prefab.transform.localScale;
            instance.transform.localScale = new Vector3(
                baseScale.x * tree.widthScale,
                baseScale.y * tree.heightScale,
                baseScale.z * tree.widthScale);

            instance.transform.SetParent(treesRoot.transform, true);
        }

        EditorUtility.ClearProgressBar();
    }

    private void InstantiateDetailPrefabs(Transform parent, TerrainData data)
    {
        var prototypes = data.detailPrototypes;
        if (prototypes == null || prototypes.Length == 0)
        {
            return;
        }

        int width = data.detailWidth;
        int height = data.detailHeight;
        if (width == 0 || height == 0)
        {
            return;
        }

        GameObject detailsRoot = null;

        for (int layer = 0; layer < prototypes.Length; layer++)
        {
            var prototype = prototypes[layer];
            if (!prototype.usePrototypeMesh || prototype.prototype == null)
            {
                continue;
            }

            int[,] layerData = data.GetDetailLayer(0, 0, width, height, layer);
            for (int y = 0; y < height; y++)
            {
                float progress = (layer * height + y) / (float)(prototypes.Length * height);
                if ((y & 15) == 0)
                {
                    EditorUtility.DisplayProgressBar("Terrain -> Mesh", $"Conversion des details (layer {layer + 1}/{prototypes.Length})", progress);
                }

                for (int x = 0; x < width; x++)
                {
                    int count = layerData[y, x];
                    if (count == 0)
                    {
                        continue;
                    }

                    for (int c = 0; c < count; c++)
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prototype.prototype);
                        if (instance == null)
                        {
                            continue;
                        }

                        if (detailsRoot == null)
                        {
                            detailsRoot = new GameObject("Details");
                            Undo.RegisterCreatedObjectUndo(detailsRoot, "Create Terrain Details");
                            detailsRoot.transform.SetParent(parent, false);
                        }

                        Undo.RegisterCreatedObjectUndo(instance, "Create Terrain Details");

                        Vector2 normalized = GetDetailNormalizedPosition(x, y, c, width, height, layer);
                        Vector3 worldPos = DetailNormalizedToWorld(normalized);

                        Vector3 normal = data.GetInterpolatedNormal(normalized.x, normalized.y);
                        normal = _terrain.transform.TransformDirection(normal);
                        instance.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
                        instance.transform.position = worldPos;

                        Vector3 baseScale = prototype.prototype.transform.localScale;
                        float widthScale = Mathf.Lerp(prototype.minWidth, prototype.maxWidth, DeterministicValue(x, y, c, layer));
                        float heightScale = Mathf.Lerp(prototype.minHeight, prototype.maxHeight, DeterministicValue(y, x, c, layer));
                        instance.transform.localScale = new Vector3(
                            baseScale.x * widthScale,
                            baseScale.y * heightScale,
                            baseScale.z * widthScale);

                        instance.transform.SetParent(detailsRoot.transform, true);
                    }
                }
            }
        }

        EditorUtility.ClearProgressBar();
    }

    private Vector3 GetWorldPositionFromNormalized(Vector3 normalizedPosition)
    {
        var data = _terrain.terrainData;
        Vector3 local = new Vector3(
            normalizedPosition.x * data.size.x,
            normalizedPosition.y * data.size.y,
            normalizedPosition.z * data.size.z);
        return _terrain.transform.TransformPoint(local);
    }

    private Vector3 DetailNormalizedToWorld(Vector2 normalized)
    {
        var data = _terrain.terrainData;
        float height = data.GetInterpolatedHeight(normalized.x, normalized.y);
        Vector3 local = new Vector3(
            normalized.x * data.size.x,
            height,
            normalized.y * data.size.z);
        return _terrain.transform.TransformPoint(local);
    }

    private static Vector2 GetDetailNormalizedPosition(int x, int y, int index, int width, int height, int layer)
    {
        float jitterX = (DeterministicValue(x, y, index, layer) - 0.5f) * 0.9f;
        float jitterY = (DeterministicValue(y, x, index, layer) - 0.5f) * 0.9f;

        float normalizedX = (x + 0.5f + jitterX) / width;
        float normalizedY = (y + 0.5f + jitterY) / height;

        return new Vector2(Mathf.Clamp01(normalizedX), Mathf.Clamp01(normalizedY));
    }

    private static float DeterministicValue(int a, int b, int c, int d)
    {
        unchecked
        {
            uint hash = 2166136261u;
            hash = (hash ^ (uint)a) * 16777619u;
            hash = (hash ^ (uint)b) * 16777619u;
            hash = (hash ^ (uint)c) * 16777619u;
            hash = (hash ^ (uint)d) * 16777619u;
            return (hash & 0x00FFFFFF) / 16777215f;
        }
    }

    private Mesh BuildMesh(TerrainData terrainData, int resolution, int tileCountX, int tileCountY)
    {
        tileCountX = Mathf.Max(1, tileCountX);
        tileCountY = Mathf.Max(1, tileCountY);
        int tileCount = tileCountX * tileCountY;
        bool useSubmeshes = tileCount > 1;

        int width = resolution;
        int height = resolution;
        int vertexCount = width * height;

        var mesh = new Mesh
        {
            name = $"{_terrain.name}_Mesh_{resolution}"
        };

        if (vertexCount > 65000)
        {
            mesh.indexFormat = IndexFormat.UInt32;
        }

        var vertices = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var uvs = new Vector2[vertexCount];

        float stepX = (terrainData.heightmapResolution - 1f) / (width - 1f);
        float stepZ = (terrainData.heightmapResolution - 1f) / (height - 1f);
        Vector3 size = terrainData.size;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;

                float normalizedX = x / (float)(width - 1);
                float normalizedZ = z / (float)(height - 1);

                int heightX = Mathf.RoundToInt(x * stepX);
                int heightZ = Mathf.RoundToInt(z * stepZ);

                float y = terrainData.GetHeight(heightX, heightZ);

                vertices[index] = new Vector3(normalizedX * size.x, y, normalizedZ * size.z);
                normals[index] = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
                uvs[index] = new Vector2(normalizedX, normalizedZ);
            }
        }

        int quadsX = width - 1;
        int quadsZ = height - 1;
        var triangles = new int[quadsX * quadsZ * 6];
        int t = 0;

        for (int z = 0; z < quadsZ; z++)
        {
            for (int x = 0; x < quadsX; x++)
            {
                int i = z * width + x;

                triangles[t++] = i;
                triangles[t++] = i + width;
                triangles[t++] = i + width + 1;

                triangles[t++] = i;
                triangles[t++] = i + width + 1;
                triangles[t++] = i + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;

        if (useSubmeshes)
        {
            var submeshTriangles = new List<int>[tileCount];
            for (int i = 0; i < tileCount; i++)
            {
                submeshTriangles[i] = new List<int>();
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];

                float avgU = (uvs[i0].x + uvs[i1].x + uvs[i2].x) / 3f;
                float avgV = (uvs[i0].y + uvs[i1].y + uvs[i2].y) / 3f;

                int tileX = Mathf.Clamp((int)(avgU * tileCountX), 0, tileCountX - 1);
                int tileY = Mathf.Clamp((int)(avgV * tileCountY), 0, tileCountY - 1);
                int tileIndex = tileY * tileCountX + tileX;

                submeshTriangles[tileIndex].Add(i0);
                submeshTriangles[tileIndex].Add(i1);
                submeshTriangles[tileIndex].Add(i2);
            }

            mesh.subMeshCount = tileCount;
            for (int i = 0; i < tileCount; i++)
            {
                mesh.SetTriangles(submeshTriangles[i], i, false);
            }
        }
        else
        {
            mesh.triangles = triangles;
        }

        mesh.RecalculateBounds();
        return mesh;
    }

    private List<Texture2D> BakeTerrainTextures(Terrain terrain, int resolution, bool applyColorCorrection, float colorMultiplier, int tilesX, int tilesY)
    {
        if (terrain == null) return null;
        var data = terrain.terrainData;
        var layers = data.terrainLayers;
        if (layers == null || layers.Length == 0)
        {
            return null;
        }

        tilesX = Mathf.Max(1, tilesX);
        tilesY = Mathf.Max(1, tilesY);

        var alphamaps = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
        int alphaHeight = alphamaps.GetLength(0);
        int alphaWidth = alphamaps.GetLength(1);
        int layerCount = alphamaps.GetLength(2);

        var readableCache = new Dictionary<Texture2D, Texture2D>();
        Vector3 size = data.size;
        var textures = new List<Texture2D>(tilesX * tilesY);

        float tileSizeX = 1f / tilesX;
        float tileSizeY = 1f / tilesY;

        for (int tileYIndex = 0; tileYIndex < tilesY; tileYIndex++)
        {
            for (int tileXIndex = 0; tileXIndex < tilesX; tileXIndex++)
            {
                var colors = new Color[resolution * resolution];

                float baseNormalizedZ = tileYIndex * tileSizeY;
                float baseNormalizedX = tileXIndex * tileSizeX;

                for (int z = 0; z < resolution; z++)
                {
                    float normalizedZ = baseNormalizedZ + (z / (float)(resolution - 1)) * tileSizeY;
                    float worldZ = normalizedZ * size.z;
                    float alphaZ = normalizedZ * (alphaHeight - 1);

                    for (int x = 0; x < resolution; x++)
                    {
                        float normalizedX = baseNormalizedX + (x / (float)(resolution - 1)) * tileSizeX;
                        float worldX = normalizedX * size.x;
                        float alphaX = normalizedX * (alphaWidth - 1);

                        Color pixel = Color.black;
                        float totalWeight = 0f;

                        for (int l = 0; l < layerCount; l++)
                        {
                            if (l >= layers.Length) break;
                            var layer = layers[l];
                            if (layer == null || layer.diffuseTexture == null || layer.tileSize == Vector2.zero)
                                continue;

                            float weight = SampleAlphamap(alphamaps, alphaX, alphaZ, l);
                            if (weight <= 0f) continue;

                            Texture2D readableTexture = GetReadableTexture(layer.diffuseTexture, readableCache);
                            if (readableTexture == null) continue;

                            float u = Mathf.Repeat((worldX + layer.tileOffset.x) / layer.tileSize.x, 1f);
                            float v = Mathf.Repeat((worldZ + layer.tileOffset.y) / layer.tileSize.y, 1f);
                            Color layerColor = readableTexture.GetPixelBilinear(u, v);

                            pixel += layerColor * weight;
                            totalWeight += weight;
                        }

                        if (totalWeight < 1f)
                        {
                            pixel += Color.gray * (1f - totalWeight);
                        }

                        if (applyColorCorrection)
                        {
                            pixel.r = Mathf.Clamp01(pixel.r * colorMultiplier);
                            pixel.g = Mathf.Clamp01(pixel.g * colorMultiplier);
                            pixel.b = Mathf.Clamp01(pixel.b * colorMultiplier);
                        }

                        colors[z * resolution + x] = pixel;
                    }
                }

                var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
                texture.SetPixels(colors);
                texture.Apply();
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.filterMode = FilterMode.Bilinear;
                textures.Add(texture);
            }
        }

        foreach (var entry in readableCache)
        {
            DestroyImmediate(entry.Value);
        }

        return textures;
    }

    private static float SampleAlphamap(float[,,] alphamaps, float x, float z, int layer)
    {
        int maxZ = alphamaps.GetLength(0) - 1;
        int maxX = alphamaps.GetLength(1) - 1;

        float clampedX = Mathf.Clamp(x, 0f, maxX);
        float clampedZ = Mathf.Clamp(z, 0f, maxZ);

        int x0 = Mathf.FloorToInt(clampedX);
        int x1 = Mathf.Min(x0 + 1, maxX);
        int z0 = Mathf.FloorToInt(clampedZ);
        int z1 = Mathf.Min(z0 + 1, maxZ);

        float tx = clampedX - x0;
        float tz = clampedZ - z0;

        float a = Mathf.Lerp(alphamaps[z0, x0, layer], alphamaps[z0, x1, layer], tx);
        float b = Mathf.Lerp(alphamaps[z1, x0, layer], alphamaps[z1, x1, layer], tx);
        return Mathf.Lerp(a, b, tz);
    }

    private static Texture2D GetReadableTexture(Texture2D source, Dictionary<Texture2D, Texture2D> cache)
    {
        if (source == null) return null;
        if (cache.TryGetValue(source, out var readable))
        {
            return readable;
        }

        var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        cache[source] = texture;
        return texture;
    }

    private static Material CreateBakedMaterial(Texture2D bakedTexture, string materialName)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        var material = new Material(shader ?? Shader.Find("Diffuse"))
        {
            name = materialName
        };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetTexture("_BaseMap", bakedTexture);
        }
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", bakedTexture);
        }
        return material;
    }

    private static void SaveTextureAsPng(Texture2D texture, string assetPath)
    {
        if (texture == null) return;
        byte[] pngData = texture.EncodeToPNG();
        if (pngData == null || pngData.Length == 0) return;

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string fullPath = string.IsNullOrEmpty(projectRoot) ? assetPath : Path.Combine(projectRoot, assetPath);
        string directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(fullPath, pngData);
    }

    private void TryAssignTerrainFromSelection()
    {
        if (Selection.activeGameObject == null)
        {
            return;
        }

        var terrain = Selection.activeGameObject.GetComponent<Terrain>();
        if (terrain != null)
        {
            _terrain = terrain;
            _meshResolution = Mathf.Clamp(_meshResolution, 2, terrain.terrainData.heightmapResolution);
        }
    }
}
