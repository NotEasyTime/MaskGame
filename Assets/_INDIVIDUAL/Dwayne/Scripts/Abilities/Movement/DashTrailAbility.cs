using UnityEngine;
namespace Dwayne.Abilities
{
    /// <summary>
    /// Fire movement: quick burst in a direction; optionally spawn trail VFX.
    /// </summary>
    public class DashTrailAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Fire;

        [Header("Dash")]
        [SerializeField] float dashSpeed = 25f;
        [SerializeField] float dashDuration = 0.2f;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Rigidbody rb = user.GetComponentInParent<Rigidbody>();
            CharacterController cc = user.GetComponentInParent<CharacterController>();
            if (rb == null && cc == null)
            {
                Debug.LogWarning("DashTrailAbility: user (and root) has no Rigidbody or CharacterController.");
                return false;
            }

            Vector3 direction = targetPosition != Vector3.zero
                ? (targetPosition - user.transform.position).normalized
                : user.transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = user.transform.forward;

            // Spawn trail VFX at starting position
            SpawnTrailVFX(user.transform.position, Quaternion.LookRotation(direction));

            if (rb != null)
            {
                Vector3 vel = direction * dashSpeed;
                vel.y = rb.linearVelocity.y;
                rb.linearVelocity = vel;
            }
            else if (cc != null)
            {
                Vector3 move = direction * dashSpeed * dashDuration;
                cc.Move(move);
            }

            return true;
        }
    }
}
