"""
Simple test script to test the meal analysis API
Usage: python test_api.py <path_to_image>
"""
import sys
import requests
from pathlib import Path


def test_analyze_meal(image_path: str):
    """Test the analyze-meal endpoint"""
    
    # Check if file exists
    if not Path(image_path).exists():
        print(f"❌ Image file not found: {image_path}")
        return
    
    # API endpoint
    url = "http://localhost:8000/api/analyze-meal"
    
    # Prepare the file
    with open(image_path, "rb") as f:
        files = {"image": (Path(image_path).name, f, "image/jpeg")}
        data = {"user_id": "test_user"}
        
        print(f"📤 Sending image to API: {image_path}")
        print(f"🔗 URL: {url}")
        
        try:
            response = requests.post(url, files=files, data=data)
            
            if response.status_code == 200:
                print("✅ Success!")
                result = response.json()
                
                # Pretty print the results
                meal = result["meal_analysis"]
                print(f"\n🍽️  Meal: {meal['meal_name']}")
                print(f"🔥 Calories: {meal['total_calories']}")
                print(f"\n📊 Macros:")
                print(f"   Protein: {meal['total_macros']['protein_g']}g")
                print(f"   Carbs: {meal['total_macros']['carbs_g']}g")
                print(f"   Fats: {meal['total_macros']['fats_g']}g")
                print(f"   Fiber: {meal['total_macros']['fiber_g']}g")
                print(f"\n🥗 Vegetable Servings: {meal['vegetables_servings']}")
                print(f"⭐ Balance Score: {meal['balance_score']}/100")
                
                if meal.get('food_items'):
                    print(f"\n📋 Food Items:")
                    for item in meal['food_items']:
                        print(f"   • {item['name']} ({item['quantity']}) - {item['calories']} cal")
                
                if meal.get('notes'):
                    print(f"\n📝 Notes: {meal['notes']}")
                
            else:
                print(f"❌ Error: {response.status_code}")
                print(response.json())
                
        except requests.exceptions.ConnectionError:
            print("❌ Could not connect to server. Is it running?")
            print("   Start server with: python main.py")
        except Exception as e:
            print(f"❌ Error: {str(e)}")


def test_health():
    """Test the health endpoint"""
    url = "http://localhost:8000/health"
    
    try:
        response = requests.get(url)
        if response.status_code == 200:
            result = response.json()
            print("✅ Server is healthy")
            print(f"   Claude API: {result.get('claude_api', 'unknown')}")
        else:
            print(f"❌ Health check failed: {response.status_code}")
    except requests.exceptions.ConnectionError:
        print("❌ Server is not running")
        print("   Start with: python main.py")


if __name__ == "__main__":
    print("🧪 Yolked API Test\n")
    
    # Test health first
    test_health()
    print()
    
    # Test meal analysis if image provided
    if len(sys.argv) > 1:
        image_path = sys.argv[1]
        test_analyze_meal(image_path)
    else:
        print("💡 To test meal analysis:")
        print("   python test_api.py path/to/meal_image.jpg")
