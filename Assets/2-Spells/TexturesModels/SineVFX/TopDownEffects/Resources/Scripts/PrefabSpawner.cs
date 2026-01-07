using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{

    public GameObject[] prefabs;
    public Camera sceneCamera;
    public string nameOfThePrefab;

    private int index;

    // Start is called before the first frame update
    void Start()
    {
        nameOfThePrefab = prefabs[index].name;
    }

    // Update is called once per frame
    void Update()
    {
        nameOfThePrefab = prefabs[index].name;
    }

    // Spawning a Prefab of a Complete Effect on mouse position
    public void SpawnPrefab()
    {
        var mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y + 75, Input.mousePosition.z);
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Instantiate(prefabs[index], hit.point, Quaternion.identity);
        }
    }

    // Changing the index on Prefab array
    public void ChangePrefabIntex(bool bo)
    {
        if (bo == true)
        {
            index++;
            if (index == prefabs.Length)
            {
                index = 0;
            }
        }
        else
        {
            index--;
            if (index == -1)
            {
                index = prefabs.Length - 1;
            }
        }
    }
}
