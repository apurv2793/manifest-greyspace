using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    public Vector3 direction;
    public float speed = 20f;
    public int damage = 25;
    public string owner = "Player";
    public float maxRange = 22f;

    float travelled;

    // Registry pattern for hot-path FindObjectsOfType replacement
    public static readonly List<Projectile> Active = new List<Projectile>();

    void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
    }

    void OnDisable()
    {
        Active.Remove(this);
    }

    void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += direction * step;
        travelled += step;

        if (travelled >= maxRange) { Destroy(gameObject); return; }

        if (owner == "Player")
        {
            // Use registry instead of FindObjectsOfType
            foreach (GunEnemy e in GunEnemy.Active)
            {
                if (Vector3.Distance(transform.position, e.transform.position + Vector3.up) < 0.75f)
                {
                    e.TakeDamage(damage);
                    SpawnHit();
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    void SpawnHit()
    {
        GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fx.transform.position = transform.position;
        fx.transform.localScale = Vector3.one * 0.35f;
        Destroy(fx.GetComponent<Collider>());
        fx.GetComponent<Renderer>().material = MaterialCache.Get(new Color(1f, 0.45f, 0.1f));
        Destroy(fx, 0.12f);
    }
}
