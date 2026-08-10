using UnityEngine;
using System.Collections.Generic;

public static class MaterialCache
{
    private static readonly Dictionary<Color, Material> _cache = new Dictionary<Color, Material>();
    private static Shader _shader;

    private static Shader GetURPLitShader()
    {
        if (_shader == null)
        {
            _shader = Shader.Find("Universal Render Pipeline/Lit");
            if (_shader == null) _shader = Shader.Find("Standard"); // fallback
        }
        return _shader;
    }

    public static Material Get(Color color)
    {
        if (_cache.TryGetValue(color, out var material))
            return material;

        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material baseMat = new Material(tmp.GetComponent<Renderer>().sharedMaterial) { shader = GetURPLitShader() };
        DestroyImmediate(tmp);

        baseMat.SetColor("_BaseColor", color);
        baseMat.color = color;
        
        // VFX materials handle their own transparency settings explicitly
        // (VFXManager.MakeMat creates per-instance clones)

        _cache[color] = material = baseMat;
        return material;
    }
}
