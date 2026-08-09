using UnityEngine;

// A named bundle of authored content — see Docs/CONTENT-PACK-CONTRACT.md (shared with
// manifest-cod-experiment's src/content/registry.js, same shape/tolerance philosophy).
[CreateAssetMenu(menuName = "Game/ContentPack")]
public class ContentPack : ScriptableObject
{
    public string packId = "base";
    public ComboData[] combos;
    public MissionDefinition[] missions;
    public SkillNode[] skills;
}
