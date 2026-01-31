using UnityEditor;
using UnityEngine;
using Dwayne.Weapons;

namespace Dwayne.Editor
{
    [CustomEditor(typeof(BaseWeapon), true)]
    [CanEditMultipleObjects]
    public class BaseWeaponEditor : UnityEditor.Editor
    {
        const string PrefsPrefix = "Dwayne.BaseWeapon.Foldout.";

        SerializedProperty _fireCooldown, _magazineSize, _refillCooldown;
        SerializedProperty _damage, _range;
        SerializedProperty _pelletsPerShot, _spreadAngle;
        SerializedProperty _useCharging, _maxChargeTime, _minChargeToFire, _fullChargeDamageMultiplier;
        SerializedProperty _fireAbility, _altFireAbility, _owner;

        void OnEnable()
        {
            var so = serializedObject;
            _fireCooldown = so.FindProperty("fireCooldown");
            _magazineSize = so.FindProperty("magazineSize");
            _refillCooldown = so.FindProperty("refillCooldown");
            _damage = so.FindProperty("damage");
            _range = so.FindProperty("range");
            _pelletsPerShot = so.FindProperty("pelletsPerShot");
            _spreadAngle = so.FindProperty("spreadAngle");
            _useCharging = so.FindProperty("useCharging");
            _maxChargeTime = so.FindProperty("maxChargeTime");
            _minChargeToFire = so.FindProperty("minChargeToFire");
            _fullChargeDamageMultiplier = so.FindProperty("fullChargeDamageMultiplier");
            _fireAbility = so.FindProperty("fireAbility");
            _altFireAbility = so.FindProperty("altFireAbility");
            _owner = so.FindProperty("owner");
        }

        bool GetFoldout(string key, bool defaultVal = false)
        {
            return EditorPrefs.GetBool(PrefsPrefix + key + "." + target.GetInstanceID(), defaultVal);
        }

        void SetFoldout(string key, bool value)
        {
            EditorPrefs.SetBool(PrefsPrefix + key + "." + target.GetInstanceID(), value);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Cooldown", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_fireCooldown);
            EditorGUILayout.PropertyField(_magazineSize);
            EditorGUILayout.PropertyField(_refillCooldown);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Damage & Range", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_damage);
            EditorGUILayout.PropertyField(_range);

            bool spreadOpen = GetFoldout("Spread");
            spreadOpen = EditorGUILayout.BeginFoldoutHeaderGroup(spreadOpen, "Spread");
            SetFoldout("Spread", spreadOpen);
            if (spreadOpen)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_pelletsPerShot);
                EditorGUILayout.PropertyField(_spreadAngle);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            bool chargingOpen = GetFoldout("Charging");
            chargingOpen = EditorGUILayout.BeginFoldoutHeaderGroup(chargingOpen, "Charging");
            SetFoldout("Charging", chargingOpen);
            if (chargingOpen)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_useCharging);
                EditorGUILayout.PropertyField(_maxChargeTime);
                EditorGUILayout.PropertyField(_minChargeToFire);
                EditorGUILayout.PropertyField(_fullChargeDamageMultiplier);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            bool abilitiesOpen = GetFoldout("Abilities");
            abilitiesOpen = EditorGUILayout.BeginFoldoutHeaderGroup(abilitiesOpen, "Abilities");
            SetFoldout("Abilities", abilitiesOpen);
            if (abilitiesOpen)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_fireAbility);
                EditorGUILayout.PropertyField(_altFireAbility);
                EditorGUILayout.PropertyField(_owner);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();

            // Subclass-added properties
            DrawPropertiesExcluding(serializedObject,
                "m_Script",
                "fireCooldown", "magazineSize", "refillCooldown",
                "damage", "range",
                "pelletsPerShot", "spreadAngle",
                "useCharging", "maxChargeTime", "minChargeToFire", "fullChargeDamageMultiplier",
                "fireAbility", "altFireAbility", "owner");
        }
    }
}
