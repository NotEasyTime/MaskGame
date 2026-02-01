using UnityEngine;

namespace Dwayne.VFX
{
    /// <summary>
    /// Simple ice trail VFX: creates a short-lived particle burst at runtime (frost/crystal look).
    /// Attach to a prefab and assign that prefab as the ice spear's trailVFX.
    /// </summary>
    public class IceTrailVFX : MonoBehaviour
    {
        [SerializeField] float duration = 0.4f;
        [SerializeField] int burstCount = 8;
        [SerializeField] float startSize = 0.15f;
        [SerializeField] float startLifetime = 0.35f;
        [SerializeField] Color colorMin = new Color(0.6f, 0.85f, 1f, 0.9f);
        [SerializeField] Color colorMax = new Color(0.9f, 1f, 1f, 0.5f);

        void Start()
        {
            var ps = gameObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.01f;
            main.loop = false;
            main.startLifetime = startLifetime;
            main.startSpeed = 0.5f;
            main.startSize = startSize;
            main.startColor = new ParticleSystem.MinMaxGradient(colorMin, colorMax);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            main.maxParticles = 32;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;

            var renderer = GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                // Use default particle material if available (Built-in or URP)
                var mat = GetDefaultParticleMaterial();
                if (mat != null) renderer.material = mat;
            }

            ps.Play();
            Destroy(gameObject, duration);
        }

        static Material GetDefaultParticleMaterial()
        {
            var mat = Resources.GetBuiltinResource<Material>("Default-Particle.mat");
            if (mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Particles/Unlit");
            if (shader != null) return new Material(shader) { color = Color.white };
            return null;
        }
    }
}
