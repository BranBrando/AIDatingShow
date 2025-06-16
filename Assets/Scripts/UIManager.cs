using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dialogueText;
    public TMP_InputField playerInputField;
    public GameObject submitButton;
    public TextMeshProUGUI submitButtonText;
    public TextMeshProUGUI phaseIndicatorText;
    public List<TextMeshProUGUI> guestNameTexts;
    public List<GameObject> guestLightIndicators;

    // Event to notify GameManager of player input submission
    public delegate void PlayerSubmitAction(string input);
    public event PlayerSubmitAction OnPlayerSubmitClicked;

    void Awake()
    {
        // Attach listeners for player input and submit button
        if (playerInputField != null)
        {
            playerInputField.onEndEdit.AddListener(OnPlayerInputEndEdit);
        }
        else
        {
            Debug.LogError("PlayerInputField reference is null in UIManager.");
        }

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
            Debug.LogError("SubmitButton reference is null in UIManager.");
        }
    }

    private void OnPlayerInputEndEdit(string input)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnPlayerSubmit(input);
        }
    }

    public void OnPlayerSubmit(string input)
    {
        OnPlayerSubmitClicked?.Invoke(input);
    }

    public void SetDialogueText(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
    }

    public void AppendDialogueText(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text += text;
        }
    }

    public void SetPhaseIndicatorText(string text)
    {
        if (phaseIndicatorText != null)
        {
            phaseIndicatorText.text = text;
        }
    }

    public void SetSubmitButtonText(string text)
    {
        if (submitButtonText != null)
        {
            submitButtonText.text = text;
        }
    }

    public void SetPlayerInputActive(bool isActive)
    {
        if (playerInputField != null)
        {
            playerInputField.gameObject.SetActive(isActive);
        }
    }

    public void SetSubmitButtonActive(bool isActive)
    {
        if (submitButton != null)
        {
            submitButton.gameObject.SetActive(isActive);
        }
    }

    public void ClearPlayerInputField()
    {
        if (playerInputField != null)
        {
            playerInputField.text = "";
        }
    }

    public void FocusPlayerInputField()
    {
        if (playerInputField != null)
        {
            playerInputField.Select();
            playerInputField.ActivateInputField();
        }
    }

    public void UpdateGuestUI(List<AIGuestProfile> guests)
    {
        for (int i = 0; i < guests.Count; i++)
        {
            if (i < guestNameTexts.Count)
            {
                guestNameTexts[i].text = guests[i].guestName;
            }
            if (i < guestLightIndicators.Count)
            {
                Color lightColor = Color.gray; // Off
                if (guests[i].currentLightStatus == LightStatus.On)
                {
                    lightColor = Color.green; // On
                }
                else if (guests[i].currentLightStatus == LightStatus.Burst)
                {
                    lightColor = Color.yellow; // Burst
                }
                Image lightImage = guestLightIndicators[i].GetComponent<Image>();
                if (lightImage != null)
                {
                    lightImage.color = lightColor;
                }
                else
                {
                    Debug.LogWarning($"Guest light indicator for {guests[i].guestName} does not have an Image component.");
                }
                Debug.Log($"{guests[i].guestName}'s light is {guests[i].currentLightStatus}");
            }
        }
    }
}
