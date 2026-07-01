using UnityEngine;
using UnityEngine.Events;

// GLM 5.1 via Manifest OS (call_id 190) — corrected: .sharedMaterial -> .material
// (every other file in this project uses .material; sharedMaterial deviates from the convention)
public class NPCStub : MonoBehaviour
{
    public string npcName = "Stranger";
    public Color bodyColor = new Color(0.4f, 0.4f, 0.5f);
    public float interactRadius = 2.2f;
    public UnityEvent onInteract;

    bool isPlayerNearby;
    public bool IsPlayerNearby => isPlayerNearby;

    void Start()
    {
        GameObject bodyObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(bodyObj.GetComponent<Collider>());
        bodyObj.transform.SetParent(transform, false);
        bodyObj.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
        Material bodyMat = new Material(bodyObj.GetComponent<Renderer>().sharedMaterial);
        bodyMat.SetColor("_BaseColor", bodyColor); bodyMat.color = bodyColor;
        bodyObj.GetComponent<Renderer>().material = bodyMat;

        GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(headObj.GetComponent<Collider>());
        headObj.transform.SetParent(transform, false);
        headObj.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        headObj.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        Material headMat = new Material(headObj.GetComponent<Renderer>().sharedMaterial);
        headMat.SetColor("_BaseColor", bodyColor); headMat.color = bodyColor;
        headObj.GetComponent<Renderer>().material = headMat;
    }

    void Update()
    {
        GunCharacter player = FindObjectOfType<GunCharacter>();
        if (player != null)
        {
            isPlayerNearby = Vector3.Distance(transform.position, player.transform.position) <= interactRadius;
            if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
                onInteract?.Invoke();
        }
        else
        {
            isPlayerNearby = false;
        }
    }
}
