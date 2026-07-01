using System.Collections.Generic;

// GLM 5.1 via Manifest OS (call_id 176) — applied as-is
public class SkillTree
{
    readonly HashSet<SkillNode> _unlocked = new HashSet<SkillNode>();
    public HashSet<SkillNode> Unlocked => _unlocked;
    public int skillPoints;

    public bool TryUnlock(SkillNode node)
    {
        if (node == null) return false;
        if (_unlocked.Contains(node)) return false;
        if (skillPoints < node.xpCost) return false;
        if (!node.CanUnlock(_unlocked)) return false;

        skillPoints -= node.xpCost;
        _unlocked.Add(node);
        return true;
    }

    public bool IsUnlocked(SkillNode node) => node != null && _unlocked.Contains(node);

    public float GetTotalBonus(SkillNode.EffectType type)
    {
        float total = 0f;
        foreach (SkillNode node in _unlocked)
            if (node.effectType == type)
                total += node.effectValue;
        return total;
    }

    public void AddSkillPoints(int amount)
    {
        skillPoints += amount;
        if (skillPoints < 0) skillPoints = 0;
    }
}
