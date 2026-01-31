using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 25f;
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        // Lock cursor to the middle of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
    }

    // This method is called by the Player Input component
    public void OnLook(InputValue value)
    {
        Vector2 mouseDelta = value.Get<Vector2>() * mouseSensitivity * Time.deltaTime;

        float mouseX = mouseDelta.x;
        float mouseY = mouseDelta.y;

        // Calculate vertical rotation (clamped to 90 degrees so you don't flip)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply rotations
        cameraTransform.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // Rotate camera up/down
        playerBody.Rotate(Vector3.up * mouseX); // Rotate player body left/right
    }
}