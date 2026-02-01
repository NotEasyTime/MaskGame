using UnityEngine;
using UnityEngine.InputSystem;
using Interfaces;
using Managers;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 15f;
    public float sprintSpeed = 22f;
    public float maxWalkSpeed = 15f;
    public float maxSprintSpeed = 22f; 
    public float groundDrag = 6f;
    
    [Header("Momentum Settings")]
    public float counterInputForce = 40f; 
    public float airMultiplier = 0.3f;
    public float strafeIntensity = 2f; 

    [Header("Jumping & Gravity")]
    public float jumpForce = 12f;
    public float gravity = -30f; 
    public float coyoteTime = 0.15f; 
    public float jumpBufferTime = 0.15f;

    [Header("Audio")]
    [Tooltip("Play when grounded and moving (footsteps)")]
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float footstepInterval = 0.4f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isSprinting; 
    private bool isGrounded;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float landingDelay;
    private bool isDead;
    private IDamagable damagable;
    private float lastFootstepTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        damagable = GetComponent<IDamagable>();
        if (damagable != null)
            damagable.OnDeath += OnPlayerDeath;
        rb.freezeRotation = true;
        rb.useGravity = false; 
        
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        PhysicsMaterial frictionless = new PhysicsMaterial("Frictionless") {
            staticFriction = 0f,
            dynamicFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
        GetComponent<Collider>().material = frictionless;
    }
    
    public void OnMove(InputValue value) => moveInput = isDead ? Vector2.zero : value.Get<Vector2>();
    public void OnJump(InputValue value) { if (!isDead && value.isPressed) jumpBufferCounter = jumpBufferTime; }
    public void OnSprint(InputValue value) => isSprinting = !isDead && value.isPressed;

    /// <summary>
    /// Called by Input System when the Pause action (e.g. Escape) is pressed. Toggles pause and PauseMenu in game.
    /// </summary>
    public void OnPause(InputValue value)
    {
        if (value.isPressed)
            GameManager.Instance?.TogglePauseInGame();
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.6f) 
            {
                if (!isGrounded) landingDelay = 0.15f; 
                isGrounded = true;
                coyoteCounter = coyoteTime;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision) => isGrounded = false;

    private void OnDestroy()
    {
        if (damagable != null)
            damagable.OnDeath -= OnPlayerDeath;
    }

    private void OnPlayerDeath()
    {
        isDead = true;
        moveInput = Vector2.zero;
    }

    void Update()
    {
        if (isDead) return;
        if (landingDelay > 0) landingDelay -= Time.deltaTime;

        // Footsteps: grounded + moving
        if (footstepSound != null && SoundManager.Instance != null && isGrounded && landingDelay <= 0
            && moveInput.magnitude > 0.1f && Time.time - lastFootstepTime >= footstepInterval)
        {
            SoundManager.Instance.PlaySFX(footstepSound, 0.6f);
            lastFootstepTime = Time.time;
        }

        // Update Coyote Time
        if (!isGrounded) coyoteCounter -= Time.deltaTime;

        // Update Jump Buffer
        if (jumpBufferCounter > 0) jumpBufferCounter -= Time.deltaTime;

        // Execute Jump if conditions are met
        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }
        ApplyDrag();
        MovePlayer();
        ApplyCustomGravity();
    }

    void ApplyDrag()
    {
        if (isGrounded && landingDelay <= 0 && moveInput.magnitude < 0.1f)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0.05f; 
    }

    void MovePlayer()
    {
        Vector3 moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;
        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        float currentMax = isSprinting ? maxSprintSpeed : maxWalkSpeed;

        if (moveInput.magnitude > 0)
        {
            Vector3 moveDirNormalized = moveDir.normalized;
            Vector3 currentHorizontalVelNormalized = currentHorizontalVel.normalized;

            float dot = Vector3.Dot(currentHorizontalVelNormalized, moveDirNormalized);

            if (dot < -0.1f)
                rb.AddForce(moveDirNormalized * counterInputForce, ForceMode.Acceleration);
            else if (isGrounded)
            {
                if (currentHorizontalVel.magnitude <= currentMax + 0.1f)
                {
                    Vector3 newVel = moveDirNormalized * targetSpeed;
                    rb.linearVelocity = new Vector3(newVel.x, rb.linearVelocity.y, newVel.z);
                }
            }
            else
            {
                float projection = Vector3.Dot(currentHorizontalVel, moveDirNormalized);
                if (projection < currentMax)
                    rb.AddForce(moveDirNormalized * targetSpeed * 2f * airMultiplier, ForceMode.Force);
            }

            if (!isGrounded && currentHorizontalVel.magnitude > 0.1f)
                ApplyAirStrafing(moveDirNormalized, currentHorizontalVelNormalized, currentHorizontalVel);
        }
    }

    void ApplyAirStrafing(Vector3 moveDirNormalized, Vector3 currentHorizontalVelNormalized, Vector3 currentHorizontalVel)
    {
        float dot = Vector3.Dot(currentHorizontalVelNormalized, moveDirNormalized);
        if (dot < 0) return;

        Vector3 targetVelocity = moveDirNormalized * currentHorizontalVel.magnitude;
        Vector3 velocityDiff = targetVelocity - currentHorizontalVel;
        rb.AddForce(velocityDiff * strafeIntensity, ForceMode.Acceleration);
    }

    void ApplyCustomGravity() => rb.AddForce(Vector3.up * gravity, ForceMode.Acceleration);

    void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // Clear vertical velocity for consistent jump height
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        
        // Reset counters to prevent double-jumping
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        isGrounded = false;
        landingDelay = 0.1f;
    }
}