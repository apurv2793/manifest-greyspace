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

        // Use cached instance instead of FindObjectOfType for hot-path player lookup
        if (GunCharacter.Instance != null && !GunCharacter.Instance.isDead)
        {
            if (Vector3.Distance(transform.position, GunCharacter.Instance.transform.position) < 0.5f)
            {
                _hit = true;
                GunCharacter.Instance.TakeDamage(damage, transform.position, knockback);
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
