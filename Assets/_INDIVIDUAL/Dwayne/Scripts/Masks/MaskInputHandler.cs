using UnityEngine;
using UnityEngine.InputSystem;

namespace Dwayne.Masks
{
    /// <summary>
    /// Handles input for mask abilities.
    /// Connects Unity Input System to MaskManager.
    /// Supports hold detection for charged abilities.
    /// </summary>
    [RequireComponent(typeof(MaskManager))]
    public class MaskInputHandler : MonoBehaviour
    {
        [Header("Hold Detection")]
        [Tooltip("Time before input is considered a hold rather than a tap")]
        [SerializeField] private float holdThreshold = 0.15f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private MaskManager maskManager;

        // Combat ability hold tracking
        private bool isCombatPressed = false;
        private bool isCombatHolding = false;
        private float combatPressTime = 0f;

        // Movement ability hold tracking
        private bool isMovementPressed = false;
        private bool isMovementHolding = false;
        private float movementPressTime = 0f;

        // Alt combat ability hold tracking
        private bool isAltCombatPressed = false;
        private bool isAltCombatHolding = false;
        private float altCombatPressTime = 0f;

        /// <summary>Is combat ability button currently held?</summary>
        public bool IsCombatHeld => isCombatPressed;

        /// <summary>Is combat ability in hold mode (past threshold)?</summary>
        public bool IsCombatHolding => isCombatHolding;

        /// <summary>How long combat ability has been held.</summary>
        public float CombatHoldDuration => isCombatPressed ? Time.time - combatPressTime : 0f;

        /// <summary>Is movement ability button currently held?</summary>
        public bool IsMovementHeld => isMovementPressed;

        /// <summary>Is movement ability in hold mode (past threshold)?</summary>
        public bool IsMovementHolding => isMovementHolding;

        /// <summary>How long movement ability has been held.</summary>
        public float MovementHoldDuration => isMovementPressed ? Time.time - movementPressTime : 0f;

        void Awake()
        {
            maskManager = GetComponent<MaskManager>();

            if (showDebugLogs)
                Debug.Log("MaskInputHandler: Initialized with hold detection");
        }

        void Update()
        {
            // Update combat hold state
            if (isCombatPressed)
            {
                float holdTime = Time.time - combatPressTime;
                if (!isCombatHolding && holdTime >= holdThreshold)
                {
                    isCombatHolding = true;
                    OnCombatHoldStart();
                }
                if (isCombatHolding)
                {
                    OnCombatHoldUpdate(holdTime);
                }
            }

            // Update movement hold state
            if (isMovementPressed)
            {
                float holdTime = Time.time - movementPressTime;
                if (!isMovementHolding && holdTime >= holdThreshold)
                {
                    isMovementHolding = true;
                    OnMovementHoldStart();
                }
                if (isMovementHolding)
                {
                    OnMovementHoldUpdate(holdTime);
                }
            }

            // Update alt combat hold state
            if (isAltCombatPressed)
            {
                float holdTime = Time.time - altCombatPressTime;
                if (!isAltCombatHolding && holdTime >= holdThreshold)
                {
                    isAltCombatHolding = true;
                    OnAltCombatHoldStart();
                }
            }
        }

        #region Combat Ability Input

        /// <summary>
        /// Called by Input System when the CombatAbility action is triggered.
        /// Maps to: Left Mouse Button / Primary Attack
        /// </summary>
        public void OnCombatAbility(InputValue value)
        {
            if (maskManager == null)
                return;

            if (value.isPressed)
            {
                // Button pressed
                isCombatPressed = true;
                isCombatHolding = false;
                combatPressTime = Time.time;

                if (showDebugLogs)
                    Debug.Log("MaskInputHandler: Combat ability pressed");

                OnCombatPressed();
            }
            else
            {
                // Button released
                float holdDuration = Time.time - combatPressTime;

                if (showDebugLogs)
                    Debug.Log($"MaskInputHandler: Combat ability released (held: {holdDuration:F2}s, wasHolding: {isCombatHolding})");

                if (isCombatHolding)
                {
                    OnCombatHoldRelease(holdDuration);
                }
                else
                {
                    OnCombatTap();
                }

                isCombatPressed = false;
                isCombatHolding = false;
            }
        }

        /// <summary>Called when combat button first pressed. Fires weapon immediately.</summary>
        protected virtual void OnCombatPressed()
        {
            // Fire weapon immediately on press
            maskManager.FireWeapon();
        }

        /// <summary>Called when combat hold threshold passed.</summary>
        protected virtual void OnCombatHoldStart()
        {
            if (showDebugLogs)
                Debug.Log("MaskInputHandler: Combat hold started");
        }

        /// <summary>Called every frame while combat is held. Fires weapon for continuous fire.</summary>
        protected virtual void OnCombatHoldUpdate(float duration)
        {
            // Continuous fire while held (weapon handles its own fire rate)
            maskManager.FireWeapon();
        }

        /// <summary>Called when combat released after holding.</summary>
        protected virtual void OnCombatHoldRelease(float duration)
        {
            // Override in subclass for charged weapon release
        }

        /// <summary>Called when combat released before hold threshold (quick tap).</summary>
        protected virtual void OnCombatTap()
        {
            // Override in subclass if needed for tap-specific behavior
        }

        #endregion

        #region Movement Ability Input

        /// <summary>
        /// Called by Input System when the Movement Ability action is triggered.
        /// Maps to: Shift / Space / F Key
        /// </summary>
        public void OnMovementAbility(InputValue value)
        {
            if (maskManager == null)
                return;

            if (value.isPressed)
            {
                // Button pressed
                isMovementPressed = true;
                isMovementHolding = false;
                movementPressTime = Time.time;

                if (showDebugLogs)
                    Debug.Log("MaskInputHandler: Movement ability pressed");

                OnMovementPressed();
            }
            else
            {
                // Button released
                float holdDuration = Time.time - movementPressTime;

                if (showDebugLogs)
                    Debug.Log($"MaskInputHandler: Movement ability released (held: {holdDuration:F2}s, wasHolding: {isMovementHolding})");

                if (isMovementHolding)
                {
                    OnMovementHoldRelease(holdDuration);
                }
                else
                {
                    OnMovementTap();
                }

                isMovementPressed = false;
                isMovementHolding = false;
            }
        }

        /// <summary>Called when movement button first pressed.</summary>
        protected virtual void OnMovementPressed()
        {
            // Start the movement ability (for charged abilities like EarthQuake)
            maskManager.UseMovementAbility();
        }

        /// <summary>Called when movement hold threshold passed.</summary>
        protected virtual void OnMovementHoldStart()
        {
            if (showDebugLogs)
                Debug.Log("MaskInputHandler: Movement hold started");
        }

        /// <summary>Called every frame while movement is held.</summary>
        protected virtual void OnMovementHoldUpdate(float duration)
        {
            // Can be used for charge UI updates
        }

        /// <summary>Called when movement released after holding.</summary>
        protected virtual void OnMovementHoldRelease(float duration)
        {
            // Cancel the movement ability (releases charged abilities)
            var ability = maskManager.CurrentMovementAbility;
            if (ability != null)
            {
                ability.Cancel();
            }
        }

        /// <summary>Called when movement released before hold threshold (quick tap).</summary>
        protected virtual void OnMovementTap()
        {
            // Quick tap - ability already started in OnMovementPressed
        }

        #endregion

        #region Alt Combat Ability Input

        /// <summary>
        /// Called by Input System when the Alt Combat Ability action is triggered.
        /// Maps to: E Key / Middle Mouse Button
        /// </summary>
        public void OnAltCombatAbility(InputValue value)
        {
            if (maskManager == null)
                return;

            if (value.isPressed)
            {
                isAltCombatPressed = true;
                isAltCombatHolding = false;
                altCombatPressTime = Time.time;

                if (showDebugLogs)
                    Debug.Log("MaskInputHandler: Alt combat ability pressed");

                maskManager.UseAltCombatAbility();
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("MaskInputHandler: Alt combat ability released");

                // Cancel channeled abilities on release
                maskManager.CancelAltCombatAbility();

                isAltCombatPressed = false;
                isAltCombatHolding = false;
            }
        }

        /// <summary>Called when alt combat hold threshold passed.</summary>
        protected virtual void OnAltCombatHoldStart()
        {
            if (showDebugLogs)
                Debug.Log("MaskInputHandler: Alt combat hold started");
        }

        #endregion

        #region Mask Switching Input

        /// <summary>
        /// Called by Input System when the Previous action is triggered.
        /// Maps to: 1 Key / D-Pad Left
        /// </summary>
        public void OnPrevious(InputValue value)
        {
            if (showDebugLogs)
                Debug.Log($"MaskInputHandler: OnPrevious triggered (isPressed: {value.isPressed})");

            if (maskManager == null || !value.isPressed)
                return;

            maskManager.PreviousMask();
        }

        /// <summary>
        /// Called by Input System when the Next action is triggered.
        /// Maps to: 2 Key / D-Pad Right
        /// </summary>
        public void OnNext(InputValue value)
        {
            if (showDebugLogs)
                Debug.Log($"MaskInputHandler: OnNext triggered (isPressed: {value.isPressed})");

            if (maskManager == null || !value.isPressed)
                return;

            maskManager.NextMask();
        }

        #endregion

        #region Public API

        /// <summary>Get hold progress (0-1) for combat ability based on target duration.</summary>
        public float GetCombatHoldPercent(float targetDuration)
        {
            if (targetDuration <= 0 || !isCombatPressed) return 0f;
            return Mathf.Clamp01(CombatHoldDuration / targetDuration);
        }

        /// <summary>Get hold progress (0-1) for movement ability based on target duration.</summary>
        public float GetMovementHoldPercent(float targetDuration)
        {
            if (targetDuration <= 0 || !isMovementPressed) return 0f;
            return Mathf.Clamp01(MovementHoldDuration / targetDuration);
        }

        /// <summary>Force cancel all held inputs.</summary>
        public void CancelAllInput()
        {
            if (isCombatPressed)
            {
                maskManager.CancelChargeWeapon();
                isCombatPressed = false;
                isCombatHolding = false;
            }

            if (isMovementPressed)
            {
                var ability = maskManager.CurrentMovementAbility;
                ability?.Cancel();
                isMovementPressed = false;
                isMovementHolding = false;
            }

            isAltCombatPressed = false;
            isAltCombatHolding = false;
        }

        #endregion
    }
}
