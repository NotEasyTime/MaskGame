using System.Collections;
using UnityEngine;
using Dwayne.Interfaces;
using Element;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Air movement: hold the player in the air with sustained upward velocity for the duration.
    /// </summary>
    public class LiftAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Air;

        [Header("Lift")]
        [Tooltip("Upward velocity applied every physics frame while lifting (holds you in the air). Should exceed gravity (e.g. 18+).")]
        [SerializeField] float upwardForce = 18f;
        [Tooltip("How long the lift sustains (seconds).")]
        [SerializeField] float duration = 0.6f;
        [SerializeField] float maxHeight = 10f;

        private Coroutine _liftRoutine;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Rigidbody rb = user.GetComponentInParent<Rigidbody>();
            CharacterController cc = user.GetComponentInParent<CharacterController>();
            if (rb == null && cc == null)
            {
                Debug.LogWarning("LiftAbility: user (and root) has no Rigidbody or CharacterController.");
                return false;
            }

            if (_liftRoutine != null)
                StopCoroutine(_liftRoutine);

            float effectiveForce = duration > 0f ? Mathf.Min(upwardForce, maxHeight / duration) : upwardForce;
            _liftRoutine = StartCoroutine(LiftForDuration(user, rb, cc, effectiveForce));
            return true;
        }

        private IEnumerator LiftForDuration(GameObject user, Rigidbody rb, CharacterController cc, float force)
        {
            float elapsed = 0f;
            bool wasUsingGravity = true;

            if (rb != null)
            {
                wasUsingGravity = rb.useGravity;
                rb.useGravity = false;

                while (elapsed < duration && rb != null)
                {
                    Vector3 vel = rb.linearVelocity;
                    vel.y = force;
                    rb.linearVelocity = vel;
                    elapsed += Time.fixedDeltaTime;
                    yield return new WaitForFixedUpdate();
                }

                if (rb != null)
                    rb.useGravity = wasUsingGravity;
            }
            else if (cc != null)
            {
                while (elapsed < duration && cc != null)
                {
                    cc.Move(Vector3.up * (force * Time.deltaTime));
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            _liftRoutine = null;
        }
    }
}
