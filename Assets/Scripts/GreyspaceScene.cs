using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GreyspaceScene : MonoBehaviour
{
    int wave;
    bool playerDead;
    GameObject playerGO, floorRoot, hudRoot;
    Image healthFill;
    Text waveText, deathText;

    // =========================================================================
    void Start()
    {
        Debug.Log("GreyspaceScene: Starting...");
        BuildFloor();
        SetupCamera();
        SpawnPlayer();
        BuildHUD();
        StartCoroutine(WaveLoop());
        Debug.Log("===== Controls: WASD=Move | Mouse=Aim | LClick=Shoot | Shift=Dash =====");
    }

    // =========================================================================
    void BuildFloor()
    {
        floorRoot = new GameObject("Floor");
        for (int x = -15; x < 15; x++)
        for (int z = -15; z < 15; z++)
        {
            GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            t.transform.parent = floorRoot.transform;
            t.transform.position = new Vector3(x, -0.5f, z);
            t.transform.localScale = new Vector3(0.98f, 0.07f, 0.98f);
            Destroy(t.GetComponent<Collider>());
            float sh = (Mathf.Abs(x + z) % 2 == 0) ? 0.15f : 0.21f;
            t.GetComponent<Renderer>().material = TileMat(new Color(sh, sh, sh + 0.02f));
        }
        Debug.Log("GreyspaceScene: Floor built");
    }

    Material TileMat(Color c)
    {
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material m = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        DestroyImmediate(tmp);
        m.SetColor("_BaseColor", c); m.color = c;
        return m;
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = new GameObject("MainCamera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.orthographic = true;
        cam.orthographicSize = 9f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.10f);
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 200f;
        cam.transform.position = new Vector3(0, 14, -12);
        cam.transform.rotation = Quaternion.Euler(40, 45, 0);
        Debug.Log("GreyspaceScene: Camera configured");
    }

    void SpawnPlayer()
    {
        playerGO = new GameObject("Player");
        playerGO.transform.position = Vector3.zero;
        GunCharacter pc = playerGO.AddComponent<GunCharacter>();
        pc.scene = this;
        Debug.Log("GreyspaceScene: Player spawned");
    }

    // =========================================================================
    void BuildHUD()
    {
        hudRoot = new GameObject("HUD");
        Canvas canvas = hudRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudRoot.AddComponent<CanvasScaler>();
        hudRoot.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // --- Health bar ---
        GameObject bg = Rect(hudRoot.transform, "HPBg");
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.02f, 0.02f, 0.88f);
        RectTransform brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = brt.pivot = Vector2.zero;
        brt.anchoredPosition = new Vector2(22, 22);
        brt.sizeDelta = new Vector2(200, 22);

        GameObject fill = Rect(bg.transform, "HPFill");
        healthFill = fill.AddComponent<Image>();
        healthFill.color = new Color(0.85f, 0.1f, 0.1f);
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillAmount = 1f;
        RectTransform frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = new Vector2(2, 2); frt.offsetMax = new Vector2(-2, -2);

        GameObject lbl = Rect(bg.transform, "HPLabel");
        Text lt = lbl.AddComponent<Text>();
        lt.text = "HP"; lt.font = font; lt.fontSize = 13;
        lt.color = new Color(1, 1, 1, 0.8f); lt.alignment = TextAnchor.MiddleLeft;
        RectTransform llrt = lbl.GetComponent<RectTransform>();
        llrt.anchorMin = Vector2.zero; llrt.anchorMax = Vector2.one;
        llrt.offsetMin = new Vector2(5, 0); llrt.offsetMax = Vector2.zero;

        // --- Wave label (top center) ---
        GameObject wGO = Rect(hudRoot.transform, "WaveText");
        waveText = wGO.AddComponent<Text>();
        waveText.font = font; waveText.fontSize = 22;
        waveText.color = new Color(0.9f, 0.8f, 0.35f);
        waveText.alignment = TextAnchor.UpperCenter;
        RectTransform wrt = wGO.GetComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0, 1); wrt.anchorMax = Vector2.one;
        wrt.pivot = new Vector2(0.5f, 1);
        wrt.offsetMin = new Vector2(0, -55); wrt.offsetMax = Vector2.zero;

        // --- Death screen ---
        GameObject dGO = Rect(hudRoot.transform, "DeathText");
        deathText = dGO.AddComponent<Text>();
        deathText.font = font; deathText.fontSize = 54;
        deathText.color = new Color(0.8f, 0.07f, 0.07f);
        deathText.alignment = TextAnchor.MiddleCenter; deathText.text = "";
        RectTransform drt = dGO.GetComponent<RectTransform>();
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = drt.offsetMax = Vector2.zero;

        // Give fill reference to player
        GunCharacter pc = playerGO != null ? playerGO.GetComponent<GunCharacter>() : null;
        if (pc != null) pc.healthFill = healthFill;

        Debug.Log("GreyspaceScene: HUD built");
    }

    static GameObject Rect(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    // =========================================================================
    IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(1.5f);
        while (!playerDead)
        {
            wave++;
            if (waveText != null) waveText.text = "~ Wave " + wave + " ~";
            Debug.Log("Wave " + wave + " starting — " + (2 + wave) + " enemies");

            int count = 2 + wave;
            Vector3 origin = playerGO != null ? playerGO.transform.position : Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count);
                float r = 9f + Random.Range(0f, 4f);
                Vector3 pos = origin + new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * r, 0, Mathf.Cos(angle * Mathf.Deg2Rad) * r);
                SpawnEnemy(pos);
            }

            while (!playerDead && FindObjectsOfType<GunEnemy>().Length > 0)
                yield return new WaitForSeconds(0.4f);

            if (!playerDead)
            {
                if (waveText != null) waveText.text = "Wave " + wave + " cleared!";
                Debug.Log("Wave " + wave + " cleared!");
                yield return new WaitForSeconds(2f);
            }
        }
    }

    void SpawnEnemy(Vector3 pos)
    {
        GameObject e = new GameObject("Enemy");
        e.transform.position = pos;
        GunEnemy en = e.AddComponent<GunEnemy>();
        if (playerGO != null) en.player = playerGO.transform;
        en.health      = 20 + wave * 12;
        en.speed       = 2.2f + wave * 0.22f;
        en.attackDamage = 8 + wave * 2;
    }

    // =========================================================================
    public void OnPlayerDied()
    {
        playerDead = true;
        if (deathText != null)
            deathText.text = "YOU DIED\n\n<size=22>Press R to restart</size>";
        Debug.Log("GAME OVER — press R to restart");
    }

    void LateUpdate()
    {
        if (playerDead && Input.GetKey(KeyCode.R))
            Restart();
    }

    void Restart()
    {
        StopAllCoroutines();

        // Destroy tracked roots
        if (floorRoot) Destroy(floorRoot);
        if (playerGO)  Destroy(playerGO);
        if (hudRoot)   Destroy(hudRoot);

        // Clean up any lingering enemies / projectiles
        foreach (GunEnemy e in FindObjectsOfType<GunEnemy>()) Destroy(e.gameObject);
        foreach (Projectile p in FindObjectsOfType<Projectile>()) Destroy(p.gameObject);

        wave = 0; playerDead = false;
        playerGO = floorRoot = hudRoot = null;
        healthFill = null; waveText = deathText = null;

        Start();
    }
}
