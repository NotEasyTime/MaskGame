using System.Collections;
using UnityEngine;
using Dwayne.Interfaces;
using Element;

namespace Dwayne.Abilities
{
    /// <summary>
    /// Ice movement: launch into sustained flight for several seconds (ice skates = gliding through the air).
    /// </summary>
    public class IceSkatesAbility : BaseAbility
    {
        public override Element.Element ElementType => Element.Element.Ice;

        [Header("Flight")]
        [Tooltip("How long you stay flying (seconds).")]
        [SerializeField] float flyDuration = 5f;
        [Tooltip("Speed while flying.")]
        [SerializeField] float flySpeed = 18f;
        [Tooltip("Upward tilt of flight direction (0 = horizontal, 0.5 = 45° up, 1 = straight up).")]
        [Range(0f, 1f)]
        [SerializeField] float upwardBias = 0.35f;

        private Coroutine _flyRoutine;

        protected override bool DoUse(GameObject user, Vector3 targetPosition)
        {
            Rigidbody rb = user.GetComponentInParent<Rigidbody>();
            CharacterController cc = user.GetComponentInParent<CharacterController>();
            if (rb == null && cc == null)
            {
                Debug.LogWarning("IceSkatesAbility: user (and root) has no Rigidbody or CharacterController.");
                return false;
            }

            if (_flyRoutine != null)
                StopCoroutine(_flyRoutine);

            Transform mover = rb != null ? rb.transform : cc.transform;
            Vector3 forward = targetPosition != Vector3.zero
                ? (targetPosition - mover.position).normalized
                : mover.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
                forward = mover.forward;
            forward.Normalize();
            Vector3 direction = (forward + Vector3.up * upwardBias).normalized;

            _flyRoutine = StartCoroutine(FlyForDuration(user, rb, cc, direction));
            return true;
        }

        private IEnumerator FlyForDuration(GameObject user, Rigidbody rb, CharacterController cc, Vector3 direction)
        {
            float elapsed = 0f;
            bool wasUsingGravity = true;

            if (rb != null)
            {
                wasUsingGravity = rb.useGravity;
                rb.useGravity = false;
            }

            while (elapsed < flyDuration && (rb != null || cc != null))
            {
                if (rb != null)
                {
                    rb.linearVelocity = direction * flySpeed;
                    elapsed += Time.fixedDeltaTime;
                    yield return new WaitForFixedUpdate();
                }
                else if (cc != null)
                {
                    cc.Move(direction * flySpeed * Time.deltaTime);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (rb != null)
            {
                rb.useGravity = wasUsingGravity;
                rb.linearVelocity = Vector3.zero;
            }

            _flyRoutine = null;
        }
    }
}
