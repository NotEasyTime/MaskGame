using System.Collections.Generic;
using UnityEngine;

namespace Dwayne.VFX
{
    /// <summary>
    /// Renders a lightning-bolt-style trail behind a moving object using a LineRenderer.
    /// Records positions over time and draws them with jagged offsets for a crackling look.
    /// Attach to any moving object (e.g. projectile, ability) that should leave a lightning trail.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LightningTrailVFX : MonoBehaviour
    {
        [Header("Trail")]
        [SerializeField] float trailDuration = 0.5f;
        [SerializeField] int maxPoints = 64;
        [SerializeField] float pointSpacing = 0.15f;

        [Header("Lightning look")]
        [SerializeField] float boltWidth = 0.08f;
        [SerializeField] float jitterAmount = 0.06f;
        [SerializeField] float jitterSpeed = 25f;
        [SerializeField] Color colorStart = new Color(0.7f, 0.9f, 1f, 0.95f);
        [SerializeField] Color colorEnd = new Color(0.4f, 0.7f, 1f, 0.2f);

        struct TrailPoint
        {
            public Vector3 Position;
            public float Time;
        }

        readonly List<TrailPoint> _points = new List<TrailPoint>();
        LineRenderer _line;
        float _seed;

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 0;
            _line.startWidth = boltWidth;
            _line.endWidth = boltWidth * 0.3f;
            _line.startColor = colorStart;
            _line.endColor = colorEnd;
            _line.material = GetLightningMaterial();
            _line.numCapVertices = 2;
            _line.numCornerVertices = 2;
            _seed = Random.Range(0f, 1000f);
        }

        void LateUpdate()
        {
            float now = Time.time;
            Vector3 pos = transform.position;

            // Add new point if far enough from last
            if (_points.Count == 0 || Vector3.SqrMagnitude(pos - _points[_points.Count - 1].Position) > pointSpacing * pointSpacing)
            {
                _points.Add(new TrailPoint { Position = pos, Time = now });
            }

            // Remove old points by time and cap count
            float cutoff = now - trailDuration;
            while (_points.Count > 1 && (_points[0].Time < cutoff || _points.Count > maxPoints))
            {
                _points.RemoveAt(0);
            }

            if (_points.Count < 2)
            {
                _line.positionCount = 0;
                return;
            }

            // Build lightning path: smooth positions + perpendicular jitter for zigzag
            int segments = (_points.Count - 1) * 2; // extra vertices for jitter
            Vector3[] smoothed = new Vector3[_points.Count];
            for (int i = 0; i < _points.Count; i++)
                smoothed[i] = _points[i].Position;

            List<Vector3> bolt = new List<Vector3>();
            bolt.Add(smoothed[0]);

            for (int i = 1; i < smoothed.Length; i++)
            {
                Vector3 a = smoothed[i - 1];
                Vector3 b = smoothed[i];
                Vector3 tangent = (b - a).normalized;
                Vector3 perp = Vector3.Cross(tangent, Vector3.up);
                if (perp.sqrMagnitude < 0.01f)
                    perp = Vector3.Cross(tangent, Vector3.right);

                perp.Normalize();
                float t = (float)i / smoothed.Length;
                float n = Mathf.PerlinNoise((_seed + now * jitterSpeed) * 2f, t * 10f) * 2f - 1f;
                Vector3 mid = Vector3.Lerp(a, b, 0.5f) + perp * (n * jitterAmount);
                bolt.Add(mid);
                bolt.Add(b);
            }

            _line.positionCount = bolt.Count;
            _line.SetPositions(bolt.ToArray());
        }

        static Material GetLightningMaterial()
        {
            var mat = Resources.GetBuiltinResource<Material>("Default-Particle.mat");
            if (mat != null) return new Material(mat);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Particles/Unlit")
                ?? Shader.Find("Sprites/Default");
            if (shader != null) return new Material(shader) { color = Color.white };
            return null;
        }

        /// <summary>
        /// Call to clear the trail immediately (e.g. when projectile is destroyed).
        /// </summary>
        public void Clear()
        {
            _points.Clear();
            if (_line != null)
                _line.positionCount = 0;
        }
    }
}
