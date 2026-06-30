using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GreyspaceScene : MonoBehaviour
{
    // ── Mode ─────────────────────────────────────────────────────────────────
    enum Mode { Hub, Mission }
    Mode mode = Mode.Hub;

    // ── Shared objects (persist across mode switches) ─────────────────────────
    GameObject playerGO, worldRoot, hudRoot;
    GunCharacter player;
    Image healthFill, xpFill;
    Text waveText, statusText, promptText, xpText, levelText;

    // ── Hub state ─────────────────────────────────────────────────────────────
    List<MissionPortal> portals = new List<MissionPortal>();
    const float INTERACT_RADIUS = 2.8f;

    // ── Mission state ─────────────────────────────────────────────────────────
    int wave, totalWaves;
    bool playerDead, missionComplete;
    MissionDefinition currentMission;

    // =========================================================================
    void OnEnable()  => GameEvents.OnEnemyDied += OnEnemyKilled;
    void OnDisable() => GameEvents.OnEnemyDied -= OnEnemyKilled;

    void OnEnemyKilled(int xpValue)
    {
        bool leveledUp = SceneState.AddXP(xpValue);
        UpdateXP();
        if (leveledUp) StartCoroutine(LevelUpFlash());
    }

    IEnumerator LevelUpFlash()
    {
        if (levelText == null) yield break;
        Color orig = levelText.color;
        levelText.color = new Color(1f, 0.9f, 0.1f);
        yield return new WaitForSeconds(0.8f);
        levelText.color = orig;
    }

    void Start()
    {
        SetupCamera();
        SpawnPlayer();
        BuildHUD();
        EnterHub();
        Debug.Log("Controls: WASD=Move | Mouse=Aim | J=Light | K=Heavy | Tab=Weapon | Space/Shift=Dash");
    }

    // ── Hub ───────────────────────────────────────────────────────────────────
    void EnterHub()
    {
        mode = Mode.Hub;
        wave = 0; playerDead = false; missionComplete = false;
        portals.Clear();

        // Clean up previous world
        ClearWorld();
        ClearEnemies();

        worldRoot = new GameObject("World");
        BuildHubFloor();
        BuildHubDecor();
        SpawnPortals();

        // Reset player to hub spawn
        if (playerGO) playerGO.transform.position = Vector3.zero;
        if (player) { player.isDead = false; if (player.health <= 0) player.health = player.maxHealth; }

        if (waveText)  waveText.text  = "";
        if (statusText) statusText.text = "";

        UpdateXP();

        if (SceneState.missionWon)
        {
            StartCoroutine(ShowRewardToast());
            SceneState.missionWon = false;
        }

        Debug.Log("Hub: Walk to a portal and press E.");
    }

    void BuildHubFloor()
    {
        Color a = new Color(0.55f, 0.48f, 0.35f);
        Color b = new Color(0.48f, 0.42f, 0.30f);
        for (int x = -18; x < 18; x++)
        for (int z = -18; z < 18; z++)
            Tile(new Vector3(x, -0.5f, z), (Mathf.Abs(x + z) % 2 == 0) ? a : b);

        Color wall = new Color(0.40f, 0.34f, 0.24f);
        for (float i = -16f; i <= 16f; i += 3f)
        {
            Block(new Vector3(i, 0.5f,  17.5f), new Vector3(2.8f, 2f, 0.5f), wall);
            Block(new Vector3(i, 0.5f, -17.5f), new Vector3(2.8f, 2f, 0.5f), wall);
            Block(new Vector3( 17.5f, 0.5f, i), new Vector3(0.5f, 2f, 2.8f), wall);
            Block(new Vector3(-17.5f, 0.5f, i), new Vector3(0.5f, 2f, 2.8f), wall);
        }
    }

    void BuildHubDecor()
    {
        Color dark = new Color(0.18f, 0.14f, 0.10f);
        Color gold = new Color(0.72f, 0.58f, 0.12f);
        Color col  = new Color(0.45f, 0.38f, 0.26f);

        // Central obelisk
        Block(new Vector3(0, 0.8f,  0), new Vector3(0.7f, 1.6f, 0.7f), dark);
        Block(new Vector3(0, 2.0f,  0), new Vector3(0.5f, 1.0f, 0.5f), dark);
        GameObject tip = Block(new Vector3(0, 2.8f, 0), new Vector3(0.38f, 0.38f, 0.38f), gold);
        tip.transform.localEulerAngles = new Vector3(45, 45, 0);

        // Corner columns
        foreach (Vector3 p in new[] {
            new Vector3( 5, 1f,  5), new Vector3(-5, 1f,  5),
            new Vector3( 5, 1f, -5), new Vector3(-5, 1f, -5) })
        {
            Prim(PrimitiveType.Cylinder, p, new Vector3(0.28f, 1.5f, 0.28f), col);
            Block(p + new Vector3(0, 1.8f, 0), new Vector3(0.45f, 0.14f, 0.45f), gold);
        }
    }

    void SpawnPortals()
    {
        var m1 = ScriptableObject.CreateInstance<MissionDefinition>();
        m1.missionId   = "proving_ground";
        m1.displayName = "The Proving Ground";
        m1.waves       = 3;
        m1.rewardXP    = 75;
        m1.description = "Survive three waves of enemies.";

        var m2 = ScriptableObject.CreateInstance<MissionDefinition>();
        m2.missionId    = "inner_sanctum";
        m2.displayName  = "The Inner Sanctum";
        m2.waves        = 5;
        m2.rewardXP     = 150;
        m2.requiredFlag  = "proving_ground_complete";
        m2.lockedReason  = "Clear The Proving Ground first";

        AddPortal(new Vector3(0, 0, 12),  Quaternion.Euler(0, 180, 0), m1);
        AddPortal(new Vector3(-10, 0, 6), Quaternion.Euler(0, 135, 0), m2);
    }

    void AddPortal(Vector3 pos, Quaternion rot, MissionDefinition def)
    {
        GameObject go = new GameObject("Portal_" + def.missionId);
        go.transform.SetParent(worldRoot.transform);
        go.transform.position = pos;
        go.transform.rotation = rot;
        MissionPortal mp = go.AddComponent<MissionPortal>();
        mp.mission = def;
        portals.Add(mp);
    }

    // ── Mission ───────────────────────────────────────────────────────────────
    void EnterMission(MissionDefinition def)
    {
        mode = Mode.Mission;
        currentMission = def;
        totalWaves = def.waves;
        wave = 0; playerDead = false; missionComplete = false;
        portals.Clear();

        ClearWorld();

        worldRoot = new GameObject("World");
        BuildArenaFloor();

        SceneState.currentMissionId   = def.missionId;
        SceneState.currentMissionName  = def.displayName;
        SceneState.currentMissionXP    = def.rewardXP;

        if (playerGO) playerGO.transform.position = Vector3.zero;
        if (player && player.isDead) { player.isDead = false; player.health = player.maxHealth; }
        if (statusText) { statusText.color = new Color(0.8f, 0.07f, 0.07f); statusText.text = ""; }

        StartCoroutine(WaveLoop());
        Debug.Log("Mission: " + def.displayName);
    }

    void BuildArenaFloor()
    {
        for (int x = -15; x < 15; x++)
        for (int z = -15; z < 15; z++)
        {
            float sh = (Mathf.Abs(x + z) % 2 == 0) ? 0.15f : 0.21f;
            Tile(new Vector3(x, -0.5f, z), new Color(sh, sh, sh + 0.02f));
        }
    }

    IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(1.5f);
        while (!playerDead && wave < totalWaves)
        {
            wave++;
            if (waveText) waveText.text = $"Wave {wave} / {totalWaves}";
            Debug.Log($"Wave {wave} starting");

            int count = 2 + wave;
            Vector3 origin = playerGO ? playerGO.transform.position : Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count);
                float r = 9f + Random.Range(0f, 4f);
                SpawnEnemy(origin + new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * r, 0,
                                                Mathf.Cos(angle * Mathf.Deg2Rad) * r));
            }

            while (!playerDead && FindObjectsOfType<GunEnemy>().Length > 0)
                yield return new WaitForSeconds(0.4f);

            if (!playerDead)
            {
                if (waveText) waveText.text = $"Wave {wave} cleared!";
                yield return new WaitForSeconds(2f);
            }
        }

        if (!playerDead)
        {
            missionComplete = true;
            GunCharacter pc = playerGO?.GetComponent<GunCharacter>();
            if (pc != null) SceneState.SavePlayer(pc);
            SceneState.SetMissionResult(true, currentMission.rewardXP);
            StoryFlags.Set(currentMission.missionId + "_complete");

            if (statusText)
            {
                statusText.color = new Color(0.85f, 0.72f, 0.1f);
                statusText.text  = $"MISSION COMPLETE\n\n<size=20>+{currentMission.rewardXP} XP\n\nE  —  Return to Hub\nR  —  Replay</size>";
            }
        }
    }

    void SpawnEnemy(Vector3 pos)
    {
        GameObject e = new GameObject("Enemy");
        e.transform.position = pos;
        GunEnemy en = e.AddComponent<GunEnemy>();
        if (playerGO) en.player = playerGO.transform;
        en.health      = 20 + wave * 12;
        en.speed       = 2.2f + wave * 0.22f;
        en.attackDamage = 8 + wave * 2;
    }

    // ── Callbacks from GunCharacter ───────────────────────────────────────────
    public void OnPlayerDied()
    {
        playerDead = true;
        if (mode == Mode.Mission && statusText != null)
            statusText.text = "YOU DIED\n\n<size=22>R  —  Retry\nE  —  Return to Hub</size>";
    }

    // ── Update ────────────────────────────────────────────────────────────────
    void Update()
    {
        if (mode == Mode.Hub)   UpdateHub();
        else                     UpdateMission();
    }

    void UpdateHub()
    {
        if (playerGO == null) return;

        MissionPortal nearest = null;
        float bestDist = INTERACT_RADIUS;
        foreach (var mp in portals)
        {
            if (mp == null) continue;
            float d = Vector3.Distance(playerGO.transform.position, mp.transform.position);
            if (d < bestDist) { bestDist = d; nearest = mp; }
        }

        if (nearest != null)
        {
            bool locked = nearest.IsLocked;
            promptText.text = locked
                ? $"[LOCKED]  {nearest.mission.displayName}\n<size=15>{nearest.mission.lockedReason}</size>"
                : $"[E]  Enter  {nearest.mission.displayName}  ({nearest.mission.waves} waves · +{nearest.mission.rewardXP} XP)";

            if (!locked && Input.GetKeyDown(KeyCode.E))
                EnterMission(nearest.mission);
        }
        else
        {
            promptText.text = "";
        }
    }

    void UpdateMission()
    {
        if (missionComplete)
        {
            if (Input.GetKeyDown(KeyCode.E)) EnterHub();
            if (Input.GetKeyDown(KeyCode.R)) EnterMission(currentMission);
            return;
        }
        if (playerDead)
        {
            if (Input.GetKeyDown(KeyCode.R)) EnterMission(currentMission);
            if (Input.GetKeyDown(KeyCode.E)) EnterHub();
        }
    }

    // ── HUD ───────────────────────────────────────────────────────────────────
    void BuildHUD()
    {
        hudRoot = new GameObject("HUD");
        Canvas canvas = hudRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudRoot.AddComponent<CanvasScaler>();
        hudRoot.AddComponent<GraphicRaycaster>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // HP bar
        GameObject bg = Rect(hudRoot.transform, "HPBg");
        bg.AddComponent<Image>().color = new Color(0.1f, 0.02f, 0.02f, 0.88f);
        RectTransform brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = brt.pivot = Vector2.zero;
        brt.anchoredPosition = new Vector2(22, 22); brt.sizeDelta = new Vector2(200, 22);

        GameObject fill = Rect(bg.transform, "HPFill");
        healthFill = fill.AddComponent<Image>();
        healthFill.color = new Color(0.85f, 0.1f, 0.1f);
        healthFill.type = Image.Type.Filled; healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillAmount = 1f;
        RectTransform frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(2, 2); frt.offsetMax = new Vector2(-2, -2);

        // Level label (top-left, above HP bar)
        GameObject lvlGO = Rect(hudRoot.transform, "LevelText");
        levelText = lvlGO.AddComponent<Text>();
        levelText.font = font; levelText.fontSize = 14;
        levelText.color = new Color(0.7f, 0.85f, 1f, 0.9f);
        levelText.alignment = TextAnchor.MiddleLeft;
        RectTransform lvlrt = lvlGO.GetComponent<RectTransform>();
        lvlrt.anchorMin = lvlrt.anchorMax = lvlrt.pivot = Vector2.zero;
        lvlrt.anchoredPosition = new Vector2(22, 78); lvlrt.sizeDelta = new Vector2(200, 18);

        // XP bar (just above HP bar)
        GameObject xpBg = Rect(hudRoot.transform, "XPBg");
        xpBg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.02f, 0.7f);
        RectTransform xpbrt = xpBg.GetComponent<RectTransform>();
        xpbrt.anchorMin = xpbrt.anchorMax = xpbrt.pivot = Vector2.zero;
        xpbrt.anchoredPosition = new Vector2(22, 48); xpbrt.sizeDelta = new Vector2(200, 8);

        GameObject xpFillGO = Rect(xpBg.transform, "XPFill");
        xpFill = xpFillGO.AddComponent<Image>();
        xpFill.color = new Color(0.35f, 0.8f, 0.2f);
        xpFill.type = Image.Type.Filled; xpFill.fillMethod = Image.FillMethod.Horizontal;
        xpFill.fillAmount = 0f;
        RectTransform xpfrt = xpFillGO.GetComponent<RectTransform>();
        xpfrt.anchorMin = Vector2.zero; xpfrt.anchorMax = Vector2.one;
        xpfrt.offsetMin = new Vector2(1, 1); xpfrt.offsetMax = new Vector2(-1, -1);

        // XP text label (hidden; level label carries this now)
        GameObject xpGO = Rect(hudRoot.transform, "XPText");
        xpText = xpGO.AddComponent<Text>();
        xpText.font = font; xpText.fontSize = 1; // invisible placeholder
        xpText.color = Color.clear;
        xpGO.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        // Wave label (top-center)
        GameObject wGO = Rect(hudRoot.transform, "WaveText");
        waveText = wGO.AddComponent<Text>();
        waveText.font = font; waveText.fontSize = 22;
        waveText.color = new Color(0.9f, 0.8f, 0.35f);
        waveText.alignment = TextAnchor.UpperCenter;
        RectTransform wrt = wGO.GetComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0, 1); wrt.anchorMax = Vector2.one;
        wrt.pivot = new Vector2(0.5f, 1);
        wrt.offsetMin = new Vector2(0, -55); wrt.offsetMax = Vector2.zero;

        // Status / death / complete (center)
        GameObject sGO = Rect(hudRoot.transform, "StatusText");
        statusText = sGO.AddComponent<Text>();
        statusText.font = font; statusText.fontSize = 44;
        statusText.color = new Color(0.8f, 0.07f, 0.07f);
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.supportRichText = true;
        RectTransform srt = sGO.GetComponent<RectTransform>();
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
        srt.offsetMin = srt.offsetMax = Vector2.zero;

        // Portal prompt (bottom-center)
        GameObject pGO = Rect(hudRoot.transform, "PromptText");
        promptText = pGO.AddComponent<Text>();
        promptText.font = font; promptText.fontSize = 19;
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.LowerCenter;
        promptText.supportRichText = true;
        RectTransform prt = pGO.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = new Vector2(0, 24); prt.offsetMax = Vector2.zero;

        // Weapon label
        GameObject wlGO = Rect(hudRoot.transform, "WeaponLabel");
        Text wlText = wlGO.AddComponent<Text>();
        wlText.font = font; wlText.fontSize = 14;
        wlText.color = new Color(0.7f, 0.85f, 1f, 0.9f);
        wlText.alignment = TextAnchor.MiddleLeft;
        wlText.text = "Sword  [TAB to switch]";
        RectTransform wlrt = wlGO.GetComponent<RectTransform>();
        wlrt.anchorMin = wlrt.anchorMax = wlrt.pivot = Vector2.zero;
        wlrt.anchoredPosition = new Vector2(22, 50); wlrt.sizeDelta = new Vector2(260, 20);

        GunCharacter pc = playerGO?.GetComponent<GunCharacter>();
        if (pc != null) { pc.healthFill = healthFill; pc.weaponLabel = wlText; }
    }

    IEnumerator ShowRewardToast()
    {
        UpdateXP();
        Color gold0 = new Color(0.4f, 1f, 0.4f, 0);
        Color goldF = new Color(0.4f, 1f, 0.4f, 1);
        statusText.color = gold0;
        statusText.text = $"Mission Complete!  +{SceneState.missionXP} XP";
        float t = 0;
        while (t < 0.5f) { t += Time.deltaTime; statusText.color = Color.Lerp(gold0, goldF, t / 0.5f); yield return null; }
        yield return new WaitForSeconds(2.5f);
        t = 0;
        while (t < 0.6f) { t += Time.deltaTime; statusText.color = Color.Lerp(goldF, gold0, t / 0.6f); yield return null; }
        statusText.text = "";
    }

    void UpdateXP()
    {
        if (levelText)
        {
            string sp = SceneState.skillPoints > 0 ? $"  [{SceneState.skillPoints} SP]" : "";
            levelText.text = $"Lv {SceneState.level}  {SceneState.xp} / {SceneState.XPToNextLevel} XP{sp}";
        }
        if (xpFill)
            xpFill.fillAmount = (float)SceneState.xp / SceneState.XPToNextLevel;
    }

    // ── Scene setup ───────────────────────────────────────────────────────────
    void SpawnPlayer()
    {
        playerGO = new GameObject("Player");
        playerGO.transform.position = Vector3.zero;
        player = playerGO.AddComponent<GunCharacter>();
        player.scene = this;
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = new GameObject("MainCamera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true; cam.orthographicSize = 9f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.10f);
        cam.nearClipPlane = 0.01f; cam.farClipPlane = 200f;
        cam.transform.position = new Vector3(0, 14, -12);
        cam.transform.rotation = Quaternion.Euler(40, 45, 0);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    void ClearWorld()
    {
        if (worldRoot) Destroy(worldRoot);
        worldRoot = null;
    }

    void ClearEnemies()
    {
        foreach (GunEnemy e in FindObjectsOfType<GunEnemy>()) Destroy(e.gameObject);
        foreach (Projectile p in FindObjectsOfType<Projectile>()) Destroy(p.gameObject);
    }

    void Tile(Vector3 pos, Color c)
    {
        GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
        t.transform.SetParent(worldRoot.transform);
        t.transform.position = pos;
        t.transform.localScale = new Vector3(0.98f, 0.07f, 0.98f);
        Destroy(t.GetComponent<Collider>());
        t.GetComponent<Renderer>().material = Mat(c);
    }

    GameObject Block(Vector3 pos, Vector3 scale, Color c)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.transform.SetParent(worldRoot.transform);
        g.transform.position = pos; g.transform.localScale = scale;
        Destroy(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().material = Mat(c);
        return g;
    }

    GameObject Prim(PrimitiveType t, Vector3 pos, Vector3 scale, Color c)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.transform.SetParent(worldRoot.transform);
        g.transform.position = pos; g.transform.localScale = scale;
        Destroy(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().material = Mat(c);
        return g;
    }

    static Material Mat(Color c)
    {
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material m = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        DestroyImmediate(tmp);
        m.SetColor("_BaseColor", c); m.color = c;
        return m;
    }

    static GameObject Rect(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }
}
