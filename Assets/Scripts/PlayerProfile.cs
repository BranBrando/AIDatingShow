using System.Collections.Generic;

[System.Serializable]
public class PlayerProfile
{
    public string playerName;
    public int age;
    public string occupation;
    public List<string> interests;
    public string personalityTraits;
    public string relationshipExpectations;
    public string fullGeneratedDescription; // To store the raw AI output initially

    // Constructor for simple initialization
    public PlayerProfile(string name, int age, string occ, List<string> interests, string personality, string goals, string fullDesc = "")
    {
        this.playerName = name;
        this.age = age;
        this.occupation = occ;
        this.interests = interests ?? new List<string>();
        this.personalityTraits = personality;
        this.relationshipExpectations = goals;
        this.fullGeneratedDescription = string.IsNullOrEmpty(fullDesc) ? GenerateFallbackDescription() : fullDesc;
    }

    // Fallback if parsing/generation fails or for simpler storage
    private string GenerateFallbackDescription()
    {
        return $"Name: {playerName}, Age: {age}, Occupation: {occupation}, Interests: {string.Join(", ", interests)}, Personality: {personalityTraits}, Looking for: {relationshipExpectations}";
    }

    // Method to get a displayable description
    public string GetDisplayDescription()
    {
        if (!string.IsNullOrEmpty(fullGeneratedDescription) && fullGeneratedDescription.Length > 50) // Arbitrary length to check if it's a real description
        {
            return fullGeneratedDescription;
        }
        return GenerateFallbackDescription();
    }
}
