// GLM 5.1 via Manifest OS (call_id 179) — corrected: dropped unused using System;
// [System.Serializable] so activeSlots shows in the Inspector under GunCharacter -> Inventory.
// skillTree.unlocked stays runtime-only (Unity can't serialize HashSet) -- unlock via TryUnlock() in code/console.
[System.Serializable]
public class PlayerInventory
{
    public SkillTree skillTree = new SkillTree();
    public SkillNode[] activeSlots = new SkillNode[4];

    public bool AssignToSlot(int slotIndex, SkillNode node)
    {
        if (slotIndex < 0 || slotIndex > 3) return false;
        if (node == null || !skillTree.IsUnlocked(node)) return false;

        activeSlots[slotIndex] = node;
        return true;
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex <= 3)
            activeSlots[slotIndex] = null;
    }

    public float GetBonus(SkillNode.EffectType type) => skillTree.GetTotalBonus(type);

    public SkillNode GetSlot(int slotIndex) => (slotIndex >= 0 && slotIndex <= 3) ? activeSlots[slotIndex] : null;
}
