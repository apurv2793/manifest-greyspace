using UnityEngine;
using System.Collections;

// GLM 5.1 via Manifest OS (call_id 163) — corrected: shader-steal Mat(), _BaseColor, SetParent false
public abstract class WeaponBase : MonoBehaviour
{
    public string weaponName;
    public int    damage;
    public float  attackCooldown;
    public float  range;
    public Color  weaponColor;

    protected bool         canAttack = true;
    protected GunCharacter owner;

    public void EquipOn(GunCharacter newOwner)
    {
        owner = newOwner;
        BuildVisual();
    }

    public void Unequip()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
        owner = null;
    }

    public abstract void PrimaryAttack();
    public abstract void SpecialAttack();
    public virtual  void OnDash() { }

    protected abstract void BuildVisual();

    protected IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSecondsRealtime(attackCooldown);
        canAttack = true;
    }

    protected static Material Mat(Color c)
    {
        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material m = new Material(tmp.GetComponent<Renderer>().sharedMaterial);
        DestroyImmediate(tmp);
        m.SetColor("_BaseColor", c); m.color = c;
        return m;
    }

    protected GameObject P(PrimitiveType t, string n, Vector3 lp, Vector3 ls, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.name = n;
        g.transform.SetParent(transform, false);
        g.transform.localPosition = lp;
        g.transform.localScale    = ls;
        Destroy(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().material = m;
        return g;
    }
}
