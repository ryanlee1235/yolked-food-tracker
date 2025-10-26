"""
User tracking models for daily progress and health metrics
"""
from pydantic import BaseModel, Field
from typing import List, Optional
from datetime import datetime, date


class FoodGroupServings(BaseModel):
    """Daily food group servings tracker"""
    protein: float = Field(0, description="Protein servings (1 serving = palm-sized portion)")
    vegetables: float = Field(0, description="Vegetable servings (1 serving = 1 cup)")
    fruits: float = Field(0, description="Fruit servings (1 serving = 1 medium fruit)")
    grains: float = Field(0, description="Grain servings (1 serving = 1 slice bread)")
    dairy: float = Field(0, description="Dairy servings (1 serving = 1 cup milk)")
    fats: float = Field(0, description="Healthy fats servings (1 serving = 1 tbsp)")


class DailyGoals(BaseModel):
    """User's daily nutritional goals"""
    calories: float = Field(2000, description="Daily calorie goal")
    protein_g: float = Field(150, description="Daily protein goal in grams")
    carbs_g: float = Field(200, description="Daily carbs goal in grams")
    fats_g: float = Field(65, description="Daily fats goal in grams")
    vegetables_servings: float = Field(5, description="Daily vegetable servings goal")


class DailyProgress(BaseModel):
    """User's progress for a specific day"""
    user_id: str
    date: str = Field(..., description="Date in YYYY-MM-DD format")
    
    # Totals consumed
    total_calories: float = Field(0, description="Total calories consumed today")
    total_protein_g: float = Field(0, description="Total protein consumed")
    total_carbs_g: float = Field(0, description="Total carbs consumed")
    total_fats_g: float = Field(0, description="Total fats consumed")
    total_fiber_g: float = Field(0, description="Total fiber consumed")
    
    # Food groups
    food_groups: FoodGroupServings = Field(default_factory=FoodGroupServings)
    
    # Goals
    goals: DailyGoals = Field(default_factory=DailyGoals)
    
    # Meals logged
    meals_logged: int = Field(0, description="Number of meals logged today")
    meal_ids: List[str] = Field(default_factory=list, description="IDs of logged meals")
    
    # Completion percentages (calculated)
    @property
    def calorie_percentage(self) -> float:
        """Percentage of daily calorie goal achieved"""
        return min((self.total_calories / self.goals.calories) * 100, 100) if self.goals.calories > 0 else 0
    
    @property
    def protein_percentage(self) -> float:
        """Percentage of daily protein goal achieved"""
        return min((self.total_protein_g / self.goals.protein_g) * 100, 100) if self.goals.protein_g > 0 else 0
    
    @property
    def veggie_percentage(self) -> float:
        """Percentage of daily vegetable goal achieved"""
        return min((self.food_groups.vegetables / self.goals.vegetables_servings) * 100, 100) if self.goals.vegetables_servings > 0 else 0


class HealthScore(BaseModel):
    """Overall health score metrics"""
    overall_score: float = Field(..., description="Overall health score 0-100")
    balance_score: float = Field(..., description="Nutritional balance score 0-100")
    consistency_score: float = Field(..., description="Tracking consistency score 0-100")
    variety_score: float = Field(..., description="Food variety score 0-100")
    
    # Insights
    strengths: List[str] = Field(default_factory=list, description="What user is doing well")
    improvements: List[str] = Field(default_factory=list, description="Areas to improve")


class WeeklyStats(BaseModel):
    """Weekly summary statistics"""
    user_id: str
    week_start: str = Field(..., description="Week start date YYYY-MM-DD")
    
    # Averages
    avg_calories: float = Field(0, description="Average daily calories")
    avg_protein_g: float = Field(0, description="Average daily protein")
    avg_vegetables: float = Field(0, description="Average daily vegetable servings")
    
    # Totals
    total_meals_logged: int = Field(0, description="Total meals logged this week")
    days_tracked: int = Field(0, description="Number of days tracked")
    
    # Streaks
    current_streak: int = Field(0, description="Current daily tracking streak")
    
    # Health score
    health_score: HealthScore


class MealLog(BaseModel):
    """Individual meal log entry"""
    meal_id: str = Field(..., description="Unique meal ID")
    user_id: str
    timestamp: str = Field(..., description="When meal was logged")
    
    # Meal data
    meal_name: str
    calories: float
    protein_g: float
    carbs_g: float
    fats_g: float
    fiber_g: float
    
    # Food groups in this meal
    food_groups: FoodGroupServings
    
    # Metadata
    image_url: Optional[str] = Field(None, description="URL to meal image")
    balance_score: float = Field(..., description="Meal balance score 0-100")
    notes: Optional[str] = None


class UserProfile(BaseModel):
    """User profile with goals and preferences"""
    user_id: str
    name: Optional[str] = None
    
    # Goals
    daily_goals: DailyGoals = Field(default_factory=DailyGoals)
    
    # Preferences
    dietary_restrictions: List[str] = Field(default_factory=list, description="e.g., vegetarian, gluten-free")
    
    # Stats
    total_meals_logged: int = Field(0, description="All-time meals logged")
    account_created: str = Field(..., description="Account creation date")
    current_streak: int = Field(0, description="Current daily tracking streak")
    longest_streak: int = Field(0, description="Longest tracking streak")


# Response models for API endpoints

class DailyProgressResponse(BaseModel):
    """Response for daily progress endpoint"""
    success: bool = True
    progress: DailyProgress
    health_score: HealthScore
    timestamp: str


class WeeklyStatsResponse(BaseModel):
    """Response for weekly stats endpoint"""
    success: bool = True
    stats: WeeklyStats
    daily_breakdown: List[DailyProgress] = Field(default_factory=list)
    timestamp: str


class LogMealResponse(BaseModel):
    """Response after logging a meal"""
    success: bool = True
    meal_log: MealLog
    updated_daily_progress: DailyProgress
    message: str = "Meal logged successfully"
    timestamp: str
