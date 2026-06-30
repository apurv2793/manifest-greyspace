using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector3 direction;
    public float speed = 20f;
    public int damage = 25;
    public string owner = "Player";
    public float maxRange = 22f;

    float travelled;

    void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += direction * step;
        travelled += step;

        if (travelled >= maxRange) { Destroy(gameObject); return; }

        if (owner == "Player")
        {
            foreach (GunEnemy e in FindObjectsOfType<GunEnemy>())
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
        Material m = new Material(GetComponent<Renderer>().sharedMaterial);
        m.SetColor("_BaseColor", new Color(1f, 0.45f, 0.1f));
        m.color = new Color(1f, 0.45f, 0.1f);
        fx.GetComponent<Renderer>().material = m;
        Destroy(fx, 0.12f);
    }
}
