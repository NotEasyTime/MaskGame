using UnityEngine;
using UnityEngine.InputSystem;
using Dwayne.Abilities;
using Element;

namespace Dwayne
{
    /// <summary>
    /// Dispatches Q (Movement ability) and E (Combat ability) to the current element's ability.
    /// Add to the player; assign movement and combat ability references and input actions.
    /// </summary>
    public class PlayerAbilityController : MonoBehaviour
    {
        [Header("Current Element")]
        [SerializeField] Element.Element currentElement = Element.Element.Air;

        [Header("Movement Abilities (Q)")]
        [SerializeField] BaseAbility liftAbility;
        [SerializeField] BaseAbility iceSkatesAbility;
        [SerializeField] BaseAbility teleportAbility;
        [SerializeField] BaseAbility dashTrailAbility;

        [Header("Combat Abilities (E)")]
        [SerializeField] BaseAbility airCombatAbility;
        [SerializeField] BaseAbility iceCombatAbility;
        [SerializeField] BaseAbility earthCombatAbility;
        [SerializeField] BaseAbility fireCombatAbility;

        [Header("Input")]
        [SerializeField] InputActionReference movementAbilityAction;
        [SerializeField] InputActionReference combatAbilityAction;
        [SerializeField] float targetPositionRange = 50f;

        private BaseAbility[] _movementAbilities;
        private BaseAbility[] _combatAbilities;

        public Element.Element CurrentElement
        {
            get => currentElement;
            set => currentElement = value;
        }

        private void Awake()
        {
            _movementAbilities = new BaseAbility[]
            {
                liftAbility,
                iceSkatesAbility,
                teleportAbility,
                dashTrailAbility
            };
            _combatAbilities = new BaseAbility[]
            {
                airCombatAbility,
                iceCombatAbility,
                earthCombatAbility,
                fireCombatAbility
            };
        }

        private void OnEnable()
        {
            if (movementAbilityAction != null)
            {
                movementAbilityAction.action.Enable();
                movementAbilityAction.action.performed += OnMovementAbilityPerformed;
            }
            if (combatAbilityAction != null)
            {
                combatAbilityAction.action.Enable();
                combatAbilityAction.action.performed += OnCombatAbilityPerformed;
            }
        }

        private void OnDisable()
        {
            if (movementAbilityAction != null)
            {
                movementAbilityAction.action.performed -= OnMovementAbilityPerformed;
                movementAbilityAction.action.Disable();
            }
            if (combatAbilityAction != null)
            {
                combatAbilityAction.action.performed -= OnCombatAbilityPerformed;
                combatAbilityAction.action.Disable();
            }
        }

        private void OnMovementAbilityPerformed(InputAction.CallbackContext _)
        {
            TryUseMovementAbility();
        }

        private void OnCombatAbilityPerformed(InputAction.CallbackContext _)
        {
            TryUseCombatAbility();
        }

        /// <summary>
        /// Target position for abilities (e.g. forward at range). Override or set from look/move input.
        /// </summary>
        protected virtual Vector3 GetTargetPosition()
        {
            return transform.position + transform.forward * targetPositionRange;
        }

        /// <summary>
        /// Try to use the movement ability for the current element (Q).
        /// </summary>
        public bool TryUseMovementAbility()
        {
            int index = (int)currentElement;
            if (index < 0 || index >= _movementAbilities.Length)
                return false;
            BaseAbility ability = _movementAbilities[index];
            if (ability == null || !ability.CanUse)
                return false;
            return ability.Use(gameObject, GetTargetPosition());
        }

        /// <summary>
        /// Try to use the combat ability for the current element (E).
        /// </summary>
        public bool TryUseCombatAbility()
        {
            int index = (int)currentElement;
            if (index < 0 || index >= _combatAbilities.Length)
                return false;
            BaseAbility ability = _combatAbilities[index];
            if (ability == null || !ability.CanUse)
                return false;
            return ability.Use(gameObject, GetTargetPosition());
        }
    }
}
