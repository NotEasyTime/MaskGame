using UnityEngine;
using Dwayne.Abilities;
using Dwayne.Weapons;
using Element;

namespace Dwayne.Masks
{
    /// <summary>
    /// Defines a mask with a color, weapon (combat ability), and movement ability.
    /// Masks can combine different elements (e.g., Fire combat + Ice movement).
    /// </summary>
    [CreateAssetMenu(fileName = "New Mask", menuName = "MaskGame/Mask", order = 0)]
    public class Mask : ScriptableObject
    {
    
        [Header("Combat")]
        [Tooltip("The weapon prefab with the combat ability")]
        public GameObject weaponPrefab;

        [Header("Movement")]
        [Tooltip("The movement ability prefab (e.g., Dash, Teleport, Ice Skates)")]
        public GameObject movementAbilityPrefab;

        [Header("Info")]
        [Tooltip("Display name for this mask")]
        public string maskName = "New Mask";


        /// <summary>
        /// Gets the element type of the combat ability (from weapon's fire ability).
        /// Returns Air if not found or weapon is not set.
        /// </summary>
        public Element.Element GetCombatElementType()
        {
            if (weaponPrefab == null)
                return Element.Element.Air;

            BaseWeapon weapon = weaponPrefab.GetComponent<BaseWeapon>();
            if (weapon == null)
                return Element.Element.Air;

            // Try to get the fire ability from the weapon
            // Note: This requires the weapon to be instantiated or we need to use reflection/serialization
            // For now, we'll use a workaround by checking the weapon's serialized fields
            #if UNITY_EDITOR
            UnityEditor.SerializedObject serializedWeapon = new UnityEditor.SerializedObject(weapon);
            UnityEditor.SerializedProperty fireAbilityProp = serializedWeapon.FindProperty("fireAbility");

            if (fireAbilityProp != null && fireAbilityProp.objectReferenceValue != null)
            {
                GameObject abilityPrefab = fireAbilityProp.objectReferenceValue as GameObject;
                if (abilityPrefab != null)
                {
                    BaseAbility ability = abilityPrefab.GetComponent<BaseAbility>();
                    if (ability != null)
                        return ability.ElementType;
                }
            }
            #endif

            return Element.Element.Air;
        }

        /// <summary>
        /// Gets the element type of the movement ability.
        /// Returns Air if not found or movement ability is not set.
        /// </summary>
        public Element.Element GetMovementElementType()
        {
            if (movementAbilityPrefab == null)
                return Element.Element.Air;

            BaseAbility ability = movementAbilityPrefab.GetComponent<BaseAbility>();
            if (ability == null)
                return Element.Element.Air;

            return ability.ElementType;
        }

        /// <summary>
        /// Gets a formatted string for UI display showing both element types.
        /// Example: "Fire | Ice" or "Air | Earth"
        /// </summary>
        public string GetElementDisplayString()
        {
            Element.Element combat = GetCombatElementType();
            Element.Element movement = GetMovementElementType();
            return $"{combat} | {movement}";
        }

        /// <summary>
        /// Validates that the mask has required components.
        /// </summary>
        public bool IsValid()
        {
            if (weaponPrefab == null)
            {
                Debug.LogError($"Mask '{maskName}' has no weapon prefab assigned!");
                return false;
            }

            if (movementAbilityPrefab == null)
            {
                Debug.LogError($"Mask '{maskName}' has no movement ability prefab assigned!");
                return false;
            }

            // Validate weapon has BaseWeapon component
            if (weaponPrefab.GetComponent<BaseWeapon>() == null)
            {
                Debug.LogError($"Mask '{maskName}' weapon prefab '{weaponPrefab.name}' does not have a BaseWeapon component!");
                return false;
            }

            // Validate movement ability has BaseAbility component
            if (movementAbilityPrefab.GetComponent<BaseAbility>() == null)
            {
                Debug.LogError($"Mask '{maskName}' movement ability prefab '{movementAbilityPrefab.name}' does not have a BaseAbility component!");
                return false;
            }

            return true;
        }
    }
}
