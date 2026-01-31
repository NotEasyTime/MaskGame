using UnityEngine;
using Dwayne.Interfaces;
using Element;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Air movement: apply upward impulse or set vertical velocity for a short time.
    /// </summary>
    public class LiftAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Air;

        [Header("Lift")]
        [SerializeField] float upwardForce = 15f;
        [SerializeField] float duration = 0.5f;
        [SerializeField] float maxHeight = 10f;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Rigidbody rb = user.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("LiftAbility: user has no Rigidbody.");
                return false;
            }

            float effectiveForce = duration > 0f ? Mathf.Min(upwardForce, maxHeight / duration) : upwardForce;
            Vector3 vel = rb.linearVelocity;
            vel.y = effectiveForce;
            rb.linearVelocity = vel;

            return true;
        }
    }
}
