"""
Claude API Client - Handles image analysis using Claude's vision capabilities
"""
import os
import base64
import logging
from typing import Dict, Any
from anthropic import Anthropic
from dotenv import load_dotenv
import json

load_dotenv()
logger = logging.getLogger(__name__)


class ClaudeClient:
    """Client for Claude API vision analysis"""
    
    def __init__(self):
        self.api_key = os.getenv("ANTHROPIC_API_KEY", "")
        if not self.api_key:
            logger.warning("ANTHROPIC_API_KEY not set in environment variables")
        
        self.client = Anthropic(api_key=self.api_key)
        self.model = "claude-sonnet-4-5-20250929"  # Latest Claude model with vision
    
    def _create_analysis_prompt(self) -> str:
        """Create the prompt for meal analysis"""
        return """You are a nutritional expert. Analyze this food image using objects, hands, or utensils as size reference and provide detailed nutritional information on calories, macros, how healthy/ balanced the meal is.
        Additionally, factor in quality of food: whether it is fast food, processed, or a healthy cooked meal in the balance score.

Return your response as a JSON object with this exact structure:
{
    "meal_name": "Brief description of the meal",
    "total_calories": <number>,
    "total_macros": {
        "protein_g": <number>,
        "carbs_g": <number>,
        "fats_g": <number>,
        "fiber_g": <number>
    },
    "food_items": [
        {
            "name": "Food item name",
            "quantity": "Estimated portion size",
            "calories": <number>,
            "macros": {
                "protein_g": <number>,
                "carbs_g": <number>,
                "fats_g": <number>
            }
        }
    ],
    "vegetables_servings": <number>,
    "balance_score": <number 0-100>,
    "notes": "Any relevant nutritional notes or warnings"
}

Guidelines:
- Be as accurate as possible with portion sizes and nutritional values
- balance_score should reflect how nutritionally balanced the meal is (protein, veggies, healthy fats)
- vegetables_servings should count estimated servings of vegetables (1 serving ≈ 1 cup raw or 0.5 cup cooked)
- Include all visible food items
- If uncertain about portions, provide reasonable estimates
- Return ONLY valid JSON, no additional text"""
    
    async def analyze_meal_image(self, image_bytes: bytes) -> Dict[str, Any]:
        """
        Analyze a meal image using Claude's vision API
        
        Args:
            image_bytes: Image data as bytes
            
        Returns:
            Dict containing parsed meal analysis
            
        Raises:
            Exception: If API call fails or response is invalid
        """
        try:
            logger.info(f"Analyzing image with Claude ({len(image_bytes)} bytes)")
            
            # Encode image to base64
            image_base64 = base64.standard_b64encode(image_bytes).decode("utf-8")
            
            # Determine media type (assume JPEG, but could detect)
            media_type = "image/jpeg"
            if image_bytes[:4] == b'\x89PNG':
                media_type = "image/png"
            
            # Call Claude API with vision
            message = self.client.messages.create(
                model=self.model,
                max_tokens=2048,
                messages=[
                    {
                        "role": "user",
                        "content": [
                            {
                                "type": "image",
                                "source": {
                                    "type": "base64",
                                    "media_type": media_type,
                                    "data": image_base64,
                                },
                            },
                            {
                                "type": "text",
                                "text": self._create_analysis_prompt()
                            }
                        ],
                    }
                ],
            )
            
            # Extract text response
            response_text = message.content[0].text
            logger.info("Claude API response received")
            logger.debug(f"Response: {response_text}")
            
            # Parse JSON response
            try:
                meal_data = json.loads(response_text)
                logger.info("Successfully parsed meal analysis JSON")
                return meal_data
            except json.JSONDecodeError as e:
                logger.error(f"Failed to parse Claude response as JSON: {e}")
                logger.error(f"Response text: {response_text}")
                raise ValueError(f"Claude returned invalid JSON: {str(e)}")
                
        except Exception as e:
            logger.error(f"Claude API request failed: {str(e)}", exc_info=True)
            raise
    
    async def test_connection(self) -> bool:
        """Test if Claude API is accessible"""
        try:
            # Simple test message
            message = self.client.messages.create(
                model=self.model,
                max_tokens=10,
                messages=[{"role": "user", "content": "Hello"}]
            )
            return True
        except Exception as e:
            logger.error(f"Claude API connection test failed: {str(e)}")
            return False
