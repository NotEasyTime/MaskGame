using UnityEngine;
using UnityEngine.InputSystem;

namespace Dwayne.Masks
{
    /// <summary>
    /// Handles input for mask abilities.
    /// Connects Unity Input System to MaskManager.
    /// </summary>
    [RequireComponent(typeof(MaskManager))]
    public class MaskInputHandler : MonoBehaviour
    {
        private MaskManager maskManager;

        void Awake()
        {
            maskManager = GetComponent<MaskManager>();
        }

        /// <summary>
        /// Called by Input System when the CombatAbility action is triggered.
        /// Maps to: Left Mouse Button / Primary Attack
        /// Uses the weapon's fire ability (combat ability from the mask).
        /// </summary>
        public void OnCombatAbility(InputValue value)
        {
            if (maskManager == null || !value.isPressed)
                return;

            maskManager.UseCombatAbility();
        }

        /// <summary>
        /// Called by Input System when the Alt Combat Ability action is triggered.
        /// Maps to: E Key / Middle Mouse Button
        /// Uses the weapon's alt-fire ability.
        /// </summary>
        public void OnAltCombatAbility(InputValue value)
        {
            if (maskManager == null || !value.isPressed)
                return;

            maskManager.UseAltCombatAbility();
        }

        /// <summary>
        /// Called by Input System when the Movement Ability action is triggered.
        /// Maps to: Shift / Space / F Key (depending on configuration)
        /// Uses the mask's movement ability (dash, teleport, etc.).
        /// </summary>
        public void OnMovementAbility(InputValue value)
        {
            if (maskManager == null || !value.isPressed)
                return;

            maskManager.UseMovementAbility();
        }
    }
}
