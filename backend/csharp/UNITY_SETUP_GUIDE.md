# Yolked Unity Setup Guide

## 📁 Files Created

### Core System Files
1. **NutritionData.cs** - Data model for tracking nutrition and Yolk Points
2. **YolkedSceneManager.cs** - Central manager (Singleton) for the app
3. **YolkStatsUI.cs** - UI controller for the stats panel
4. **MealCaptureController.cs** - Handles meal capture and analysis flow
5. **ClaudeAPIService.cs** - (Your existing file) Claude API integration

## 🎮 Unity Scene Setup

### Step 1: Create Scene Manager GameObject

1. Create empty GameObject: `YolkedSceneManager`
2. Add component: `YolkedSceneManager.cs`
3. Configure in Inspector:
   - Auto Save: ✓ (checked)
   - Auto Save Interval: 30 seconds
4. This object will persist across scenes (DontDestroyOnLoad)

### Step 2: Create Claude API Service GameObject

1. Create empty GameObject: `ClaudeAPIService`
2. Add component: `ClaudeAPIService.cs`
3. Set API Key in Inspector:
   - API Key: `your_anthropic_api_key_here`

### Step 3: Create Stats Panel UI

Create a Canvas with these UI elements:

```
Canvas
└── StatsPanel
    ├── YolkPetDisplay
    │   ├── YolkImage (Image)
    │   ├── StageText (TextMeshPro)
    │   └── LevelText (TextMeshPro)
    │
    ├── YolkPointsSection
    │   ├── PointsText (TextMeshPro) - "910 out of 2,000 Yolk Points needed"
    │   ├── ProgressBar (Image - Type: Filled, Horizontal)
    │   └── GoalText (TextMeshPro) - "Next level: 2000 total points"
    │
    ├── MacroCircles
    │   ├── ProteinCircle
    │   │   ├── CircleImage (Image - Type: Filled, Radial 360)
    │   │   ├── PercentageText (TextMeshPro) - "18.5%"
    │   │   └── LabelText (TextMeshPro) - "Strength"
    │   │
    │   ├── CarbsCircle
    │   │   ├── CircleImage (Image - Type: Filled, Radial 360)
    │   │   ├── PercentageText (TextMeshPro) - "65.5%"
    │   │   └── LabelText (TextMeshPro) - "Energy"
    │   │
    │   └── VeggiesCircle
    │       ├── CircleImage (Image - Type: Filled, Radial 360)
    │       ├── PercentageText (TextMeshPro) - "27.8%"
    │       └── LabelText (TextMeshPro) - "Nutrient Absorption"
    │
    └── ProTipSection
        └── ProTipText (TextMeshPro)
```

### Step 4: Configure YolkStatsUI Component

1. Add `YolkStatsUI.cs` to StatsPanel GameObject
2. Drag and drop UI elements in Inspector:

**Yolk Pet Display:**
- Yolk Pet Image → YolkImage
- Yolk Stage Text → StageText
- Yolk Level Text → LevelText

**Yolk Points:**
- Yolk Points Text → PointsText
- Yolk Points Progress Bar → ProgressBar
- Points Goal Text → GoalText

**Macro Progress Circles:**
- Protein Progress Circle → ProteinCircle/CircleImage
- Protein Percentage Text → ProteinCircle/PercentageText
- Protein Label Text → ProteinCircle/LabelText
- (Repeat for Carbs and Veggies)

**Pro Tip:**
- Pro Tip Text → ProTipText

**Optional - Yolk Pet Sprites:**
- Blob Sprite → Your blob sprite asset
- Egg Sprite → Your egg sprite asset
- Chick Sprite → Your chick sprite asset
- Chicken Sprite → Your chicken sprite asset

### Step 5: Create Meal Capture System

1. Create empty GameObject: `MealCaptureController`
2. Add component: `MealCaptureController.cs`
3. Configure in Inspector:
   - Claude API → Drag ClaudeAPIService GameObject
   - Capture Camera → Drag your Quest camera
   - Loading Panel → Create UI panel for "Analyzing..."
   - Result Panel → Create UI panel for results
   - Capture Width: 1024
   - Capture Height: 1024

4. Create a Button for capturing meals:
   - Add Button to Canvas
   - OnClick() → MealCaptureController.CaptureMeal()

## 🎨 UI Image Setup for Circular Progress

For the macro circles (Protein, Carbs, Veggies):

1. Select the CircleImage
2. In Inspector:
   - Image Type: **Filled**
   - Fill Method: **Radial 360**
   - Fill Origin: **Top**
   - Clockwise: ✓
   - Fill Amount: 0 (will be animated by script)

## 🔄 Data Flow

```
User captures meal photo
        ↓
MealCaptureController.CaptureMeal()
        ↓
ClaudeAPIService.AnalyzeMeal()
        ↓
Claude API returns MealAnalysisResponse
        ↓
YolkedSceneManager.LogMeal()
        ↓
NutritionData.AddMeal() (calculates points, updates totals)
        ↓
YolkedSceneManager fires OnNutritionUpdated event
        ↓
YolkStatsUI.UpdateUI() (animates progress bars)
```

## 🎮 How It Works

### Nutrition Tracking
- **Calories** → Converted to **Yolk Points** (1:1 ratio)
- **Quality bonus** → Balanced meals get +20% points
- **Rating bonus** → Excellent meals get +30% points
- **Veggie bonus** → High veggie content gets +10% points

### Yolk Pet Progression
- **Level 1-2**: Blob (starting stage)
- **Level 3-5**: Egg (500+ total points)
- **Level 6-9**: Chick (2000+ total points)
- **Level 10+**: Chicken (8000+ total points)

### Macro Progress Circles
- **Strength (Protein)**: Target 150g/day
- **Energy (Carbs)**: Target 200g/day
- **Nutrient Absorption (Veggies)**: Target 400g/day (~5 servings)

### Daily Reset
- Automatically resets at midnight
- Tracks consecutive day streaks
- Daily Yolk Points reset, but total points persist

## 🧪 Testing

### Test in Editor:
1. Run scene
2. Click "Capture Meal" button
3. Should see loading panel
4. After analysis, stats panel updates with:
   - New calorie total
   - Updated macro percentages
   - Yolk Points earned
   - Progress bars animate

### Test Data Persistence:
1. Log a meal
2. Stop play mode
3. Start play mode again
4. Data should be loaded from PlayerPrefs

### Reset Data (for testing):
```csharp
// Call from Inspector or debug button
YolkedSceneManager.Instance.ResetAllData();
```

## 📊 Accessing Data from Other Scripts

```csharp
// Get current nutrition data
NutritionData data = YolkedSceneManager.Instance.GetNutritionData();

// Access specific values
float calories = data.totalCalories;
int points = data.yolkPoints;
int level = data.yolkLevel;
string stage = data.yolkStage;

// Get percentages
float proteinPercent = data.GetProteinPercentage(); // 0.0 to 1.0
float carbsPercent = data.GetCarbsPercentage();
float veggiesPercent = data.GetVeggiesPercentage();

// Manually refresh UI
YolkedSceneManager.Instance.RefreshUI();
```

## 🎯 Events You Can Subscribe To

```csharp
// In your script's Start():
YolkedSceneManager.Instance.OnNutritionUpdated.AddListener(OnNutritionChanged);
YolkedSceneManager.Instance.OnYolkPointsEarned.AddListener(OnPointsEarned);
YolkedSceneManager.Instance.OnLevelUp.AddListener(OnLevelUp);
YolkedSceneManager.Instance.OnMealLogged.AddListener(OnMealLogged);

// Event handlers:
void OnNutritionChanged(NutritionData data) { }
void OnPointsEarned(int points) { }
void OnLevelUp(int level, string stage) { }
void OnMealLogged(MealAnalysisResponse meal) { }
```

## 🚀 For Demo

### Key Features to Show:
1. **Meal Capture** - Point Quest at food, press button
2. **Instant Analysis** - Claude identifies food and nutrition
3. **Points Earned** - "You earned 520 Yolk Points!"
4. **Progress Update** - Bars animate to new values
5. **Yolk Pet Growth** - Show level progression
6. **Pro Tips** - Personalized nutrition advice

### Demo Flow:
1. Show empty stats (Level 1 Blob, 0 points)
2. Capture first meal → Show analysis
3. Stats update with animation
4. Capture more meals → Level up to Egg
5. Show macro balance improving
6. Demonstrate streak tracking

## 🐛 Troubleshooting

### "YolkedSceneManager not found"
- Make sure YolkedSceneManager GameObject exists in scene
- Check it has YolkedSceneManager.cs component

### "UI not updating"
- Check YolkStatsUI has all UI references assigned
- Verify YolkedSceneManager.OnNutritionUpdated event is firing

### "Data not persisting"
- Check PlayerPrefs are being saved (auto-save enabled)
- Try manually calling YolkedSceneManager.Instance.SaveData()

### "Circular progress not working"
- Verify Image Type is set to "Filled"
- Verify Fill Method is "Radial 360"
- Check Fill Amount starts at 0

## 📱 Quest 3S Specific Notes

- Use OVR Camera Rig for capture camera
- Test with passthrough enabled
- Optimize UI for VR viewing distance
- Add hand tracking for button presses
- Consider spatial UI placement

Good luck with your demo! 🎉
