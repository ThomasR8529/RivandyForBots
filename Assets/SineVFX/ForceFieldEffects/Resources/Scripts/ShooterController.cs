using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShooterController : MonoBehaviour {

    public GameObject projectile;
    public float rotationSpeed = 15f;

    private Vector3 mouseWorldPosition;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Instantiate(projectile, this.gameObject.transform.position, this.gameObject.transform.rotation);
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            mouseWorldPosition = hit.point;
        }

        Quaternion toRotation = Quaternion.LookRotation(mouseWorldPosition - transform.position);
        transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);

    }
}
