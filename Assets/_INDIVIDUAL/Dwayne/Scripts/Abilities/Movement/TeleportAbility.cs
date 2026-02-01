using UnityEngine;
using Dwayne.Interfaces;
using Element;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Earth movement: short-range instant teleport (e.g. raycast or ground snap).
    /// </summary>
    public class TeleportAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Earth;

        [Header("Teleport")]
        [SerializeField] float maxDistance = 15f;
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] bool groundOnly = true;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Rigidbody rb = user.GetComponentInParent<Rigidbody>();
            CharacterController cc = user.GetComponentInParent<CharacterController>();
            if (rb == null && cc == null)
            {
                Debug.LogWarning("TeleportAbility: user (and root) has no Rigidbody or CharacterController.");
                return false;
            }

            Vector3 origin = user.transform.position;
            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - origin).normalized
                : user.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = user.transform.forward;

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, groundMask))
                return false;

            if (groundOnly && Vector3.Angle(hit.normal, Vector3.up) > 45f)
                return false;

            Vector3 destination = hit.point;
            if (cc != null)
                destination += hit.normal * 0.1f;

            user.transform.position = destination;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            return true;
        }
    }
}
