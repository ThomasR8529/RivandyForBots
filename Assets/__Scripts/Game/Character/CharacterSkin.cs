using System.Collections.Generic;
using UnityEngine;

public class CharacterSkin : MonoBehaviour
{
    [System.Serializable]
    public class SkinData
    {
        public int skinId;
        public List<GameObject> listObj;
        
    }

    [SerializeField]
    private List<SkinData> skins = new List<SkinData>();

    [SerializeField] Animator animator;

    /// <summary>
    /// Sélectionne un skin en fonction de l'ID donné.
    /// Les objets du skin sélectionné sont déplacés au premier rang dans la hiérarchie.
    /// </summary>
    /// <param name="skinId">L'identifiant du skin à activer</param>
    public void SelectSkin(int skinId)
    {

        // Trouver le skin correspondant à l'ID fourni
        SkinData selectedSkin = skins.Find(s => s.skinId == skinId);

        if (selectedSkin == null)
        {
            Debug.LogWarning($"Skin ID {skinId} not found.");
            return;
        }

        // Déplacer tous les objets des autres skins à la fin de la hiérarchie
        foreach (SkinData skin in skins)
        {
            if (skin.skinId != skinId)
            {
                foreach (GameObject obj in skin.listObj)
                {
                    if (obj != null)
                    {
                        obj.transform.SetSiblingIndex(transform.childCount - 1);
                        obj.SetActive(false);
                    }
                }
            }
        }

        // Déplacer les objets du skin sélectionné au premier rang
        for (int i = selectedSkin.listObj.Count - 1; i >= 0; i--)
        {
            if (selectedSkin.listObj[i] != null)
            {
                selectedSkin.listObj[i].transform.SetSiblingIndex(0);
                selectedSkin.listObj[i].SetActive(true);
            }
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        float normalizedTime = currentState.normalizedTime;

        animator.Rebind();
        // Après le changement
        animator.Play(currentState.fullPathHash, -1, normalizedTime);
    }
}