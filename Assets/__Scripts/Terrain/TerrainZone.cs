using System.Collections.Generic;
using UnityEngine;


public class TerrainZone : MonoBehaviour {
    public Terrain terrain;
    public Vector3 centralPosition; // Position centrale de la zone
    public float zoneDistance = 10.0f; // Distance à partir de la position centrale pour la zone

    private int textureCount;
    private float[,,] originalAlphas;

    private void Start() {
        TerrainData terrainData = terrain.terrainData;
        textureCount = terrainData.alphamapLayers;

        // Sauvegarder les données d'alphamap originales
        originalAlphas = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            ChangeTerrainMaterials(true); // Changer les matériaux en dehors de la zone
        }
    }

    private void ChangeTerrainMaterials(bool changeOutsideZone) {
        TerrainData terrainData = terrain.terrainData;
        float[,,] alphas = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, textureCount];

        for (int i = 0 ; i < terrainData.alphamapWidth ; i++) {
            for (int j = 0 ; j < terrainData.alphamapHeight ; j++) {
                Vector3 worldPos = terrain.transform.TransformPoint(new Vector3(
                    i * 1.0f / (terrainData.alphamapWidth - 1),
                    0,
                    j * 1.0f / (terrainData.alphamapHeight - 1)
                ));
                float distanceToCentral = Vector3.Distance(worldPos, centralPosition);

                // Si à l'extérieur de la zone
                Debug.Log(distanceToCentral);

                if (changeOutsideZone && distanceToCentral > zoneDistance) {
                    int randomTextureIndex = Random.Range(0, textureCount);
                    for (int t = 0 ; t < textureCount ; t++) {
                        if (t == randomTextureIndex)
                            alphas[i, j, t] = 1.0f;
                        else
                            alphas[i, j, t] = 0.0f;
                    }
                }
                // Si à l'intérieur de la zone
                else {
                    for (int t = 0 ; t < textureCount ; t++) {
                        alphas[i, j, t] = originalAlphas[i, j, t];
                    }
                }
            }
        }

        // Appliquer les modifications d'alphamap
        terrainData.SetAlphamaps(0, 0, alphas);
    }

    private void OnDestroy() {
        // Rétablir les matériaux originaux lorsque l'objet est détruit
        terrain.terrainData.SetAlphamaps(0, 0, originalAlphas);
    }
}



