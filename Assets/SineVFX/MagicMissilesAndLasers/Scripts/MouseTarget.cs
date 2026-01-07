using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MouseTarget : MonoBehaviour {

    public Transform ms;
    public float speed = 1f;
    public ParticleSystem startWavePS;
    public ParticleSystem startParticles;
    public ParticleSystem smallMissiles;
    public int smallMissilesCount = 100;
    public ParticleSystem bigMissileOne;
    public ParticleSystem bigMissileTwo;
    public ParticleSystem bigMissileThree;
    public int bigMissileThreeCount = 6;

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
                startParticles.Emit(smallMissilesCount);
            }

            if (mouse.leftButton.isPressed)
            {
                var em = smallMissiles.emission;
                em.enabled = true;
                anim.SetBool("Fire", true);
            }
            else
            {
                var em = smallMissiles.emission;
                em.enabled = false;
                anim.SetBool("Fire", false);
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                anim.SetBool("Fire", true);
                bigMissileOne.Emit(1);
                if (bigMissileTwo)
                {
                    bigMissileTwo.Emit(1);
                }
                if (bigMissileThree)
                {
                    bigMissileThree.Emit(bigMissileThreeCount);
                }
                startWavePS.Emit(1);
                startParticles.Emit(smallMissilesCount);
            }
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            startWavePS.Emit(1);
            startParticles.Emit(smallMissilesCount);
        }       

        if (Input.GetMouseButton(0))
        {
            var em = smallMissiles.emission;
            em.enabled = true;
            anim.SetBool("Fire", true);
        }
        else
        {
            var em = smallMissiles.emission;
            em.enabled = false;
            anim.SetBool("Fire", false);
        }

        if (Input.GetMouseButtonDown(1))
        {
            anim.SetBool("Fire", true);
            bigMissileOne.Emit(1);
            if (bigMissileTwo)
            {
                bigMissileTwo.Emit(1);
            }                
            if (bigMissileThree)
            {
                bigMissileThree.Emit(bigMissileThreeCount);
            }            
            startWavePS.Emit(1);
            startParticles.Emit(smallMissilesCount);
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
        ms.position = mouseWorldPosition;

    }
}
