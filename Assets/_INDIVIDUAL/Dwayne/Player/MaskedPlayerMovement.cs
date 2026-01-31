using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
     [RequireComponent(typeof(Rigidbody))]
    public class MaskedPlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public Transform orientation;
        public float walkSpeed = 20f;
        public float sprintSpeed = 30f;
        public float maxWalkSpeed = 25f;
        public float maxSprintSpeed = 40f;
        public float hardSpeedCap = 100f;
        public float stopSpeed = 6f;           // minimum speed used for friction math
        
        [Header("Movement Settings")]
        public float playerHeight = 2f;
        public LayerMask groundLayer;
        public float groundDrag = 6f;
        public float groundAccel = 120f;
        public float groundFriction = 14f;     // how fast you slow down when grounded

        [Header("Slide Settings")]
        public float slideSpeed = 35f;
        public float slideSpeedBoost = 1.5f;
        public float slideDuration = 1.2f;
        public float slideCooldown = 0.5f;
        public float slideControlMultiplier = 0.6f;

        [Header("Dash Settings")]
        public float dashForce = 25f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 1f;
        public int maxDashes = 2;

        [Header("Wall Jump Settings")]
        public float wallJumpForce = 15f;
        public float wallJumpUpwardForce = 12f;
        public float wallCheckDistance = 0.7f;
        public LayerMask wallLayer;

        [Header("Ground Slam Settings")]
        public float slamForce = 50f;
        public float slamRadius = 5f;
        public bool canSlamInAir = true;

        [Header("Air Control")]
        public float airControl = 0.25f;       // 0 = none, 0.3 = snappy
        public float airMultiplier = 0.8f; // Higher than normal for Ultrakill feel
        public float airAccel = 60f;
        public float airMaxSpeed = 35f;        // caps air accel contribution, not total speed
        public float strafeIntensity = 3f;
        public float counterInputForce = 50f;

        [Header("Jumping & Gravity")]
        public float jumpForce = 15f;
        public float jumpCooldown = 0.2f;
        public float gravity = -35f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;

        [Header("Speed Preservation")]
        public float speedPreservationFactor = 0.65f; // Preserve momentum on landing
        public float bhopSpeedBonus = 1.1f; // Small speed boost for perfect bhops

        [Header("Landing Prediction")]
        [Tooltip("Enable landing prediction (for UI/gameplay features)")]
        public bool enableLandingPrediction = false;

        [Tooltip("Maximum time to simulate trajectory (seconds)")]
        public float maxPredictionTime = 2f;

        [Tooltip("Time step for prediction simulation (smaller = more accurate but slower)")]
        public float predictionTimeStep = 0.05f;

        [Tooltip("How far to raycast down when checking for landing")]
        public float predictionGroundCheckDist = 2f;

        private Rigidbody rb;
        private Vector2 moveInput;
        private Vector3 moveDirection;
        private bool isSprinting;
        private bool isSliding;
        private bool isDashing;
        private bool isSlamming;
        private bool isGrounded;
        private bool canWallJump;
        private bool canJump;

        private float coyoteCounter;
        private float jumpBufferCounter;
        private float slideTimer;
        private float slideCooldownTimer;
        private float dashTimer;
        private float dashCooldownTimer;
        private int dashesRemaining;
        private int wallCheckFrameCounter = 0;

        private Vector3 wallNormal;
        private Vector3 lastGroundVelocity;
        private float landingDelay;
        private bool jumpHeld;
        private float jumpCooldownTimer;

        // Landing prediction
        private Vector3 predictedLandingPoint;
        private float predictedLandingTime;
        private bool hasPredictedLanding;
        private int predictionFrameCounter = 0;
        private int groundContacts;
        private const int PREDICTION_UPDATE_INTERVAL = 3;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            PhysicsMaterial frictionless = new PhysicsMaterial("Frictionless")
            {
                staticFriction = 0f,
                dynamicFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };
            GetComponent<Collider>().material = frictionless;

            dashesRemaining = maxDashes;
            canJump = true;
        }
        
        void Update()
        {
            CheckWallContact();

            // Update landing prediction when airborne
            if (enableLandingPrediction && !isGrounded)
                UpdateLandingPrediction();

            if (isGrounded)
                rb.linearDamping = groundDrag;
            else
                rb.linearDamping = 0.05f;

            // Jump logic - hold to jump repeatedly
            if (jumpHeld && isGrounded && canJump && !isSliding)
            {
                Jump();
                canJump = false;
                jumpCooldownTimer = jumpCooldown;
            }
            // Jump buffer logic (for single press jumps)
            else if (jumpBufferCounter > 0 && coyoteCounter > 0 && !isSliding)
            {
                Jump();
            }

            // Update jump cooldown
            if (jumpCooldownTimer > 0)
            {
                jumpCooldownTimer -= Time.deltaTime;
                if (jumpCooldownTimer <= 0)
                    canJump = true;
            }
            if (!jumpHeld && rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.up * (gravity * 0.5f), ForceMode.Acceleration); // Extra fall gravity when releasing jump
            }
        }

        void FixedUpdate()
        {
            if (isDashing) { HandleDash(); return; }
            if (isSlamming) { HandleGroundSlam(); return; }
            
            UpdateTimers();
            
            Vector3 wishDir = GetWishDir();

            float dt = Time.fixedDeltaTime;
            ApplyCustomGravity();
            
            if (isGrounded)
            {
                ApplyGroundFriction(dt);

                float wishSpeed = isSprinting ? sprintSpeed : walkSpeed;
                if (isSliding) wishSpeed = slideSpeed;

                Accelerate(wishDir, wishSpeed, groundAccel, dt);
            }
            else
            {
                // Air accel: capped contribution so you can’t instantly “turn” 90 degrees at 200 speed
                float wishSpeed = (isSprinting ? sprintSpeed : walkSpeed) * airMultiplier;
                wishSpeed = Mathf.Min(wishSpeed, airMaxSpeed);

                Accelerate(wishDir, wishSpeed, airAccel, dt);
                AirControl(wishDir, wishSpeed, dt);
            }

            ApplyDrag();
            MovePlayer();
            EnforceSpeedCap();
            
            groundContacts = 0;
            
            bool groundedNow = CheckGrounded();

            if (groundedNow && !isGrounded)
                OnLanding();

            isGrounded = groundedNow;

        }
        
        private void LateUpdate()
        {
            isGrounded = groundContacts > 0;
        }

        void UpdateTimers()
        {
            if (landingDelay > 0) landingDelay -= Time.deltaTime;
            if (!isGrounded) coyoteCounter -= Time.deltaTime;
            if (jumpBufferCounter > 0) jumpBufferCounter -= Time.deltaTime;
            if (slideCooldownTimer > 0) slideCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;

            if (isSliding)
            {
                slideTimer -= Time.deltaTime;
                if (slideTimer <= 0)
                    EndSlide();
            }

            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0)
                    EndDash();
            }
        }
        
        Vector3 GetWishDir()
        {
            Vector3 wish = orientation.forward * moveInput.y + orientation.right * moveInput.x;
            wish.y = 0f;
            return wish.sqrMagnitude > 0.0001f ? wish.normalized : Vector3.zero;
        }

        public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

        public void OnJump(InputValue value)
        {
            jumpHeld = value.isPressed;

            if (!value.isPressed) return;
            jumpBufferCounter = jumpBufferTime;

            // Wall jump check
            if (!isGrounded && canWallJump)
            {
                WallJump();
            }
        }

        public void OnSprint(InputValue value) => isSprinting = value.isPressed;

        public void OnSlide(InputValue value)
        {
            if (value.isPressed && isGrounded && slideCooldownTimer <= 0 && !isSliding)
            {
                StartSlide();
            }
        }

        public void OnDash(InputValue value)
        {
            if (value.isPressed && dashCooldownTimer <= 0 && dashesRemaining > 0 && !isDashing)
            {
                StartDash();
            }
        }

        public void OnSlam(InputValue value)
        {
            if (value.isPressed && !isGrounded && canSlamInAir && !isSlamming)
            {
                StartGroundSlam();
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.6f)
                {
                    if (groundContacts == 0 && !isGrounded)
                    {
                        landingDelay = 0.1f;
                        OnLanding();
                    }

                    groundContacts++;
                    isGrounded = true;
                    coyoteCounter = coyoteTime;
                    dashesRemaining = maxDashes;
                    return;
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            isGrounded = false;
        }

        static Vector3 Horizontal(Vector3 v) => new Vector3(v.x, 0f, v.z);

        void ApplyGroundFriction(float dt)
        {
            if (!isGrounded || isSliding) return;

            Vector3 v = rb.linearVelocity;
            Vector3 hv = Horizontal(v);
            float sqrSpeed = hv.sqrMagnitude;

            if (sqrSpeed < 0.0001f) return; // 0.01f * 0.01f

            float speed = Mathf.Sqrt(sqrSpeed);

            // Friction amount this frame
            float control = Mathf.Max(speed, stopSpeed);
            float drop = control * groundFriction * dt;

            float newSpeed = Mathf.Max(speed - drop, 0f);
            if (!Mathf.Approximately(newSpeed, speed))
            {
                hv *= (newSpeed / speed);
                rb.linearVelocity = new Vector3(hv.x, v.y, hv.z);
            }
        }

        void Accelerate(Vector3 wishDir, float wishSpeed, float accel, float dt)
        {
            if (wishDir == Vector3.zero) return;

            Vector3 v = rb.linearVelocity;
            Vector3 hv = Horizontal(v);

            float currentSpeed = Vector3.Dot(hv, wishDir);
            float addSpeed = wishSpeed - currentSpeed;
            if (addSpeed <= 0f) return;

            float accelSpeed = accel * wishSpeed * dt;
            if (accelSpeed > addSpeed) accelSpeed = addSpeed;

            hv += wishDir * accelSpeed;
            rb.linearVelocity = new Vector3(hv.x, v.y, hv.z);
        }

        void AirControl(Vector3 wishDir, float wishSpeed, float dt)
        {
            // Optional but very "arena shooter"
            if (wishDir == Vector3.zero) return;

            Vector3 v = rb.linearVelocity;
            if (Mathf.Abs(v.y) < 0.001f) return;

            Vector3 hv = Horizontal(v);
            float sqrSpeed = hv.sqrMagnitude;
            if (sqrSpeed < 0.01f) return; // 0.1f * 0.1f

            // Calculate speed and normalize in one pass
            float speed = Mathf.Sqrt(sqrSpeed);
            hv /= speed; // Normalize in place, reusing speed calculation

            // Only when moving forward-ish
            float dot = Vector3.Dot(hv, wishDir);
            if (dot <= 0f) return;

            float k = airControl * dot * dot * dt * 32f; // 32f is the classic "feel" scaler
            Vector3 newHv = hv * speed + wishDir * k;
            float newSqrSpeed = newHv.sqrMagnitude;
            if (newSqrSpeed > 0.0001f)
            {
                newHv *= speed / Mathf.Sqrt(newSqrSpeed); // Normalize and scale in one operation
            }

            rb.linearVelocity = new Vector3(newHv.x, v.y, newHv.z);
        }


        void CheckWallContact()
        {
            // Early exit if grounded
            if (isGrounded)
            {
                canWallJump = false;
                return;
            }

            // Throttle checks to every 3 frames
            wallCheckFrameCounter++;
            if (wallCheckFrameCounter < 3)
                return;

            wallCheckFrameCounter = 0;

            // Check directions without allocating array
            RaycastHit hit;
            Vector3 pos = transform.position;

            // Check forward
            if (Physics.Raycast(pos, transform.forward, out hit, wallCheckDistance, wallLayer))
            {
                canWallJump = true;
                wallNormal = hit.normal;
                return;
            }

            // Check backward
            if (Physics.Raycast(pos, -transform.forward, out hit, wallCheckDistance, wallLayer))
            {
                canWallJump = true;
                wallNormal = hit.normal;
                return;
            }

            // Check right
            if (Physics.Raycast(pos, transform.right, out hit, wallCheckDistance, wallLayer))
            {
                canWallJump = true;
                wallNormal = hit.normal;
                return;
            }

            // Check left
            if (Physics.Raycast(pos, -transform.right, out hit, wallCheckDistance, wallLayer))
            {
                canWallJump = true;
                wallNormal = hit.normal;
                return;
            }

            canWallJump = false;
        }
        
        bool CheckGrounded()
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            float radius = 0.45f;
            float distance = 0.3f;

            return Physics.SphereCast(
                origin,
                radius,
                Vector3.down,
                out RaycastHit hit,
                distance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            ) && hit.normal.y > 0.6f;
        }


        void UpdateLandingPrediction()
        {
            // Throttle updates to every N frames for performance
            predictionFrameCounter++;
            if (predictionFrameCounter < PREDICTION_UPDATE_INTERVAL)
                return;

            predictionFrameCounter = 0;
            PredictLanding();
        }

        void PredictLanding()
        {
            hasPredictedLanding = false;
            predictedLandingTime = 0f;

            Vector3 pos = transform.position;
            Vector3 vel = rb.linearVelocity;
            Vector3 gravityVec = new Vector3(0, gravity, 0);

            float time = 0f;

            // Simulate trajectory using kinematic equations
            while (time < maxPredictionTime)
            {
                time += predictionTimeStep;

                // Apply gravity and update velocity/position
                vel += gravityVec * predictionTimeStep;
                pos += vel * predictionTimeStep;

                // Only check for ground when moving downward
                if (vel.y < 0)
                {
                    // Raycast down to check for ground
                    if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, predictionGroundCheckDist, groundLayer))
                    {
                        predictedLandingPoint = hit.point;
                        predictedLandingTime = time;
                        hasPredictedLanding = true;
                        return;
                    }
                }

                // Early exit if fell too far (likely fell off map)
                if (pos.y < transform.position.y - 200f)
                    return;
            }
        }

        void ApplyDrag()
        {
            if (isGrounded && landingDelay <= 0 && moveInput.magnitude < 0.1f && !isSliding)
                rb.linearDamping = groundDrag;
            else
                rb.linearDamping = 0.05f;
        }

        void MovePlayer()
        {
            Vector3 moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;
            Vector3 currentHorizontalVel = Horizontal(rb.linearVelocity);

            float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            float currentMax = isSprinting ? maxSprintSpeed : maxWalkSpeed;

            if (isSliding)
            {
                targetSpeed = slideSpeed;
                currentMax *= slideSpeedBoost;
            }

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
                        float controlMultiplier = isSliding ? slideControlMultiplier : 1f;
                        Vector3 newVel = moveDirNormalized * (targetSpeed * controlMultiplier);
                        rb.linearVelocity = new Vector3(newVel.x, rb.linearVelocity.y, newVel.z);
                    }
                }
                else
                {
                    float projection = Vector3.Dot(currentHorizontalVel, moveDirNormalized);
                    if (projection < currentMax)
                        rb.AddForce(moveDirNormalized * (targetSpeed * 2f * airMultiplier), ForceMode.Force);
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

        void ApplyCustomGravity()
        {
            if (!isGrounded)
                rb.AddForce(Vector3.up * gravity, ForceMode.Acceleration);
        }

        void EnforceSpeedCap()
        {
            Vector3 vel = rb.linearVelocity;
            float sqrHorizontalSpeed = vel.x * vel.x + vel.z * vel.z;
            float sqrCap = hardSpeedCap * hardSpeedCap;

            if (sqrHorizontalSpeed > sqrCap)
            {
                float horizontalSpeed = Mathf.Sqrt(sqrHorizontalSpeed);
                float scale = hardSpeedCap / horizontalSpeed;
                rb.linearVelocity = new Vector3(vel.x * scale, vel.y, vel.z * scale);
            }
        }

        void Jump()
        {
            // Store pre-jump horizontal velocity for bhop
            Vector3 vel = rb.linearVelocity;
            float preJumpHorizontalX = vel.x;
            float preJumpHorizontalZ = vel.z;

            rb.linearVelocity = new Vector3(vel.x, 0f, vel.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

            // Bhop bonus: small speed boost for jumping right after landing
            if (landingDelay > 0)
            {
                float sqrHorizontalSpeed = preJumpHorizontalX * preJumpHorizontalX + preJumpHorizontalZ * preJumpHorizontalZ;
                if (sqrHorizontalSpeed > walkSpeed * walkSpeed)
                {
                    float boostedX = preJumpHorizontalX * bhopSpeedBonus;
                    float boostedZ = preJumpHorizontalZ * bhopSpeedBonus;
                    rb.linearVelocity = new Vector3(boostedX, rb.linearVelocity.y, boostedZ);
                }
            }

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            isGrounded = false;
            landingDelay = 0f;
        }
        
        private void ResetJump()
        {
            canJump = true;
        }

        void WallJump()
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            // Jump away from wall and upward
            Vector3 wallJumpDirection = wallNormal + Vector3.up;
            rb.AddForce(wallJumpDirection.normalized * wallJumpForce, ForceMode.Impulse);
            rb.AddForce(Vector3.up * wallJumpUpwardForce, ForceMode.Impulse);

            canWallJump = false;
            jumpBufferCounter = 0f;
        }

        void StartSlide()
        {
            isSliding = true;
            slideTimer = slideDuration;

            // Boost forward on slide start
            Vector3 slideDirection = transform.forward;
            rb.AddForce(slideDirection * slideSpeed * 0.5f, ForceMode.Impulse);
        }

        void EndSlide()
        {
            isSliding = false;
            slideCooldownTimer = slideCooldown;
        }

        void StartDash()
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashesRemaining--;
            dashCooldownTimer = dashCooldown;

            // Dash in movement direction or forward if no input
            Vector3 dashDirection = moveInput.magnitude > 0.1f
                ? (transform.forward * moveInput.y + transform.right * moveInput.x).normalized
                : transform.forward;

            rb.linearVelocity = Vector3.zero; // Reset velocity for clean dash
            rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);
        }

        void EndDash()
        {
            isDashing = false;
        }

        void HandleDash()
        {
            // Dash maintains velocity, no additional forces
        }

        void StartGroundSlam()
        {
            isSlamming = true;
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(Vector3.down * slamForce, ForceMode.Impulse);
        }

        void HandleGroundSlam()
        {
            if (isGrounded)
            {
                isSlamming = false;
                // Optional: Add explosion/damage radius here
            }
        }

        void SpeedControl()
        {
            float flatVelocity = Mathf.Sqrt(rb.linearVelocity.x * rb.linearVelocity.x + rb.linearVelocity.z * rb.linearVelocity.z);
            float maxSpeed = isSprinting ? maxSprintSpeed : maxWalkSpeed;
            if (flatVelocity > maxSpeed)
            {
                float limitedX = rb.linearVelocity.x * maxSpeed / flatVelocity;
                float limitedZ = rb.linearVelocity.z * maxSpeed / flatVelocity;
                rb.linearVelocity = new Vector3(limitedX, rb.linearVelocity.y, limitedZ);
            }
        }

        void OnLanding()
        {
            // Preserve horizontal momentum on landing (Ultrakill-style)
            Vector3 v = rb.linearVelocity;
            Vector3 hv = Horizontal(v);

            bool hasInput = moveInput.sqrMagnitude > 0.01f;

            if (!hasInput && !isSliding)
            {
                // Kill most horizontal drift when you land without input
                hv *= 0.35f;
            }
            else
            {
                // Preserve tech momentum
                hv *= speedPreservationFactor; // keep your tuning
            }

            rb.linearVelocity = new Vector3(hv.x, v.y, hv.z);
        }

        // Public getters for UI/other systems
        public bool IsSliding() => isSliding;
        public bool IsDashing() => isDashing;
        public int GetDashesRemaining() => dashesRemaining;
        public float GetCurrentSpeed()
        {
            Vector3 vel = rb.linearVelocity;
            return Mathf.Sqrt(vel.x * vel.x + vel.z * vel.z);
        }

        // Landing prediction getters
        public Vector3 GetPredictedLandingPoint() => predictedLandingPoint;
        public float GetPredictedLandingTime() => predictedLandingTime;
        public bool HasPredictedLanding() => hasPredictedLanding && !isGrounded;
    }
}