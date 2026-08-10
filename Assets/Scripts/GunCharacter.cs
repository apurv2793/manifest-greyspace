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
    public int maxHealth = 100;

    // Assign to override the default Sword 3-hit combo (e.g. ComboData.Rapid5Hit())
    public ComboData comboData;

    // State (read by GreyspaceScene)
    [HideInInspector] public int health;
    [HideInInspector] public bool isDead;
    [HideInInspector] public GreyspaceScene scene;

    // Set by GreyspaceScene after HUD is built
    public Image healthFill;

    // --- Option B hook: assign a custom visual root here to skip primitive build ---
    // Leave null to use built-in primitive character.
    public GameObject customVisualRoot;

    float nextDash;
    bool isDashing, invincible;
    MeleeAttack _melee;
    System.Collections.Generic.List<ComboData> _weapons;
    int _weaponIndex;
    const int MAX_WEAPONS = 2;

    // Set by GreyspaceScene so weapon name shows on HUD
    [HideInInspector] public UnityEngine.UI.Text weaponLabel;
    Vector3 dashDir;

    // Phase 4: skill tree + loadout. No UI yet — unlock/assign via Inspector or console for now.
    public PlayerInventory inventory = new PlayerInventory();

    // "Humanistic" pass — idle liveliness. All primitive-built visuals parent under
    // this instead of `transform` directly, so a subtle breathing/weight-shift bob
    // can animate the character without touching the root transform Move()/DashInput()
    // actually drive. Only used by the built-in primitive path (BuildPrimitiveCharacter);
    // customVisualRoot (Option B) is a different, unrelated hook and is left alone.
    Transform _visualRoot;
    float _idlePhase;

    // Registry pattern for hot-path FindObjectOfType replacement
    public static GunCharacter Instance { get; private set; }

    // -------------------------------------------------------------------------
    void Start()
    {
        health = maxHealth;
        if (customVisualRoot == null) BuildPrimitiveCharacter();
        else Debug.Log("GunCharacter: Using custom visual root — " + customVisualRoot.name);

        // Player starts with only Sword; second slot is empty (max 2 weapons)
        _weapons = new System.Collections.Generic.List<ComboData> { ContentLibrary.Combo("Sword") };
        _weaponIndex = 0;
        _melee = gameObject.AddComponent<MeleeAttack>();
        _melee.comboData = comboData != null ? comboData : _weapons[0];
        var _special = gameObject.AddComponent<SpecialAttack>();
        UpdateWeaponLabel();
    }

    // =========================================================================
    // OPTION A — Hades-style primitive character
    // =========================================================================
    void BuildPrimitiveCharacter()
    {
        GameObject visualRootGO = new GameObject("VisualRoot");
        visualRootGO.transform.SetParent(transform, false);
        _visualRoot = visualRootGO.transform;
        // Per-character phase offset so multiple instances (if ever spawned) don't
        // bob in lockstep — deterministic-enough for a purely cosmetic wobble.
        _idlePhase = (GetInstanceID() % 1000) * 0.01f;

        Material body   = MaterialCache.Get(new Color(0.17f, 0.17f, 0.22f));
        Material accent = MaterialCache.Get(new Color(0.52f, 0.04f, 0.04f));
        // Warmed slightly from the original flat tan for a more natural read.
        Material skin   = MaterialCache.Get(new Color(0.76f, 0.58f, 0.46f));
        Material silver = MaterialCache.Get(new Color(0.76f, 0.76f, 0.86f));
        Material gold   = MaterialCache.Get(new Color(0.74f, 0.60f, 0.12f));
        Material hair   = MaterialCache.Get(new Color(0.07f, 0.04f, 0.04f));
        Material eye    = MaterialCache.Get(new Color(0.18f, 0.38f, 0.72f));

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
        pivot.transform.SetParent(_visualRoot, false);
        pivot.transform.localPosition = new Vector3(0.44f, 0.74f, 0.08f);
        pivot.transform.localEulerAngles = new Vector3(-18f, 0, -12f);

        PC(pivot.transform, PrimitiveType.Cylinder, "Handle",  new Vector3(0, 0,     0),    new Vector3(0.05f, 0.18f, 0.05f), body);
        PC(pivot.transform, PrimitiveType.Cube,     "Guard",   new Vector3(0, 0.20f, 0),    new Vector3(0.26f, 0.04f, 0.06f), gold);
        PC(pivot.transform, PrimitiveType.Cube,     "Blade",   new Vector3(0, 0.20f+0.44f,0), new Vector3(0.055f, 0.88f, 0.04f), silver);
        PC(pivot.transform, PrimitiveType.Cube,     "Tip",     new Vector3(0, 0.20f+0.88f+0.14f,0), new Vector3(0.035f, 0.28f, 0.03f), silver);
        // Gripping hand — closes the "floating weapon" gap: previously the sword was
        // pivoted from empty space near the forearm with nothing actually holding it.
        // Slightly squashed sphere reads as a closed fist at this primitive fidelity.
        PC(pivot.transform, PrimitiveType.Sphere,   "GripHand", new Vector3(0, -0.05f, 0),  new Vector3(0.10f, 0.08f, 0.09f), skin);

        Debug.Log("GunCharacter: Primitive character built (Option A)");
    }

    // Idle liveliness — a small breathing/weight-shift bob on the visual root only,
    // gated on standing still so it never fights Move()/DashInput()'s actual motion.
    // Purely cosmetic: never touches transform.position, only the visual child.
    void UpdateIdle()
    {
        if (_visualRoot == null || isDashing) return;
        bool moving = InputRouter.MoveAxis().sqrMagnitude > 0.01f;
        float targetBob = moving ? 0f : Mathf.Sin((Time.time + _idlePhase) * 1.6f) * 0.018f;
        float targetSway = moving ? 0f : Mathf.Sin((Time.time + _idlePhase) * 0.8f) * 1.1f;
        Vector3 lp = _visualRoot.localPosition;
        lp.y = Mathf.Lerp(lp.y, targetBob, 6f * Time.deltaTime);
        _visualRoot.localPosition = lp;
        Vector3 le = _visualRoot.localEulerAngles;
        float currentZ = le.z > 180f ? le.z - 360f : le.z;
        le.z = Mathf.Lerp(currentZ, targetSway, 4f * Time.deltaTime);
        _visualRoot.localEulerAngles = le;
    }

    // Helpers
    void P(PrimitiveType t, string n, Vector3 lp, Vector3 ls, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.name = n; g.transform.SetParent(_visualRoot, false);
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

    // =========================================================================
    // GAMEPLAY
    // =========================================================================
    void Update()
    {
        if (isDead) return;
        Move();
        Aim();
        _melee?.HandleInput();
        DashInput();
        WeaponSwitch();
        UpdateIdle();
        if (healthFill != null)
        {
            float target = Mathf.Max(0f, (float)health / maxHealth);
            healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, target, 6f * Time.deltaTime);
        }
    }

    // Optional (Phase 8) — set by GreyspaceScene per mission to clamp the camera to a zone. Null = no clamp.
    public ZoneBounds currentZone;

    void LateUpdate()
    {
        if (isDead || Camera.main == null) return;
        Vector3 target = transform.position + new Vector3(0, 14, -12);
        if (currentZone != null) target = currentZone.ClampToZone(target);
        Camera.main.transform.position = Vector3.Lerp(
            Camera.main.transform.position, target, 8f * Time.deltaTime);
    }

    void Move()
    {
        if (isDashing) return;
        Vector2 axis = InputRouter.MoveAxis();
        float h = axis.x, v = axis.y;
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
        if (InputRouter.TryAimWorldPoint(Camera.main, transform.position, out Vector3 worldPoint))
        {
            Vector3 dir = worldPoint - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.04f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }


    void WeaponSwitch()
    {
        if (!InputRouter.SwapPressed()) return;
        if (_weapons.Count <= 1) return; // nothing to switch to
        _weaponIndex = (_weaponIndex + 1) % _weapons.Count;
        _melee.comboData = _weapons[_weaponIndex];
        _melee.ResetCombo();
        string wname = _weapons[_weaponIndex].name;
        UpdateWeaponLabel();
        Debug.Log("[Weapon] Switched to " + wname);
    }

    void UpdateWeaponLabel()
    {
        if (weaponLabel == null) return;
        string label = _weapons[_weaponIndex].name;
        if (_weapons.Count > 1)
        {
            int other = (_weaponIndex + 1) % _weapons.Count;
            label += "  |  " + _weapons[other].name + "  [TAB]";
        }
        weaponLabel.text = label;
    }

    public void PickupWeapon(ComboData picked)
    {
        if (_weapons.Count < MAX_WEAPONS)
        {
            // Empty slot available — just add it
            _weapons.Add(picked);
            Debug.Log("[Pickup] Added weapon: " + picked.name
                      + "  (now have " + _weapons.Count + " weapons)");
        }
        else
        {
            // Replace the weapon that is NOT currently equipped (the stored slot)
            int replaceIdx = (_weaponIndex + 1) % _weapons.Count;
            string oldName = _weapons[replaceIdx].name;
            _weapons[replaceIdx] = picked;
            Debug.Log("[Pickup] Replaced " + oldName + " with " + picked.name);
        }

        UpdateWeaponLabel();

        // Keep MeleeAttack in sync with currently equipped weapon
        if (_melee != null) _melee.comboData = _weapons[_weaponIndex];
    }

    void DashInput()
    {
        bool dashPressed = InputRouter.DashPressed();
        if (!dashPressed || Time.time < nextDash) return;
        nextDash = Time.time + dashCooldown;

        Vector2 axis = InputRouter.MoveAxis();
        float h = axis.x, v = axis.y;
        Transform cam = Camera.main.transform;
        Vector3 fwd = cam.forward; fwd.y = 0; fwd.Normalize();
        Vector3 right = cam.right; right.y = 0; right.Normalize();
        dashDir = (fwd * v + right * h).normalized;
        if (dashDir == Vector3.zero) dashDir = transform.forward;
        AudioManager.Play("dash");
        VFXManager.Spawn(EffectType.DashAfterimage, transform.position, new Color(0.6f, 0.8f, 1f));
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

    public void TakeDamage(int dmg, Vector3 sourcePos = default, float force = 0f)
    {
        if (isDead || invincible) return;
        health -= dmg;
        Debug.Log("Player HP: " + health + "/" + maxHealth);
        DamageNumber.Spawn(transform.position + Vector3.up * 2f, dmg, new Color(0.95f, 0.2f, 0.2f));
        AudioManager.Play("player_hit");
        CameraShake.Shake(0.12f, 0.15f);
        if (force > 0f)
        {
            Vector3 dir = transform.position - sourcePos; dir.y = 0;
            if (dir.sqrMagnitude > 0f) StartCoroutine(PlayerKnockback(dir.normalized * force));
        }
        StartCoroutine(DamageFlash());
        if (health <= 0) Die();
    }

    IEnumerator PlayerKnockback(Vector3 push)
    {
        float elapsed = 0f, duration = 0.2f;
        while (elapsed < duration && !isDead)
        {
            float decay = 1f - (elapsed / duration);
            transform.position += push * decay * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator DamageFlash()
    {
        invincible = true;
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        Material[] orig = new Material[rends.Length];
        Material flash = MaterialCache.Get(new Color(1f, 0.15f, 0.15f));
        // sharedMaterial, not material - avoids auto-instantiating a per-renderer clone
        // on every hit, which would defeat MaterialCache's pooling.
        for (int i = 0; i < rends.Length; i++) { orig[i] = rends[i].sharedMaterial; rends[i].sharedMaterial = flash; }
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < rends.Length; i++) { if (rends[i] != null) rends[i].sharedMaterial = orig[i]; }
        invincible = false;
    }

    void Die()
    {
        isDead = true;
        Debug.Log("GunCharacter: Player died.");
        CameraShake.Shake(0.3f, 0.4f);
        if (scene != null) scene.OnPlayerDied();
    }

    // Phase 3 — exposes the equipped weapon for SpecialAttack.cs (fields above are private).
    public ComboData CurrentWeapon => _weapons[_weaponIndex];

    void OnEnable()
    {
        Instance = this;
    }
    
    void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }
}
