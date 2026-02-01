using UnityEngine;
using UnityEngine.InputSystem;
using Dwayne.Interfaces;
using Element;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Ice movement (Kelvin-style): create an ice path/trail the player glides on;
    /// movement bonus and slow resistance while on the path; smooth 3DOF-style sliding.
    /// Supports up/down input via look direction (pitch) and optional vertical input action.
    /// </summary>
    public class IceSkatesAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Ice;

        [Header("Ice Path")]
        [SerializeField] float pathDuration = 4f;
        [SerializeField] float movementSpeedMultiplier = 1.5f;
        [SerializeField] float slowResistance = 0.6f;
        [SerializeField] float pathWidth = 2f;
        [SerializeField] float pathSegmentLength = 2f;

        [Header("Up / Down Input")]
        [Tooltip("Look direction (pitch) adds vertical movement. Scale for up/down strength.")]
        [SerializeField] float verticalLookScale = 1f;
        [Tooltip("Optional: bind to an axis (e.g. right stick Y, or Jump/Crouch) for explicit up/down. -1 to 1.")]
        [SerializeField] InputActionReference verticalInputAction;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Rigidbody rb = user.GetComponentInParent<Rigidbody>();
            CharacterController cc = user.GetComponentInParent<CharacterController>();
            if (rb == null && cc == null)
            {
                Debug.LogWarning("IceSkatesAbility: user (and root) has no Rigidbody or CharacterController.");
                return false;
            }

            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - user.transform.position).normalized
                : user.transform.forward;

            float verticalInput = 0f;
            if (verticalInputAction != null && verticalInputAction.action != null)
            {
                verticalInputAction.action.Enable();
                string controlType = verticalInputAction.action.expectedControlType;
                if (controlType == "Vector2")
                    verticalInput = verticalInputAction.action.ReadValue<Vector2>().y;
                else
                    verticalInput = verticalInputAction.action.ReadValue<float>();
            }

            if (verticalInput != 0f)
                direction.y += verticalInput * verticalLookScale;
            else
                direction.y *= verticalLookScale;

            if (direction.sqrMagnitude < 0.01f)
                direction = user.transform.forward;
            else
                direction.Normalize();

            float baseSpeed = movementSpeedMultiplier * 5f;
            float pathBonus = (pathSegmentLength / Mathf.Max(0.01f, pathDuration)) * (1f + slowResistance) * pathWidth * 0.1f;
            float speed = baseSpeed + pathBonus;

            if (rb != null)
            {
                rb.linearVelocity = direction * speed;
            }
            else if (cc != null)
            {
                cc.Move(direction * speed * 0.15f);
            }

            return true;
        }
    }
}
