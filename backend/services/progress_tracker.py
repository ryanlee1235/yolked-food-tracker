"""
Progress tracking service - Calculates health scores and tracks user progress
Simple in-memory storage for hackathon (replace with database later)
"""
from datetime import datetime, date
from typing import Dict, Optional
import uuid

from models.user_tracking import (
    DailyProgress,
    HealthScore,
    WeeklyStats,
    MealLog,
    FoodGroupServings,
    DailyGoals,
    UserProfile
)
from models.schemas import MealAnalysis


class ProgressTracker:
    """Tracks user progress and calculates health metrics"""
    
    def __init__(self):
        # In-memory storage (for hackathon - use database in production)
        self.daily_progress: Dict[str, DailyProgress] = {}  # key: "user_id:date"
        self.meal_logs: Dict[str, MealLog] = {}  # key: meal_id
        self.user_profiles: Dict[str, UserProfile] = {}  # key: user_id
    
    def _get_progress_key(self, user_id: str, date_str: str) -> str:
        """Generate key for daily progress storage"""
        return f"{user_id}:{date_str}"
    
    def get_or_create_daily_progress(self, user_id: str, date_str: Optional[str] = None) -> DailyProgress:
        """Get or create daily progress for a user"""
        if date_str is None:
            date_str = date.today().isoformat()
        
        key = self._get_progress_key(user_id, date_str)
        
        if key not in self.daily_progress:
            # Get user's goals if they exist
            goals = DailyGoals()
            if user_id in self.user_profiles:
                goals = self.user_profiles[user_id].daily_goals
            
            self.daily_progress[key] = DailyProgress(
                user_id=user_id,
                date=date_str,
                goals=goals
            )
        
        return self.daily_progress[key]
    
    def log_meal(self, user_id: str, meal_analysis: MealAnalysis) -> MealLog:
        """Log a meal and update daily progress"""
        meal_id = str(uuid.uuid4())
        timestamp = datetime.utcnow().isoformat()
        date_str = date.today().isoformat()
        
        # Estimate food groups from meal
        food_groups = self._estimate_food_groups(meal_analysis)
        
        # Create meal log
        meal_log = MealLog(
            meal_id=meal_id,
            user_id=user_id,
            timestamp=timestamp,
            meal_name=meal_analysis.meal_name,
            calories=meal_analysis.total_calories,
            protein_g=meal_analysis.total_macros.protein_g,
            carbs_g=meal_analysis.total_macros.carbs_g,
            fats_g=meal_analysis.total_macros.fats_g,
            fiber_g=meal_analysis.total_macros.fiber_g,
            food_groups=food_groups,
            balance_score=meal_analysis.balance_score,
            notes=meal_analysis.notes
        )
        
        self.meal_logs[meal_id] = meal_log
        
        # Update daily progress
        progress = self.get_or_create_daily_progress(user_id, date_str)
        progress.total_calories += meal_log.calories
        progress.total_protein_g += meal_log.protein_g
        progress.total_carbs_g += meal_log.carbs_g
        progress.total_fats_g += meal_log.fats_g
        progress.total_fiber_g += meal_log.fiber_g
        progress.meals_logged += 1
        progress.meal_ids.append(meal_id)
        
        # Update food groups
        progress.food_groups.protein += food_groups.protein
        progress.food_groups.vegetables += food_groups.vegetables
        progress.food_groups.fruits += food_groups.fruits
        progress.food_groups.grains += food_groups.grains
        progress.food_groups.dairy += food_groups.dairy
        progress.food_groups.fats += food_groups.fats
        
        return meal_log
    
    def _estimate_food_groups(self, meal: MealAnalysis) -> FoodGroupServings:
        """Estimate food group servings from meal analysis"""
        # Simple heuristic estimation for hackathon
        # In production, Claude could return this directly
        
        protein_servings = meal.total_macros.protein_g / 25  # ~25g protein per serving
        veggie_servings = meal.vegetables_servings
        
        # Estimate grains from carbs (rough approximation)
        grain_servings = max(0, (meal.total_macros.carbs_g - veggie_servings * 10) / 15)
        
        # Estimate fats servings
        fat_servings = meal.total_macros.fats_g / 14  # ~14g fat per serving
        
        return FoodGroupServings(
            protein=round(protein_servings, 1),
            vegetables=round(veggie_servings, 1),
            fruits=0,  # Could enhance this
            grains=round(grain_servings, 1),
            dairy=0,  # Could enhance this
            fats=round(fat_servings, 1)
        )
    
    def calculate_health_score(self, user_id: str, date_str: Optional[str] = None) -> HealthScore:
        """Calculate overall health score for a user"""
        progress = self.get_or_create_daily_progress(user_id, date_str)
        
        # Balance score (macro balance)
        balance_score = self._calculate_balance_score(progress)
        
        # Consistency score (based on meals logged)
        consistency_score = min((progress.meals_logged / 3) * 100, 100)  # 3 meals = 100%
        
        # Variety score (based on food groups)
        variety_score = self._calculate_variety_score(progress)
        
        # Overall score (weighted average)
        overall_score = (
            balance_score * 0.4 +
            consistency_score * 0.3 +
            variety_score * 0.3
        )
        
        # Generate insights
        strengths = []
        improvements = []
        
        if progress.protein_percentage >= 80:
            strengths.append("Great protein intake! 💪")
        elif progress.protein_percentage < 50:
            improvements.append("Try to add more protein to your meals")
        
        if progress.veggie_percentage >= 80:
            strengths.append("Excellent vegetable consumption! 🥗")
        elif progress.veggie_percentage < 50:
            improvements.append("Add more vegetables to your diet")
        
        if progress.meals_logged >= 3:
            strengths.append("Consistent meal tracking! 📊")
        elif progress.meals_logged < 2:
            improvements.append("Log more meals for better insights")
        
        if balance_score >= 80:
            strengths.append("Well-balanced nutrition! ⭐")
        
        return HealthScore(
            overall_score=round(overall_score, 1),
            balance_score=round(balance_score, 1),
            consistency_score=round(consistency_score, 1),
            variety_score=round(variety_score, 1),
            strengths=strengths,
            improvements=improvements
        )
    
    def _calculate_balance_score(self, progress: DailyProgress) -> float:
        """Calculate nutritional balance score"""
        if progress.total_calories == 0:
            return 0
        
        # Check if macros are in healthy ranges
        protein_pct = (progress.total_protein_g * 4 / progress.total_calories) * 100
        carbs_pct = (progress.total_carbs_g * 4 / progress.total_calories) * 100
        fats_pct = (progress.total_fats_g * 9 / progress.total_calories) * 100
        
        # Ideal ranges: Protein 20-35%, Carbs 45-65%, Fats 20-35%
        protein_score = 100 - abs(protein_pct - 27.5) * 3  # Target 27.5%
        carbs_score = 100 - abs(carbs_pct - 55) * 2  # Target 55%
        fats_score = 100 - abs(fats_pct - 27.5) * 3  # Target 27.5%
        
        # Ensure scores are between 0-100
        protein_score = max(0, min(100, protein_score))
        carbs_score = max(0, min(100, carbs_score))
        fats_score = max(0, min(100, fats_score))
        
        return (protein_score + carbs_score + fats_score) / 3
    
    def _calculate_variety_score(self, progress: DailyProgress) -> float:
        """Calculate food variety score based on food groups"""
        groups = progress.food_groups
        
        # Count how many food groups have at least 1 serving
        groups_hit = sum([
            groups.protein > 0,
            groups.vegetables > 0,
            groups.fruits > 0,
            groups.grains > 0,
            groups.dairy > 0,
            groups.fats > 0
        ])
        
        # 6 groups = 100%, scale accordingly
        return (groups_hit / 6) * 100
    
    def get_weekly_stats(self, user_id: str) -> WeeklyStats:
        """Get weekly statistics for a user"""
        # For hackathon, just return current day stats
        # In production, aggregate last 7 days
        today = date.today().isoformat()
        progress = self.get_or_create_daily_progress(user_id, today)
        health_score = self.calculate_health_score(user_id, today)
        
        return WeeklyStats(
            user_id=user_id,
            week_start=today,
            avg_calories=progress.total_calories,
            avg_protein_g=progress.total_protein_g,
            avg_vegetables=progress.food_groups.vegetables,
            total_meals_logged=progress.meals_logged,
            days_tracked=1 if progress.meals_logged > 0 else 0,
            current_streak=1 if progress.meals_logged > 0 else 0,
            health_score=health_score
        )


# Global instance for hackathon (use dependency injection in production)
progress_tracker = ProgressTracker()
