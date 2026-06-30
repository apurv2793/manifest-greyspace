using UnityEngine;
using System.Collections;

// Handles light (J) + heavy (K) combo input for both melee and ranged weapon types.
// Swap comboData at runtime to change weapon — no other code changes needed.
public class MeleeAttack : MonoBehaviour
{
    public ComboData comboData;

    int   _step;
    float _windowEnd;
    bool  _locked;

    public void ResetCombo() { _step = 0; _windowEnd = 0f; }

    void Start()
    {
        if (comboData == null) comboData = ComboData.Sword();
        Debug.Log("[Combat] " + comboData.name + " ready — " + comboData.steps.Length +
                  " steps | J=Light  K=Heavy");
    }

    // Called every frame from GunCharacter.Update()
    public void HandleInput()
    {
        if (_locked) return;

        bool light = Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0);
        bool heavy = Input.GetKeyDown(KeyCode.K) || Input.GetMouseButtonDown(1);
        if (!light && !heavy) return;

        // Window expired → restart combo from step 0
        if (_step > 0 && Time.time > _windowEnd)
        {
            Debug.Log("[Combat] Window expired — back to step 1");
            _step = 0;
        }

        HitConfig h = light ? comboData.steps[_step].light : comboData.steps[_step].heavy;
        string tag = light ? "L" : "H";
        Debug.Log("[Combat] Step " + (_step + 1) + "/" + comboData.steps.Length + " [" + tag + "]  " +
                  (h.type == AttackType.Melee ? "melee" : "projectile") +
                  "  ×" + h.damageMultiplier);

        StartCoroutine(ExecuteHit(h, _step));
        _step = (_step + 1 >= comboData.steps.Length) ? 0 : _step + 1;
    }

    IEnumerator ExecuteHit(HitConfig h, int step)
    {
        _locked = true;
        bool isLast = (step == comboData.steps.Length - 1) || h.inputWindow <= 0f;

        yield return new WaitForSeconds(h.windupTime);

        if (h.type == AttackType.Melee)
            DoMeleeHit(h);
        else
            SpawnProjectile(h);

        yield return new WaitForSeconds(h.hitboxDuration);

        _windowEnd = isLast ? 0f : Time.time + h.inputWindow;
        if (isLast) _step = 0;
        _locked = false;
    }

    void DoMeleeHit(HitConfig h)
    {
        bool hitAny = false;
        foreach (GunEnemy e in FindObjectsOfType<GunEnemy>())
        {
            Vector3 toE = e.transform.position - transform.position;
            toE.y = 0;
            if (toE.magnitude > h.range) continue;
            if (Vector3.Angle(transform.forward, toE) > h.arcAngle * 0.5f) continue;

            int dmg = Mathf.RoundToInt(comboData.baseDamage * h.damageMultiplier);
            e.TakeDamage(dmg, transform.position, h.knockbackForce);
            DamageNumber.Spawn(e.transform.position + Vector3.up * 2.3f, dmg, Color.white);
            hitAny = true;
        }
        if (hitAny) CombatFeel.HitStop(h.hitstopFrames);
    }

    void SpawnProjectile(HitConfig h)
    {
        int dmg = Mathf.RoundToInt(comboData.baseDamage * h.damageMultiplier);
        float size = 0.14f + h.damageMultiplier * 0.04f;

        Vector3 spawn = transform.position + transform.forward * 0.7f + Vector3.up * 1.1f;
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.name = "Projectile";
        p.transform.position = spawn;
        p.transform.localScale = Vector3.one * size;
        Object.Destroy(p.GetComponent<Collider>());

        // Steal shader from a temp primitive (URP-safe)
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material mat = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        Object.DestroyImmediate(tmp);
        mat.SetColor("_BaseColor", h.projectileColor);
        mat.color = h.projectileColor;
        p.GetComponent<Renderer>().material = mat;

        Projectile proj = p.AddComponent<Projectile>();
        proj.direction  = transform.forward;
        proj.damage     = dmg;
        proj.speed      = h.projectileSpeed;
        proj.owner      = "Player";
    }
}
