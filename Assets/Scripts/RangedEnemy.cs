using System.Collections;
using UnityEngine;

// GLM 5.1 via Manifest OS (call_id 166) — corrected: public fields, null check, projectile collider removed, yield return null
public class RangedEnemy : EnemyBase
{
    public float preferredRange   = 10f;
    public float retreatRange     = 5f;
    public float projectileSpeed  = 6f;
    public float fireRate         = 2f;

    protected override void BuildVisual()
    {
        P(PrimitiveType.Sphere, "Body",    new Vector3(0, 0.45f, 0), new Vector3(0.75f, 0.75f, 0.75f), Mat(new Color(0.1f, 0.7f, 0.65f)));
        P(PrimitiveType.Cube,   "Antenna", new Vector3(0, 1.05f, 0), new Vector3(0.07f, 0.4f, 0.07f),  Mat(Color.white));
    }

    protected override void UpdateBehaviour()
    {
        if (isDead || player == null) return;

        Vector3 dir  = player.position - transform.position; dir.y = 0;
        float   dist = dir.magnitude;
        Vector3 norm = dist > 0.001f ? dir / dist : Vector3.forward;

        if (dist < retreatRange)
            transform.position += -norm * speed * Time.deltaTime;
        else if (dist > preferredRange + 2f)
            transform.position +=  norm * speed * Time.deltaTime;
        else
            transform.position += new Vector3(-norm.z, 0, norm.x) * speed * Time.deltaTime;

        if (dist < preferredRange + 2f && Time.time >= nextAttack)
        {
            nextAttack = Time.time + fireRate;
            StartCoroutine(FireProjectile());
        }
    }

    IEnumerator FireProjectile()
    {
        if (player == null) yield break;

        GameObject proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(proj.GetComponent<Collider>());
        proj.transform.localScale = Vector3.one * 0.25f;
        proj.transform.position   = transform.position;
        proj.GetComponent<Renderer>().material = Mat(Color.red);

        Vector3 fireDir = (player.position - transform.position); fireDir.y = 0;
        if (fireDir.sqrMagnitude > 0.001f) fireDir.Normalize(); else fireDir = transform.forward;

        float traveled = 0f;
        while (proj != null)
        {
            float step = projectileSpeed * Time.deltaTime;
            proj.transform.position += fireDir * step;
            traveled += step;

            if (player != null && Vector3.Distance(proj.transform.position, player.position) < 0.4f)
            {
                GunCharacter gc = player.GetComponent<GunCharacter>();
                if (gc != null) gc.TakeDamage(attackDamage);
                Destroy(proj);
                yield break;
            }
            if (traveled > 18f) { Destroy(proj); yield break; }

            yield return null;
        }
    }
}
