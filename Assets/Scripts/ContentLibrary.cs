using UnityEngine;

// Static content accessor — resolves combos/missions from the active ContentPack
// asset (Resources/ContentPacks/Base.asset), falling back to the original
// hand-written factories/inline construction if the pack, or a specific entry
// within it, is absent. Same "peek-style tolerance" as the JS engine's
// ctx.content.get() — see Docs/CONTENT-PACK-CONTRACT.md. The fallback path is not
// a placeholder to delete later: it's what keeps a missing/broken pack asset from
// taking the whole game down, on both sides of this project.
public static class ContentLibrary
{
    const string DefaultPackId = "Base";
    static ContentPack _active;
    static bool _loaded;

    static ContentPack Active
    {
        get
        {
            if (!_loaded)
            {
                _active = Resources.Load<ContentPack>("ContentPacks/" + DefaultPackId);
                _loaded = true;
                if (_active == null)
                    Debug.LogWarning("[ContentLibrary] No ContentPack at Resources/ContentPacks/" +
                        DefaultPackId + " — falling back to code-defined content for everything.");
            }
            return _active;
        }
    }

    public static ComboData Combo(string weaponName)
    {
        if (Active != null && Active.combos != null)
        {
            foreach (var c in Active.combos)
                if (c != null && c.name == weaponName) return c;
        }
        switch (weaponName)
        {
            case "Sword":  return ComboData.Sword();
            case "Bow":    return ComboData.Bow();
            case "Staff":  return ComboData.Staff();
            case "Shield": return ComboData.Shield();
            default:
                Debug.LogWarning("[ContentLibrary] Unknown weapon '" + weaponName + "', falling back to Sword.");
                return ComboData.Sword();
        }
    }

    // Returns null (not a fallback instance) when absent — the two current missions
    // are still built inline in GreyspaceScene.SpawnPortals() as the fallback, since
    // that construction already lives right next to where it's used and duplicating
    // it here would just be a second copy to keep in sync.
    public static MissionDefinition Mission(string missionId)
    {
        if (Active != null && Active.missions != null)
        {
            foreach (var m in Active.missions)
                if (m != null && m.missionId == missionId) return m;
        }
        return null;
    }
}
