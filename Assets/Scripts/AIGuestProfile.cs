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
    public Transform modelTransform; // Reference to the guest's 3D model in the scene
    // public float initialDisposition; // Removed, replaced by affectionScore
    public LightStatus currentLightStatus;
    public List<string> dialogueHistory;

    // New preference parameters
    public float affectionScore; // Range: 0-100 (represents 好感度)
    public float interestMatchScore; // Range: 0-100 (兴趣爱好匹配度)
    public float valuesMatchScore; // Range: 0-100 (价值观匹配度)
    public int negativeInteractionBuffer; // For the "缓冲区"

    // Thresholds
    public const float AffectionThresholdLightOn = 70f;
    public const float AffectionThresholdLightOff = 40f;
    public const float AffectionThresholdBurst = 90f;
    public const int InitialNegativeBuffer = 1; // Example: 1 free pass for a minor negative interaction

    public AIGuestProfile(string name, int age, string occ, List<string> interests, string personality, string goals, float initialAffection, Transform modelTrans = null)
    {
        this.guestName = name;
        this.age = age;
        this.occupation = occ;
        this.interests = interests;
        this.personalityTraits = personality;
        this.relationshipGoals = goals;
        this.modelTransform = modelTrans; // Assign the model transform
        
        this.affectionScore = initialAffection; // Use the passed value (e.g., 50-70 as a base)
        this.interestMatchScore = 50f; // Start neutral
        this.valuesMatchScore = 50f;   // Start neutral
        this.negativeInteractionBuffer = InitialNegativeBuffer;

        // Initial light status based on affection
        if (this.affectionScore >= AffectionThresholdLightOn)
        {
            this.currentLightStatus = LightStatus.On;
        }
        else if (this.affectionScore < AffectionThresholdLightOff)
        {
            this.currentLightStatus = LightStatus.Off;
        }
        else // In between, could be on or off based on game design, let's default to On if not clearly off
        {
            this.currentLightStatus = LightStatus.On; 
        }
        
        this.dialogueHistory = new List<string>();
    }

    public void AddDialogue(string speaker, string dialogue)
    {
        dialogueHistory.Add($"{speaker}: {dialogue}");
    }

    // Methods to update scores
    public void UpdateAffection(float amount)
    {
        this.affectionScore = Mathf.Clamp(this.affectionScore + amount, 0f, 100f);
        Debug.Log($"{guestName} affection changed by {amount}, new score: {this.affectionScore}");
    }

    public void UpdateInterestMatch(float amount)
    {
        this.interestMatchScore = Mathf.Clamp(this.interestMatchScore + amount, 0f, 100f);
    }

    public void UpdateValuesMatch(float amount)
    {
        this.valuesMatchScore = Mathf.Clamp(this.valuesMatchScore + amount, 0f, 100f);
    }

    // Core Logic: Evaluate and Set Light Status
    public void EvaluateAndSetLightStatus(string playerInput, string aiDialogueResponse)
    {
        // NEW: If light is already off, it stays off permanently.
        if (this.currentLightStatus == LightStatus.Off)
        {
            Debug.Log($"{guestName}'s light is already OFF and will remain OFF.");
            return; 
        }

        LightStatus previousLightStatus = this.currentLightStatus;

        // Rule: Burst Light (好感度极高且玩家触发特定关键词或行为)
        if (this.affectionScore >= AffectionThresholdBurst)
        {
            this.currentLightStatus = LightStatus.Burst;
            Debug.Log($"{guestName} triggered BURST light due to high affection!");
            return; // Burst light overrides other logic (except permanent OFF)
        }

        // Rule: Light Off (好感度低于某个阈值 or 玩家言论与女嘉宾价值观严重冲突)
        bool potentialLightOff = false;
        if (this.affectionScore <= AffectionThresholdLightOff)
        {
            potentialLightOff = true;
            Debug.Log($"{guestName} affection ({this.affectionScore}) below threshold for light OFF ({AffectionThresholdLightOff}).");
        }
        // TODO: Add more sophisticated check for "价值观严重冲突" (severe value conflict)

        if (potentialLightOff)
        {
            if (this.negativeInteractionBuffer > 0 && previousLightStatus != LightStatus.Off) // Ensure buffer only applies if light wasn't already off
            {
                this.negativeInteractionBuffer--;
                this.currentLightStatus = LightStatus.On; // Keep it on for now due to buffer
                Debug.Log($"{guestName} light remains ON due to buffer (buffer remaining: {this.negativeInteractionBuffer}). Affection: {this.affectionScore}");
            }
            else
            {
                this.currentLightStatus = LightStatus.Off; // Turn it off
                Debug.Log($"{guestName} light turned OFF. Affection: {this.affectionScore}, Buffer: {this.negativeInteractionBuffer}");
                return; // Once it's off, it's off.
            }
        }
        // Rule: Light On (好感度达到某个阈值) - This part is now only reachable if the light wasn't turned off above.
        else if (this.affectionScore >= AffectionThresholdLightOn)
        {
            this.currentLightStatus = LightStatus.On; // Stays On or turns On (if it wasn't Burst)
            if (this.affectionScore > (AffectionThresholdLightOn + AffectionThresholdLightOff) / 2) 
            {
                this.negativeInteractionBuffer = InitialNegativeBuffer;
            }
            Debug.Log($"{guestName} light is ON. Affection: {this.affectionScore}");
        }
        else
        {
            // Affection is in a middling range (e.g., between 30 and 70)
            // If light was on, and no strong reason to turn off, keep it on.
            // This path is only taken if light is not Off, not Burst, and affection is not high enough for On explicitly.
            // So, it implies the light was already On and stays On.
            if (previousLightStatus == LightStatus.On) { // Or Burst, but Burst is handled above
                 this.currentLightStatus = LightStatus.On;
            }
            // No need for an else to set to Off, as that's handled by the potentialLightOff logic or the initial check.
            Debug.Log($"{guestName} light status maintained ({this.currentLightStatus}) in middling affection range. Affection: {this.affectionScore}");
        }
    }
}
