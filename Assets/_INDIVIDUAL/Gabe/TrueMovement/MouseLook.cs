using UnityEngine;
using UnityEngine.InputSystem;
using Managers;

public class MouseLook : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot; 
    public Transform playerBody;

    [Header("Settings")]
    public float mouseSensitivity = 0.1f;
    [Range(0, 0.99f)] public float positionSmoothing = 0.2f; 
    public float eyeHeight = 1.6f;
    
    private float xRotation = 0f;
    private float yRotation = 0f;
    private Vector2 lookBuffer;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        if (cameraPivot != null)
        {
            cameraPivot.SetParent(null);
            yRotation = playerBody.eulerAngles.y;
        }
    }

    public void OnLook(InputValue value)
    {
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;
        lookBuffer += value.Get<Vector2>();
    }

    void LateUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.isPaused)
        {
            lookBuffer = Vector2.zero;
            return;
        }

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
        
        cameraPivot.position = Vector3.Lerp(cameraPivot.position, targetPos, 1f - positionSmoothing);
    }
}