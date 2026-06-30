using UnityEngine;

[CreateAssetMenu(menuName = "Game/MissionDefinition")]
public class MissionDefinition : ScriptableObject
{
    public string missionId;
    public string displayName;
    public string sceneName;        // Unity scene to load
    public int    waves      = 3;
    public int    rewardXP   = 75;
    public string requiredFlag;     // empty = always open
    public string lockedReason;
    [TextArea(2, 4)]
    public string description;
}
