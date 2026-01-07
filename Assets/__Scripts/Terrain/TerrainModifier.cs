using UnityEngine;

public class TerrainModifier : MonoBehaviour {
    public Terrain terrain;
    public SphereCollider sphereCollider;
    public Material[] materials;

    private Material originalMaterial; // Pour stocker les matériaux d'origine du terrain

    private void Start() {
        // Sauvegarder les matériaux d'origine du terrain
        originalMaterial = terrain.materialTemplate; // Utiliser terrain.materialTemplate au lieu de terrain.materials
    }

    private void Update() {
        float reductionRatio = Mathf.Clamp01(1f - (sphereCollider.radius / sphereCollider.radius));

        int numMaterials = materials.Length;
        int firstMaterialIndex = Mathf.FloorToInt(reductionRatio * (numMaterials - 1));
        int secondMaterialIndex = Mathf.Min(firstMaterialIndex + 1, numMaterials - 1);
        float mixFactor = reductionRatio * (numMaterials - 1) - firstMaterialIndex;

        terrain.materialTemplate = MixMaterials(materials[firstMaterialIndex], materials[secondMaterialIndex], mixFactor);
    }

    private Material MixMaterials(Material materialA, Material materialB, float mixFactor) {
        Material mixedMaterial = new Material(materialA);

        mixedMaterial.color = Color.Lerp(materialA.color, materialB.color, mixFactor);
        mixedMaterial.SetTexture("_MainTex", TextureLerp(materialA.GetTexture("_MainTex"), materialB.GetTexture("_MainTex"), mixFactor));

        return mixedMaterial;
    }

    private Texture TextureLerp(Texture textureA, Texture textureB, float t) {
        return t < 0.5f ? textureA : textureB;
    }

    // Appelé lorsque le script est désactivé ou détruit
    private void OnDestroy() {
        // Restaurer les matériaux d'origine du terrain
        terrain.materialTemplate = originalMaterial; // Utiliser terrain.materialTemplate au lieu de terrain.materials
    }
}