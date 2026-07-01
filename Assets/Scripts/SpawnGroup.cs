using UnityEngine;

// GLM 5.1 via Manifest OS (call_id 171) — applied as-is
[CreateAssetMenu(menuName = "Game/SpawnGroup")]
public class SpawnGroup : ScriptableObject
{
    [System.Serializable]
    public struct EnemyEntry
    {
        public GameObject enemyPrefab;
        public int   count;
        public float spawnDelay;
    }

    public EnemyEntry[] entries;
    public string waveName;

    public int TotalEnemyCount()
    {
        int total = 0;
        if (entries != null)
            for (int i = 0; i < entries.Length; i++)
                total += entries[i].count;
        return total;
    }
}
