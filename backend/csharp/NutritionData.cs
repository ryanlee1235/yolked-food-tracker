using System;
using UnityEngine;

/// <summary>
/// Persistent nutrition data for the user's daily tracking
/// Stores raw nutrition values and converts to gamified metrics
/// </summary>
[System.Serializable]
public class NutritionData
{
    [Header("Daily Nutrition Totals")]
    public float totalCalories = 0f;
    public float totalProtein = 0f;
    public float totalCarbs = 0f;
    public float totalFats = 0f;
    public float totalVeggies = 0f; // in grams
    
    [Header("Daily Goals")]
    public float calorieGoal = 2000f;
    public float proteinGoal = 150f;
    public float carbsGoal = 200f;
    public float veggieGoal = 400f; // ~5 servings
    
    [Header("Gamification")]
    public int yolkPoints = 0;
    public int totalYolkPoints = 0;
    public int yolkLevel = 1;
    public string yolkStage = "Blob"; // Blob, Egg, Chick, Chicken
    public int mealsLoggedToday = 0;
    public int consecutiveDaysTracked = 0;
    
    // Conversion: 1 calorie = 1 Yolk Point
    private const float CALORIES_TO_POINTS = 1f;
    
    /// <summary>
    /// Add a meal to today's totals and calculate Yolk Points
    /// </summary>
    public void AddMeal(MealAnalysisResponse meal)
    {
        if (!meal.meal_identified) return;
        
        // Add raw nutrition values
        totalCalories += meal.calories.total;
        totalProtein += meal.macros.protein_g;
        totalCarbs += meal.macros.carbohydrates_g;
        totalFats += meal.macros.fats_g;
        totalVeggies += meal.food_groups.fruits_vegetables;
        
        mealsLoggedToday++;
        
        // Calculate Yolk Points earned from this meal
        int pointsEarned = CalculatePointsFromMeal(meal);
        yolkPoints += pointsEarned;
        totalYolkPoints += pointsEarned;
        
        // Check for level up
        CheckLevelUp();
        
        Debug.Log($"Meal added! Earned {pointsEarned} Yolk Points. Total today: {yolkPoints}");
    }
    
    /// <summary>
    /// Calculate Yolk Points from a meal based on calories and quality
    /// </summary>
    private int CalculatePointsFromMeal(MealAnalysisResponse meal)
    {
        // Base points from calories
        int basePoints = Mathf.RoundToInt(meal.calories.total * CALORIES_TO_POINTS);
        
        // Bonus multiplier based on meal quality
        float qualityMultiplier = 1f;
        
        if (meal.health_assessment.is_balanced)
        {
            qualityMultiplier += 0.2f; // +20% for balanced meals
        }
        
        switch (meal.health_assessment.overall_rating.ToLower())
        {
            case "excellent":
                qualityMultiplier += 0.3f;
                break;
            case "good":
                qualityMultiplier += 0.15f;
                break;
            case "fair":
                qualityMultiplier += 0.05f;
                break;
        }
        
        // Bonus for high veggie content
        if (meal.food_groups.fruits_vegetables >= 100f) // ~1+ serving
        {
            qualityMultiplier += 0.1f;
        }
        
        return Mathf.RoundToInt(basePoints * qualityMultiplier);
    }
    
    /// <summary>
    /// Check if user leveled up and update Yolk stage
    /// </summary>
    private void CheckLevelUp()
    {
        int newLevel = CalculateLevel(totalYolkPoints);
        
        if (newLevel > yolkLevel)
        {
            yolkLevel = newLevel;
            UpdateYolkStage();
            Debug.Log($"Level up! Now level {yolkLevel} - {yolkStage}");
        }
    }
    
    /// <summary>
    /// Calculate level based on total points (exponential curve)
    /// </summary>
    private int CalculateLevel(int points)
    {
        // Level formula: level = sqrt(points / 500) + 1
        // Level 1: 0 points
        // Level 2: 500 points
        // Level 3: 2000 points
        // Level 4: 4500 points
        // Level 5: 8000 points
        return Mathf.FloorToInt(Mathf.Sqrt(points / 500f)) + 1;
    }
    
    /// <summary>
    /// Get points needed for next level
    /// </summary>
    public int GetPointsForNextLevel()
    {
        int nextLevel = yolkLevel + 1;
        return (nextLevel - 1) * (nextLevel - 1) * 500;
    }
    
    /// <summary>
    /// Get progress to next level as percentage
    /// </summary>
    public float GetLevelProgress()
    {
        int currentLevelPoints = (yolkLevel - 1) * (yolkLevel - 1) * 500;
        int nextLevelPoints = GetPointsForNextLevel();
        int pointsIntoLevel = totalYolkPoints - currentLevelPoints;
        int pointsNeeded = nextLevelPoints - currentLevelPoints;
        
        return Mathf.Clamp01((float)pointsIntoLevel / pointsNeeded);
    }
    
    /// <summary>
    /// Update Yolk pet stage based on level
    /// </summary>
    private void UpdateYolkStage()
    {
        if (yolkLevel >= 10)
            yolkStage = "Chicken";
        else if (yolkLevel >= 6)
            yolkStage = "Chick";
        else if (yolkLevel >= 3)
            yolkStage = "Egg";
        else
            yolkStage = "Blob";
    }
    
    /// <summary>
    /// Get percentage of daily goal for protein
    /// </summary>
    public float GetProteinPercentage()
    {
        return Mathf.Clamp01(totalProtein / proteinGoal);
    }
    
    /// <summary>
    /// Get percentage of daily goal for carbs
    /// </summary>
    public float GetCarbsPercentage()
    {
        return Mathf.Clamp01(totalCarbs / carbsGoal);
    }
    
    /// <summary>
    /// Get percentage of daily goal for veggies
    /// </summary>
    public float GetVeggiesPercentage()
    {
        return Mathf.Clamp01(totalVeggies / veggieGoal);
    }
    
    /// <summary>
    /// Get percentage of daily calorie goal
    /// </summary>
    public float GetCaloriePercentage()
    {
        return Mathf.Clamp01(totalCalories / calorieGoal);
    }
    
    /// <summary>
    /// Reset daily totals (call at midnight or start of new day)
    /// </summary>
    public void ResetDailyTotals()
    {
        totalCalories = 0f;
        totalProtein = 0f;
        totalCarbs = 0f;
        totalFats = 0f;
        totalVeggies = 0f;
        mealsLoggedToday = 0;
        yolkPoints = 0; // Daily points reset, but totalYolkPoints persists
        
        Debug.Log("Daily nutrition data reset");
    }
    
    /// <summary>
    /// Get pro tip based on current nutrition status
    /// </summary>
    public string GetProTip()
    {
        if (GetProteinPercentage() < 0.5f)
        {
            return "Try to eat more foods with lean protein. Foods such as low fat Greek yogurt, chicken breast, tofu, or cottage cheese are good examples.";
        }
        else if (GetVeggiesPercentage() < 0.5f)
        {
            return "Add more vegetables to your meals! Aim for colorful variety - greens, reds, oranges, and purples.";
        }
        else if (mealsLoggedToday < 3)
        {
            return "Keep tracking your meals! Consistency is key to reaching your nutrition goals.";
        }
        else
        {
            return "Great job tracking your nutrition! Your Yolk is proud of you! 🌟";
        }
    }
}
