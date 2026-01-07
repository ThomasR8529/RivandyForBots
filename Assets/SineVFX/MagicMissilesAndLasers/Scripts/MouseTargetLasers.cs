using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MouseTargetLasers : MonoBehaviour {

    public Transform laserShotPosition;
    public float speed = 1f;
    public ParticleSystem startWavePS;
    public ParticleSystem startParticles;
    public int startParticlesCount = 100;
    public GameObject laserShotPrefab;

    private Vector3 mouseWorldPosition;
    private Animator anim;
    


    void Start () {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                startWavePS.Emit(1);
                startParticles.Emit(startParticlesCount);
            }

            anim.SetBool("Fire", mouse.leftButton.isPressed);

            if (mouse.rightButton.wasPressedThisFrame)
            {
                anim.SetBool("Fire", true);
                startWavePS.Emit(1);
                startParticles.Emit(startParticlesCount);
                Instantiate(laserShotPrefab, laserShotPosition.position, transform.rotation);
            }
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            startWavePS.Emit(1);
            startParticles.Emit(startParticlesCount);
        }

        anim.SetBool("Fire", Input.GetMouseButton(0));

        if (Input.GetMouseButtonDown(1))
        {
            anim.SetBool("Fire", true);
            startWavePS.Emit(1);
            startParticles.Emit(startParticlesCount);
            Instantiate(laserShotPrefab, laserShotPosition.position, transform.rotation);
        }
        #endif
    }

    // Raycasting and positioning Cursor GameObject at Collision point
    void FixedUpdate () {  
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        Vector3 screenPos = Vector3.zero;
        if (mouse != null)
        {
            screenPos = mouse.position.ReadValue();
        }
        else
        {
            // fallback to center if mouse is not available
            screenPos = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        }
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
#else
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
#endif
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            mouseWorldPosition = hit.point;
        }

        Quaternion toRotation = Quaternion.LookRotation(mouseWorldPosition - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, speed * Time.deltaTime);

    }
}
