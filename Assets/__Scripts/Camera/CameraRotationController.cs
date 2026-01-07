using UnityEngine;

public class CameraRotationController : MonoBehaviour {
    public float rotationSpeed = 100f;

    void Update() {
        // Récupère les mouvements de la souris
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        // Applique la rotation de la caméra
        transform.Rotate(Vector3.up, mouseX); // Rotation autour de l'axe Y
        transform.Rotate(Vector3.left, mouseY); // Rotation autour de l'axe X
    }
}