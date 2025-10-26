using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class MealAnalysisResponse
{
    public bool meal_identified;
    public List<string> food_items;
    public CalorieInfo calories;
    public Macros macros;
    public FoodGroups food_groups;
    public HealthAssessment health_assessment;
    public List<string> tips;
    public string encouragement;
    public string portion_note;
}

[System.Serializable]
public class CalorieInfo
{
    public int total;
    public string confidence;
}

[System.Serializable]
public class Macros
{
    public float protein_g;
    public float carbohydrates_g;
    public float fats_g;
    public float fiber_g;
}

[System.Serializable]
public class FoodGroups
{
    public float protein;
    public float grains_carbs;
    public float fruits_vegetables;
    public float dairy;
    public float fats_oils;
}

[System.Serializable]
public class HealthAssessment
{
    public bool is_balanced;
    public string overall_rating;
    public List<string> strengths;
    public List<string> areas_to_improve;
}

// Claude API Request/Response structures
[System.Serializable]
public class ClaudeRequest
{
    public string model;
    public int max_tokens;
    public List<ClaudeMessage> messages;
}

[System.Serializable]
public class ClaudeMessage
{
    public string role;
    public List<ClaudeContent> content;
}

[System.Serializable]
public class ClaudeContent
{
    public string type;
    public ClaudeImageSource source;
    public string text;
}

[System.Serializable]
public class ClaudeImageSource
{
    public string type;
    public string media_type;
    public string data;
}

[System.Serializable]
public class ClaudeResponse
{
    public string id;
    public string type;
    public string role;
    public List<ClaudeContentBlock> content;
}

[System.Serializable]
public class ClaudeContentBlock
{
    public string type;
    public string text;
}

public class ClaudeAPIService : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string apiKey = "YOUR_API_KEY_HERE";
    private const string API_URL = "https://api.anthropic.com/v1/messages";
    private const string MODEL = "claude-sonnet-4-5-20250929";
    private const string ANTHROPIC_VERSION = "2023-06-01";

    public delegate void OnAnalysisComplete(MealAnalysisResponse response);
    public delegate void OnAnalysisError(string error);

    /// <summary>
    /// Analyzes a meal from a Texture2D image
    /// </summary>
    public void AnalyzeMeal(Texture2D mealImage, OnAnalysisComplete onSuccess, OnAnalysisError onError)
    {
        StartCoroutine(AnalyzeMealCoroutine(mealImage, onSuccess, onError));
    }

    private IEnumerator AnalyzeMealCoroutine(Texture2D mealImage, OnAnalysisComplete onSuccess, OnAnalysisError onError)
    {
        // Convert image to base64
        byte[] imageBytes = mealImage.EncodeToJPG(85); // 85% quality
        string base64Image = Convert.ToBase64String(imageBytes);

        // Create the prompt
        string promptText = @"You are a nutritional expert. Analyze this food image using objects, hands, or utensils as size reference and provide detailed nutritional information on calories, macros, how healthy/ balanced the meal is.
        Additionally, factor in quality of food: whether it is fast food, processed, or a healthy cooked meal in the balance score.
        Any tips, encouragement, or notes for the user should be written in a bubbly and positive format.
        Return your response in the following JSON format ONLY, with no additional text before or after:

{
  ""meal_identified"": boolean,
  ""food_items"": [""item1"", ""item2""],
  ""calories"": {
    ""total"": number,
    ""confidence"": ""high|medium|low""
  },
  ""macros"": {
    ""protein_g"": number,
    ""carbohydrates_g"": number,
    ""fats_g"": number,
    ""fiber_g"": number
  },
  ""food_groups"": {
    ""protein"": number,
    ""grains_carbs"": number,
    ""fruits_vegetables"": number,
    ""dairy"": number,
    ""fats_oils"": number
  },
  ""health_assessment"": {
    ""is_balanced"": boolean,
    ""overall_rating"": ""excellent|good|fair|needs_improvement"",
    ""strengths"": [""strength1"", ""strength2""],
    ""areas_to_improve"": [""area1"", ""area2""]
  },
  ""tips"": [
    ""tip1"",
    ""tip2"",
    ""tip3""
  ],
  ""encouragement"": ""A positive, supportive message about the meal choice"",
  ""portion_note"": ""Any notes about portion sizes and estimation accuracy""
}

Guidelines:
- Estimate calories and macros based on visible portion sizes
- food_groups values are in grams
- Be encouraging and supportive in tone and messages should be from the perspective of if the user is feeding you these meals
- If the image doesn't contain food, set meal_identified to false and populate only that field
- Provide 2-3 actionable tips for improving the meal's nutritional balance
- Keep encouragement positive and motivating, even for less healthy meals";

        // Build the request
        ClaudeRequest request = new ClaudeRequest
        {
            model = MODEL,
            max_tokens = 2048,
            messages = new List<ClaudeMessage>
            {
                new ClaudeMessage
                {
                    role = "user",
                    content = new List<ClaudeContent>
                    {
                        new ClaudeContent
                        {
                            type = "image",
                            source = new ClaudeImageSource
                            {
                                type = "base64",
                                media_type = "image/jpeg",
                                data = base64Image
                            }
                        },
                        new ClaudeContent
                        {
                            type = "text",
                            text = promptText
                        }
                    }
                }
            }
        };

        string jsonRequest = JsonUtility.ToJson(request);

        // Create web request
        using (UnityWebRequest webRequest = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            // Set headers
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("x-api-key", apiKey);
            webRequest.SetRequestHeader("anthropic-version", ANTHROPIC_VERSION);

            // Send request
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // Parse Claude response
                    string responseText = webRequest.downloadHandler.text;
                    ClaudeResponse claudeResponse = JsonUtility.FromJson<ClaudeResponse>(responseText);

                    if (claudeResponse.content != null && claudeResponse.content.Count > 0)
                    {
                        string mealDataJson = claudeResponse.content[0].text;
                        
                        // Parse meal analysis
                        MealAnalysisResponse mealAnalysis = JsonUtility.FromJson<MealAnalysisResponse>(mealDataJson);
                        onSuccess?.Invoke(mealAnalysis);
                    }
                    else
                    {
                        onError?.Invoke("No content in Claude response");
                    }
                }
                catch (Exception e)
                {
                    onError?.Invoke($"Error parsing response: {e.Message}");
                }
            }
            else
            {
                onError?.Invoke($"API Error: {webRequest.error}\n{webRequest.downloadHandler.text}");
            }
        }
    }
}