using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Phase 3 — per-weapon special attack, bound to F. Reads the equipped ComboData's
// specialAttack HitConfig and branches on weapon name. Same hit-detection pattern as
// MeleeAttack.DoMeleeHit (FindObjectsOfType, manual range/angle checks — no Physics).
public class SpecialAttack : MonoBehaviour
{
    float nextSpecial = 0f;
    GunCharacter player;

    void Awake()
    {
        player = GetComponent<GunCharacter>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && Time.time >= nextSpecial)
        {
            nextSpecial = Time.time + 2f;
            var weapon = player.CurrentWeapon;
            HitConfig special = weapon.specialAttack;

            switch (weapon.name)
            {
                case "Sword":
                    DoSwordSpecial(special);
                    break;
                case "Bow":
                    DoBowSpecial(special);
                    break;
                case "Shield":
                    StartCoroutine(DoShieldThrow());
                    break;
            }
        }
    }

    // 360° AOE — same as DoMeleeHit but no arc-angle facing check.
    void DoSwordSpecial(HitConfig h)
    {
        bool hitAny = false;
        int dmg = Mathf.RoundToInt(player.CurrentWeapon.baseDamage * h.damageMultiplier);

        foreach (GunEnemy e in FindObjectsOfType<GunEnemy>())
        {
            Vector3 toE = e.transform.position - transform.position;
            toE.y = 0;
            if (toE.magnitude > h.range) continue;
            e.TakeDamage(dmg, transform.position, h.knockbackForce);
            DamageNumber.Spawn(e.transform.position + Vector3.up * 2.3f, dmg, Color.white);
            VFXManager.Spawn(EffectType.HitSparks, e.transform.position + Vector3.up * 1f, Color.white);
            hitAny = true;
        }
        foreach (EnemyBase e in FindObjectsOfType<EnemyBase>())
        {
            Vector3 toE = e.transform.position - transform.position;
            toE.y = 0;
            if (toE.magnitude > h.range) continue;
            e.TakeDamage(dmg, transform.position, h.knockbackForce);
            DamageNumber.Spawn(e.transform.position + Vector3.up * 2.3f, dmg, Color.white);
            VFXManager.Spawn(EffectType.HitSparks, e.transform.position + Vector3.up * 1f, Color.white);
            hitAny = true;
        }
        if (hitAny) { CombatFeel.HitStop(h.hitstopFrames); AudioManager.Play("hit_enemy"); }
    }

    // Arrow rain — 5 projectiles fanned across a 45° arc centered on facing.
    void DoBowSpecial(HitConfig h)
    {
        int dmg = Mathf.RoundToInt(player.CurrentWeapon.baseDamage * h.damageMultiplier);
        Vector3 spawn = transform.position + transform.forward * 0.7f + Vector3.up * 1.1f;
        float arcHalf = 45f * 0.5f;

        for (int i = 0; i < 5; i++)
        {
            float angleOffset = -arcHalf + (2f * arcHalf / 4f) * i;
            Quaternion rot = Quaternion.AngleAxis(angleOffset, Vector3.up);
            Vector3 dir = rot * transform.forward;

            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.transform.position = spawn;
            Object.Destroy(p.GetComponent<Collider>());

            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Material mat = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
            Object.DestroyImmediate(tmp);
            mat.SetColor("_BaseColor", h.projectileColor);
            mat.color = h.projectileColor;
            p.GetComponent<Renderer>().material = mat;

            Projectile proj = p.AddComponent<Projectile>();
            proj.direction = dir;
            proj.damage = dmg;
            proj.speed = h.projectileSpeed;
            proj.owner = "Player";
        }
    }

    // Shield throw — plain moving object (not a Projectile component, which
    // self-destroys on its first hit) so it can bounce off up to 2 enemies.
    IEnumerator DoShieldThrow()
    {
        HitConfig h = player.CurrentWeapon.specialAttack;
        int dmg = Mathf.RoundToInt(player.CurrentWeapon.baseDamage * h.damageMultiplier);
        Vector3 spawn = transform.position + transform.forward * 0.7f + Vector3.up * 1.1f;

        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(p.GetComponent<Collider>());
        p.transform.position = spawn;

        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material mat = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        Object.DestroyImmediate(tmp);
        mat.SetColor("_BaseColor", h.projectileColor);
        mat.color = h.projectileColor;
        p.GetComponent<Renderer>().material = mat;

        Vector3 dir = transform.forward;
        float speed = h.projectileSpeed;
        float rangeLimit = 25f;
        float distTraveled = 0f;
        int hitsTaken = 0;
        HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

        while (distTraveled < rangeLimit && hitsTaken < 2)
        {
            p.transform.position += dir * speed * Time.deltaTime;
            distTraveled += speed * Time.deltaTime;

            foreach (GunEnemy e in FindObjectsOfType<GunEnemy>())
            {
                if (!hitEnemies.Contains(e.gameObject) && Vector3.Distance(e.transform.position, p.transform.position) <= 1f)
                {
                    hitEnemies.Add(e.gameObject);
                    e.TakeDamage(dmg, transform.position, h.knockbackForce);
                    DamageNumber.Spawn(e.transform.position + Vector3.up * 2.3f, dmg, Color.white);
                    VFXManager.Spawn(EffectType.HitSparks, e.transform.position + Vector3.up * 1f, Color.white);
                    hitsTaken++;
                }
            }
            foreach (EnemyBase e in FindObjectsOfType<EnemyBase>())
            {
                if (!hitEnemies.Contains(e.gameObject) && Vector3.Distance(e.transform.position, p.transform.position) <= 1f)
                {
                    hitEnemies.Add(e.gameObject);
                    e.TakeDamage(dmg, transform.position, h.knockbackForce);
                    DamageNumber.Spawn(e.transform.position + Vector3.up * 2.3f, dmg, Color.white);
                    VFXManager.Spawn(EffectType.HitSparks, e.transform.position + Vector3.up * 1f, Color.white);
                    hitsTaken++;
                }
            }

            yield return null;
        }

        Object.Destroy(p);
    }
}
