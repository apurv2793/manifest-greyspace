using System.Collections.Generic;

public static class StoryFlags
{
    static readonly HashSet<string> _set = new HashSet<string>();
    public static void Set(string key)   => _set.Add(key);
    public static bool Has(string key)   => _set.Contains(key);
    public static void Clear(string key) => _set.Remove(key);

    // For SaveManager persistence.
    public static List<string> GetAll() => new List<string>(_set);
    public static void LoadFrom(List<string> data)
    {
        _set.Clear();
        if (data != null)
            foreach (string s in data)
                _set.Add(s);
    }
}
