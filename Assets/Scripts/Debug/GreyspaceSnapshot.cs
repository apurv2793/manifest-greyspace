using UnityEngine;

// Plain state snapshot for GreyspaceDebug.State() — JsonUtility-friendly.
[System.Serializable]
public class GreyspaceSnapshot
{
    public string  mode;
    public string  missionId;
    public int     wave, totalWaves;
    public bool    playerDead, missionComplete;
    public int     playerHp, playerMaxHp;
    public Vector3 playerPos;
    public int     level, xp, skillPoints;
    public int     enemiesAlive;
    public int     frame;
    public float   fps;
}
