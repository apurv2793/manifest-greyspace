using UnityEngine;

// Pure visual + data. HubScene drives proximity detection and entry.
public class MissionPortal : MonoBehaviour
{
    [HideInInspector] public MissionDefinition mission;

    public bool IsLocked => !string.IsNullOrEmpty(mission?.requiredFlag)
                         && !StoryFlags.Has(mission.requiredFlag);

    void Start() => BuildVisual();

    void BuildVisual()
    {
        bool locked   = IsLocked;
        Color stone   = locked ? new Color(0.28f, 0.22f, 0.15f) : new Color(0.15f, 0.35f, 0.55f);
        Color glow    = locked ? new Color(0.55f, 0.28f, 0.05f) : new Color(0.25f, 0.75f, 1.0f);
        Color lockRed = new Color(0.9f, 0.15f, 0.1f);

        // Pedestal
        Prim(PrimitiveType.Cylinder, new Vector3(0, 0.4f, 0),    new Vector3(0.6f, 0.4f, 0.6f), stone);
        // Left pillar
        Prim(PrimitiveType.Cube,     new Vector3(-0.55f, 1.6f, 0), new Vector3(0.2f, 2.2f, 0.2f), stone);
        // Right pillar
        Prim(PrimitiveType.Cube,     new Vector3( 0.55f, 1.6f, 0), new Vector3(0.2f, 2.2f, 0.2f), stone);
        // Lintel
        Prim(PrimitiveType.Cube,     new Vector3(0, 2.8f, 0),     new Vector3(1.5f, 0.24f, 0.2f), stone);
        // Glowing inner panel
        Prim(PrimitiveType.Cube,     new Vector3(0, 1.6f, 0),     new Vector3(0.7f, 1.6f, 0.06f), glow);

        if (locked)
        {
            // X-mark — two overlapping bars
            GameObject x1 = Prim(PrimitiveType.Cube, new Vector3(0, 1.6f, -0.1f), new Vector3(0.5f, 0.1f, 0.05f), lockRed);
            x1.transform.localEulerAngles = new Vector3(0, 0, 45);
            GameObject x2 = Prim(PrimitiveType.Cube, new Vector3(0, 1.6f, -0.11f), new Vector3(0.5f, 0.1f, 0.05f), lockRed);
            x2.transform.localEulerAngles = new Vector3(0, 0, -45);
        }
        else
        {
            // Arrow chevron pointing into the portal
            Prim(PrimitiveType.Cube, new Vector3(0, 1.4f, -0.1f), new Vector3(0.08f, 0.4f, 0.05f), Color.white);
            GameObject tip = Prim(PrimitiveType.Cube, new Vector3(0, 1.65f, -0.1f), new Vector3(0.28f, 0.08f, 0.05f), Color.white);
            tip.transform.localEulerAngles = new Vector3(0, 0, 45);
            GameObject tip2 = Prim(PrimitiveType.Cube, new Vector3(0, 1.65f, -0.11f), new Vector3(0.28f, 0.08f, 0.05f), Color.white);
            tip2.transform.localEulerAngles = new Vector3(0, 0, -45);
        }
    }

    GameObject Prim(PrimitiveType t, Vector3 lp, Vector3 ls, Color c)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        g.transform.SetParent(transform, false);
        g.transform.localPosition = lp;
        g.transform.localScale    = ls;
        Destroy(g.GetComponent<Collider>());
        Material m = new Material(g.GetComponent<Renderer>().sharedMaterial);
        m.SetColor("_BaseColor", c); m.color = c;
        g.GetComponent<Renderer>().material = m;
        return g;
    }
}
