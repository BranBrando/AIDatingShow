using UnityEngine;
using System.Collections; // Added for IEnumerator
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text; // For StringBuilder
using TMPro; // For TextMeshPro UI elements
using UnityEngine.UI; // Required for Button component
using Unity.Cinemachine; // Required for Cinemachine


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
    public UIManager uiManager; // Reference to the new UIManager

    [Header("Camera References")]
    [SerializeField] private DynamicCameraController dynamicCameraController;
    [SerializeField] private Transform playerModelTransform; // Assign your player's 3D model Transform here
    [SerializeField] private List<Transform> aiGuestModelTransforms; // Assign your AI guests' 3D model Transforms here

    [Header("Random Camera Cycling")]
    public float cameraSwitchInterval = 5f;
    private Coroutine _randomCameraCycleCoroutineRef;

    private GeminiLLMService geminiService;
    private bool isWaitingToAdvanceGuestDialogue = false; // Flag to pause for player input between guest dialogues

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
        uiManager.SetPhaseIndicatorText("Phase: Player Introduction"); // Changed
        uiManager.SetDialogueText("Welcome to 'Fei Cheng Wu Rao'! Generating your contestant profile..."); // Changed

        // Activate the static opening camera
        if (dynamicCameraController != null)
        {
            dynamicCameraController.ActivateCamera(VCamType.StaticOpening);
        }

        // Initialize AI Guests with their transforms
        aiGuests = new List<AIGuestProfile>();
        for (int i = 0; i < 5; i++) // Assuming 5 guests as per original list
        {
            Transform guestTransform = (aiGuestModelTransforms != null && i < aiGuestModelTransforms.Count) ? aiGuestModelTransforms[i] : null;
            switch (i)
            {
                case 0: aiGuests.Add(new AIGuestProfile("Alice", 28, "Software Engineer", new List<string> { "coding", "hiking", "sci-fi" }, "analytical, witty, adventurous", "a partner who shares my intellectual curiosity", 75f, guestTransform)); break;
                case 1: aiGuests.Add(new AIGuestProfile("Bella", 25, "Artist", new List<string> { "painting", "music", "travel" }, "creative, free-spirited, empathetic", "someone who appreciates art and passion", 60f, guestTransform)); break;
                case 2: aiGuests.Add(new AIGuestProfile("Chloe", 30, "Doctor", new List<string> { "reading", "volunteering", "cooking" }, "caring, intelligent, practical", "a stable and supportive relationship", 50f, guestTransform)); break;
                case 3: aiGuests.Add(new AIGuestProfile("Daisy", 22, "Student", new List<string> { "gaming", "social media", "fashion" }, "energetic, trendy, playful", "a fun and exciting relationship", 65f, guestTransform)); break;
                case 4: aiGuests.Add(new AIGuestProfile("Eve", 33, "Entrepreneur", new List<string> { "business", "networking", "fitness" }, "ambitious, confident, direct", "a driven and independent partner", 40f, guestTransform)); break;
            }
        }

        // Subscribe to UIManager's submit event
        if (uiManager != null)
        {
            uiManager.OnPlayerSubmitClicked += OnPlayerSubmit;
        }
        else
        {
            Debug.LogError("UIManager reference is null in GameManager. Cannot subscribe to events.");
        }

        // Start the player introduction process
        StartCoroutine(PlayerIntroductionSequence()); // New coroutine
        AudioManager.Instance.PlayBGM("So Boring"); // Play BGM
    }

    void UpdateGuestUI()
    {
        uiManager.UpdateGuestUI(aiGuests);
    }

    private IEnumerator PlayerIntroductionSequence()
    {
        // Disable player input during generation
        uiManager.SetPlayerInputActive(false);
        uiManager.SetSubmitButtonActive(false);

        yield return StartCoroutine(GeneratePlayerBackground()); // Wait for background to be generated

        // The StaticOpening camera is already active from InitializeGame()
        // Add the 5-second delay here
        yield return new WaitForSeconds(5.0f); 

        // Activate player focus camera after profile generation is complete AND after the delay
        if (dynamicCameraController != null && currentPlayerProfile.modelTransform != null)
        {
            dynamicCameraController.ActivateCamera(VCamType.PlayerFocus, currentPlayerProfile.modelTransform);
        }
        // Add the 5-second delay here
        yield return new WaitForSeconds(5.0f); 

        uiManager.AppendDialogueText("\n\n--- Guests' First Impressions ---"); // New header
        yield return StartCoroutine(ProcessGuestFirstImpressions()); // Process guest impressions

        // Stop continuous camera cycling during player choice/interaction
        // StopContinuousCameraCycling();

        uiManager.AppendDialogueText("\n\nClick the 'Submit' button (or press Enter) to continue.");
        // playerInputField should remain disabled as no text input is needed here.
        uiManager.SetSubmitButtonActive(true); // Ensure the Submit/Next button is active
        // No automatic transition here. OnPlayerSubmit will handle it.
        // Set button text for this "continue" action
        uiManager.SetSubmitButtonText("Continue");
    }

    private IEnumerator GeneratePlayerBackground()
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

        uiManager.AppendDialogueText("\n\nSystem: Generating your unique profile with AI...");
        
        Task<string> generationTask = geminiService.GetGeminiResponse(prompt);
        yield return new WaitUntil(() => generationTask.IsCompleted); // Wait for the task to complete
        string generatedBackgroundText = generationTask.Result;

        if (generatedBackgroundText.StartsWith("Error:"))
        {
            uiManager.AppendDialogueText($"\n\nSystem: Error generating profile: {generatedBackgroundText}");
            // Handle error, maybe use a default profile
            currentPlayerProfile = new PlayerProfile("Default Player", 30, "Adventurer", new List<string>{"Exploring"}, "Curious", "An interesting connection", "Default profile generated due to error.", playerModelTransform);
        }
        else
        {
            uiManager.AppendDialogueText($"\n\n--- Your AI-Generated Profile ---");
            uiManager.AppendDialogueText($"\n{generatedBackgroundText}");
            currentPlayerProfile = ParsePlayerBackground(generatedBackgroundText); 
            currentPlayerProfile.modelTransform = playerModelTransform; // Ensure the transform is assigned
        }
        // Camera will be activated after the PlayerIntroductionSequence completes, not immediately here.
    }

    private PlayerProfile ParsePlayerBackground(string backgroundText)
    {
        string name = "The Contestant"; 
        int age = 30; 
        string occupation = "To be revealed"; 
        List<string> interests = new List<string>(); 
        string personality = "Intriguing"; 
        string relationshipGoals = "A meaningful connection"; 

        var ageMatch = System.Text.RegularExpressions.Regex.Match(backgroundText, @"\b(\d{2})\b-year-old");
        if (ageMatch.Success) int.TryParse(ageMatch.Groups[1].Value, out age);

        return new PlayerProfile(name, age, occupation, interests, personality, relationshipGoals, backgroundText, playerModelTransform);
    }

    private IEnumerator ProcessGuestFirstImpressions()
    {
        foreach (var guest in aiGuests)
        {
            string prompt = $"You are {guest.guestName}, a character in a dating show. Your profile: Age {guest.age}, Occupation {guest.occupation}, Interests {string.Join(", ", guest.interests)}, Personality {guest.personalityTraits}, Relationship Goals {guest.relationshipGoals}. A new male contestant has just been introduced with this background: '{currentPlayerProfile.fullGeneratedDescription}'. Based on your personality and preferences, what is your very brief, one-sentence internal first impression or thought about him? After your thought, on a NEW LINE, provide an estimated initial adjustment to your affection score for him (a number between -15.0 and +15.0, e.g., 5.0, -2.5, 0.0). Format exactly as: Thought: [Your one-sentence thought].\nAffectionAdjustment: [Numerical score]";
            
            Task<string> impressionTask = geminiService.GetGeminiResponse(prompt);
            yield return new WaitUntil(() => impressionTask.IsCompleted); // Wait for the task to complete
            string impressionResponse = impressionTask.Result;

            Debug.Log($"Raw AI Impression Response for {guest.guestName}: {impressionResponse}");

            string thought;
            float adjustment;
            ParseGuestImpressionResponse(impressionResponse, out thought, out adjustment);

            string playerInputText = currentPlayerProfile.GetDisplayDescription(); // Use player's profile description for context
            string guestDialogue = thought; // Default to thought for dialogue

            guest.UpdateAffection(adjustment);
            guest.EvaluateAndSetLightStatus(playerInputText, guestDialogue); 
            guest.AddDialogue(guest.guestName + " (Initial Thought)", thought);
            uiManager.AppendDialogueText($"\n{guest.guestName}: \"{thought}\"");
            Debug.Log($"{guest.guestName} initial thought: '{thought}', affection adjusted by {adjustment}. New score: {guest.affectionScore}");

            // Activate guest focus camera for each impression
            if (dynamicCameraController != null && guest.modelTransform != null)
            {
                dynamicCameraController.ActivateCamera(VCamType.GuestFocus, guest.modelTransform);
                yield return new WaitForSeconds(3.0f); // Shorter pause before waiting for input
            }

            // Wait for player to click "Next Guest"
            isWaitingToAdvanceGuestDialogue = true;
            uiManager.SetSubmitButtonActive(true); // Ensure it's active
            uiManager.SetSubmitButtonText("Next Guest");
            
            yield return new WaitUntil(() => !isWaitingToAdvanceGuestDialogue);
            // submitButtonText will be reset by PlayerIntroductionSequence or next phase
        }
        UpdateGuestUI(); // Update light indicators after all impressions
        // Start the continuous random camera cycling
        StartContinuousCameraCycling();
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

    // Removed OnPlayerInputEndEdit as it's now handled by UIManager
    // Removed OnPlayerSubmit as it's now handled by UIManager and subscribed to

    public void OnPlayerSubmit(string playerInputText) // Changed from async void
    {
        if (isWaitingToAdvanceGuestDialogue)
        {
            isWaitingToAdvanceGuestDialogue = false;
            uiManager.ClearPlayerInputField(); // Clear any accidental input if the field was active
            return; 
        }

        if (currentPhase == GamePhase.PlayerIntroduction)
        {
            // This is the "Continue" click after player profile is shown
            uiManager.SetSubmitButtonText("Submit"); // Revert for next phase
            TransitionToLovesFirstImpression();
            return; 
        }

        if (string.IsNullOrWhiteSpace(playerInputText) && currentPhase != GamePhase.PlayerIntroduction) return; // Allow empty submit only for initial continue

        uiManager.AppendDialogueText($"\n\nPlayer: {playerInputText}");
        uiManager.ClearPlayerInputField(); // Clear input field

        // Add player dialogue to all active guests' history for context
        foreach (var guest in aiGuests)
        {
            if (guest.currentLightStatus != LightStatus.Off)
            {
                guest.AddDialogue("Player", playerInputText);
            }
        }

        // Activate a random camera for player focus when it's the player's turn to input
        TriggerSingleCameraSwitchWithContext("PlayerSpeakingOrChoosing");

        StartCoroutine(ProcessPlayerTurn(playerInputText)); // Changed from await
    }

    private IEnumerator ProcessPlayerTurn(string playerInputText) // Changed from async Task
    {
        switch (currentPhase)
        {
            case GamePhase.PlayerIntroduction: // New case
                // This phase is handled by PlayerIntroductionSequence and the initial part of OnPlayerSubmit
                break;
            case GamePhase.LovesFirstImpression:
                yield return StartCoroutine(HandleLovesFirstImpression(playerInputText)); // Changed from await
                break;
            case GamePhase.LovesReassessment:
                yield return StartCoroutine(HandleLovesReassessment(playerInputText)); // Changed from await
                break;
            case GamePhase.LovesFinalChoice:
                // HandleLovesFinalChoice is not async, so no change here if it doesn't become a coroutine
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
        uiManager.SetPhaseIndicatorText("Phase: Love's First Impression");
        uiManager.AppendDialogueText("\n\nLet's meet our 5 lovely AI guests."); 

        UpdateGuestUI(); // Update guest UI for the new phase

        // Activate a random camera for guest introductions
        // TriggerSingleCameraSwitchWithContext("PhaseTransitionGeneral");

        // Re-enable player input field for the LovesFirstImpression phase
        uiManager.SetPlayerInputActive(true);
        uiManager.SetSubmitButtonText("Submit"); // Ensure button text is correct
        // The submitButton is already active.
        uiManager.FocusPlayerInputField();
    }

    private IEnumerator HandleLovesFirstImpression(string playerInputText) // Changed from async Task
    {
        uiManager.AppendDialogueText("\n\n--- AI Guest Responses (Love's First Impression) ---");
        uiManager.SetSubmitButtonText("Next Guest"); // Set for the loop

        for (int i = 0; i < aiGuests.Count; i++)
        {
            var guest = aiGuests[i];
            if (guest.currentLightStatus != LightStatus.Off) // Only active guests respond
            {
                // Activate a random camera focusing on the guest who is about to respond
                TriggerSingleCameraSwitchWithContext("GuestSpeaking", guest);
                yield return new WaitForSeconds(1.5f); // Short delay for camera transition

                string prompt = GeneratePromptForGuest(guest, playerInputText);
                
                Task<string> geminiTask_LFI = geminiService.GetGeminiResponse(prompt);
                yield return new WaitUntil(() => geminiTask_LFI.IsCompleted);
                if (geminiTask_LFI.IsFaulted) { Debug.LogError($"Gemini task error for {guest.guestName}: {geminiTask_LFI.Exception}"); continue; }
                string aiResponse = geminiTask_LFI.Result;
                Debug.Log($"Raw AI Response for {guest.guestName}: {aiResponse}");
                
                string guestDialogue = aiResponse; // Default to full response
                float affectionChange = 0f;

                // Parse AI response for dialogue and affection change
                int affectionChangeIndex = aiResponse.LastIndexOf("\nAffectionChange: ");
                if (affectionChangeIndex != -1)
                {
                    guestDialogue = aiResponse.Substring(0, affectionChangeIndex).Trim();
                    string affectionChangeStr = aiResponse.Substring(affectionChangeIndex + "\nAffectionChange: ".Length).Trim();
                    if (float.TryParse(affectionChangeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float parsedAdjustment))
                    {
                        affectionChange = parsedAdjustment;
                    }
                    else
                    {
                        Debug.LogWarning($"Could not parse AffectionChange value '{affectionChangeStr}' from AI response: {aiResponse}");
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
                uiManager.AppendDialogueText($"\n{guest.guestName}: {guestDialogue} (Light: {guest.currentLightStatus})"); // UI shows the new status

                // Wait for player to click "Next Guest" or "Submit" (if it's the last guest)
                isWaitingToAdvanceGuestDialogue = true;
                uiManager.SetSubmitButtonActive(true);
                // Keep "Next Guest" text unless it's the very last guest of all active ones in this phase
                bool isLastActiveGuestInPhase = true;
                for (int j = i + 1; j < aiGuests.Count; j++)
                {
                    if (aiGuests[j].currentLightStatus != LightStatus.Off)
                    {
                        isLastActiveGuestInPhase = false;
                        break;
                    }
                }
                if (isLastActiveGuestInPhase)
                {
                    // If it's the last guest to speak in this round, button might imply phase end
                    // For now, let's keep it "Next Guest" and handle phase transition text later
                }


                yield return new WaitUntil(() => !isWaitingToAdvanceGuestDialogue);
            }
        }
        uiManager.SetSubmitButtonText("Submit"); // Revert for next player input phase

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
            uiManager.AppendDialogueText("\n\n--- WARNING: Collective Light Off! --- \nMany guests have lost interest. The pressure is on!");
        }

        UpdateGuestUI();
        // After all guests respond, transition to next phase or allow more interaction in current phase
        // For MVP, let's transition after one round of responses
        currentPhase = GamePhase.LovesReassessment;
        uiManager.SetPhaseIndicatorText("Phase: Love's Reassessment");
        uiManager.AppendDialogueText("\n\n--- Phase Transition: Love's Reassessment --- \n\nPlayer, you can now interact further with the remaining guests.");
        uiManager.SetPlayerInputActive(true); // Ensure input field is active
        uiManager.SetSubmitButtonText("Submit");
        TriggerSingleCameraSwitchWithContext("PlayerSpeakingOrChoosing"); // Return to player focus after phase transition
    }

    private IEnumerator HandleLovesReassessment(string playerInputText) // Changed from async Task
    {
        uiManager.AppendDialogueText("\n\n--- AI Guest Responses (Love's Reassessment) ---");
        uiManager.SetSubmitButtonText("Next Guest"); // Set for the loop

        for (int i = 0; i < aiGuests.Count; i++)
        {
            var guest = aiGuests[i];
            if (guest.currentLightStatus != LightStatus.Off) // Only active guests respond
            {
                // Activate a random camera focusing on the guest who is about to respond
                TriggerSingleCameraSwitchWithContext("GuestSpeaking", guest);
                yield return new WaitForSeconds(1.5f); // Short delay for camera transition

                string prompt = GeneratePromptForGuest(guest, playerInputText);

                Task<string> geminiTask_LR = geminiService.GetGeminiResponse(prompt); // Unique task name
                yield return new WaitUntil(() => geminiTask_LR.IsCompleted);
                if (geminiTask_LR.IsFaulted) { Debug.LogError($"Gemini task error for {guest.guestName}: {geminiTask_LR.Exception}"); continue; }
                string aiResponse = geminiTask_LR.Result;
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
                uiManager.AppendDialogueText($"\n{guest.guestName}: {guestDialogue} (Light: {guest.currentLightStatus})"); // UI shows the new status

                // Wait for player to click "Next Guest" or "Submit" (if it's the last guest)
                isWaitingToAdvanceGuestDialogue = true;
                uiManager.SetSubmitButtonActive(true);
                // Keep "Next Guest" text unless it's the very last guest of all active ones in this phase
                bool isLastActiveGuestInPhase = true;
                for (int j = i + 1; j < aiGuests.Count; j++)
                {
                    if (aiGuests[j].currentLightStatus != LightStatus.Off)
                    {
                        isLastActiveGuestInPhase = false;
                        break;
                    }
                }
                if (isLastActiveGuestInPhase)
                {
                     // If it's the last guest to speak in this round, button might imply phase end
                }

                yield return new WaitUntil(() => !isWaitingToAdvanceGuestDialogue);
            }
        }
        uiManager.SetSubmitButtonText("Submit"); // Revert for next player input phase

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
            uiManager.AppendDialogueText("\n\n--- WARNING: Collective Light Off! --- \nMany guests have lost interest. The pressure is on!");
        }

        UpdateGuestUI();
        // After all guests respond, transition to final choice or allow more interaction
        currentPhase = GamePhase.LovesFinalChoice;
        uiManager.SetPhaseIndicatorText("Phase: Love's Final Choice");
        uiManager.AppendDialogueText("\n\n--- Phase Transition: Love's Final Choice --- \n\nPlayer, choose one of the remaining guests by typing their name, or type 'skip' for a smart recommendation.");
        uiManager.SetPlayerInputActive(true); // Ensure input field is active
        uiManager.SetSubmitButtonText("Submit");
        TriggerSingleCameraSwitchWithContext("PlayerSpeakingOrChoosing");
    }

    private void HandleLovesFinalChoice(string playerInputText)
    {
        // Activate a random camera for player focus as they make their choice
        TriggerSingleCameraSwitchWithContext("PlayerSpeakingOrChoosing");

        if (playerInputText.ToLower() == "skip")
        {
            // Smart Recommendation (simplified: pick a random 'On' or 'Burst' light)
            List<AIGuestProfile> remainingGuests = aiGuests.FindAll(g => g.currentLightStatus != LightStatus.Off);
            if (remainingGuests.Count > 0)
            {
                selectedGuestForFinalChoice = remainingGuests[Random.Range(0, remainingGuests.Count)];
                uiManager.AppendDialogueText($"\n\nSystem: You chose to skip. The system recommends {selectedGuestForFinalChoice.guestName}!");
                StartCoroutine(EndGameWithResult(selectedGuestForFinalChoice)); // Changed to StartCoroutine
            }
            else
            {
                uiManager.AppendDialogueText("\n\nSystem: No guests remaining with lights on. Game Over.");
                currentPhase = GamePhase.GameEnd;
            }
        }
        else
        {
            AIGuestProfile chosenGuest = aiGuests.Find(g => g.guestName.ToLower() == playerInputText.ToLower() && g.currentLightStatus != LightStatus.Off);
            if (chosenGuest != null)
            {
                selectedGuestForFinalChoice = chosenGuest;
                uiManager.AppendDialogueText($"\n\nPlayer: I choose {chosenGuest.guestName}.");
                StartCoroutine(EndGameWithResult(chosenGuest)); // Changed to StartCoroutine
            }
            else
            {
                uiManager.AppendDialogueText("\n\nSystem: Invalid choice or guest's light is off. Please choose an active guest by typing their name.");
            }
        }
    }

    private IEnumerator EndGameWithResult(AIGuestProfile finalGuest) // Changed from async void
    {
        string prompt = $"You are {finalGuest.guestName}, a {finalGuest.age}-year-old {finalGuest.occupation}. Your personality is {finalGuest.personalityTraits}. You are on a dating show. The male contestant has chosen you for the final decision. Based on your previous interactions and your current light status ({finalGuest.currentLightStatus}), do you accept or reject him? Respond with your decision and a brief message. State 'Decision: ACCEPT' or 'Decision: REJECT'.";
        
        Task<string> geminiTask_End = geminiService.GetGeminiResponse(prompt);
        yield return new WaitUntil(() => geminiTask_End.IsCompleted);
        if (geminiTask_End.IsFaulted) { Debug.LogError($"Gemini task error for endgame with {finalGuest.guestName}: {geminiTask_End.Exception}"); yield break; }
        string aiResponse = geminiTask_End.Result;

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

        uiManager.AppendDialogueText($"\n\n{finalGuest.guestName}: {finalMessage}");

        if (decision == "ACCEPT")
        {
            uiManager.AppendDialogueText($"\n\n--- CONGRATULATIONS! You and {finalGuest.guestName} are a match! ---");
        }
        else
        {
            uiManager.AppendDialogueText($"\n\n--- Too bad! {finalGuest.guestName} has rejected you. ---");
        }

        currentPhase = GamePhase.GameEnd;
        uiManager.SetPhaseIndicatorText("Game Over!");
        uiManager.SetPlayerInputActive(false);
        uiManager.SetSubmitButtonActive(false);

        // Activate a random camera for the end-game
        TriggerSingleCameraSwitchWithContext("GameEnd");
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

    /// <summary>
    /// Returns a list of suitable camera types based on the current game context.
    /// </summary>
    /// <param name="contextKey">A string indicating the context (e.g., "GuestSpeaking", "PlayerSpeakingOrChoosing", "PhaseTransitionGeneral", "GameEnd").</param>
    /// <param name="currentGuest">The guest currently interacting, if applicable.</param>
    /// <returns>A list of VCamType suitable for the context.</returns>
    private List<VCamType> GetRandomCameraPool(string contextKey, AIGuestProfile currentGuest = null)
    {
        List<VCamType> pool = new List<VCamType>();

        switch (contextKey)
        {
            case "GuestSpeaking":
                pool.Add(VCamType.GuestFocus);
                pool.Add(VCamType.OverShoulderPlayer); // Player's OTS looking at guest
                pool.Add(VCamType.OverShoulderGuest); // Guest's OTS looking at guest
                // Add GroupShotActiveGuests if multiple guests are active and relevant
                break;
            case "PlayerSpeakingOrChoosing":
                pool.Add(VCamType.PlayerFocus);
                pool.Add(VCamType.OverShoulderGuest); // Guest's OTS looking at player
                pool.Add(VCamType.OverShoulderPlayer); // Player's OTS looking at guest
                pool.Add(VCamType.Panoramic);
                break;
            case "PhaseTransitionGeneral":
                pool.Add(VCamType.Panoramic);
                pool.Add(VCamType.PlayerFocus); // Default to player focus during transitions if no other specific camera is needed
                break;
            case "GameEnd":
                pool.Add(VCamType.Panoramic);
                // Could add a specific "GameEnd" camera if one exists
                break;
            default:
                pool.Add(VCamType.Panoramic); // Fallback
                break;
        }

        // Filter out cameras if their required targets are null
        if (currentPlayerProfile == null || currentPlayerProfile.modelTransform == null)
        {
            pool.Remove(VCamType.PlayerFocus);
            pool.Remove(VCamType.OverShoulderGuest);
        }
        if (currentGuest == null || currentGuest.modelTransform == null)
        {
            pool.Remove(VCamType.GuestFocus);
            pool.Remove(VCamType.OverShoulderPlayer);
        }

        // Ensure there's always at least one camera
        if (pool.Count == 0)
        {
            pool.Add(VCamType.Panoramic);
        }

        return pool;
    }

    /// <summary>
    /// Starts the continuous random camera cycling coroutine.
    /// </summary>
    public void StartContinuousCameraCycling()
    {
        if (_randomCameraCycleCoroutineRef != null)
        {
            StopCoroutine(_randomCameraCycleCoroutineRef);
        }
        _randomCameraCycleCoroutineRef = StartCoroutine(ActivateRandomCameraWithContextCoroutine());
        Debug.Log("Started continuous random camera cycling.");
    }

    /// <summary>
    /// Stops the continuous random camera cycling coroutine.
    /// </summary>
    public void StopContinuousCameraCycling()
    {
        if (_randomCameraCycleCoroutineRef != null)
        {
            StopCoroutine(_randomCameraCycleCoroutineRef);
            _randomCameraCycleCoroutineRef = null;
            Debug.Log("Stopped continuous random camera cycling.");
        }
    }

    /// <summary>
    /// Coroutine that randomly activates a camera every 'cameraSwitchInterval' seconds,
    /// considering contextual targets.
    /// </summary>
    public IEnumerator ActivateRandomCameraWithContextCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(cameraSwitchInterval);

            string contextKeyForPhase = "Panoramic"; // Default context
            AIGuestProfile targetGuestForPhase = null;

            switch (currentPhase)
            {
                case GamePhase.PlayerIntroduction:
                    // During player intro, alternate between player focus and panoramic
                    if (currentPlayerProfile != null && currentPlayerProfile.modelTransform != null && Random.value > 0.5f)
                    {
                        contextKeyForPhase = "PlayerSpeakingOrChoosing";
                    }
                    else
                    {
                        contextKeyForPhase = "Panoramic";
                    }
                    break;

                case GamePhase.LovesFirstImpression:
                case GamePhase.LovesReassessment:
                    // During interaction phases, prioritize active guests or player, then panoramic
                    List<AIGuestProfile> activeGuests = aiGuests.FindAll(g => g.currentLightStatus != LightStatus.Off && g.modelTransform != null);
                    if (activeGuests.Count > 0 && Random.value > 0.3f) // 70% chance to focus on an active guest
                    {
                        targetGuestForPhase = activeGuests[Random.Range(0, activeGuests.Count)];
                        contextKeyForPhase = "GuestSpeaking";
                    }
                    else if (currentPlayerProfile != null && currentPlayerProfile.modelTransform != null && Random.value > 0.5f) // 50% chance of remaining to focus on player
                    {
                        contextKeyForPhase = "PlayerSpeakingOrChoosing";
                    }
                    else // Fallback to panoramic
                    {
                        contextKeyForPhase = "Panoramic";
                    }
                    break;

                case GamePhase.LovesFinalChoice:
                    // Focus heavily on the player making the choice, or a wide shot
                    if (currentPlayerProfile != null && currentPlayerProfile.modelTransform != null && Random.value > 0.3f)
                    {
                        contextKeyForPhase = "PlayerSpeakingOrChoosing";
                    }
                    else
                    {
                        contextKeyForPhase = "Panoramic";
                    }
                    break;

                case GamePhase.GameEnd:
                    // Mostly panoramic or a general end-game shot
                    contextKeyForPhase = "GameEnd";
                    break;

                default:
                    contextKeyForPhase = "Panoramic"; // Fallback for any unhandled phase
                    break;
            }
            
            TriggerSingleCameraSwitchWithContext(contextKeyForPhase, targetGuestForPhase);
        }
    }

    /// <summary>
    /// Activates a specific camera from a pool suitable for the given context (single switch).
    /// </summary>
    /// <param name="contextKey">A string indicating the context (e.g., "GuestSpeaking", "PlayerSpeakingOrChoosing").</param>
    /// <param name="currentGuest">The guest currently interacting, if applicable.</param>
    private void TriggerSingleCameraSwitchWithContext(string contextKey, AIGuestProfile currentGuest = null)
    {
        if (dynamicCameraController == null)
        {
            Debug.LogWarning("DynamicCameraController is not assigned. Cannot activate camera.");
            return;
        }

        List<VCamType> cameraPool = GetRandomCameraPool(contextKey, currentGuest);
        VCamType selectedCameraType = cameraPool[Random.Range(0, cameraPool.Count)];

        Transform primaryTarget = null; // For Follow
        Transform secondaryTarget = null; // For LookAt (especially for OTS)

        switch (selectedCameraType)
        {
            case VCamType.PlayerFocus:
                primaryTarget = currentPlayerProfile?.modelTransform;
                break;
            case VCamType.GuestFocus:
                primaryTarget = currentGuest?.modelTransform;
                break;
            case VCamType.OverShoulderPlayer:
                primaryTarget = currentPlayerProfile?.modelTransform;
                secondaryTarget = currentGuest?.modelTransform; // Player looking at guest
                if (primaryTarget != null && secondaryTarget != null)
                {
                    dynamicCameraController.SetOverShoulderPlayerTargets(primaryTarget, secondaryTarget);
                }
                break;
            case VCamType.OverShoulderGuest:
                primaryTarget = currentGuest?.modelTransform;
                if (contextKey == "GuestSpeaking")
                {
                    secondaryTarget = currentGuest?.modelTransform; // Guest looking at themselves (the speaking guest)
                }
                else
                {
                    secondaryTarget = currentPlayerProfile?.modelTransform; // Guest looking at player
                }
                
                if (primaryTarget != null && secondaryTarget != null)
                {
                    dynamicCameraController.SetOverShoulderGuestTargets(primaryTarget, secondaryTarget);
                }
                break;
            case VCamType.Panoramic:
            case VCamType.StaticOpening: // Should only be used at start, but included for completeness
            case VCamType.GroupShotActiveGuests:
                // These cameras typically don't need a specific Follow/LookAt target set via ActivateCamera
                break;
        }

        // If an OTS camera was selected, its targets are already set.
        // For other cameras, pass the primary target to ActivateCamera.
        dynamicCameraController.ActivateCamera(selectedCameraType, primaryTarget);
    }
}
