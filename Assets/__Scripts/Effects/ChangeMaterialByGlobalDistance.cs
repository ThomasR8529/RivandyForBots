using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChangeMaterialByGlobalDistance : MonoBehaviour
{
    public List<MaterialByDistance> materialByDistances;


    [Serializable]
    public class MaterialByDistance {
        public MeshRenderer objectMaterialRenderer;
        public int materialIndex = 1;
        public Material materialOutside;
        public float distanceToApply;
        public bool applied;
    }

    public void ApplyToDistance(float distanceToApply)
    {
        foreach (MaterialByDistance elt in materialByDistances.Where(elt => !elt.applied && distanceToApply > elt.distanceToApply)) {
            elt.applied = true;
            elt.objectMaterialRenderer.materials[elt.materialIndex] = elt.materialOutside;
        }
    }
}
