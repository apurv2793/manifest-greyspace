using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GunCharacter : MonoBehaviour
{
    // Tuning
    public float moveSpeed = 6f;
    public float dashSpeed = 22f;
    public float dashDuration = 0.12f;
    public float dashCooldown = 0.85f;
    public float shootCooldown = 0.16f;
    public int maxHealth = 100;

    // State (read by GreyspaceScene)
    [HideInInspector] public int health;
    [HideInInspector] public bool isDead;
    [HideInInspector] public GreyspaceScene scene;

    // Set by GreyspaceScene after HUD is built
    public Image healthFill;

    // --- Option B hook: assign a custom visual root here to skip primitive build ---
    // Leave null to use built-in primitive character.
    public GameObject customVisualRoot;

    float nextShoot, nextDash;
    bool isDashing, invincible;
    Vector3 dashDir;

    // -------------------------------------------------------------------------
    void Start()
    {
        health = maxHealth;
        if (customVisualRoot == null) BuildPrimitiveCharacter();
        else Debug.Log("GunCharacter: Using custom visual root — " + customVisualRoot.name);
    }

    // =========================================================================
    // OPTION A — Hades-style primitive character
    // =========================================================================
    void BuildPrimitiveCharacter()
    {
        Material body   = Mat(new Color(0.17f, 0.17f, 0.22f));
        Material accent = Mat(new Color(0.52f, 0.04f, 0.04f));
        Material skin   = Mat(new Color(0.70f, 0.53f, 0.42f));
        Material silver = Mat(new Color(0.76f, 0.76f, 0.86f));
        Material gold   = Mat(new Color(0.74f, 0.60f, 0.12f));
        Material hair   = Mat(new Color(0.07f, 0.04f, 0.04f));
        Material eye    = Mat(new Color(0.18f, 0.38f, 0.72f));

        // Cape (behind torso — drawn first so it sits behind)
        P(PrimitiveType.Cube,    "Cape",      new Vector3(0, 0.88f, -0.27f), new Vector3(0.62f, 0.95f, 0.07f), accent);

        // Legs
        P(PrimitiveType.Capsule, "LegL",      new Vector3(-0.13f, 0.32f, 0),  new Vector3(0.16f, 0.34f, 0.16f), body);
        P(PrimitiveType.Capsule, "LegR",      new Vector3( 0.13f, 0.32f, 0),  new Vector3(0.16f, 0.34f, 0.16f), body);

        // Hips
        P(PrimitiveType.Cube,    "Hips",      new Vector3(0, 0.62f, 0),       new Vector3(0.40f, 0.10f, 0.28f), body);

        // Belt
        P(PrimitiveType.Cube,    "Belt",      new Vector3(0, 0.64f, 0.01f),   new Vector3(0.42f, 0.07f, 0.30f), gold);

        // Torso lower / upper
        P(PrimitiveType.Cube,    "TorsoLo",   new Vector3(0, 0.78f, 0),       new Vector3(0.40f, 0.20f, 0.28f), body);
        P(PrimitiveType.Cube,    "TorsoHi",   new Vector3(0, 1.05f, 0),       new Vector3(0.50f, 0.22f, 0.28f), body);

        // Chest plate
        P(PrimitiveType.Cube,    "Chest",     new Vector3(0, 1.05f, 0.13f),   new Vector3(0.42f, 0.18f, 0.06f), accent);

        // Shoulders
        P(PrimitiveType.Sphere,  "ShoL",      new Vector3(-0.33f, 1.18f, 0),  new Vector3(0.19f, 0.19f, 0.19f), accent);
        P(PrimitiveType.Sphere,  "ShoR",      new Vector3( 0.33f, 1.18f, 0),  new Vector3(0.19f, 0.19f, 0.19f), accent);

        // Upper arms
        P(PrimitiveType.Capsule, "UArmL",     new Vector3(-0.36f, 0.95f, 0),  new Vector3(0.12f, 0.20f, 0.12f), body);
        P(PrimitiveType.Capsule, "UArmR",     new Vector3( 0.36f, 0.95f, 0),  new Vector3(0.12f, 0.20f, 0.12f), body);

        // Forearms
        P(PrimitiveType.Cylinder,"FArmL",     new Vector3(-0.35f, 0.72f, 0),  new Vector3(0.09f, 0.17f, 0.09f), skin);
        P(PrimitiveType.Cylinder,"FArmR",     new Vector3( 0.35f, 0.72f, 0),  new Vector3(0.09f, 0.17f, 0.09f), skin);

        // Head
        P(PrimitiveType.Sphere,  "Head",      new Vector3(0, 1.64f, 0),       new Vector3(0.35f, 0.38f, 0.35f), skin);

        // Hair
        P(PrimitiveType.Cube,    "HairTop",   new Vector3(0, 1.88f, -0.03f),  new Vector3(0.29f, 0.16f, 0.26f), hair);
        P(PrimitiveType.Cube,    "HairBack",  new Vector3(0, 1.78f, -0.21f),  new Vector3(0.26f, 0.30f, 0.09f), hair);

        // Eyes
        P(PrimitiveType.Sphere,  "EyeL",      new Vector3(-0.095f, 1.65f, 0.16f), new Vector3(0.065f, 0.055f, 0.04f), eye);
        P(PrimitiveType.Sphere,  "EyeR",      new Vector3( 0.095f, 1.65f, 0.16f), new Vector3(0.065f, 0.055f, 0.04f), eye);

        // ---- Sword (right side, tilted) ----
        GameObject pivot = new GameObject("SwordPivot");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = new Vector3(0.44f, 0.74f, 0.08f);
        pivot.transform.localEulerAngles = new Vector3(-18f, 0, -12f);

        PC(pivot.transform, PrimitiveType.Cylinder, "Handle",  new Vector3(0, 0,     0),    new Vector3(0.05f, 0.18f, 0.05f), body);
        PC(pivot.transform, PrimitiveType.Cube,     "Guard",   new Vector3(0, 0.20f, 0),    new Vector3(0.26f, 0.04f, 0.06f), gold);
        PC(pivot.transform, PrimitiveType.Cube,     "Blade",   new Vector3(0, 0.20f+0.44f,0), new Vector3(0.055f, 0.88f, 0.04f), silver);
        PC(pivot.transform, PrimitiveType.Cube,     "Tip",     new Vector3(0, 0.20f+0.88f+0.14f,0), new Vector3(0.035f, 0.28f, 0.03f), silver);

        Debug.Log("GunCharacter: Primitive character built (Option A)");
    }

    // Helpers
    void P(PrimitiveType t, string n, Vector3 lp, Vector3 ls, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.name = n; g.transform.SetParent(transform, false);
        g.transform.localPosition = lp; g.transform.localScale = ls;
        Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().material = m;
    }

    void PC(Transform parent, PrimitiveType t, string n, Vector3 lp, Vector3 ls, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.name = n; g.transform.SetParent(parent, false);
        g.transform.localPosition = lp; g.transform.localScale = ls;
        Destroy(g.GetComponent<Collider>()); g.GetComponent<Renderer>().material = m;
    }

    static Material Mat(Color c)
    {
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material m = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        DestroyImmediate(tmp);
        m.SetColor("_BaseColor", c); m.color = c;
        return m;
    }

    // =========================================================================
    // GAMEPLAY
    // =========================================================================
    void Update()
    {
        if (isDead) return;
        Move();
        Aim();
        Shoot();
        DashInput();
        if (healthFill != null)
            healthFill.fillAmount = Mathf.Max(0f, (float)health / maxHealth);
    }

    void LateUpdate()
    {
        if (isDead || Camera.main == null) return;
        Vector3 target = transform.position + new Vector3(0, 14, -12);
        Camera.main.transform.position = Vector3.Lerp(
            Camera.main.transform.position, target, 8f * Time.deltaTime);
    }

    void Move()
    {
        if (isDashing) return;
        float h = 0, v = 0;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  h -= 1;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    v += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  v -= 1;
        if (h == 0 && v == 0) return;

        // Camera-relative movement — W goes "into" the screen from the player's view
        Transform cam = Camera.main.transform;
        Vector3 fwd = cam.forward; fwd.y = 0; fwd.Normalize();
        Vector3 right = cam.right; right.y = 0; right.Normalize();
        transform.position += (fwd * v + right * h).normalized * moveSpeed * Time.deltaTime;
    }

    void Aim()
    {
        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);
        if (plane.Raycast(ray, out float dist))
        {
            Vector3 dir = ray.GetPoint(dist) - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.04f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    void Shoot()
    {
        if (!Input.GetMouseButton(0) || Time.time < nextShoot) return;
        nextShoot = Time.time + shootCooldown;

        Vector3 spawn = transform.position + transform.forward * 0.7f + Vector3.up * 1.1f;
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.name = "Projectile";
        p.transform.position = spawn;
        p.transform.localScale = Vector3.one * 0.17f;
        Destroy(p.GetComponent<Collider>());
        p.GetComponent<Renderer>().material = Mat(new Color(1f, 0.85f, 0.1f));
        Projectile proj = p.AddComponent<Projectile>();
        proj.direction = transform.forward;
        proj.owner = "Player";
    }

    void DashInput()
    {
        if (!Input.GetKeyDown(KeyCode.LeftShift) || Time.time < nextDash) return;
        nextDash = Time.time + dashCooldown;

        float h = 0, v = 0;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  h -= 1;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    v += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  v -= 1;
        Transform cam = Camera.main.transform;
        Vector3 fwd = cam.forward; fwd.y = 0; fwd.Normalize();
        Vector3 right = cam.right; right.y = 0; right.Normalize();
        dashDir = (fwd * v + right * h).normalized;
        if (dashDir == Vector3.zero) dashDir = transform.forward;
        StartCoroutine(DoDash());
    }

    IEnumerator DoDash()
    {
        isDashing = true; invincible = true;
        float end = Time.time + dashDuration;
        while (Time.time < end)
        {
            transform.position += dashDir * dashSpeed * Time.deltaTime;
            yield return null;
        }
        isDashing = false;
        yield return new WaitForSeconds(0.18f);
        invincible = false;
    }

    public void TakeDamage(int dmg)
    {
        if (isDead || invincible) return;
        health -= dmg;
        Debug.Log("Player HP: " + health + "/" + maxHealth);
        StartCoroutine(DamageFlash());
        if (health <= 0) Die();
    }

    IEnumerator DamageFlash()
    {
        invincible = true;
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        Material[] orig = new Material[rends.Length];
        Material flash = Mat(new Color(1f, 0.15f, 0.15f));
        for (int i = 0; i < rends.Length; i++) { orig[i] = rends[i].material; rends[i].material = flash; }
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < rends.Length; i++) { if (rends[i] != null) rends[i].material = orig[i]; }
        invincible = false;
    }

    void Die()
    {
        isDead = true;
        Debug.Log("GunCharacter: Player died.");
        if (scene != null) scene.OnPlayerDied();
    }
}
