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
    public float airMultiplier = 0.4f;

    [Header("Air Control")]
    public float airBrakeForce = 20f;
    
    [Header("Jumping & Gravity")]
    public float jumpForce = 12f;
    public float gravity = -20f;
    
    [Header("Camera & FOV")]
    public Camera playerCamera;
    public float baseFOV = 90f;
    public float sprintFOV = 105f;
    public float fovLerpSpeed = 10f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isSprinting; 
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = false; 

        if (playerCamera != null) playerCamera.fieldOfView = baseFOV;
    }
    
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnJump(InputValue value) { if (value.isPressed && isGrounded) Jump(); }
    public void OnSprint(InputValue value) => isSprinting = value.isPressed;


    private void OnCollisionStay(Collision collision)
    {
        // Check if we are colliding with something below us (normal.y > 0.5)
        // This prevents "grounding" yourself by walking into a wall
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.6f) 
            {
                isGrounded = true;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }

    // --- Core Logic ---
    void Update()
    {
        rb.linearDamping = isGrounded ? groundDrag : 0f;
        HandleFOV();
    }

    void FixedUpdate()
    {
        MovePlayer();
        SpeedControl();
        ApplyCustomGravity();
        
        // Safety: If no collisions are active, OnCollisionStay won't run.
        // We don't want to be "stuck" grounded if we fly off a ramp.
    }

    void MovePlayer()
    {
        Vector3 moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;
        float currentForce = (isSprinting && moveInput.magnitude > 0) ? sprintSpeed : walkSpeed;

        if (isGrounded)
        {
            rb.AddForce(moveDir.normalized * currentForce, ForceMode.Force);
        }
        else
        {
            Vector3 airForce = moveDir.normalized * currentForce * airMultiplier;
            Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
            if (moveInput.magnitude > 0 && currentHorizontalVel.magnitude > 0.1f)
            {
                float lookComparison = Vector3.Dot(currentHorizontalVel.normalized, moveDir.normalized);
                if (lookComparison < 0) airForce += moveDir.normalized * airBrakeForce;
            }

            rb.AddForce(airForce, ForceMode.Force);
        }
    }

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentMax = (isSprinting && moveInput.magnitude > 0) ? maxSprintSpeed : maxWalkSpeed;

        if (flatVel.magnitude > currentMax)
        {
            Vector3 limitedVel = flatVel.normalized * currentMax;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    void HandleFOV()
    {
        if (playerCamera == null) return;
        bool isMoving = moveInput.magnitude > 0.1f;
        float targetFOV = (isSprinting && isMoving) ? sprintFOV : baseFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovLerpSpeed);
    }

    void ApplyCustomGravity() => rb.AddForce(Vector3.up * gravity, ForceMode.Acceleration);

    void Jump()
    {
        isGrounded = false; // Manually unset grounded so we don't double jump instantly
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
}