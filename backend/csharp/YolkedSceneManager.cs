using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Central scene manager for Yolked app
/// Manages nutrition data, persistence, and coordinates between systems
/// </summary>
public class YolkedSceneManager : MonoBehaviour
{
    // Singleton instance
    public static YolkedSceneManager Instance { get; private set; }
    
    [Header("Nutrition Data")]
    [SerializeField] private NutritionData nutritionData;
    
    [Header("Events")]
    public UnityEvent<NutritionData> OnNutritionUpdated;
    public UnityEvent<int> OnYolkPointsEarned;
    public UnityEvent<int, string> OnLevelUp; // level, stage
    public UnityEvent<MealAnalysisResponse> OnMealLogged;
    
    [Header("Persistence")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float autoSaveInterval = 30f; // seconds
    private float autoSaveTimer = 0f;
    
    private const string SAVE_KEY = "YolkedNutritionData";
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize nutrition data
        if (nutritionData == null)
        {
            nutritionData = new NutritionData();
        }
        
        // Load saved data
        LoadData();
        
        // Check if it's a new day
        CheckNewDay();
    }
    
    private void Update()
    {
        // Auto-save timer
        if (autoSave)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                SaveData();
                autoSaveTimer = 0f;
            }
        }
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveData();
        }
    }
    
    private void OnApplicationQuit()
    {
        SaveData();
    }
    
    /// <summary>
    /// Get current nutrition data (read-only access)
    /// </summary>
    public NutritionData GetNutritionData()
    {
        return nutritionData;
    }
    
    /// <summary>
    /// Log a meal and update all nutrition data
    /// </summary>
    public void LogMeal(MealAnalysisResponse meal)
    {
        if (meal == null || !meal.meal_identified)
        {
            Debug.LogWarning("Cannot log meal: meal not identified");
            return;
        }
        
        // Store previous level for level-up detection
        int previousLevel = nutritionData.yolkLevel;
        int previousPoints = nutritionData.yolkPoints;
        
        // Add meal to nutrition data
        nutritionData.AddMeal(meal);
        
        // Calculate points earned
        int pointsEarned = nutritionData.yolkPoints - previousPoints;
        
        // Trigger events
        OnMealLogged?.Invoke(meal);
        OnYolkPointsEarned?.Invoke(pointsEarned);
        OnNutritionUpdated?.Invoke(nutritionData);
        
        // Check for level up
        if (nutritionData.yolkLevel > previousLevel)
        {
            OnLevelUp?.Invoke(nutritionData.yolkLevel, nutritionData.yolkStage);
        }
        
        // Save data
        SaveData();
        
        Debug.Log($"Meal logged: {meal.food_items.Count} items, {meal.calories.total} calories, +{pointsEarned} points");
    }
    
    /// <summary>
    /// Manually trigger nutrition data update event (for UI refresh)
    /// </summary>
    public void RefreshUI()
    {
        OnNutritionUpdated?.Invoke(nutritionData);
    }
    
    /// <summary>
    /// Check if it's a new day and reset daily totals if needed
    /// </summary>
    private void CheckNewDay()
    {
        string lastDate = PlayerPrefs.GetString("LastActiveDate", "");
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        
        if (lastDate != today)
        {
            if (!string.IsNullOrEmpty(lastDate))
            {
                // It's a new day - check if consecutive
                DateTime lastDateTime = DateTime.Parse(lastDate);
                DateTime todayDateTime = DateTime.Parse(today);
                
                if ((todayDateTime - lastDateTime).Days == 1)
                {
                    // Consecutive day!
                    nutritionData.consecutiveDaysTracked++;
                    Debug.Log($"Consecutive day streak: {nutritionData.consecutiveDaysTracked}");
                }
                else if ((todayDateTime - lastDateTime).Days > 1)
                {
                    // Streak broken
                    nutritionData.consecutiveDaysTracked = 1;
                    Debug.Log("Streak broken, starting fresh");
                }
                
                // Reset daily totals
                nutritionData.ResetDailyTotals();
            }
            
            PlayerPrefs.SetString("LastActiveDate", today);
            PlayerPrefs.Save();
        }
    }
    
    /// <summary>
    /// Save nutrition data to PlayerPrefs
    /// </summary>
    public void SaveData()
    {
        try
        {
            string json = JsonUtility.ToJson(nutritionData);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            Debug.Log("Nutrition data saved");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load nutrition data from PlayerPrefs
    /// </summary>
    public void LoadData()
    {
        try
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                nutritionData = JsonUtility.FromJson<NutritionData>(json);
                Debug.Log($"Nutrition data loaded: Level {nutritionData.yolkLevel}, {nutritionData.totalYolkPoints} total points");
            }
            else
            {
                Debug.Log("No saved data found, starting fresh");
                nutritionData = new NutritionData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load data: {e.Message}");
            nutritionData = new NutritionData();
        }
    }
    
    /// <summary>
    /// Reset all data (for testing or user request)
    /// </summary>
    public void ResetAllData()
    {
        nutritionData = new NutritionData();
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.DeleteKey("LastActiveDate");
        PlayerPrefs.Save();
        OnNutritionUpdated?.Invoke(nutritionData);
        Debug.Log("All data reset");
    }
    
    /// <summary>
    /// Get formatted stats for display
    /// </summary>
    public string GetStatsString()
    {
        return $"Calories: {nutritionData.totalCalories:F0}/{nutritionData.calorieGoal:F0}\n" +
               $"Protein: {nutritionData.totalProtein:F1}g/{nutritionData.proteinGoal:F0}g\n" +
               $"Carbs: {nutritionData.totalCarbs:F1}g/{nutritionData.carbsGoal:F0}g\n" +
               $"Veggies: {nutritionData.totalVeggies:F0}g/{nutritionData.veggieGoal:F0}g\n" +
               $"Yolk Points: {nutritionData.yolkPoints}\n" +
               $"Level: {nutritionData.yolkLevel} ({nutritionData.yolkStage})\n" +
               $"Meals Today: {nutritionData.mealsLoggedToday}\n" +
               $"Streak: {nutritionData.consecutiveDaysTracked} days";
    }
}
