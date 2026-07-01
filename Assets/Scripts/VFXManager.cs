using System.Collections;
using UnityEngine;

// Groq llama-3.3-70b via Manifest OS (call_id 186) — corrected: dropped unused spawnedObjects field
public enum EffectType { HitSparks, DeathBurst, DashAfterimage, LevelUpBurst }

public class VFXManager : MonoBehaviour
{
    static VFXManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        instance = new GameObject("VFXManager").AddComponent<VFXManager>();
        instance.gameObject.hideFlags = HideFlags.HideInHierarchy;
    }

    public static void Spawn(EffectType effect, Vector3 pos, Color color)
    {
        if (instance == null) Initialize();
        instance.StartCoroutine(EffectCoroutine(effect, pos, color));
    }

    static IEnumerator EffectCoroutine(EffectType effect, Vector3 pos, Color color)
    {
        switch (effect)
        {
            case EffectType.HitSparks:       yield return HitSparks(pos, color); break;
            case EffectType.DeathBurst:      yield return DeathBurst(pos, color); break;
            case EffectType.DashAfterimage:  yield return DashAfterimage(pos, color); break;
            case EffectType.LevelUpBurst:    yield return LevelUpBurst(pos, color); break;
        }
    }

    static Material MakeMat(Color color)
    {
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material mat = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        Destroy(tmp);
        mat.SetColor("_BaseColor", color); mat.color = color;
        mat.SetFloat("_Surface", 1); mat.renderQueue = 3000;
        return mat;
    }

    static IEnumerator HitSparks(Vector3 pos, Color color)
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(p.GetComponent<Collider>());
            p.transform.position = pos;
            p.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            p.GetComponent<Renderer>().material = MakeMat(color);

            Vector3 dir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            float speed = Random.Range(3f, 6f);

            float timer = 0f;
            while (timer < 0.15f)
            {
                p.transform.position += dir * speed * Time.deltaTime;
                p.transform.localScale = Vector3.Lerp(new Vector3(0.08f, 0.08f, 0.08f), Vector3.zero, timer / 0.15f);
                timer += Time.unscaledDeltaTime;
                yield return new WaitForEndOfFrame();
            }
            Destroy(p);
        }
    }

    static IEnumerator DeathBurst(Vector3 pos, Color color)
    {
        for (int i = 0; i < 8; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(p.GetComponent<Collider>());
            p.transform.position = pos;
            p.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            Material mat = MakeMat(color);
            p.GetComponent<Renderer>().material = mat;

            Vector3 dir = new Vector3(Random.Range(-1f, 1f), Random.Range(0f, 1f), Random.Range(-1f, 1f)).normalized;
            float speed = 5f;

            float timer = 0f;
            while (timer < 0.35f)
            {
                p.transform.position += dir * speed * Time.deltaTime;
                Color c = color; c.a = Mathf.Lerp(1f, 0f, timer / 0.35f);
                mat.color = c;
                timer += Time.unscaledDeltaTime;
                yield return new WaitForEndOfFrame();
            }
            Destroy(p);
        }
    }

    static IEnumerator DashAfterimage(Vector3 pos, Color color)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(p.GetComponent<Collider>());
        p.transform.position = pos;
        p.transform.localScale = new Vector3(0.4f, 0.9f, 0.4f);
        Material mat = MakeMat(color);
        Color start = color; start.a = 0.4f;
        mat.color = start;
        p.GetComponent<Renderer>().material = mat;

        float timer = 0f;
        while (timer < 0.2f)
        {
            Color c = color; c.a = Mathf.Lerp(0.4f, 0f, timer / 0.2f);
            mat.color = c;
            timer += Time.unscaledDeltaTime;
            yield return new WaitForEndOfFrame();
        }
        Destroy(p);
    }

    static IEnumerator LevelUpBurst(Vector3 pos, Color color)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 ringDir = new Vector3(Mathf.Cos(i * Mathf.PI * 2f / 10f), 0, Mathf.Sin(i * Mathf.PI * 2f / 10f));
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(p.GetComponent<Collider>());
            p.transform.position = pos + ringDir * 0.5f;
            p.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            Material mat = MakeMat(color);
            p.GetComponent<Renderer>().material = mat;

            float speed = 5f;
            float timer = 0f;
            while (timer < 0.5f)
            {
                p.transform.position += Vector3.up * speed * Time.deltaTime;
                p.transform.position += ringDir * (timer / 0.5f) * 0.5f * Time.deltaTime;
                p.transform.localScale = Vector3.Lerp(new Vector3(0.1f, 0.1f, 0.1f), new Vector3(0.2f, 0.2f, 0.2f), timer / 0.5f);
                Color c = color; c.a = Mathf.Lerp(1f, 0f, timer / 0.5f);
                mat.color = c;
                timer += Time.unscaledDeltaTime;
                yield return new WaitForEndOfFrame();
            }
            Destroy(p);
        }
    }
}
