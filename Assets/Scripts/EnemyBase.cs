using UnityEngine;
using System.Collections;

// GLM 5.1 via Manifest OS (call_id 162) — corrected: URP _BaseColor, P() parent, GameEvents stub removed
public abstract class EnemyBase : MonoBehaviour
{
    public Transform player;
    public int   health;
    public int   maxHealth;
    public float speed;
    public int   attackDamage;
    public float attackCooldown;
    public int   xpValue = 10;

    protected bool  isDead;
    protected float nextAttack;

    private GameObject hpBarBg, hpBarFill;
    private Vector3    hpBarBaseScale, hpBarBasePos;
    private Renderer[] childRenderers;
    private Material[] originalMaterials;

    void Start()
    {
        maxHealth = health;
        BuildHPBar();
        BuildVisual();
    }

    protected abstract void BuildVisual();
    protected abstract void UpdateBehaviour();

    void Update()
    {
        if (isDead || player == null) return;
        UpdateBehaviour();
        UpdateHPBar();
    }

    // ── Combat ────────────────────────────────────────────────────────────────
    public void TakeDamage(int dmg, Vector3 sourcePos = default, float knockbackForce = 0f)
    {
        if (isDead) return;
        health -= dmg;
        if (knockbackForce > 0f)
        {
            Vector3 dir = (transform.position - sourcePos); dir.y = 0;
            if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
            StartCoroutine(Knockback(dir.normalized * knockbackForce));
        }
        StartCoroutine(HitFlash());
        if (health <= 0) StartCoroutine(Die());
    }

    IEnumerator Knockback(Vector3 push)
    {
        float elapsed = 0f, duration = 0.25f;
        while (elapsed < duration && !isDead)
        {
            transform.position += push * (1f - elapsed / duration) * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator HitFlash()
    {
        childRenderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[childRenderers.Length];
        for (int i = 0; i < childRenderers.Length; i++) originalMaterials[i] = childRenderers[i].material;

        Material white = Mat(Color.white);
        foreach (var r in childRenderers) r.material = white;
        yield return new WaitForSecondsRealtime(0.07f);
        for (int i = 0; i < childRenderers.Length; i++)
            if (childRenderers[i] != null) childRenderers[i].material = originalMaterials[i];
    }

    IEnumerator Die()
    {
        isDead = true;
        GameEvents.FireEnemyDied(xpValue);
        if (hpBarBg)   hpBarBg.SetActive(false);
        if (hpBarFill) hpBarFill.SetActive(false);

        float elapsed = 0f, duration = 0.28f;
        Vector3 s0 = transform.localScale;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(s0, Vector3.zero, elapsed / duration);
            yield return null;
        }
        Destroy(gameObject);
    }

    // ── HP bar ────────────────────────────────────────────────────────────────
    void BuildHPBar()
    {
        float y = 2.4f;
        hpBarBg   = P(PrimitiveType.Cube, "HPBg",   new Vector3(0, y, 0),        new Vector3(0.92f, 0.08f, 0.022f), Mat(new Color(0.12f, 0.02f, 0.02f)));
        hpBarFill = P(PrimitiveType.Cube, "HPFill", new Vector3(0, y, -0.013f),  new Vector3(0.88f, 0.07f, 0.025f), Mat(new Color(0.85f, 0.1f, 0.1f)));
        hpBarBaseScale = hpBarFill.transform.localScale;
        hpBarBasePos   = hpBarFill.transform.localPosition;
    }

    void UpdateHPBar()
    {
        if (hpBarFill == null || isDead) return;
        float pct = Mathf.Clamp01((float)health / maxHealth);
        Vector3 s = hpBarBaseScale; s.x = hpBarBaseScale.x * pct;
        hpBarFill.transform.localScale = s;
        Vector3 p = hpBarBasePos; p.x = -(hpBarBaseScale.x / 2f) * (1f - pct);
        hpBarFill.transform.localPosition = p;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    protected static Material Mat(Color c)
    {
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material m = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        DestroyImmediate(tmp);
        m.SetColor("_BaseColor", c); m.color = c;
        return m;
    }

    protected GameObject P(PrimitiveType t, string n, Vector3 lp, Vector3 ls, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.name = n;
        g.transform.SetParent(transform, false);
        g.transform.localPosition = lp;
        g.transform.localScale    = ls;
        Destroy(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().material = m;
        return g;
    }
}
