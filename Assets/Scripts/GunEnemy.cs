using UnityEngine;
using System.Collections;

public class GunEnemy : MonoBehaviour
{
    public Transform player;
    public float speed = 2.5f;
    public int health = 30;
    public int attackDamage = 10;
    public float attackCooldown = 1.1f;
    public int xpValue = 10;

    int maxHealth;
    float nextAttack;
    bool isDead;
    GameObject hpFill;
    float hpBarWidth = 0.88f;

    // -------------------------------------------------------------------------
    void Start()
    {
        maxHealth = health;
        BuildBody();
        BuildHPBar();
    }

    void BuildBody()
    {
        Material body  = Mat(new Color(0.38f, 0.07f, 0.07f));
        Material dark  = Mat(new Color(0.18f, 0.03f, 0.03f));
        Material glow  = Mat(new Color(1.0f,  0.18f, 0.08f));

        // Legs (stubby)
        P(PrimitiveType.Cylinder, "LegL", new Vector3(-0.14f, 0.28f, 0), new Vector3(0.19f, 0.28f, 0.19f), body);
        P(PrimitiveType.Cylinder, "LegR", new Vector3( 0.14f, 0.28f, 0), new Vector3(0.19f, 0.28f, 0.19f), body);

        // Hulking body
        P(PrimitiveType.Cube,     "Body",  new Vector3(0, 0.88f, 0),      new Vector3(0.72f, 0.62f, 0.52f), body);

        // Big shoulders
        P(PrimitiveType.Sphere,   "ShoL",  new Vector3(-0.48f, 1.08f, 0), new Vector3(0.28f, 0.28f, 0.28f), dark);
        P(PrimitiveType.Sphere,   "ShoR",  new Vector3( 0.48f, 1.08f, 0), new Vector3(0.28f, 0.28f, 0.28f), dark);

        // Arms
        P(PrimitiveType.Capsule,  "ArmL",  new Vector3(-0.50f, 0.72f, 0), new Vector3(0.20f, 0.28f, 0.20f), body);
        P(PrimitiveType.Capsule,  "ArmR",  new Vector3( 0.50f, 0.72f, 0), new Vector3(0.20f, 0.28f, 0.20f), body);

        // Blocky head
        P(PrimitiveType.Cube,     "Head",  new Vector3(0, 1.68f, 0),       new Vector3(0.48f, 0.46f, 0.46f), body);

        // Horns
        GameObject hL = P(PrimitiveType.Cylinder, "HornL", new Vector3(-0.14f, 2.08f, 0), new Vector3(0.065f, 0.20f, 0.065f), dark);
        hL.transform.localEulerAngles = new Vector3(0, 0, -18f);
        GameObject hR = P(PrimitiveType.Cylinder, "HornR", new Vector3( 0.14f, 2.08f, 0), new Vector3(0.065f, 0.20f, 0.065f), dark);
        hR.transform.localEulerAngles = new Vector3(0, 0,  18f);

        // Glowing eyes
        P(PrimitiveType.Sphere, "EyeL", new Vector3(-0.12f, 1.70f, 0.24f), new Vector3(0.09f, 0.08f, 0.05f), glow);
        P(PrimitiveType.Sphere, "EyeR", new Vector3( 0.12f, 1.70f, 0.24f), new Vector3(0.09f, 0.08f, 0.05f), glow);

        // Axe
        P(PrimitiveType.Cylinder, "AxeHaft",  new Vector3(0.62f, 0.8f,  0.12f), new Vector3(0.06f, 0.52f, 0.06f), dark);
        P(PrimitiveType.Cube,     "AxeHead",  new Vector3(0.60f, 1.32f, 0.08f), new Vector3(0.07f, 0.32f, 0.28f), Mat(new Color(0.42f, 0.32f, 0.32f)));
    }

    GameObject P(PrimitiveType t, string n, Vector3 lp, Vector3 ls, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.name = n; g.transform.SetParent(transform, false);
        g.transform.localPosition = lp; g.transform.localScale = ls;
        Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().material = m;
        return g;
    }

    void BuildHPBar()
    {
        float y = 2.55f;
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.name = "HPBG"; bg.transform.SetParent(transform, false);
        bg.transform.localPosition = new Vector3(0, y, 0);
        bg.transform.localScale   = new Vector3(0.92f, 0.08f, 0.022f);
        Destroy(bg.GetComponent<Collider>());
        bg.GetComponent<Renderer>().material = Mat(new Color(0.12f, 0.02f, 0.02f));

        hpFill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hpFill.name = "HPFill"; hpFill.transform.SetParent(transform, false);
        hpFill.transform.localPosition = new Vector3(0, y, -0.013f);
        hpFill.transform.localScale   = new Vector3(hpBarWidth, 0.07f, 0.025f);
        Destroy(hpFill.GetComponent<Collider>());
        hpFill.GetComponent<Renderer>().material = Mat(new Color(0.85f, 0.1f, 0.1f));
    }

    static Material Mat(Color c)
    {
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material m = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        DestroyImmediate(tmp);
        m.SetColor("_BaseColor", c); m.color = c;
        return m;
    }

    // -------------------------------------------------------------------------
    void Update()
    {
        if (isDead || player == null) return;

        Vector3 dir = player.position - transform.position; dir.y = 0;
        float dist = dir.magnitude;

        if (dist > 1.4f)
        {
            transform.position += dir.normalized * speed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        if (dist < 1.4f && Time.time > nextAttack)
        {
            nextAttack = Time.time + attackCooldown;
            GunCharacter pc = player.GetComponent<GunCharacter>();
            if (pc != null) pc.TakeDamage(attackDamage, transform.position, 12f);
        }

        // Update HP bar (left-anchored scale)
        if (hpFill != null)
        {
            float pct = Mathf.Clamp01((float)health / maxHealth);
            Vector3 s = hpFill.transform.localScale; s.x = hpBarWidth * pct;
            hpFill.transform.localScale = s;
            Vector3 p = hpFill.transform.localPosition; p.x = -(hpBarWidth / 2f) * (1f - pct);
            hpFill.transform.localPosition = p;
        }
    }

    // sourcePos and knockbackForce are optional — old callers (Projectile) still work
    public void TakeDamage(int dmg, Vector3 sourcePos = default, float knockbackForce = 0f)
    {
        if (isDead) return;
        health -= dmg;
        if (knockbackForce > 0f)
        {
            Vector3 dir = transform.position - sourcePos; dir.y = 0;
            if (dir.sqrMagnitude > 0f) StartCoroutine(Knockback(dir.normalized * knockbackForce));
        }
        if (health <= 0) StartCoroutine(Die());
        else StartCoroutine(HitFlash());
    }

    IEnumerator Knockback(Vector3 push)
    {
        float elapsed = 0f, duration = 0.25f;
        while (elapsed < duration && !isDead)
        {
            float decay = 1f - (elapsed / duration);
            transform.position += push * decay * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator HitFlash()
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        Material[] orig = new Material[rends.Length];
        Material flash = Mat(Color.white);
        for (int i = 0; i < rends.Length; i++) { orig[i] = rends[i].material; rends[i].material = flash; }
        yield return new WaitForSeconds(0.07f);
        for (int i = 0; i < rends.Length; i++) { if (rends[i] != null) rends[i].material = orig[i]; }
    }

    IEnumerator Die()
    {
        isDead = true;
        GameEvents.FireEnemyDied(xpValue);
        VFXManager.Spawn(EffectType.DeathBurst, transform.position + Vector3.up * 0.8f, new Color(0.8f, 0.2f, 0.2f));
        float t = 0; Vector3 s0 = transform.localScale;
        while (t < 0.28f) { t += Time.deltaTime; transform.localScale = Vector3.Lerp(s0, Vector3.zero, t / 0.28f); yield return null; }
        Destroy(gameObject);
    }
}
