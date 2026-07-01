using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

// Groq llama-3.3-70b via Manifest OS (call_id 182) — corrected: added [System.Serializable]
// on SaveData (JsonUtility silently writes "{}" without it), replaced LINQ .Select with
// foreach (missing using System.Linq), added missing using System for Exception/Math.Min.
public static class SaveManager
{
    [System.Serializable]
    class SaveData
    {
        public List<string> worldStateKeys;
        public List<string> worldStateValues;
        public List<string> storyFlags;
        public int hp;
        public int maxHp;
        public int xp;
        public int level;
        public int skillPoints;
        public string currentMissionId;
        public string doneMissionId;
        public List<string> unlockedSkillNames;
        public string[] activeSlotNames;
    }

    static string SavePath => Application.persistentDataPath + "/save.json";

    public static void Save(PlayerInventory inventory)
    {
        try
        {
            var worldStateDict = WorldState.GetAll();
            var worldStateKeys = new List<string>();
            var worldStateValues = new List<string>();
            foreach (var kvp in worldStateDict)
            {
                worldStateKeys.Add(kvp.Key);
                worldStateValues.Add(kvp.Value);
            }

            var unlockedSkillNames = new List<string>();
            foreach (SkillNode node in inventory.skillTree.Unlocked)
                unlockedSkillNames.Add(node.skillName);

            var activeSlotNames = new string[inventory.activeSlots.Length];
            for (int i = 0; i < inventory.activeSlots.Length; i++)
                activeSlotNames[i] = inventory.activeSlots[i] != null ? inventory.activeSlots[i].skillName : "";

            var saveData = new SaveData
            {
                worldStateKeys = worldStateKeys,
                worldStateValues = worldStateValues,
                storyFlags = StoryFlags.GetAll(),
                hp = SceneState.hp,
                maxHp = SceneState.maxHp,
                xp = SceneState.xp,
                level = SceneState.level,
                skillPoints = SceneState.skillPoints,
                currentMissionId = SceneState.currentMissionId,
                doneMissionId = SceneState.doneMissionId,
                unlockedSkillNames = unlockedSkillNames,
                activeSlotNames = activeSlotNames,
            };

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save: " + ex.Message);
        }
    }

    public static bool Load(PlayerInventory inventory)
    {
        if (!File.Exists(SavePath)) return false;

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            var worldStateDict = new Dictionary<string, string>();
            int minCount = saveData.worldStateKeys.Count < saveData.worldStateValues.Count
                ? saveData.worldStateKeys.Count : saveData.worldStateValues.Count;
            for (int i = 0; i < minCount; i++)
                worldStateDict[saveData.worldStateKeys[i]] = saveData.worldStateValues[i];
            WorldState.LoadFrom(worldStateDict);
            StoryFlags.LoadFrom(saveData.storyFlags);

            SceneState.hp = saveData.hp;
            SceneState.maxHp = saveData.maxHp;
            SceneState.xp = saveData.xp;
            SceneState.level = saveData.level;
            SceneState.skillPoints = saveData.skillPoints;
            SceneState.currentMissionId = saveData.currentMissionId;
            SceneState.doneMissionId = saveData.doneMissionId;

            var skillNodeDict = new Dictionary<string, SkillNode>();
            foreach (SkillNode node in Resources.LoadAll<SkillNode>("Skills"))
                skillNodeDict[node.skillName] = node;

            foreach (string name in saveData.unlockedSkillNames)
                if (skillNodeDict.TryGetValue(name, out SkillNode node))
                    inventory.skillTree.TryUnlock(node);

            for (int i = 0; i < saveData.activeSlotNames.Length; i++)
            {
                string name = saveData.activeSlotNames[i];
                if (string.IsNullOrEmpty(name))
                    inventory.activeSlots[i] = null;
                else if (skillNodeDict.TryGetValue(name, out SkillNode node))
                    inventory.activeSlots[i] = node;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to load: " + ex.Message);
            return false;
        }
    }

    public static bool SaveExists() => File.Exists(SavePath);
}
