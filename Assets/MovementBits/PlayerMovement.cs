using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 45f;
    public float sprintSpeed = 80f; 
    public float maxWalkSpeed = 15f;
    public float maxSprintSpeed = 22f; 
    public float groundDrag = 6f;
    
    [Header("Momentum Settings")]
    // How fast the player slows down when pressing the opposite direction
    public float counterInputForce = 40f; 
    public float airMultiplier = 0.3f;
    public float strafeIntensity = 2f; 

    [Header("Jumping & Gravity")]
    public float jumpForce = 12f;
    public float gravity = -30f; 
    
    [Header("Camera & FOV")]
    public Camera playerCamera;
    public float baseFOV = 90f;
    public float sprintFOV = 105f;
    public float fovLerpSpeed = 10f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isSprinting; 
    private bool isGrounded;
    private float landingDelay;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = false; 

        PhysicsMaterial frictionless = new PhysicsMaterial("Frictionless") {
            staticFriction = 0f,
            dynamicFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
        GetComponent<Collider>().material = frictionless;
    }
    
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnJump(InputValue value) { if (value.isPressed && isGrounded) Jump(); }
    public void OnSprint(InputValue value) => isSprinting = value.isPressed;

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.6f) 
            {
                if (!isGrounded) landingDelay = 0.15f; 
                isGrounded = true;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision) => isGrounded = false;

    void Update()
    {
        if (landingDelay > 0) landingDelay -= Time.deltaTime;
        HandleFOV();
    }

    void FixedUpdate()
    {
        ApplyDrag();
        MovePlayer();
        ApplyCustomGravity();
    }

    void ApplyDrag()
    {
        // Drag only applies if we aren't touching keys
        if (isGrounded && landingDelay <= 0 && moveInput.magnitude < 0.1f)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0.05f; // Very low damping to allow sliding
        }
    }

    void MovePlayer()
    {
        Vector3 moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;
        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentMax = isSprinting ? maxSprintSpeed : maxWalkSpeed;

        if (moveInput.magnitude > 0)
        {
            // --- THE KEY CHANGE ---
            // Check if we are trying to move in the opposite direction of our current velocity
            float dot = Vector3.Dot(currentHorizontalVel.normalized, moveDir.normalized);

            if (dot < -0.1f) // We are pressing a "Counter" key (like S while moving forward)
            {
                // Apply a steady braking force instead of a velocity snap
                rb.AddForce(moveDir.normalized * counterInputForce, ForceMode.Acceleration);
            }
            else
            {
                // Normal acceleration logic
                float projection = Vector3.Dot(currentHorizontalVel, moveDir.normalized);
                if (projection < currentMax)
                {
                    float multiplier = isGrounded ? 1f : airMultiplier;
                    rb.AddForce(moveDir.normalized * (isSprinting ? sprintSpeed : walkSpeed) * multiplier, ForceMode.Force);
                }
            }

            if (!isGrounded && currentHorizontalVel.magnitude > 0.1f)
            {
                ApplyAirStrafing(moveDir, currentHorizontalVel);
            }
        }
    }

    void ApplyAirStrafing(Vector3 moveDir, Vector3 currentHorizontalVel)
    {
        // If we are pressing S (dot < 0), we skip air strafing so it doesn't fight the momentum
        float dot = Vector3.Dot(currentHorizontalVel.normalized, moveDir.normalized);
        if (dot < 0) return;

        Vector3 targetVelocity = moveDir * currentHorizontalVel.magnitude;
        Vector3 velocityDiff = targetVelocity - currentHorizontalVel;
        rb.AddForce(velocityDiff * strafeIntensity, ForceMode.Acceleration);
    }

    void HandleFOV()
    {
        if (playerCamera == null) return;
        float targetFOV = (isSprinting && moveInput.magnitude > 0.1f) ? sprintFOV : baseFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);
    }

    void ApplyCustomGravity() => rb.AddForce(Vector3.up * gravity, ForceMode.Acceleration);

    void Jump()
    {
        isGrounded = false;
        landingDelay = 0.1f;
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
}