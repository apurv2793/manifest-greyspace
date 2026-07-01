using UnityEngine;

// GLM 5.1 via Manifest OS (call_id 169) — corrected: collapsed redundant shield material creation
public class ShielderEnemy : EnemyBase
{
    public int   shieldHits      = 3;
    public float shieldReduction = 0.8f;
    public float frontalArc      = 100f;

    public int   _health         = 60;
    public float _speed          = 1.8f;
    public int   _attackDamage   = 12;
    public float _attackCooldown = 1.2f;
    public int   _xpValue        = 25;

    GameObject shieldVisual;
    Material   shieldMat;
    float      shieldFlashTimer;

    void Start()
    {
        health         = _health;
        maxHealth      = _health;
        speed          = _speed;
        attackDamage   = _attackDamage;
        attackCooldown = _attackCooldown;
        xpValue        = _xpValue;

        BuildVisual();
    }

    protected override void BuildVisual()
    {
        Material bodyMat = Mat(new Color(0.35f, 0.35f, 0.4f));
        P(PrimitiveType.Capsule, "Body", Vector3.up * 0.5f, new Vector3(0.8f, 0.5f, 0.8f), bodyMat);

        shieldMat = Mat(new Color(0.2f, 0.4f, 1f));
        shieldVisual = P(PrimitiveType.Cube, "Shield", new Vector3(0, 0.75f, 0.6f), new Vector3(1.2f, 1.1f, 0.1f), shieldMat);
    }

    protected override void UpdateBehaviour()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            transform.forward = dir.normalized;

        transform.position += dir.normalized * speed * Time.deltaTime;

        if (shieldFlashTimer > 0)
        {
            shieldFlashTimer -= Time.deltaTime;
            if (shieldFlashTimer <= 0 && shieldHits > 0)
            {
                Color blue = new Color(0.2f, 0.4f, 1f);
                shieldMat.SetColor("_BaseColor", blue); shieldMat.color = blue;
            }
        }
    }

    public void TakeDamageDirectional(int dmg, Vector3 sourcePos, float knockbackForce)
    {
        if (shieldHits > 0)
        {
            Vector3 toSource = sourcePos - transform.position;
            toSource.y = 0;
            float angle = Vector3.Angle(transform.forward, toSource);

            if (angle <= frontalArc / 2f)
            {
                int reducedDmg = Mathf.Max(1, dmg - Mathf.RoundToInt(dmg * shieldReduction));
                shieldHits--;

                if (shieldHits <= 0)
                {
                    Color broken = new Color(0.4f, 0.4f, 0.4f, 0.3f);
                    shieldMat.SetColor("_BaseColor", broken); shieldMat.color = broken;
                }
                else
                {
                    shieldMat.SetColor("_BaseColor", Color.white); shieldMat.color = Color.white;
                    shieldFlashTimer = 0.1f;
                }

                TakeDamage(reducedDmg, sourcePos, knockbackForce);
                return;
            }
        }

        TakeDamage(dmg, sourcePos, knockbackForce);
    }
}
