using UnityEngine;

// GLM 5.1 via Manifest OS (call_id 165) — corrected: SetParent(transform, false) on both children
public class WeaponPickup : MonoBehaviour
{
    public WeaponBase weaponPrefab;
    public string     weaponDisplayName;
    public Color      orbColor;
    public float      interactRadius = 2f;

    GameObject orbGO, glowRing;
    Transform  playerTransform;
    bool       playerNearby;
    float      bobTime;

    void Start()
    {
        playerTransform = GameObject.FindWithTag("Player")?.transform;

        GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material   src = tmp.GetComponent<Renderer>().sharedMaterial;
        DestroyImmediate(tmp);

        Material orbMat = new Material(src);
        orbMat.SetColor("_BaseColor", orbColor); orbMat.color = orbColor;

        orbGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(orbGO.GetComponent<Collider>());
        orbGO.transform.SetParent(transform, false);
        orbGO.transform.localPosition = Vector3.up * 0.6f;
        orbGO.transform.localScale    = Vector3.one * 0.4f;
        orbGO.GetComponent<Renderer>().material = orbMat;

        Color glowColor = orbColor; glowColor.a = 0.5f;
        Material glowMat = new Material(src);
        glowMat.SetColor("_BaseColor", glowColor); glowMat.color = glowColor;

        glowRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(glowRing.GetComponent<Collider>());
        glowRing.transform.SetParent(transform, false);
        glowRing.transform.localPosition = Vector3.zero;
        glowRing.transform.localScale    = new Vector3(0.6f, 0.02f, 0.6f);
        glowRing.GetComponent<Renderer>().material = glowMat;
    }

    void Update()
    {
        bobTime += Time.deltaTime;
        orbGO.transform.localPosition = Vector3.up * (0.6f + Mathf.Sin(bobTime * 2.5f) * 0.12f);

        playerNearby = playerTransform != null &&
                       Vector3.Distance(transform.position, playerTransform.position) <= interactRadius;

        if (playerNearby && Input.GetKeyDown(KeyCode.E))
            PickUp();
    }

    void PickUp()
    {
        if (playerTransform == null) return;
        GunCharacter gc = playerTransform.GetComponent<GunCharacter>();
        if (gc != null)
            gc.EquipWeapon(Instantiate(weaponPrefab, gc.transform));
        Destroy(gameObject);
    }
}
