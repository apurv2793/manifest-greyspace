using UnityEngine;

// Autopilot: walks to the nearest enemy, aims at it, attacks L-L-H, dashes away when low.
// Plays ONLY through InputRouter (SimMoveAxis / SimAimPoint / Signal*) — the same path a
// touch player uses — so what it exercises is the real game, not a shortcut.
public class ComboBot : MonoBehaviour
{
    public float attackRange      = 1.7f;   // Sword light range is 1.8
    public float retreatHpFraction = 0.25f;
    public float stepGap          = 0.22f;  // seconds between combo presses

    static readonly int[] Combo = { 0, 0, 1 };   // 0 = light, 1 = heavy
    int   comboIndex;
    float nextPress;

    [System.Serializable]
    public class BotReport
    {
        public bool  running = true;
        public int   attacksIssued, dashesIssued, enemiesSeen;
        public float secondsActive;
    }
    readonly BotReport report = new BotReport();
    public BotReport Report() => report;

    void OnDisable()
    {
        InputRouter.SimMoveAxis = null;
        InputRouter.SimAimPoint = null;
    }

    void Update()
    {
        report.secondsActive += Time.unscaledDeltaTime;
        GunCharacter pc = GunCharacter.Instance;
        if (pc == null || pc.isDead || Camera.main == null) { InputRouter.SimMoveAxis = Vector2.zero; return; }

        Transform target = NearestEnemy();
        if (target == null) { InputRouter.SimMoveAxis = Vector2.zero; return; }
        report.enemiesSeen = EnemyBase.Active.Count + GunEnemy.Active.Count;

        Vector3 to = target.position - transform.position; to.y = 0f;
        InputRouter.SimAimPoint = target.position;

        bool low = pc.health < pc.maxHealth * retreatHpFraction;
        if (low && to.magnitude < 3f && Time.time >= nextPress)
        {
            InputRouter.SimMoveAxis = ToAxis(-to.normalized);
            InputRouter.SignalDash();
            report.dashesIssued++;
            nextPress = Time.time + 0.6f;
            return;
        }

        if (to.magnitude > attackRange)
        {
            InputRouter.SimMoveAxis = ToAxis(to.normalized);
            return;
        }

        InputRouter.SimMoveAxis = Vector2.zero;
        if (Time.time >= nextPress)
        {
            if (Combo[comboIndex] == 0) InputRouter.SignalLight(); else InputRouter.SignalHeavy();
            comboIndex = (comboIndex + 1) % Combo.Length;
            report.attacksIssued++;
            nextPress = Time.time + stepGap;
        }
    }

    // World direction -> the (h, v) space InputRouter.MoveAxis() returns
    // (GunCharacter.Move maps it back through camera right/forward).
    static Vector2 ToAxis(Vector3 worldDir)
    {
        Transform cam = Camera.main.transform;
        Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = cam.right; right.y = 0f; right.Normalize();
        return new Vector2(Vector3.Dot(worldDir, right), Vector3.Dot(worldDir, fwd));
    }

    Transform NearestEnemy()
    {
        Transform best = null; float bestD = float.MaxValue;
        Vector3 p = transform.position;
        foreach (var e in GunEnemy.Active)
            if (e != null) { float d = (e.transform.position - p).sqrMagnitude; if (d < bestD) { bestD = d; best = e.transform; } }
        foreach (var e in EnemyBase.Active)
            if (e != null) { float d = (e.transform.position - p).sqrMagnitude; if (d < bestD) { bestD = d; best = e.transform; } }
        return best;
    }
}
