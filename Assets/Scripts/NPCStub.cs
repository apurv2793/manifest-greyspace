using UnityEngine;
using UnityEngine.Events;

// GLM 5.1 via Manifest OS (call_id 190) — corrected: .sharedMaterial -> .material
// (every other file in this project uses .material; sharedMaterial deviates from the convention)
//
// "Humanistic" pass: was a single capsule + a same-colored sphere — about as far from
// a person as a primitive build can get. Now a small limbed figure (torso/head/arms/
// legs) matching GunCharacter's construction idiom, plus the same idle-breathing
// treatment, so NPCs standing in the hub don't read as a totem pole.
public class NPCStub : MonoBehaviour
{
    public string npcName = "Stranger";
    public Color bodyColor = new Color(0.4f, 0.4f, 0.5f);
    public Color skinColor = new Color(0.74f, 0.57f, 0.45f);
    public float interactRadius = 2.2f;
    public UnityEvent onInteract;

    bool isPlayerNearby;
    public bool IsPlayerNearby => isPlayerNearby;

    Transform _visualRoot;
    float _idlePhase;

    void Start()
    {
        GameObject rootGO = new GameObject("VisualRoot");
        rootGO.transform.SetParent(transform, false);
        _visualRoot = rootGO.transform;
        _idlePhase = (GetInstanceID() % 1000) * 0.01f;

        Material body = MaterialCache.Get(bodyColor);
        Material skin = MaterialCache.Get(skinColor);

        // Legs
        P(PrimitiveType.Capsule, new Vector3(-0.11f, 0.28f, 0), new Vector3(0.13f, 0.30f, 0.13f), body);
        P(PrimitiveType.Capsule, new Vector3( 0.11f, 0.28f, 0), new Vector3(0.13f, 0.30f, 0.13f), body);

        // Torso
        P(PrimitiveType.Capsule, new Vector3(0, 0.72f, 0), new Vector3(0.32f, 0.36f, 0.24f), body);

        // Arms, resting at the sides
        P(PrimitiveType.Capsule, new Vector3(-0.30f, 0.68f, 0), new Vector3(0.09f, 0.28f, 0.09f), skin);
        P(PrimitiveType.Capsule, new Vector3( 0.30f, 0.68f, 0), new Vector3(0.09f, 0.28f, 0.09f), skin);

        // Head
        P(PrimitiveType.Sphere, new Vector3(0, 1.15f, 0), new Vector3(0.30f, 0.32f, 0.30f), skin);

        // A closed-hand suggestion at each cuff, same idiom as the player's grip hand.
        P(PrimitiveType.Sphere, new Vector3(-0.30f, 0.42f, 0), new Vector3(0.08f, 0.07f, 0.08f), skin);
        P(PrimitiveType.Sphere, new Vector3( 0.30f, 0.42f, 0), new Vector3(0.08f, 0.07f, 0.08f), skin);
    }

    GameObject P(PrimitiveType t, Vector3 lp, Vector3 ls, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(t);
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(_visualRoot, false);
        g.transform.localPosition = lp;
        g.transform.localScale = ls;
        g.GetComponent<Renderer>().material = m;
        return g;
    }

    void Update()
    {
        GunCharacter player = GunCharacter.Instance;
        if (player != null)
        {
            isPlayerNearby = Vector3.Distance(transform.position, player.transform.position) <= interactRadius;
            if (isPlayerNearby && InputRouter.InteractPressed())
                onInteract?.Invoke();
        }
        else
        {
            isPlayerNearby = false;
        }

        if (_visualRoot != null)
        {
            Vector3 lp = _visualRoot.localPosition;
            lp.y = Mathf.Sin((Time.time + _idlePhase) * 1.3f) * 0.012f;
            _visualRoot.localPosition = lp;
        }
    }
}
