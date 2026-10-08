using UnityEngine;

// Static commands for the Unity CLI, e.g.
//   unity --non-interactive command eval 'GreyspaceDebug.State()'
// Every method is safe to call outside Play mode (returns an error string, changes nothing).
public static class GreyspaceDebug
{
    static GreyspaceScene S => GreyspaceScene.Instance;
    const string NotPlaying = "not playing";

    // ── State + navigation ───────────────────────────────────────────────────
    public static string State()
        => S == null ? "{\"error\":\"" + NotPlaying + "\"}" : JsonUtility.ToJson(S.Snapshot());

    public static string GoHub()
    {
        if (S == null) return NotPlaying;
        S.DebugGoHub();
        return "ok";
    }

    public static string GoMission(string missionId)
    {
        if (S == null) return NotPlaying;
        return S.DebugGoMission(missionId) ? "ok" : "unknown mission " + missionId;
    }

    // ── Spawning ─────────────────────────────────────────────────────────────
    // Charger/Ranged have no stat defaults of their own (mission assets set them),
    // so debug spawns use the engine-plan defaults. Shielder sets its own in Start().
    public static string SpawnEnemy(string type, float x, float z)
    {
        if (S == null) return NotPlaying;
        GunCharacter pc = GunCharacter.Instance;
        Transform target = pc != null ? pc.transform : null;

        GameObject go = new GameObject("Debug_" + type);
        go.transform.position = new Vector3(x, 0f, z);
        switch (type)
        {
            case "stalker":
            {
                var e = go.AddComponent<GunEnemy>();
                e.player = target; e.health = 30; e.speed = 2.4f; e.attackDamage = 10;
                break;
            }
            case "charger":
            {
                var e = go.AddComponent<ChargerEnemy>();
                e.player = target; e.health = 45; e.speed = 2.2f; e.attackDamage = 14; e.xpValue = 20;
                break;
            }
            case "ranged":
            {
                var e = go.AddComponent<RangedEnemy>();
                e.player = target; e.health = 25; e.speed = 2.0f; e.attackDamage = 8; e.xpValue = 15;
                break;
            }
            case "shielder":
            {
                var e = go.AddComponent<ShielderEnemy>();
                e.player = target;
                break;
            }
            default:
                Object.Destroy(go);
                return "unknown type " + type + " (stalker|charger|ranged|shielder)";
        }
        return "ok";
    }

    // ── Player ───────────────────────────────────────────────────────────────
    public static string GrantSkillPoints(int n)
    {
        if (S == null || GunCharacter.Instance == null) return NotPlaying;
        GunCharacter.Instance.inventory.skillTree.AddSkillPoints(n);
        return "ok";
    }

    public static string SetGodMode(bool on)
    {
        if (S == null || GunCharacter.Instance == null) return NotPlaying;
        GunCharacter.Instance.debugGodMode = on;
        return "ok";
    }

    // Writes a PNG of the Game view at the end of the current frame.
    public static string Screenshot(string absolutePath)
    {
        if (S == null) return NotPlaying;
        ScreenCapture.CaptureScreenshot(absolutePath);
        return "ok";
    }

    // ── Autopilot ────────────────────────────────────────────────────────────
    public static string StartAutopilot()
    {
        if (S == null || S.DebugPlayerGO == null) return NotPlaying;
        if (S.DebugPlayerGO.GetComponent<ComboBot>() == null) S.DebugPlayerGO.AddComponent<ComboBot>();
        return "ok";
    }

    public static string StopAutopilot()
    {
        if (S == null || S.DebugPlayerGO == null) return NotPlaying;
        var bot = S.DebugPlayerGO.GetComponent<ComboBot>();
        if (bot != null) Object.Destroy(bot);
        InputRouter.SimMoveAxis = null;
        InputRouter.SimAimPoint = null;
        return "ok";
    }

    public static string AutopilotReport()
    {
        if (S == null || S.DebugPlayerGO == null) return NotPlaying;
        var bot = S.DebugPlayerGO.GetComponent<ComboBot>();
        return bot == null ? "{\"running\":false}" : JsonUtility.ToJson(bot.Report());
    }
}
