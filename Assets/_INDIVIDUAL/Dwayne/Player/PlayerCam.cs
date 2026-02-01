using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerCam : MonoBehaviour
    {
        [Header("Sensitivity Settings")]
        [Tooltip("Master sensitivity multiplier")]
        public float masterSensitivity = 1f;

        [Tooltip("Horizontal look sensitivity")]
        public float sensX = 100f;

        [Tooltip("Vertical look sensitivity")]
        public float sensY = 100f;

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
        [Tooltip("Reference to the Player GameObject (parent with Rigidbody)")]
        public Transform orientation;

        private Vector2 lookInput;
        private float xRotation;
        private float yRotation;
        private float currentFOV;
        private Rigidbody rb;
        private Camera cam;
        private const float FOV_UPDATE_THRESHOLD = 0.01f;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Get camera component since this script is on the camera
            cam = GetComponent<Camera>();

            currentFOV = baseFOV;
            if (cam != null)
                cam.fieldOfView = baseFOV;

            // Try to find Rigidbody on orientation (player body)
            if (orientation != null)
                rb = orientation.GetComponent<Rigidbody>();
        }

        public void OnLook(InputValue value)
        {
            lookInput = value.Get<Vector2>();
        }

        private void Update()
        {
            HandleMouseLook();

            if (enableFOVKick && cam != null && rb != null)
                HandleFOVKick();
        }

        void HandleMouseLook()
        {
            // Apply sensitivity with all multipliers
            float mouseX = lookInput.x * sensX * masterSensitivity * dpiScale * Time.deltaTime;
            float mouseY = lookInput.y * sensY * masterSensitivity * dpiScale * Time.deltaTime;

            if (invertY)
                mouseY = -mouseY;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -maxVerticalAngle, maxVerticalAngle);

            // Rotate camera up/down and left/right
            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);

            // Rotate player body left/right (Y-axis only)
            if (orientation != null)
                orientation.rotation = Quaternion.Euler(0, yRotation, 0);
        }

        void HandleFOVKick()
        {
            // Calculate horizontal speed without Vector3 allocation
            Vector3 velocity = rb.linearVelocity;
            float horizontalSpeed = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);

            float speedRatio = Mathf.Clamp01(horizontalSpeed / speedForMaxFOV);
            float targetFOV = baseFOV + (maxFOVIncrease * speedRatio);

            float newFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovSmoothSpeed);

            // Only update camera if change is significant (avoids expensive camera updates)
            if (Mathf.Abs(newFOV - currentFOV) > FOV_UPDATE_THRESHOLD)
            {
                currentFOV = newFOV;
                cam.fieldOfView = currentFOV;
            }
        }

        public void SetSensitivity(float x, float y)
        {
            sensX = x;
            sensY = y;
        }

        public void SetMasterSensitivity(float value)
        {
            masterSensitivity = value;
        }
    }
}