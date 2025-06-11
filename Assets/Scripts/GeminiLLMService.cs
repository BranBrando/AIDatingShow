using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System; // Required for [Serializable]

public class GeminiLLMService : MonoBehaviour
{
    private string apiKey = "YOUR_GEMINI_API_KEY_HERE"; // Placeholder, will be set by GameManager
    private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=";

    public void SetApiKey(string key)
    {
        apiKey = key;
    }

    public async Task<string> GetGeminiResponse(string prompt)
    {
        string url = GEMINI_API_URL + apiKey;

        // Use a serializable class for the request body
        var requestBody = new GeminiRequest
        {
            contents = new GeminiContentRequest[]
            {
                new GeminiContentRequest
                {
                    parts = new GeminiPartRequest[]
                    {
                        new GeminiPartRequest { text = prompt }
                    }
                }
            }
        };

        string jsonRequestBody = JsonUtility.ToJson(requestBody);

        using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            var asyncOperation = webRequest.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                await Task.Yield(); // Wait for the next frame
            }

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + webRequest.error);
                Debug.LogError("Response: " + webRequest.downloadHandler.text);
                return "Error: " + webRequest.error;
            }
            else
            {
                string responseJson = webRequest.downloadHandler.text;
                Debug.Log("Gemini Raw Response: " + responseJson);

                try
                {
                    GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(responseJson);

                    if (geminiResponse != null && geminiResponse.candidates != null && geminiResponse.candidates.Length > 0 &&
                        geminiResponse.candidates[0].content != null && geminiResponse.candidates[0].content.parts != null &&
                        geminiResponse.candidates[0].content.parts.Length > 0 && geminiResponse.candidates[0].content.parts[0].text != null)
                    {
                        return geminiResponse.candidates[0].content.parts[0].text;
                    }
                    else
                    {
                        Debug.LogError("Gemini response structure unexpected or text not found.");
                        return "Error: Could not parse Gemini response text.";
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to parse Gemini response: " + e.Message);
                    return "Error: Failed to parse Gemini response.";
                }
            }
        }
    }
}

// Helper classes for JSON serialization/deserialization
[Serializable]
public class GeminiRequest
{
    public GeminiContentRequest[] contents;
}

[Serializable]
public class GeminiContentRequest
{
    public GeminiPartRequest[] parts;
}

[Serializable]
public class GeminiPartRequest
{
    public string text;
}

[Serializable]
public class GeminiResponse
{
    public GeminiCandidate[] candidates;
    // Add other fields if needed, like promptFeedback
}

[Serializable]
public class GeminiCandidate
{
    public GeminiContent content;
    // Add other fields if needed, like finishReason, safetyRatings
}

[Serializable]
public class GeminiContent
{
    public GeminiPart[] parts;
    public string role; // "model" or "user"
}

[Serializable]
public class GeminiPart
{
    public string text;
}
