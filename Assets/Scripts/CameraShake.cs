using UnityEngine;
using System.Collections;

// GLM 5.1 via Manifest OS (call_id 187) — applied as-is
public class CameraShake
{
    static Coroutine runningCoroutine;
    static GameObject runnerObject;

    public static void Shake(float intensity, float duration)
    {
        if (runnerObject == null)
        {
            runnerObject = new GameObject("_CameraShakeRunner");
            runnerObject.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(runnerObject);
        }

        CameraShakeRunner runner = runnerObject.GetComponent<CameraShakeRunner>();
        if (runner == null)
            runner = runnerObject.AddComponent<CameraShakeRunner>();

        if (runningCoroutine != null)
            runner.StopCoroutine(runningCoroutine);

        if (Camera.main != null)
            runningCoroutine = runner.StartCoroutine(DoShake(intensity, duration));
    }

    static IEnumerator DoShake(float intensity, float duration)
    {
        Vector3 previousOffset = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return new WaitForEndOfFrame();

            Camera cam = Camera.main;
            if (cam == null) yield break;

            cam.transform.position -= previousOffset;

            float t = Mathf.Clamp01(elapsed / duration);
            float currentIntensity = intensity * (1f - t);

            Vector3 rawOffset = Random.insideUnitSphere * currentIntensity;
            rawOffset.y = 0f;

            Vector3 currentOffset = cam.transform.rotation * rawOffset;
            cam.transform.position += currentOffset;
            previousOffset = currentOffset;

            elapsed += Time.unscaledDeltaTime;
        }

        if (Camera.main != null)
            Camera.main.transform.position -= previousOffset;

        runningCoroutine = null;
    }

    class CameraShakeRunner : MonoBehaviour { }
}
