using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class LaunchDirector : MonoBehaviour
{

    PlayableDirector playableDirector;
    // Start is called before the first frame update
    void Start()
    {
        playableDirector = GetComponent<PlayableDirector>(); 
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            playableDirector.time = 0;
            playableDirector.Play();
        }
    }
}
