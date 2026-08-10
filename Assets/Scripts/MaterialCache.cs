using UnityEngine;
using System.Collections.Generic;

// Shader-steal pattern only (per Docs/LINUX-SETUP.md hard constraints: no Shader.Find()).
// Cloning an existing primitive's sharedMaterial always carries the project's actual
// default shader forward, whatever it is - explicit shader assignment isn't needed.
public static class MaterialCache
{
    private static readonly Dictionary<Color, Material> _cache = new Dictionary<Color, Material>();

    public static Material Get(Color color)
    {
        if (_cache.TryGetValue(color, out var material))
            return material;

        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material baseMat = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        Object.DestroyImmediate(tmp);

        baseMat.SetColor("_BaseColor", color);
        baseMat.color = color;

        // VFX materials handle their own transparency settings explicitly
        // (VFXManager.MakeMat creates per-instance clones)

        _cache[color] = material = baseMat;
        return material;
    }
}
