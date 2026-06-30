// Cross-scene persistence via static fields (survives LoadScene, resets on app quit)
public static class SceneState
{
    // Player state carried between scenes
    public static int  hp          = -1;   // -1 = use class default
    public static int  maxHp       = 100;
    public static int  xp          = 0;
    public static int  level       = 1;
    public static int  skillPoints = 0;
    public static int  weaponIndex = 0;

    // XP required to reach next level: 100 * level^1.4 (roughly 100, 214, 342, ...)
    public static int XPToNextLevel => (int)(100 * System.Math.Pow(level, 1.4));

    // Call after adding XP — returns true if leveled up
    public static bool AddXP(int amount)
    {
        xp += amount;
        if (xp >= XPToNextLevel)
        {
            xp -= XPToNextLevel;
            level++;
            skillPoints++;
            GameEvents.FireLevelUp(level);
            return true;
        }
        return false;
    }

    // Set by MissionPortal before loading the mission scene
    public static string currentMissionId   = "";
    public static string currentMissionName = "";
    public static int    currentMissionWaves = 3;
    public static int    currentMissionXP    = 0;
    public static string returnHubScene     = "HubA";

    // Set by GreyspaceScene on mission end; read by HubScene on return
    public static bool   missionWon = false;
    public static int    missionXP  = 0;
    public static string doneMissionId = "";

    public static void SavePlayer(GunCharacter pc)
    {
        hp          = pc.health;
        maxHp       = pc.maxHealth;
    }

    public static void ApplyToPlayer(GunCharacter pc)
    {
        if (hp >= 0) { pc.maxHealth = maxHp; pc.health = hp; }
    }

    public static void SetMissionResult(bool won, int xpEarned)
    {
        missionWon    = won;
        missionXP     = xpEarned;
        doneMissionId = currentMissionId;
        if (won) AddXP(xpEarned);
    }
}
