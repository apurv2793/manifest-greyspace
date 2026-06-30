using System;

public static class GameEvents
{
    public static event Action OnPlayerDeath;
    public static event Action<string> OnMissionComplete;
    public static event Action<int> OnEnemyDied;       // xpValue
    public static event Action<int> OnLevelUp;          // new level

    public static void FirePlayerDeath()               => OnPlayerDeath?.Invoke();
    public static void FireMissionComplete(string id)  => OnMissionComplete?.Invoke(id);
    public static void FireEnemyDied(int xp)           => OnEnemyDied?.Invoke(xp);
    public static void FireLevelUp(int level)          => OnLevelUp?.Invoke(level);
}
