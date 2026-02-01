using UnityEditor;
using UnityEngine;
using Pool;

namespace Dwayne.Editor
{
    [CustomEditor(typeof(ObjectPoolManager))]
    public class ObjectPoolManagerEditor : UnityEditor.Editor
    {
        const int DropZoneHeight = 36;
        SerializedProperty _poolConfigs;

        void OnEnable()
        {
            _poolConfigs = serializedObject.FindProperty("poolConfigs");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUI.EndDisabledGroup();

            DrawDropZone();

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_poolConfigs, true);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawDropZone()
        {
            Rect rect = GUILayoutUtility.GetRect(0, DropZoneHeight);
            EditorGUI.DrawRect(rect, new Color(0.22f, 0.22f, 0.22f, 1f));
            Rect border = new Rect(rect.x, rect.y, rect.width, rect.height);
            border.xMin += 1; border.xMax -= 1; border.yMin += 1; border.yMax -= 1;
            EditorGUI.DrawRect(border, new Color(0.35f, 0.35f, 0.35f, 1f));

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            GUI.Label(rect, "Drag prefabs here to add to pool", labelStyle);

            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!rect.Contains(Event.current.mousePosition))
                        break;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (Event.current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (Object obj in DragAndDrop.objectReferences)
                        {
                            if (obj is not GameObject go)
                                continue;
                            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
                            if (prefab == null && AssetDatabase.Contains(go))
                                prefab = go;
                            if (prefab == null)
                                prefab = go;
                            if (prefab == null)
                                continue;

                            int i = _poolConfigs.arraySize;
                            _poolConfigs.arraySize++;
                            SerializedProperty element = _poolConfigs.GetArrayElementAtIndex(i);
                            element.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                            element.FindPropertyRelative("poolName").stringValue = "";
                            element.FindPropertyRelative("preloadCount").intValue = 50;
                            element.FindPropertyRelative("maxSize").intValue = 200;
                            element.FindPropertyRelative("allowExpansion").boolValue = true;
                            element.FindPropertyRelative("container").objectReferenceValue = null;
                        }
                        Event.current.Use();
                    }
                    break;
            }
        }
    }
}
