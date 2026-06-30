using UnityEngine;

public enum AttackType { Melee, Projectile }

[System.Serializable]
public struct HitConfig
{
    public AttackType type;
    public float damageMultiplier;

    // Melee
    public float range;
    public float arcAngle;

    // Projectile
    public float projectileSpeed;
    public Color projectileColor;

    // Shared
    public float knockbackForce;
    public int   hitstopFrames;
    public float windupTime;
    public float hitboxDuration;   // melee: hitbox window; projectile: recovery after firing
    public float inputWindow;      // seconds to press next hit; 0 = combo ends here
}

[System.Serializable]
public struct ComboStep
{
    public HitConfig light;   // J
    public HitConfig heavy;   // K
}

[CreateAssetMenu(menuName = "Combat/ComboData")]
public class ComboData : ScriptableObject
{
    public int        baseDamage = 15;
    public ComboStep[] steps;

    // ── Presets ──────────────────────────────────────────────────────────────

    // Sword — all melee, 3 steps
    // L-L-L: three fast slashes
    // L-L-H: two lights into a big launcher
    // H-H-H: slow heavy trio
    public static ComboData Sword()
    {
        var c = CreateInstance<ComboData>();
        c.name = "Sword";
        c.baseDamage = 15;
        c.steps = new[]
        {
            new ComboStep
            {
                light = Melee(1.0f, 1.8f, 90f,  10f, 4, 0.05f, 0.08f, 0.50f),
                heavy = Melee(1.6f, 2.0f, 110f, 16f, 6, 0.10f, 0.12f, 0.55f),
            },
            new ComboStep
            {
                light = Melee(1.0f, 1.8f, 90f,  10f, 4, 0.05f, 0.08f, 0.50f),
                heavy = Melee(1.8f, 2.0f, 100f, 18f, 6, 0.10f, 0.12f, 0.55f),
            },
            new ComboStep                          // finisher — inputWindow=0 ends the combo
            {
                light = Melee(1.3f, 2.0f, 140f, 14f, 5, 0.05f, 0.10f, 0f),
                heavy = Melee(2.5f, 2.2f, 90f,  28f, 8, 0.12f, 0.14f, 0f),
            },
        };
        return c;
    }

    // Bow — all projectile, 2 steps
    // L = quick arrow, H = heavy arrow; step 2 = power shot finisher
    public static ComboData Bow()
    {
        var c = CreateInstance<ComboData>();
        c.name = "Bow";
        c.baseDamage = 12;
        c.steps = new[]
        {
            new ComboStep
            {
                light = Proj(0.8f, 22f, new Color(1f, 0.85f, 0.2f), 0f, 3, 0.04f, 0.06f, 0.50f),
                heavy = Proj(2.0f, 18f, new Color(1f, 0.45f, 0.0f), 12f, 5, 0.15f, 0.10f, 0.55f),
            },
            new ComboStep                          // second press = power shot, combo ends
            {
                light = Proj(1.0f, 22f, new Color(1f, 0.85f, 0.2f), 0f, 3, 0.04f, 0.06f, 0f),
                heavy = Proj(3.0f, 15f, new Color(1f, 0.25f, 0.0f), 2f, 7, 0.22f, 0.14f, 0f),
            },
        };
        return c;
    }

    // Staff — mixed: light = melee poke, heavy = magic bolt; 2 steps
    public static ComboData Staff()
    {
        var c = CreateInstance<ComboData>();
        c.name = "Staff";
        c.baseDamage = 13;
        c.steps = new[]
        {
            new ComboStep
            {
                light = Melee(1.1f, 1.6f, 80f, 8f, 4, 0.06f, 0.08f, 0.50f),
                heavy = Proj( 1.8f, 16f, new Color(0.3f, 0.5f, 1.0f), 8f, 5, 0.12f, 0.10f, 0.55f),
            },
            new ComboStep
            {
                light = Proj(1.0f, 20f, new Color(0.6f, 0.3f, 1.0f), 0f, 3, 0.05f, 0.07f, 0f),
                heavy = Proj(2.8f, 13f, new Color(0.9f, 0.9f, 1.0f), 2f, 8, 0.18f, 0.14f, 0f),
            },
        };
        return c;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static HitConfig Melee(float dmgMult, float range, float arc, float knockback,
                           int hitstop, float windup, float hitboxDur, float inputWin)
        => new HitConfig
        {
            type = AttackType.Melee,
            damageMultiplier = dmgMult, range = range, arcAngle = arc,
            knockbackForce = knockback, hitstopFrames = hitstop,
            windupTime = windup, hitboxDuration = hitboxDur, inputWindow = inputWin,
        };

    static HitConfig Proj(float dmgMult, float speed, Color col, float knockback,
                          int hitstop, float windup, float recovery, float inputWin)
        => new HitConfig
        {
            type = AttackType.Projectile,
            damageMultiplier = dmgMult, projectileSpeed = speed, projectileColor = col,
            knockbackForce = knockback, hitstopFrames = hitstop,
            windupTime = windup, hitboxDuration = recovery, inputWindow = inputWin,
        };
}
