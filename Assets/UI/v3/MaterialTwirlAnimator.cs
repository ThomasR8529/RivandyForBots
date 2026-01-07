using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
[ExecuteInEditMode]
public class MaterialTwirlAnimator : MonoBehaviour
{
    [System.Serializable]
    public class MaterialPropertyAnimation
    {
        public string propertyName; // Nom de la propriété du material
        public float minValue = -1f; // Valeur minimale
        public float maxValue = 1f;  // Valeur maximale
        public float defaultValue = 0f; // Valeur par défaut
        public float speed = 2f; // Durée de l'animation
    }

    [SerializeField] private Material targetMaterial; // Matériel à animer
    [SerializeField] private List<MaterialPropertyAnimation> properties = new List<MaterialPropertyAnimation>();

    private void Start()
    {
        if (targetMaterial == null)
        {
            Debug.LogError("Material non assigné au script.");
            return;
        }

        foreach (var property in properties)
        {
            if (!targetMaterial.HasProperty(property.propertyName))
            {
                Debug.LogWarning($"La propriété {property.propertyName} n'existe pas sur le material {targetMaterial.name}.");
                continue;
            }

            // Appliquer la valeur par défaut au départ
            targetMaterial.SetFloat(property.propertyName, property.defaultValue);

            // Lancer l'animation
            AnimateProperty(property);
        }
    }

    private void AnimateProperty(MaterialPropertyAnimation property)
    {
        float targetValue = Random.Range(property.minValue, property.maxValue);

        targetMaterial.DOFloat(targetValue, property.propertyName, property.speed)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => AnimateProperty(property)); // Relance l'animation
    }
}