using UnityEngine;

// GLM 5.1 via Manifest OS (call_id 191) — applied as-is
public class ZoneBounds : MonoBehaviour
{
    public Vector2 center;
    public Vector2 size;

    public bool Contains(Vector3 worldPos)
    {
        float halfW = size.x * 0.5f, halfD = size.y * 0.5f;
        return worldPos.x >= center.x - halfW && worldPos.x <= center.x + halfW &&
               worldPos.z >= center.y - halfD && worldPos.z <= center.y + halfD;
    }

    public Vector3 ClampToZone(Vector3 worldPos)
    {
        float halfW = size.x * 0.5f, halfD = size.y * 0.5f;
        float x = Mathf.Clamp(worldPos.x, center.x - halfW, center.x + halfW);
        float z = Mathf.Clamp(worldPos.z, center.y - halfD, center.y + halfD);
        return new Vector3(x, worldPos.y, z);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(center.x, 0f, center.y), new Vector3(size.x, 10f, size.y));
    }
}
