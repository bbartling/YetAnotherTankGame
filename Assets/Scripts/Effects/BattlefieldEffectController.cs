using System.Collections.Generic;
using UnityEngine;

public static class BattlefieldEffectController
{
    private static readonly Dictionary<string, Queue<GameObject>> TemporaryEffects = new Dictionary<string, Queue<GameObject>>();

    public static GameObject CreateLoopingEffect(Transform parent, string effectName, Color color, int maxParticles, float startSize, float lifetime)
    {
        GameObject effect = new GameObject(effectName);
        effect.transform.SetParent(parent, false);
        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.maxParticles = Mathf.Clamp(maxParticles, 1, 32);
        main.startLifetime = lifetime;
        main.startSize = startSize;
        main.startSpeed = 0.8f;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = Mathf.Min(12f, maxParticles / Mathf.Max(0.5f, lifetime));

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.35f;

        ParticleSystemRenderer renderer = effect.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateParticleMaterial(effectName, color);
        }

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.SetActive(false);
        return effect;
    }

    private static Material CreateParticleMaterial(string effectName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
        material.name = effectName + "Material";
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        return material;
    }

    public static void RegisterTemporary(GameObject effect, string category, int maxActive)
    {
        if (effect == null)
        {
            return;
        }

        if (!TemporaryEffects.TryGetValue(category, out Queue<GameObject> effects))
        {
            effects = new Queue<GameObject>();
            TemporaryEffects[category] = effects;
        }

        while (effects.Count >= Mathf.Max(1, maxActive))
        {
            GameObject oldest = effects.Dequeue();
            if (oldest != null)
            {
                oldest.SetActive(false);
                Object.Destroy(oldest);
            }
        }

        effects.Enqueue(effect);
    }
}
