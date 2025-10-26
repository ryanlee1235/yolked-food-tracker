"""
Yolked Backend - FastAPI server for Quest 3S MR calorie tracking app
"""
from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.middleware.cors import CORSMiddleware
import uvicorn
from datetime import datetime
import logging
import os
from dotenv import load_dotenv

from api.claude_client import ClaudeClient
from models.schemas import (
    MealAnalysis,
    MealAnalysisResponse,
    ErrorResponse
)
from models.user_tracking import (
    DailyProgressResponse,
    WeeklyStatsResponse,
    LogMealResponse
)
from services.progress_tracker import progress_tracker

# Load environment variables
load_dotenv()

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize FastAPI app
app = FastAPI(
    title="Yolked API",
    description="Backend for Quest 3S Mixed Reality Calorie Tracking",
    version="1.0.0"
)

# CORS middleware for Unity/Mobile requests
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, specify your app origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize Claude client
claude_client = ClaudeClient()


@app.get("/")
async def root():
    """Health check endpoint"""
    return {
        "status": "online",
        "service": "Yolked Backend",
        "timestamp": datetime.utcnow().isoformat(),
        "version": "1.0.0"
    }


@app.get("/health")
async def health_check():
    """Detailed health check"""
    claude_status = await claude_client.test_connection()
    return {
        "status": "healthy",
        "claude_api": "connected" if claude_status else "disconnected",
        "timestamp": datetime.utcnow().isoformat(),
        "environment": os.getenv("DEBUG", "False")
    }


@app.post("/api/analyze-meal", response_model=MealAnalysisResponse)
async def analyze_meal(
    image: UploadFile = File(...),
    user_id: str = "default_user"
):
    """
    Analyze a meal image from Quest 3S camera using Claude AI
    
    Args:
        image: Image file from Quest camera
        user_id: User identifier for tracking
        
    Returns:
        MealAnalysisResponse with detailed nutrition information
    """
    try:
        logger.info(f"Received meal analysis request from user: {user_id}")
        
        # Validate image file
        if not image.content_type or not image.content_type.startswith("image/"):
            raise HTTPException(
                status_code=400,
                detail="File must be an image (JPEG, PNG, etc.)"
            )
        
        # Read image bytes
        image_bytes = await image.read()
        logger.info(f"Image size: {len(image_bytes)} bytes")
        
        # Validate image size (max 5MB)
        max_size = 5 * 1024 * 1024  # 5MB
        if len(image_bytes) > max_size:
            raise HTTPException(
                status_code=400,
                detail=f"Image too large. Maximum size is {max_size / 1024 / 1024}MB"
            )
        
        # Analyze with Claude
        logger.info("Sending image to Claude for analysis...")
        meal_data = await claude_client.analyze_meal_image(image_bytes)
        
        # Parse into Pydantic model for validation
        meal_analysis = MealAnalysis(**meal_data)
        
        logger.info(f"Analysis complete: {meal_analysis.meal_name}")
        
        # Log the meal and update progress
        meal_log = progress_tracker.log_meal(user_id, meal_analysis)
        updated_progress = progress_tracker.get_or_create_daily_progress(user_id)
        
        logger.info(f"Meal logged: {meal_log.meal_id}")
        logger.info(f"Daily progress: {updated_progress.total_calories}/{updated_progress.goals.calories} cal")
        
        return LogMealResponse(
            success=True,
            meal_log=meal_log,
            updated_daily_progress=updated_progress,
            message=f"Meal logged! {updated_progress.meals_logged} meals today",
            timestamp=datetime.utcnow().isoformat()
        )
        
    except HTTPException:
        raise
    except ValueError as e:
        logger.error(f"Validation error: {str(e)}")
        raise HTTPException(status_code=422, detail=str(e))
    except Exception as e:
        logger.error(f"Error analyzing meal: {str(e)}", exc_info=True)
        raise HTTPException(
            status_code=500,
            detail=f"Failed to analyze meal: {str(e)}"
        )


@app.get("/api/progress/{user_id}", response_model=DailyProgressResponse)
async def get_daily_progress(user_id: str):
    """
    Get user's daily progress and health score
    
    Args:
        user_id: User identifier
        
    Returns:
        DailyProgressResponse with progress and health metrics
    """
    try:
        logger.info(f"Fetching daily progress for user: {user_id}")
        
        progress = progress_tracker.get_or_create_daily_progress(user_id)
        health_score = progress_tracker.calculate_health_score(user_id)
        
        return DailyProgressResponse(
            success=True,
            progress=progress,
            health_score=health_score,
            timestamp=datetime.utcnow().isoformat()
        )
        
    except Exception as e:
        logger.error(f"Error fetching progress: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/api/stats/{user_id}", response_model=WeeklyStatsResponse)
async def get_weekly_stats(user_id: str):
    """
    Get user's weekly statistics
    
    Args:
        user_id: User identifier
        
    Returns:
        WeeklyStatsResponse with weekly summary
    """
    try:
        logger.info(f"Fetching weekly stats for user: {user_id}")
        
        stats = progress_tracker.get_weekly_stats(user_id)
        progress = progress_tracker.get_or_create_daily_progress(user_id)
        
        return WeeklyStatsResponse(
            success=True,
            stats=stats,
            daily_breakdown=[progress],
            timestamp=datetime.utcnow().isoformat()
        )
        
    except Exception as e:
        logger.error(f"Error fetching stats: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail=str(e))


if __name__ == "__main__":
    host = os.getenv("HOST", "0.0.0.0")
    port = int(os.getenv("PORT", 8000))
    
    logger.info(f"Starting Yolked Backend on {host}:{port}")
    
    uvicorn.run(
        "main:app",
        host=host,
        port=port,
        reload=True,
        log_level="info"
    )
