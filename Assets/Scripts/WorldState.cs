using System.Collections.Generic;

// GLM 5.1 via Manifest OS (call_id 181) — applied as-is
public static class WorldState
{
    static readonly Dictionary<string, string> _dict = new Dictionary<string, string>();

    public static void Set(string key, string value) => _dict[key] = value;

    public static string Get(string key, string defaultValue = "") => _dict.TryGetValue(key, out var val) ? val : defaultValue;

    public static bool Has(string key) => _dict.ContainsKey(key);

    public static void Clear(string key) => _dict.Remove(key);

    public static void ClearAll() => _dict.Clear();

    public static Dictionary<string, string> GetAll() => _dict;

    public static void LoadFrom(Dictionary<string, string> data)
    {
        _dict.Clear();
        if (data != null)
            foreach (var kvp in data)
                _dict[kvp.Key] = kvp.Value;
    }
}
