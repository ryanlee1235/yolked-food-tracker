"""
Data models and schemas for Yolked API
"""
from pydantic import BaseModel, Field
from typing import Dict, List, Optional


class MacroNutrients(BaseModel):
    """Macronutrient breakdown"""
    protein_g: float = Field(..., description="Protein in grams")
    carbs_g: float = Field(..., description="Carbohydrates in grams")
    fats_g: float = Field(..., description="Fats in grams")
    fiber_g: Optional[float] = Field(0, description="Fiber in grams")


class FoodItem(BaseModel):
    """Individual food item detected in the meal"""
    name: str
    quantity: str
    calories: float
    macros: MacroNutrients


class MealAnalysis(BaseModel):
    """Complete meal analysis from Claude"""
    meal_name: str = Field(..., description="Name/description of the meal")
    total_calories: float = Field(..., description="Total calories")
    total_macros: MacroNutrients = Field(..., description="Total macronutrients")
    food_items: List[FoodItem] = Field(default_factory=list, description="Individual food items")
    vegetables_servings: float = Field(0, description="Estimated vegetable servings")
    balance_score: float = Field(..., description="Nutritional balance score 0-100")
    notes: Optional[str] = Field(None, description="Additional notes or warnings")


class MealAnalysisRequest(BaseModel):
    """Request for meal analysis"""
    user_id: Optional[str] = Field("default_user", description="User identifier")


class MealAnalysisResponse(BaseModel):
    """Response from meal analysis endpoint"""
    success: bool
    meal_analysis: MealAnalysis
    timestamp: str
    message: Optional[str] = None


class ErrorResponse(BaseModel):
    """Error response"""
    success: bool = False
    error: str
    timestamp: str
