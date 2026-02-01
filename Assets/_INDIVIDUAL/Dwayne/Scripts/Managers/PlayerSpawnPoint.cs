using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Attach to an empty GameObject to mark a player spawn/respawn location.
    /// Assign this transform to GameManager's Player Spawn Point.
    /// In the editor, a capsule mesh is shown for placement; it is hidden at runtime.
    /// </summary>
    [ExecuteAlways]
    public class PlayerSpawnPoint : MonoBehaviour
    {
        const string EditorCapsuleName = "EditorCapsule";

        [Header("Editor Preview")]
        [Tooltip("Scale of the editor-only capsule (for placement reference)")]
        [SerializeField] Vector3 capsuleScale = new Vector3(0.5f, 1f, 0.5f);

        void OnEnable()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;
            EnsureEditorCapsule();
#endif
        }

        void Awake()
        {
            if (Application.isPlaying)
                HideEditorCapsule();
        }

#if UNITY_EDITOR
        void EnsureEditorCapsule()
        {
            Transform existing = transform.Find(EditorCapsuleName);
            if (existing != null)
                return;

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cap.name = EditorCapsuleName;
            cap.transform.SetParent(transform);
            cap.transform.localPosition = Vector3.zero;
            cap.transform.localRotation = Quaternion.identity;
            cap.transform.localScale = capsuleScale;

            // No collider – editor preview only
            if (cap.TryGetComponent<Collider>(out var col))
                DestroyImmediate(col);

            // Semi-transparent in editor so it doesn't block view
            if (cap.TryGetComponent<Renderer>(out var rend) && rend.sharedMaterial != null)
            {
                var mat = new Material(rend.sharedMaterial);
                mat.color = new Color(0.2f, 0.6f, 1f, 0.4f);
                if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3); // Transparent
                if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                rend.sharedMaterial = mat;
            }
        }
#endif

        void HideEditorCapsule()
        {
            Transform cap = transform.Find(EditorCapsuleName);
            if (cap != null)
                cap.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // Wire capsule when selected (visible even if mesh child is missing)
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            DrawWireCapsule(transform.position, transform.rotation, capsuleScale.y, capsuleScale.x * 0.5f);
        }

        static void DrawWireCapsule(Vector3 center, Quaternion rotation, float height, float radius)
        {
            float halfHeight = Mathf.Max(0f, (height * 0.5f) - radius);
            Vector3 up = rotation * Vector3.up;
            Vector3 top = center + up * halfHeight;
            Vector3 bottom = center - up * halfHeight;

            Gizmos.matrix = Matrix4x4.TRS(top, rotation, Vector3.one);
            DrawWireHemisphere(Vector3.zero, radius);
            Gizmos.matrix = Matrix4x4.TRS(bottom, rotation * Quaternion.Euler(180f, 0f, 0f), Vector3.one);
            DrawWireHemisphere(Vector3.zero, radius);
            Gizmos.matrix = Matrix4x4.identity;

            // Four vertical lines
            Vector3 r = rotation * Vector3.right * radius;
            Vector3 f = rotation * Vector3.forward * radius;
            Gizmos.DrawLine(top + r, bottom + r);
            Gizmos.DrawLine(top - r, bottom - r);
            Gizmos.DrawLine(top + f, bottom + f);
            Gizmos.DrawLine(top - f, bottom - f);
        }

        static void DrawWireHemisphere(Vector3 center, float radius)
        {
            int segments = 8;
            float step = Mathf.PI * 0.5f / segments;
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * step;
                float a1 = (i + 1) * step;
                float x0 = Mathf.Cos(a0) * radius;
                float z0 = Mathf.Sin(a0) * radius;
                float x1 = Mathf.Cos(a1) * radius;
                float z1 = Mathf.Sin(a1) * radius;
                Gizmos.DrawLine(center + new Vector3(x0, 0, z0), center + new Vector3(x1, 0, z1));
                Gizmos.DrawLine(center + new Vector3(0, x0, z0), center + new Vector3(0, x1, z1));
                Gizmos.DrawLine(center + new Vector3(x0, z0, 0), center + new Vector3(x1, z1, 0));
            }
        }
#endif
    }
}
