using UnityEngine;
using System.Collections;

public class CombatFeel : MonoBehaviour
{
    static CombatFeel _inst;

    void Awake() { _inst = this; }

    // Call from anywhere: CombatFeel.HitStop(5);
    public static void HitStop(int frames)
    {
        if (_inst == null)
        {
            var go = new GameObject("CombatFeel");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<CombatFeel>();
        }
        _inst.StartCoroutine(_inst.DoStop(frames));
    }

    IEnumerator DoStop(int frames)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(frames / 60f);
        Time.timeScale = 1f;
    }
}
