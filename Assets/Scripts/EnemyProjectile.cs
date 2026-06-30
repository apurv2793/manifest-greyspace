using UnityEngine;
using System.Collections;

// Tiny self-contained projectile fired by RangedEnemy.
// Moves in a fixed direction, deals damage + knockback on player contact,
// destroys itself after lifetime or on hit.
public class EnemyProjectile : MonoBehaviour
{
    public Vector3 direction;    // normalized, set by spawner
    public float   moveSpeed  = 10f;
    public int     damage     = 12;
    public float   knockback  = 5f;
    public float   lifetime   = 3f;

    bool _hit;

    void Start()
    {
        StartCoroutine(LifetimeKill());
    }

    void Update()
    {
        if (_hit) return;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Player proximity check — no Physics.OverlapSphere, just distance
        GunCharacter[] players = FindObjectsOfType<GunCharacter>();
        foreach (var pc in players)
        {
            if (pc.isDead) continue;
            if (Vector3.Distance(transform.position, pc.transform.position) < 0.5f)
            {
                _hit = true;
                pc.TakeDamage(damage, transform.position, knockback);
                Destroy(gameObject);
                return;
            }
        }
    }

    IEnumerator LifetimeKill()
    {
        yield return new WaitForSeconds(lifetime);
        if (!_hit) Destroy(gameObject);
    }
}
