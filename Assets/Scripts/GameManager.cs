using UnityEngine;
using System.Collections; // Added for IEnumerator
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text; // For StringBuilder
using TMPro; // For TextMeshPro UI elements
using UnityEngine.UI; // Required for Button component

public enum GamePhase
{
    PlayerIntroduction, // New phase
    LovesFirstImpression,
    LovesReassessment,
    LovesFinalChoice,
    GameEnd
}

public class GameManager : MonoBehaviour
{
    [Header("API Keys")]
    [SerializeField] private string geminiApiKey = "AIzaSyCTbaWNJX-S8W-YMI6jqsO5OrkhS3tfn9g"; // Set in Inspector

    [Header("Game State")]
    public GamePhase currentPhase;
    public List<AIGuestProfile> aiGuests;
    public PlayerProfile currentPlayerProfile; // New: Player's AI-generated profile
    public AIGuestProfile selectedGuestForFinalChoice;
    public int currentPlayerTurn = 0; // To track which guest is currently interacting or being addressed

    [Header("UI References")]
    public TextMeshProUGUI dialogueText;
    public TMP_InputField playerInputField;
    public GameObject submitButton; // Or a Button component reference
    public TextMeshProUGUI phaseIndicatorText;
    public List<TextMeshProUGUI> guestNameTexts; // To display guest names
    public List<GameObject> guestLightIndicators; // To display light status (e.g., colored circles)

    private GeminiLLMService geminiService;

    void Awake()
    {
        // Ensure there's only one GameManager
        if (FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        geminiService = gameObject.AddComponent<GeminiLLMService>();
        geminiService.SetApiKey(geminiApiKey);

        InitializeGame();
    }

    void InitializeGame()
    {
        currentPhase = GamePhase.PlayerIntroduction; // Changed
        phaseIndicatorText.text = "Phase: Player Introduction"; // Changed
        dialogueText.text = "Welcome to 'Fei Cheng Wu Rao'! Generating your contestant profile..."; // Changed

        // Initialize AI Guests here, or after player intro. Keeping it here for now.
        aiGuests = new List<AIGuestProfile>
        {
            // Create 5 placeholder AI guests for MVP with initial affection scores (0-100)
            new AIGuestProfile("Alice", 28, "Software Engineer", new List<string> { "coding", "hiking", "sci-fi" }, "analytical, witty, adventurous", "a partner who shares my intellectual curiosity", 75f),
            new AIGuestProfile("Bella", 25, "Artist", new List<string> { "painting", "music", "travel" }, "creative, free-spirited, empathetic", "someone who appreciates art and passion", 60f),
            new AIGuestProfile("Chloe", 30, "Doctor", new List<string> { "reading", "volunteering", "cooking" }, "caring, intelligent, practical", "a stable and supportive relationship", 50f),
            new AIGuestProfile("Daisy", 22, "Student", new List<string> { "gaming", "social media", "fashion" }, "energetic, trendy, playful", "a fun and exciting relationship", 65f),
            new AIGuestProfile("Eve", 33, "Entrepreneur", new List<string> { "business", "networking", "fitness" }, "ambitious, confident, direct", "a driven and independent partner", 40f)
        };

        // Hook up input field and button (will be disabled/enabled by PlayerIntroductionSequence)
        playerInputField.onEndEdit.AddListener(OnPlayerInputEndEdit);
        if (submitButton != null)
        {
            Button buttonComponent = submitButton.GetComponent<Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() => OnPlayerSubmit(playerInputField.text));
            }
            else
            {
                Debug.LogError("SubmitButton GameObject does not have a Button component attached.");
            }
        }
        else
        {
            Debug.LogError("SubmitButton reference is null in GameManager.");
        }

        // Start the player introduction process
        StartCoroutine(PlayerIntroductionSequence()); // New coroutine
    }

    void UpdateGuestUI()
    {
        for (int i = 0; i < aiGuests.Count; i++)
        {
            if (i < guestNameTexts.Count)
            {
                guestNameTexts[i].text = aiGuests[i].guestName;
            }
            if (i < guestLightIndicators.Count)
            {
                // Simple color change for light status
                Color lightColor = Color.gray; // Off
                if (aiGuests[i].currentLightStatus == LightStatus.On)
                {
                    lightColor = Color.green; // On
                }
                else if (aiGuests[i].currentLightStatus == LightStatus.Burst)
                {
                    lightColor = Color.yellow; // Burst
                }
                // Assuming guestLightIndicators are Image components or similar
                Image lightImage = guestLightIndicators[i].GetComponent<Image>();
                if (lightImage != null)
                {
                    lightImage.color = lightColor;
                }
                else
                {
                    Debug.LogWarning($"Guest light indicator for {aiGuests[i].guestName} does not have an Image component.");
                }
                Debug.Log($"{aiGuests[i].guestName}'s light is {aiGuests[i].currentLightStatus}");
            }
        }
    }

    private IEnumerator PlayerIntroductionSequence()
    {
        // Disable player input during generation
        playerInputField.gameObject.SetActive(false);
        if (submitButton != null) submitButton.gameObject.SetActive(false);

        yield return StartCoroutine(GeneratePlayerBackground().AsCoroutine()); // Wait for background to be generated

        dialogueText.text += "\n\n--- Guests' First Impressions ---"; // New header
        yield return StartCoroutine(ProcessGuestFirstImpressions().AsCoroutine()); // Process guest impressions

        dialogueText.text += "\n\nClick the 'Submit' button (or press Enter) to continue.";
        // playerInputField should remain disabled as no text input is needed here.
        // playerInputField.gameObject.SetActive(false); // Already set at the start of the coroutine
        if (submitButton != null)
        {
            submitButton.gameObject.SetActive(true); // Ensure the Submit/Next button is active
        }
        // No automatic transition here. OnPlayerSubmit will handle it.
    }

    private async Task GeneratePlayerBackground()
    {
        // 1. Construct the prompt
        string prompt = "You are an AI assistant for a dating game show called 'Fei Cheng Wu Rao'. " +
                        "Generate a compelling and interesting background for a male contestant. " +
                        "The background should be a single paragraph and include: " +
                        "His approximate age (between 25 and 40), " +
                        "his profession (choose from diverse fields like tech, arts, science, business, education, etc.), " +
                        "a couple of his main interests/hobbies, " +
                        "a key personality trait (e.g., adventurous, thoughtful, humorous, ambitious), " +
                        "and a brief mention of what he's looking for in a partner. " +
                        "Make the description sound natural and engaging for a dating show introduction. Do not use placeholders like [Age] or [Profession]. Directly state the generated values. Example: 'Our next contestant is Michael, a 32-year-old software architect who loves rock climbing and playing the guitar. He's known for his witty humor and is hoping to find someone who shares his passion for adventure and a good laugh.'";

        dialogueText.text += "\n\nSystem: Generating your unique profile with AI...";
        string generatedBackgroundText = await geminiService.GetGeminiResponse(prompt);

        if (generatedBackgroundText.StartsWith("Error:"))
        {
            dialogueText.text += $"\n\nSystem: Error generating profile: {generatedBackgroundText}";
            // Handle error, maybe use a default profile
            currentPlayerProfile = new PlayerProfile("Default Player", 30, "Adventurer", new List<string>{"Exploring"}, "Curious", "An interesting connection");
        }
        else
        {
            dialogueText.text += $"\n\n--- Your AI-Generated Profile ---";
            dialogueText.text += $"\n{generatedBackgroundText}";
            // For now, store the whole text. Later, parse it into PlayerProfile fields.
            // This is a simplification. Ideally, the LLM returns structured data or we parse this string.
            currentPlayerProfile = ParsePlayerBackground(generatedBackgroundText); // Need to implement ParsePlayerBackground
        }
    }

    private PlayerProfile ParsePlayerBackground(string backgroundText)
    {
        // This is a very basic placeholder.
        // Ideally, the LLM would provide structured data, or we'd use more robust NLP parsing.
        // For now, we can try to extract some info or just store the description.
        // Let's assume the LLM gives a paragraph. We'll need to decide how to populate PlayerProfile fields.
        // For the MVP, we might just store the full description in PlayerProfile.
        // Or, we can try to extract some keywords if the prompt is well-defined.

        // Example: if PlayerProfile has a 'fullDescription' field:
        // return new PlayerProfile { fullDescription = backgroundText };

        // For now, let's create a simple profile with the text as a general description
        // and try to guess some fields for demonstration. This is NOT robust.
        string name = "The Contestant"; // Or extract if possible
        int age = 30; // Default or try to parse
        string occupation = "To be revealed"; // Default or try to parse
        List<string> interests = new List<string>(); // Default or try to parse
        string personality = "Intriguing"; // Default or try to parse
        string relationshipGoals = "A meaningful connection"; // Default or try to parse

        // Attempt to parse age (very naive)
        var ageMatch = System.Text.RegularExpressions.Regex.Match(backgroundText, @"\b(\d{2})\b-year-old");
        if (ageMatch.Success) int.TryParse(ageMatch.Groups[1].Value, out age);

        // This parsing logic needs to be significantly improved or the LLM prompt needs to enforce a structure.
        // For the initial implementation, we might just have a single "description" field in PlayerProfile.
        // Let's assume PlayerProfile will have a constructor that takes the full text for now.
        return new PlayerProfile(name, age, occupation, interests, personality, relationshipGoals, backgroundText);
    }

    private async Task ProcessGuestFirstImpressions()
    {
        foreach (var guest in aiGuests)
        {
            string prompt = $"You are {guest.guestName}, a character in a dating show. Your profile: Age {guest.age}, Occupation {guest.occupation}, Interests {string.Join(", ", guest.interests)}, Personality {guest.personalityTraits}, Relationship Goals {guest.relationshipGoals}. A new male contestant has just been introduced with this background: '{currentPlayerProfile.fullGeneratedDescription}'. Based on your personality and preferences, what is your very brief, one-sentence internal first impression or thought about him? After your thought, on a NEW LINE, provide an estimated initial adjustment to your affection score for him (a number between -15.0 and +15.0, e.g., 5.0, -2.5, 0.0). Format exactly as: Thought: [Your one-sentence thought].\nAffectionAdjustment: [Numerical score]";
            
            string impressionResponse = await geminiService.GetGeminiResponse(prompt);
            Debug.Log($"Raw AI Impression Response for {guest.guestName}: {impressionResponse}");

            string thought;
            float adjustment;
            ParseGuestImpressionResponse(impressionResponse, out thought, out adjustment);

            string playerInputText = currentPlayerProfile.GetDisplayDescription(); // Use player's profile description for context
            string guestDialogue = thought; // Default to thought for dialogue

            guest.UpdateAffection(adjustment);
            guest.EvaluateAndSetLightStatus(playerInputText, guestDialogue); 
            guest.AddDialogue(guest.guestName + " (Initial Thought)", thought);
            dialogueText.text += $"\n{guest.guestName}: \"{thought}\"";
            Debug.Log($"{guest.guestName} initial thought: '{thought}', affection adjusted by {adjustment}. New score: {guest.affectionScore}");
        }
        UpdateGuestUI(); // Update light indicators after all impressions
    }

    private void ParseGuestImpressionResponse(string response, out string thought, out float adjustment)
    {
        thought = "Hmm, interesting."; // Default thought
        adjustment = 0.0f; // Default adjustment

        int thoughtIndex = response.IndexOf("Thought: ");
        int affectionIndex = response.IndexOf("\nAffectionAdjustment: ");

        if (thoughtIndex != -1)
        {
            if (affectionIndex != -1 && affectionIndex > thoughtIndex)
            {
                thought = response.Substring(thoughtIndex + "Thought: ".Length, affectionIndex - (thoughtIndex + "Thought: ".Length)).Trim();
                string adjustmentStr = response.Substring(affectionIndex + "\nAffectionAdjustment: ".Length).Trim();
                if (float.TryParse(adjustmentStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float parsedAdjustment))
                {
                    adjustment = parsedAdjustment;
                }
                else
                {
                    Debug.LogWarning($"Could not parse AffectionAdjustment value '{adjustmentStr}' from AI impression response.");
                }
            }
            else
            {
                thought = response.Substring(thoughtIndex + "Thought: ".Length).Trim();
                Debug.LogWarning($"Could not find 'AffectionAdjustment:' in AI impression response. Defaulting adjustment to 0.");
            }
        }
        else
        {
            Debug.LogWarning($"Could not find 'Thought:' in AI impression response. Defaulting thought and adjustment.");
        }
    }

    private void OnPlayerInputEndEdit(string input)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnPlayerSubmit(input);
        }
    }

    public async void OnPlayerSubmit(string playerInputText)
    {
        if (currentPhase == GamePhase.PlayerIntroduction)
        {
            TransitionToLovesFirstImpression();
            return; // Skip the rest of the input processing logic for this phase
        }

        if (string.IsNullOrWhiteSpace(playerInputText)) return;

        dialogueText.text += $"\n\nPlayer: {playerInputText}";
        playerInputField.text = ""; // Clear input field

        // Add player dialogue to all active guests' history for context
        foreach (var guest in aiGuests)
        {
            if (guest.currentLightStatus != LightStatus.Off)
            {
                guest.AddDialogue("Player", playerInputText);
            }
        }

        await ProcessPlayerTurn(playerInputText);
    }

    private async Task ProcessPlayerTurn(string playerInputText)
    {
        switch (currentPhase)
        {
            case GamePhase.PlayerIntroduction: // New case
                // This phase is handled by PlayerIntroductionSequence, no player input expected here
                break;
            case GamePhase.LovesFirstImpression:
                await HandleLovesFirstImpression(playerInputText);
                break;
            case GamePhase.LovesReassessment:
                await HandleLovesReassessment(playerInputText);
                break;
            case GamePhase.LovesFinalChoice:
                HandleLovesFinalChoice(playerInputText);
                break;
            case GamePhase.GameEnd:
                // Handle game end state, maybe restart option
                break;
        }
    }

    private void TransitionToLovesFirstImpression()
    {
        currentPhase = GamePhase.LovesFirstImpression;
        phaseIndicatorText.text = "Phase: Love's First Impression";
        dialogueText.text += "\n\nLet's meet our 5 lovely AI guests."; 

        UpdateGuestUI(); // Update guest UI for the new phase

        // Re-enable player input field for the LovesFirstImpression phase
        playerInputField.gameObject.SetActive(true);
        // The submitButton is already active, so no need to set it again unless its text was changed.
        playerInputField.Select(); 
        playerInputField.ActivateInputField();
    }

    private async Task HandleLovesFirstImpression(string playerInputText)
    {
        dialogueText.text += "\n\n--- AI Guest Responses (Love's First Impression) ---";
        foreach (var guest in aiGuests)
        {
            if (guest.currentLightStatus != LightStatus.Off) // Only active guests respond
            {
                string prompt = GeneratePromptForGuest(guest, playerInputText);
                string aiResponse = await geminiService.GetGeminiResponse(prompt);
                Debug.Log($"Raw AI Response for {guest.guestName}: {aiResponse}");
                
                string guestDialogue = aiResponse; // Default to full response
                float affectionChange = 0f;

                // Parse AI response for dialogue and affection change
                int affectionChangeIndex = aiResponse.LastIndexOf("\nAffectionChange: ");
                if (affectionChangeIndex != -1)
                {
                    guestDialogue = aiResponse.Substring(0, affectionChangeIndex).Trim();
                    string affectionChangeStr = aiResponse.Substring(affectionChangeIndex + "\nAffectionChange: ".Length).Trim();
                    if (float.TryParse(affectionChangeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out affectionChange))
                    {
                        Debug.Log($"{guest.guestName}: Parsed AffectionChange: {affectionChange}");
                    }
                    else
                    {
                        Debug.LogWarning($"{guest.guestName}: Could not parse AffectionChange value '{affectionChangeStr}' from AI response: {aiResponse}");
                    }
                }
                else
                {
                    Debug.LogWarning($"{guest.guestName}: Could not find 'AffectionChange:' in AI response. Defaulting affectionChange to 0. Raw response: {aiResponse}");
                }

                guest.UpdateAffection(affectionChange);
                // guest.UpdateInterestMatch(interestChange); // Call these when implemented
                // guest.UpdateValuesMatch(valuesChange);   // Call these when implemented

                // Let the guest profile decide its light status based on new scores
                guest.EvaluateAndSetLightStatus(playerInputText, guestDialogue); 

                guest.AddDialogue(guest.guestName, guestDialogue); // Add the verbal response to history
                dialogueText.text += $"\n{guest.guestName}: {guestDialogue} (Light: {guest.currentLightStatus})"; // UI shows the new status
            }
        }
        // Collective Light Off check
        int lightsOffCount = 0;
        foreach (var g in aiGuests)
        {
            if (g.currentLightStatus == LightStatus.Off)
            {
                lightsOffCount++;
            }
        }

        if (aiGuests.Count >= 3 && lightsOffCount >= (aiGuests.Count / 2) + 1)
        {
            dialogueText.text += "\n\n--- WARNING: Collective Light Off! --- \nMany guests have lost interest. The pressure is on!";
        }

        UpdateGuestUI();
        // After all guests respond, transition to next phase or allow more interaction in current phase
        // For MVP, let's transition after one round of responses
        currentPhase = GamePhase.LovesReassessment;
        phaseIndicatorText.text = "Phase: Love's Reassessment";
        dialogueText.text += "\n\n--- Phase Transition: Love's Reassessment --- \n\nPlayer, you can now interact further with the remaining guests.";
    }

    private async Task HandleLovesReassessment(string playerInputText)
    {
        dialogueText.text += "\n\n--- AI Guest Responses (Love's Reassessment) ---";
        foreach (var guest in aiGuests)
        {
            if (guest.currentLightStatus != LightStatus.Off) // Only active guests respond
            {
                string prompt = GeneratePromptForGuest(guest, playerInputText);
                string aiResponse = await geminiService.GetGeminiResponse(prompt);
                Debug.Log($"Raw AI Response for {guest.guestName}: {aiResponse}");

                string guestDialogue = aiResponse; // Default to full response
                float affectionChange = 0f;

                // Parse AI response for dialogue and affection change
                int affectionChangeIndex = aiResponse.LastIndexOf("\nAffectionChange: ");
                if (affectionChangeIndex != -1)
                {
                    guestDialogue = aiResponse.Substring(0, affectionChangeIndex).Trim();
                    string affectionChangeStr = aiResponse.Substring(affectionChangeIndex + "\nAffectionChange: ".Length).Trim();
                    if (float.TryParse(affectionChangeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out affectionChange))
                    {
                        Debug.Log($"{guest.guestName}: Parsed AffectionChange: {affectionChange}");
                    }
                    else
                    {
                        Debug.LogWarning($"{guest.guestName}: Could not parse AffectionChange value '{affectionChangeStr}' from AI response: {aiResponse}");
                    }
                }
                else
                {
                    Debug.LogWarning($"{guest.guestName}: Could not find 'AffectionChange:' in AI response. Defaulting affectionChange to 0. Raw response: {aiResponse}");
                }

                guest.UpdateAffection(affectionChange);
                // guest.UpdateInterestMatch(interestChange); // Call these when implemented
                // guest.UpdateValuesMatch(valuesChange);   // Call these when implemented

                // Let the guest profile decide its light status based on new scores
                guest.EvaluateAndSetLightStatus(playerInputText, guestDialogue);

                guest.AddDialogue(guest.guestName, guestDialogue); // Add the verbal response to history
                dialogueText.text += $"\n{guest.guestName}: {guestDialogue} (Light: {guest.currentLightStatus})"; // UI shows the new status
            }
        }
        // Collective Light Off check
        int lightsOffCount = 0;
        foreach (var g in aiGuests)
        {
            if (g.currentLightStatus == LightStatus.Off)
            {
                lightsOffCount++;
            }
        }

        if (aiGuests.Count >= 3 && lightsOffCount >= (aiGuests.Count / 2) + 1)
        {
            dialogueText.text += "\n\n--- WARNING: Collective Light Off! --- \nMany guests have lost interest. The pressure is on!";
        }

        UpdateGuestUI();
        // After all guests respond, transition to final choice or allow more interaction
        currentPhase = GamePhase.LovesFinalChoice;
        phaseIndicatorText.text = "Phase: Love's Final Choice";
        dialogueText.text += "\n\n--- Phase Transition: Love's Final Choice --- \n\nPlayer, choose one of the remaining guests by typing their name, or type 'skip' for a smart recommendation.";
    }

    private void HandleLovesFinalChoice(string playerInputText)
    {
        if (playerInputText.ToLower() == "skip")
        {
            // Smart Recommendation (simplified: pick a random 'On' or 'Burst' light)
            List<AIGuestProfile> remainingGuests = aiGuests.FindAll(g => g.currentLightStatus != LightStatus.Off);
            if (remainingGuests.Count > 0)
            {
                selectedGuestForFinalChoice = remainingGuests[Random.Range(0, remainingGuests.Count)];
                dialogueText.text += $"\n\nSystem: You chose to skip. The system recommends {selectedGuestForFinalChoice.guestName}!";
                EndGameWithResult(selectedGuestForFinalChoice);
            }
            else
            {
                dialogueText.text += "\n\nSystem: No guests remaining with lights on. Game Over.";
                currentPhase = GamePhase.GameEnd;
            }
        }
        else
        {
            AIGuestProfile chosenGuest = aiGuests.Find(g => g.guestName.ToLower() == playerInputText.ToLower() && g.currentLightStatus != LightStatus.Off);
            if (chosenGuest != null)
            {
                selectedGuestForFinalChoice = chosenGuest;
                dialogueText.text += $"\n\nPlayer: I choose {chosenGuest.guestName}.";
                EndGameWithResult(chosenGuest);
            }
            else
            {
                dialogueText.text += "\n\nSystem: Invalid choice or guest's light is off. Please choose an active guest by typing their name.";
            }
        }
    }

    private async void EndGameWithResult(AIGuestProfile finalGuest)
    {
        string prompt = $"You are {finalGuest.guestName}, a {finalGuest.age}-year-old {finalGuest.occupation}. Your personality is {finalGuest.personalityTraits}. You are on a dating show. The male contestant has chosen you for the final decision. Based on your previous interactions and your current light status ({finalGuest.currentLightStatus}), do you accept or reject him? Respond with your decision and a brief message. State 'Decision: ACCEPT' or 'Decision: REJECT'.";
        string aiResponse = await geminiService.GetGeminiResponse(prompt);

        string decision = "REJECT"; // Default
        string finalMessage = aiResponse;

        if (aiResponse.Contains("Decision: ACCEPT"))
        {
            decision = "ACCEPT";
            finalMessage = finalMessage.Replace("Decision: ACCEPT", "").Trim();
        }
        else if (aiResponse.Contains("Decision: REJECT"))
        {
            decision = "REJECT";
            finalMessage = finalMessage.Replace("Decision: REJECT", "").Trim();
        }

        dialogueText.text += $"\n\n{finalGuest.guestName}: {finalMessage}";

        if (decision == "ACCEPT")
        {
            dialogueText.text += $"\n\n--- CONGRATULATIONS! You and {finalGuest.guestName} are a match! ---";
        }
        else
        {
            dialogueText.text += $"\n\n--- Too bad! {finalGuest.guestName} has rejected you. ---";
        }

        currentPhase = GamePhase.GameEnd;
        phaseIndicatorText.text = "Game Over!";
        playerInputField.gameObject.SetActive(false);
        if (submitButton != null)
        {
            submitButton.gameObject.SetActive(false);
        }
    }

    private string GeneratePromptForGuest(AIGuestProfile guest, string playerInput)
    {
        StringBuilder promptBuilder = new StringBuilder();
        promptBuilder.AppendLine($"You are {guest.guestName}, a {guest.age}-year-old {guest.occupation}.");
        promptBuilder.AppendLine($"Your personality traits are: {guest.personalityTraits}.");
        promptBuilder.AppendLine($"Your interests include: {string.Join(", ", guest.interests)}.");
        promptBuilder.AppendLine($"You are looking for: {guest.relationshipGoals}.");
        promptBuilder.AppendLine($"You are a female contestant on a dating show called 'Fei Cheng Wu Rao'.");
        promptBuilder.AppendLine($"The current phase is '{currentPhase}'.");
        promptBuilder.AppendLine($"Your current light status is '{guest.currentLightStatus}'.");
        // ADDED: Provide current emotional context to LLM
        promptBuilder.AppendLine($"Your current affection towards the player is {guest.affectionScore}/100.");

        promptBuilder.AppendLine("Here is the conversation history:");
        foreach (string line in guest.dialogueHistory)
        {
            promptBuilder.AppendLine(line);
        }
        promptBuilder.AppendLine($"The male contestant has just said: \"{playerInput}\"");
        
        // MODIFIED: New instruction for LLM
        promptBuilder.AppendLine("Based on your persona, the conversation, and your current feelings, how do you respond to the male contestant? ");
        promptBuilder.AppendLine("In your response, subtly hint at your current level of interest or feelings (e.g., if you are pleased, curious, disappointed, etc.). Do NOT explicitly state 'Light: ON', 'Light: OFF', or 'Light: BURST'. Your dialogue alone should convey your state.");
        promptBuilder.AppendLine("After your dialogue, on a NEW LINE, provide an estimated change in your affection towards the player based ONLY on this specific interaction. Use the format 'AffectionChange: X.X'.");
        promptBuilder.AppendLine("If the change is positive, X.X should be a number between 5.0 and 20.0.");
        promptBuilder.AppendLine("If the change is negative, X.X should be a number between -20.0 and -5.0.");
        promptBuilder.AppendLine("If the interaction is neutral or the change is less than 5.0 (positive or negative), provide 'AffectionChange: 0.0'.");
        promptBuilder.AppendLine("For example: 'AffectionChange: 10.0', 'AffectionChange: -7.5', or 'AffectionChange: 0.0'.");
        
        return promptBuilder.ToString();
    }
}
