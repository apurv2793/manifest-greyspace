using UnityEngine;

// GLM 5.1 via Manifest OS (call_id 184) — applied as-is
public class Checkpoint : MonoBehaviour
{
    public float activateRadius = 2.5f;
    public static Vector3 LastActivatedPosition { get; private set; }
    public static bool HasActivatedAny { get; private set; }

    // Call at the start of a fresh mission — checkpoint GameObjects are destroyed by
    // ClearWorld() but these static fields aren't, and a stale one would leak into the next mission.
    public static void ResetForNewMission()
    {
        LastActivatedPosition = Vector3.zero;
        HasActivatedAny = false;
    }

    static readonly Color InactiveColor = new Color(0.2f, 0.2f, 0.6f, 1f);
    static readonly Color ActiveColor = new Color(1f, 0.85f, 0.1f, 1f);

    bool _activated;
    Material _sphereMat;

    void Start()
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.transform.SetParent(transform, false);
        pillar.transform.localScale = new Vector3(0.5f, 1.0f, 0.5f);
        Destroy(pillar.GetComponent<Collider>());

        Material pillarMat = new Material(pillar.GetComponent<Renderer>().sharedMaterial);
        pillarMat.SetColor("_BaseColor", Color.grey); pillarMat.color = Color.grey;
        pillar.GetComponent<Renderer>().material = pillarMat;

        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crystal.transform.SetParent(transform, false);
        crystal.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        crystal.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        Destroy(crystal.GetComponent<Collider>());

        _sphereMat = new Material(crystal.GetComponent<Renderer>().sharedMaterial);
        _sphereMat.SetColor("_BaseColor", InactiveColor); _sphereMat.color = InactiveColor;
        crystal.GetComponent<Renderer>().material = _sphereMat;
    }

    void Update()
    {
        if (_activated) return;

        GunCharacter player = FindObjectOfType<GunCharacter>();
        if (player != null && Vector3.Distance(transform.position, player.transform.position) <= activateRadius)
            Activate();
    }

    void Activate()
    {
        _activated = true;

        _sphereMat.SetColor("_BaseColor", ActiveColor); _sphereMat.color = ActiveColor;

        LastActivatedPosition = transform.position;
        HasActivatedAny = true;

        GunCharacter player = FindObjectOfType<GunCharacter>();
        if (player != null) SaveManager.Save(player.inventory);
    }
}
