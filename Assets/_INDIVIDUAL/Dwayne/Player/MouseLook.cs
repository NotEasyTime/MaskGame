using UnityEngine;
using UnityEngine.InputSystem;
using Managers;

namespace Player
{
     public class MouseLook : MonoBehaviour
    {
        [Header("Sensitivity Settings")]
        [Tooltip("Master sensitivity multiplier")]
        public float masterSensitivity = 1f;

        [Tooltip("Horizontal look sensitivity")]
        public float sensitivityX = 20f;

        [Tooltip("Vertical look sensitivity")]
        public float sensitivityY = 20f;

        [Tooltip("DPI scaling factor (set to your mouse DPI / 800)")]
        public float dpiScale = 1f;

        [Header("Look Settings")]
        [Tooltip("Invert Y-axis")]
        public bool invertY = false;

        [Tooltip("Maximum vertical look angle")]
        public float maxVerticalAngle = 90f;

        [Header("FOV Kick Settings")]
        [Tooltip("Enable speed-based FOV changes")]
        public bool enableFOVKick = true;

        [Tooltip("Base FOV")]
        public float baseFOV = 90f;

        [Tooltip("Maximum FOV increase from speed")]
        public float maxFOVIncrease = 20f;

        [Tooltip("Speed required for max FOV (units/second)")]
        public float speedForMaxFOV = 30f;

        [Tooltip("FOV change smoothing speed")]
        public float fovSmoothSpeed = 5f;

        [Header("References")]
        public Transform playerBody;
        public Camera playerCamera;

        private Vector2 lookInput;
        private float rotationX = 0f;
        private float currentFOV;
        private Rigidbody rb;
        private const float FOV_UPDATE_THRESHOLD = 0.01f;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerCamera == null)
                playerCamera = Camera.main;

            if (playerBody == null)
                playerBody = transform.parent != null ? transform.parent : transform;

            currentFOV = baseFOV;
            if (playerCamera != null)
                playerCamera.fieldOfView = baseFOV;

            rb = playerBody.GetComponent<Rigidbody>();
        }

        public void OnLook(InputValue value)
        {
            if (GameManager.Instance != null && GameManager.Instance.isPaused) return;
            lookInput = value.Get<Vector2>();
        }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.isPaused)
            {
                lookInput = Vector2.zero;
                return;
            }
            HandleMouseLook();

            if (enableFOVKick && playerCamera != null)
                HandleFOVKick();
        }

        void HandleMouseLook()
        {
            float mouseX = lookInput.x * (sensitivityX * 0.1f) * masterSensitivity * dpiScale;
            float mouseY = lookInput.y * (sensitivityY * 0.1f) * masterSensitivity * dpiScale;

            if (invertY)
                mouseY = -mouseY;

            // Rotate camera vertically
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -maxVerticalAngle, maxVerticalAngle);
            transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

            // Rotate player body horizontally
            playerBody.Rotate(Vector3.up * mouseX);
        }

        void HandleFOVKick()
        {
            if (rb != null)
            {
                // Calculate horizontal speed without allocation
                Vector3 velocity = rb.linearVelocity;
                float horizontalSpeed = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);

                float speedRatio = Mathf.Clamp01(horizontalSpeed / speedForMaxFOV);
                float targetFOV = baseFOV + (maxFOVIncrease * speedRatio);

                float newFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovSmoothSpeed);

                // Only update camera if change is significant
                if (Mathf.Abs(newFOV - currentFOV) > FOV_UPDATE_THRESHOLD)
                {
                    currentFOV = newFOV;
                    playerCamera.fieldOfView = currentFOV;
                }
            }
        }

        public void SetSensitivity(float x, float y)
        {
            sensitivityX = x;
            sensitivityY = y;
        }

        public void SetMasterSensitivity(float value)
        {
            masterSensitivity = value;
        }
    }   
}