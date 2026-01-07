using System.Collections;
using TMPro;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private Transform cam;
    public TextMeshPro entityName;
#if !UNITY_SERVER
    void Start()
    {
        cam = Camera.main.transform; // Récupère la caméra principale
        StartCoroutine(RefreshTMP());
    }

    private IEnumerator RefreshTMP()
    {
        yield return null; // attendre 1 frame complète

        bool wasActive = entityName.gameObject.activeSelf;
        entityName.gameObject.SetActive(false);
        entityName.gameObject.SetActive(wasActive);

        entityName.ForceMeshUpdate();
    }
    void LateUpdate()
    {
        if (cam != null)
        {
            transform.LookAt(transform.position + cam.forward);
        }
    }
#endif
}
