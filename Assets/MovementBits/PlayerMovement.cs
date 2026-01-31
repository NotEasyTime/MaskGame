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
    public float strafeIntensity = 5f; 
    
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
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.6f) 
            {
                isGrounded = true;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision) => isGrounded = false;

    void Update()
    {
        // MOMENTUM PRESERVATION: 
        // If we are grounded but moving/pressing keys, reduce drag to allow sliding/momentum.
        // If we stop pressing keys, apply full drag to stop the player.
        if (isGrounded)
        {
            rb.linearDamping = (moveInput.magnitude > 0.1f) ? groundDrag * 0.2f : groundDrag;
        }
        else
        {
            rb.linearDamping = 0f;
        }

        HandleFOV();
    }

    void FixedUpdate()
    {
        MovePlayer();
        SpeedControl();
        ApplyCustomGravity();
    }

    void MovePlayer()
    {
        Vector3 moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;
        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentMax = (isSprinting && moveInput.magnitude > 0) ? maxSprintSpeed : maxWalkSpeed;

        if (isGrounded)
        {
            // Allow force application only if we aren't already exceeding the speed cap in that direction
            float speedInInputDirection = Vector3.Dot(currentHorizontalVel, moveDir.normalized);
            
            if (speedInInputDirection < currentMax)
            {
                rb.AddForce(moveDir.normalized * (isSprinting ? sprintSpeed : walkSpeed), ForceMode.Force);
            }
        }
        else
        {
            // Air Logic
            float speedInInputDirection = Vector3.Dot(currentHorizontalVel, moveDir.normalized);
            if (speedInInputDirection < currentMax)
            {
                rb.AddForce(moveDir.normalized * walkSpeed * airMultiplier, ForceMode.Force);
            }

            if (moveInput.magnitude > 0 && currentHorizontalVel.magnitude > 0.1f)
            {
                ApplyAirStrafing(moveDir, currentHorizontalVel);
            }
        }
    }

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentMax = (isSprinting && moveInput.magnitude > 0) ? maxSprintSpeed : maxWalkSpeed;

        // MOMENTUM PRESERVATION:
        // Only hard-clamp speed if the player is NOT pressing any input.
        // If they ARE pressing input, we let them keep their excess speed (e.g., from a fall or explosion).
        if (moveInput.magnitude == 0 && flatVel.magnitude > currentMax)
        {
            Vector3 brakeForce = -flatVel.normalized * groundDrag;
            rb.AddForce(brakeForce, ForceMode.Acceleration);
        }
    }

    void ApplyAirStrafing(Vector3 moveDir, Vector3 currentHorizontalVel)
    {
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
        // Notice: We don't zero out horizontal velocity here, preserving the speed you had.
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
}