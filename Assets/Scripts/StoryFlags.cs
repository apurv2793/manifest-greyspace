using System.Collections.Generic;

public static class StoryFlags
{
    static readonly HashSet<string> _set = new HashSet<string>();
    public static void Set(string key)   => _set.Add(key);
    public static bool Has(string key)   => _set.Contains(key);
    public static void Clear(string key) => _set.Remove(key);
}
