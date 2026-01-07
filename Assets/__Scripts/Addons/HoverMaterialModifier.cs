using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

[Serializable]
public class MaterialPropertyChange
{
    public string propertyName;
    public PropertyType propertyType;
    public bool boolValue;
    public float floatValue;
    public Sprite spriteValue;

    public enum PropertyType
    {
        Boolean,
        Float,
        Sprite,
        BooleanKeyword
    }
}

public class HoverMaterialModifier : MonoBehaviour
{
    [SerializeField] public List<MaterialPropertyChange> onHoverProperties = new List<MaterialPropertyChange>();
    [SerializeField] public List<MaterialPropertyChange> onUnHoverProperties = new List<MaterialPropertyChange>();

    private Material _clonedMaterial;
    private Image _image;
    [SerializeField] private bool IsOnStart = false;

    void Awake()
    {
        _image = GetComponent<Image>();
        if (_image.material != null)
        {
            _clonedMaterial = new Material(_image.material);
            _image.material = _clonedMaterial;
        }
    }

    public void Start(){
        if(IsOnStart){
            ApplyProperties(onHoverProperties);
        }
    }

    public void OnHover()
    {
        ApplyProperties(onHoverProperties);
    }

    public void OnUnHover()
    {
        ApplyProperties(onUnHoverProperties);
    }


    private void ApplyProperties(List<MaterialPropertyChange> properties)
    {
        foreach (var prop in properties)
        {
            switch (prop.propertyType)
            {
                case MaterialPropertyChange.PropertyType.Boolean:
                    Debug.Log(prop.propertyName + " : " + prop.boolValue);
                    _clonedMaterial.SetFloat(prop.propertyName, prop.boolValue ? 1f : 0f);
                    break;
                case MaterialPropertyChange.PropertyType.Float:
                    _clonedMaterial.SetFloat(prop.propertyName, prop.floatValue);
                    break;
                case MaterialPropertyChange.PropertyType.Sprite:
                    if (prop.spriteValue != null)
                    {
                        _clonedMaterial.SetTexture(prop.propertyName, prop.spriteValue.texture);
                    }
                    break;
                case MaterialPropertyChange.PropertyType.BooleanKeyword:
                    if (prop.boolValue)
                        _clonedMaterial.EnableKeyword(prop.propertyName);
                    else
                        _clonedMaterial.DisableKeyword(prop.propertyName);
                    break;
            }
        }
    }
} 
