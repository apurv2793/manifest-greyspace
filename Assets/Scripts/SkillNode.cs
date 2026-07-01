using UnityEngine;

// GLM 5.1 via Manifest OS (call_id 177) — corrected: EffectType nested inside SkillNode
// (SkillTree references it as SkillNode.EffectType)
[CreateAssetMenu(menuName = "Game/SkillNode")]
public class SkillNode : ScriptableObject
{
    public enum EffectType { DamageBonus, SpeedBonus, MaxHPBonus, DashCharges, ComboExtend }

    public string skillName;
    [TextArea(2, 3)] public string description;
    public int xpCost;
    public EffectType effectType;
    public float effectValue;
    public SkillNode[] prerequisites;
    public string requiredFlag;

    public bool CanUnlock(System.Collections.Generic.HashSet<SkillNode> alreadyUnlocked)
    {
        if (!string.IsNullOrEmpty(requiredFlag) && !StoryFlags.Has(requiredFlag))
            return false;

        for (int i = 0; i < prerequisites.Length; i++)
            if (!alreadyUnlocked.Contains(prerequisites[i]))
                return false;

        return true;
    }
}
