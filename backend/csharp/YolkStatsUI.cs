using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI Controller for the Yolk stats panel
/// Displays nutrition progress, Yolk Points, and pet status
/// </summary>
public class YolkStatsUI : MonoBehaviour
{
    [Header("Yolk Pet Display")]
    [SerializeField] private Image yolkPetImage;
    [SerializeField] private TextMeshProUGUI yolkStageText;
    [SerializeField] private TextMeshProUGUI yolkLevelText;
    
    [Header("Yolk Points")]
    [SerializeField] private TextMeshProUGUI yolkPointsText;
    [SerializeField] private Image yolkPointsProgressBar;
    [SerializeField] private TextMeshProUGUI pointsGoalText;
    
    [Header("Macro Progress Circles")]
    [SerializeField] private Image proteinProgressCircle;
    [SerializeField] private TextMeshProUGUI proteinPercentageText;
    [SerializeField] private TextMeshProUGUI proteinLabelText;
    
    [SerializeField] private Image carbsProgressCircle;
    [SerializeField] private TextMeshProUGUI carbsPercentageText;
    [SerializeField] private TextMeshProUGUI carbsLabelText;
    
    [SerializeField] private Image veggiesProgressCircle;
    [SerializeField] private TextMeshProUGUI veggiesPercentageText;
    [SerializeField] private TextMeshProUGUI veggiesLabelText;
    
    [Header("Pro Tip")]
    [SerializeField] private TextMeshProUGUI proTipText;
    
    [Header("Animation")]
    [SerializeField] private float updateAnimationDuration = 0.5f;
    [SerializeField] private AnimationCurve updateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Yolk Pet Sprites (Optional)")]
    [SerializeField] private Sprite blobSprite;
    [SerializeField] private Sprite eggSprite;
    [SerializeField] private Sprite chickSprite;
    [SerializeField] private Sprite chickenSprite;
    
    private NutritionData currentData;
    
    private void Start()
    {
        // Subscribe to scene manager events
        if (YolkedSceneManager.Instance != null)
        {
            YolkedSceneManager.Instance.OnNutritionUpdated.AddListener(UpdateUI);
            YolkedSceneManager.Instance.OnLevelUp.AddListener(OnLevelUp);
            
            // Initial update
            UpdateUI(YolkedSceneManager.Instance.GetNutritionData());
        }
        else
        {
            Debug.LogError("YolkedSceneManager not found! Make sure it exists in the scene.");
        }
        
        // Set default labels
        if (proteinLabelText != null) proteinLabelText.text = "Strength";
        if (carbsLabelText != null) carbsLabelText.text = "Energy";
        if (veggiesLabelText != null) veggiesLabelText.text = "Nutrient Absorption";
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (YolkedSceneManager.Instance != null)
        {
            YolkedSceneManager.Instance.OnNutritionUpdated.RemoveListener(UpdateUI);
            YolkedSceneManager.Instance.OnLevelUp.RemoveListener(OnLevelUp);
        }
    }
    
    /// <summary>
    /// Update all UI elements with current nutrition data
    /// </summary>
    public void UpdateUI(NutritionData data)
    {
        currentData = data;
        
        // Update Yolk pet display
        UpdateYolkDisplay(data);
        
        // Update Yolk Points
        UpdateYolkPoints(data);
        
        // Update macro progress circles
        UpdateMacroCircles(data);
        
        // Update pro tip
        UpdateProTip(data);
    }
    
    /// <summary>
    /// Update Yolk pet image and stage text
    /// </summary>
    private void UpdateYolkDisplay(NutritionData data)
    {
        if (yolkStageText != null)
        {
            yolkStageText.text = $"Current Stage: {data.yolkStage}";
        }
        
        if (yolkLevelText != null)
        {
            yolkLevelText.text = $"Level {data.yolkLevel}";
        }
        
        // Update pet sprite based on stage
        if (yolkPetImage != null)
        {
            switch (data.yolkStage)
            {
                case "Blob":
                    if (blobSprite != null) yolkPetImage.sprite = blobSprite;
                    break;
                case "Egg":
                    if (eggSprite != null) yolkPetImage.sprite = eggSprite;
                    break;
                case "Chick":
                    if (chickSprite != null) yolkPetImage.sprite = chickSprite;
                    break;
                case "Chicken":
                    if (chickenSprite != null) yolkPetImage.sprite = chickenSprite;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Update Yolk Points display and progress bar
    /// </summary>
    private void UpdateYolkPoints(NutritionData data)
    {
        if (yolkPointsText != null)
        {
            yolkPointsText.text = $"{data.yolkPoints} out of {data.calorieGoal:F0} Yolk Points needed";
        }
        
        if (pointsGoalText != null)
        {
            int pointsToNextLevel = data.GetPointsForNextLevel();
            pointsGoalText.text = $"Next level: {pointsToNextLevel} total points";
        }
        
        // Update progress bar (daily calorie goal)
        if (yolkPointsProgressBar != null)
        {
            float progress = data.GetCaloriePercentage();
            StartCoroutine(AnimateProgressBar(yolkPointsProgressBar, progress));
        }
    }
    
    /// <summary>
    /// Update the three macro progress circles
    /// </summary>
    private void UpdateMacroCircles(NutritionData data)
    {
        // Protein (Strength)
        if (proteinProgressCircle != null)
        {
            float proteinProgress = data.GetProteinPercentage();
            StartCoroutine(AnimateCircularProgress(proteinProgressCircle, proteinProgress));
        }
        if (proteinPercentageText != null)
        {
            proteinPercentageText.text = $"{(data.GetProteinPercentage() * 100):F1}%";
        }
        
        // Carbs (Energy)
        if (carbsProgressCircle != null)
        {
            float carbsProgress = data.GetCarbsPercentage();
            StartCoroutine(AnimateCircularProgress(carbsProgressCircle, carbsProgress));
        }
        if (carbsPercentageText != null)
        {
            carbsPercentageText.text = $"{(data.GetCarbsPercentage() * 100):F1}%";
        }
        
        // Veggies (Nutrient Absorption)
        if (veggiesProgressCircle != null)
        {
            float veggiesProgress = data.GetVeggiesPercentage();
            StartCoroutine(AnimateCircularProgress(veggiesProgressCircle, veggiesProgress));
        }
        if (veggiesPercentageText != null)
        {
            veggiesPercentageText.text = $"{(data.GetVeggiesPercentage() * 100):F1}%";
        }
    }
    
    /// <summary>
    /// Update pro tip text
    /// </summary>
    private void UpdateProTip(NutritionData data)
    {
        if (proTipText != null)
        {
            proTipText.text = data.GetProTip();
        }
    }
    
    /// <summary>
    /// Animate a progress bar fill amount
    /// </summary>
    private IEnumerator AnimateProgressBar(Image progressBar, float targetFill)
    {
        float startFill = progressBar.fillAmount;
        float elapsed = 0f;
        
        while (elapsed < updateAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / updateAnimationDuration;
            float curveValue = updateCurve.Evaluate(t);
            
            progressBar.fillAmount = Mathf.Lerp(startFill, targetFill, curveValue);
            
            yield return null;
        }
        
        progressBar.fillAmount = targetFill;
    }
    
    /// <summary>
    /// Animate a circular progress indicator
    /// For circular progress, use Image with Type: Filled, Fill Method: Radial 360
    /// </summary>
    private IEnumerator AnimateCircularProgress(Image circle, float targetFill)
    {
        float startFill = circle.fillAmount;
        float elapsed = 0f;
        
        while (elapsed < updateAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / updateAnimationDuration;
            float curveValue = updateCurve.Evaluate(t);
            
            circle.fillAmount = Mathf.Lerp(startFill, targetFill, curveValue);
            
            yield return null;
        }
        
        circle.fillAmount = targetFill;
    }
    
    /// <summary>
    /// Called when user levels up - play celebration animation
    /// </summary>
    private void OnLevelUp(int newLevel, string newStage)
    {
        Debug.Log($"🎉 Level Up! Now level {newLevel} - {newStage}");
        
        // TODO: Add celebration effects here
        // - Particle effects
        // - Sound effects
        // - Screen flash
        // - Yolk pet animation
        
        StartCoroutine(LevelUpAnimation());
    }
    
    /// <summary>
    /// Play level up celebration animation
    /// </summary>
    private IEnumerator LevelUpAnimation()
    {
        // Simple scale pulse animation for Yolk pet
        if (yolkPetImage != null)
        {
            Vector3 originalScale = yolkPetImage.transform.localScale;
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = 1f + Mathf.Sin((elapsed / duration) * Mathf.PI) * 0.2f;
                yolkPetImage.transform.localScale = originalScale * scale;
                yield return null;
            }
            
            yolkPetImage.transform.localScale = originalScale;
        }
    }
    
    /// <summary>
    /// Manual refresh button (for testing)
    /// </summary>
    public void OnRefreshButtonClicked()
    {
        if (YolkedSceneManager.Instance != null)
        {
            YolkedSceneManager.Instance.RefreshUI();
        }
    }
}
