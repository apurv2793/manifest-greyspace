using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageNumber : MonoBehaviour
{
    // DamageNumber.Spawn(position, damage, color)
    public static void Spawn(Vector3 worldPos, int dmg, Color col)
    {
        GameObject go = new GameObject("DmgNum");
        Canvas cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.WorldSpace;
        go.AddComponent<CanvasScaler>();
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * 0.012f;

        GameObject textGO = new GameObject("t");
        textGO.transform.SetParent(go.transform, false);
        Text t = textGO.AddComponent<Text>();
        t.text = dmg.ToString();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 80;
        t.color = col;
        t.alignment = TextAnchor.MiddleCenter;
        textGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 80);

        go.AddComponent<DamageNumber>().StartCoroutine(
            go.GetComponent<DamageNumber>().Animate(go, t, col));
    }

    IEnumerator Animate(GameObject go, Text t, Color startCol)
    {
        float dur = 0.6f, elapsed = 0;
        Vector3 origin = go.transform.position;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float n = elapsed / dur;
            go.transform.position = origin + Vector3.up * (n * 1.6f);
            t.color = new Color(startCol.r, startCol.g, startCol.b, 1f - n);
            if (Camera.main != null) go.transform.rotation = Camera.main.transform.rotation;
            yield return null;
        }
        Destroy(go);
    }
}
