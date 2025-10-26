using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller for meal capture and analysis flow
/// Connects camera capture -> Claude API -> Scene Manager -> UI update
/// </summary>
public class MealCaptureController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ClaudeAPIService claudeAPI;
    [SerializeField] private Camera captureCamera;
    
    [Header("UI Feedback")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI encouragementText;
    
    [Header("Capture Settings")]
    [SerializeField] private int captureWidth = 1024;
    [SerializeField] private int captureHeight = 1024;
    
    private bool isAnalyzing = false;
    
    private void Start()
    {
        // Hide panels initially
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        
        // Get ClaudeAPI if not assigned
        if (claudeAPI == null)
        {
            claudeAPI = FindObjectOfType<ClaudeAPIService>();
        }
        
        // Get main camera if not assigned
        if (captureCamera == null)
        {
            captureCamera = Camera.main;
        }
    }
    
    /// <summary>
    /// Capture current camera view and analyze meal
    /// Call this from a button or controller input
    /// </summary>
    public void CaptureMeal()
    {
        if (isAnalyzing)
        {
            Debug.LogWarning("Already analyzing a meal, please wait...");
            return;
        }
        
        if (captureCamera == null)
        {
            Debug.LogError("No capture camera assigned!");
            return;
        }
        
        if (claudeAPI == null)
        {
            Debug.LogError("ClaudeAPIService not found!");
            return;
        }
        
        // Capture screenshot
        Texture2D screenshot = CaptureScreenshot();
        
        if (screenshot != null)
        {
            AnalyzeMeal(screenshot);
        }
    }
    
    /// <summary>
    /// Capture screenshot from camera
    /// </summary>
    private Texture2D CaptureScreenshot()
    {
        // Create render texture
        RenderTexture rt = new RenderTexture(captureWidth, captureHeight, 24);
        RenderTexture previousRT = captureCamera.targetTexture;
        
        captureCamera.targetTexture = rt;
        captureCamera.Render();
        
        // Read pixels
        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        screenshot.Apply();
        
        // Cleanup
        captureCamera.targetTexture = previousRT;
        RenderTexture.active = null;
        Destroy(rt);
        
        Debug.Log("Screenshot captured");
        return screenshot;
    }
    
    /// <summary>
    /// Analyze meal using Claude API
    /// </summary>
    private void AnalyzeMeal(Texture2D mealImage)
    {
        isAnalyzing = true;
        
        // Show loading UI
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (loadingText != null)
            {
                loadingText.text = "Analyzing your meal...";
            }
        }
        
        // Call Claude API
        claudeAPI.AnalyzeMeal(
            mealImage,
            OnAnalysisSuccess,
            OnAnalysisError
        );
    }
    
    /// <summary>
    /// Called when Claude API successfully analyzes the meal
    /// </summary>
    private void OnAnalysisSuccess(MealAnalysisResponse response)
    {
        isAnalyzing = false;
        
        // Hide loading UI
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        if (!response.meal_identified)
        {
            ShowError("No meal detected in the image. Please try again with a clearer view of your food.");
            return;
        }
        
        // Log meal to scene manager
        if (YolkedSceneManager.Instance != null)
        {
            YolkedSceneManager.Instance.LogMeal(response);
        }
        else
        {
            Debug.LogError("YolkedSceneManager not found!");
        }
        
        // Show result UI
        ShowResult(response);
        
        Debug.Log($"Meal analyzed: {response.food_items.Count} items, {response.calories.total} calories");
    }
    
    /// <summary>
    /// Called when Claude API encounters an error
    /// </summary>
    private void OnAnalysisError(string error)
    {
        isAnalyzing = false;
        
        // Hide loading UI
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
        
        ShowError($"Analysis failed: {error}");
        Debug.LogError($"Meal analysis error: {error}");
    }
    
    /// <summary>
    /// Display meal analysis results
    /// </summary>
    private void ShowResult(MealAnalysisResponse response)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            
            if (resultText != null)
            {
                // Format result text
                string foodList = string.Join(", ", response.food_items);
                string result = $"<b>Great!</b> This is a {(response.health_assessment.is_balanced ? "balanced" : "")} meal.\n\n";
                result += $"<b>Food Items:</b> {foodList}\n\n";
                result += $"<b>Calories:</b> {response.calories.total}\n";
                result += $"<b>Protein:</b> {response.macros.protein_g:F1}g\n";
                result += $"<b>Carbs:</b> {response.macros.carbohydrates_g:F1}g\n";
                result += $"<b>Fats:</b> {response.macros.fats_g:F1}g\n\n";
                result += $"<b>Rating:</b> {response.health_assessment.overall_rating}";
                
                resultText.text = result;
            }
            
            if (encouragementText != null && !string.IsNullOrEmpty(response.encouragement))
            {
                encouragementText.text = response.encouragement;
            }
            
            // Auto-hide after 5 seconds
            Invoke(nameof(HideResultPanel), 5f);
        }
    }
    
    /// <summary>
    /// Display error message
    /// </summary>
    private void ShowError(string error)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            
            if (resultText != null)
            {
                resultText.text = $"<color=red><b>Error</b></color>\n\n{error}";
            }
            
            if (encouragementText != null)
            {
                encouragementText.text = "Don't worry, try again!";
            }
            
            // Auto-hide after 3 seconds
            Invoke(nameof(HideResultPanel), 3f);
        }
    }
    
    /// <summary>
    /// Hide result panel
    /// </summary>
    private void HideResultPanel()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Test method - analyze a pre-loaded image
    /// </summary>
    public void TestAnalyzeImage(Texture2D testImage)
    {
        if (testImage != null)
        {
            AnalyzeMeal(testImage);
        }
    }
}
