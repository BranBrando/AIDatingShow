using System.Collections.Generic;
using UnityEngine;

public enum LightStatus
{
    Off,
    On,
    Burst
}

[System.Serializable]
public class AIGuestProfile
{
    public string guestName;
    public int age;
    public string occupation;
    public List<string> interests;
    public string personalityTraits; // Can be a comma-separated string or a list
    public string relationshipGoals;
    public float initialDisposition; // 0-1, how likely they are to keep light on initially
    public LightStatus currentLightStatus;
    public List<string> dialogueHistory;

    public AIGuestProfile(string name, int age, string occ, List<string> interests, string personality, string goals, float disposition)
    {
        this.guestName = name;
        this.age = age;
        this.occupation = occ;
        this.interests = interests;
        this.personalityTraits = personality;
        this.relationshipGoals = goals;
        this.initialDisposition = disposition;
        this.currentLightStatus = LightStatus.On; // Default to light on
        this.dialogueHistory = new List<string>();
    }

    public void AddDialogue(string speaker, string dialogue)
    {
        dialogueHistory.Add($"{speaker}: {dialogue}");
    }
}
