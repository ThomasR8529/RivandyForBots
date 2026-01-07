using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SpriteScroller : MonoBehaviour
{
    [Header("Activer le défilement en mode Éditeur")]
    public bool enableInEditMode = false;

    [Header("Sprites à défiler")]
    public List<Sprite> sprites;
    
    [Header("Matériau à modifier")]
    public Material targetMaterial;
    
    [Header("Nom du paramètre de texture dans le shader")]
    public string shaderTextureParameter = "_MainTex";
    
    [Header("Vitesse de défilement (secondes)")]
    public float scrollInterval = 0.5f;

    private int currentSpriteIndex = 0;
    private float timer;

    void Start()
    {
        if (sprites == null || sprites.Count == 0)
        {
            Debug.LogWarning("La liste des sprites est vide.");
            enabled = false;
            return;
        }
        
        if (targetMaterial == null)
        {
            Debug.LogWarning("Aucun matériau assigné.");
            enabled = false;
            return;
        }
        
        UpdateMaterialTexture();
    }

    void Update()
    {
        if (!Application.isPlaying && !enableInEditMode)
            return;
        
        timer += Time.deltaTime;
        if (timer >= scrollInterval)
        {
            timer = 0f;
            NextSprite();
        }
    }

    void NextSprite()
    {
        currentSpriteIndex = (currentSpriteIndex + 1) % sprites.Count;
        UpdateMaterialTexture();
    }

    void UpdateMaterialTexture()
    {
        Sprite currentSprite = sprites[currentSpriteIndex];
        if (currentSprite != null)
        {
            targetMaterial.SetTexture(shaderTextureParameter, currentSprite.texture);
        }
    }
}