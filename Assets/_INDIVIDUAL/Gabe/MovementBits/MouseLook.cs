using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot; 
    public Transform playerBody;

    [Header("Settings")]
    public float mouseSensitivity = 0.1f;
    [Range(0, 0.99f)] public float positionSmoothing = 0.2f; 
    public float eyeHeight = 1.6f;

    // We store these as private floats to maintain high precision
    private float xRotation = 0f;
    private float yRotation = 0f;
    private Vector2 lookBuffer;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        if (cameraPivot != null)
        {
            cameraPivot.SetParent(null);
            // Initialize rotation values to match current player state
            yRotation = playerBody.eulerAngles.y;
        }
    }

    public void OnLook(InputValue value)
    {
        lookBuffer += value.Get<Vector2>();
    }

    void LateUpdate()
    {
        // 1. INPUT PROCESSING
        float mouseX = lookBuffer.x * mouseSensitivity;
        float mouseY = lookBuffer.y * mouseSensitivity;
        lookBuffer = Vector2.zero; 

        // Accumulate rotation in floats for sub-pixel precision
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        yRotation += mouseX;

        // 2. APPLY SNAPPY ROTATION
        // Apply vertical to pivot, horizontal to body
        cameraPivot.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // 3. SMOOTH POSITION (The "Anti-Jitter" step)
        Vector3 targetPos = playerBody.position + (Vector3.up * eyeHeight);
        
        // If it still feels "grid-like" when moving, lower this lerp speed
        // or set it to 1f (no smoothing) to see if the grid is in the rotation or position
        cameraPivot.position = Vector3.Lerp(cameraPivot.position, targetPos, 1f - positionSmoothing);
    }
}