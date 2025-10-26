# Yolked Backend

Python FastAPI backend for Yolked - Quest 3S Mixed Reality calorie tracking application.

## 🚀 Quick Start

### 1. Create Virtual Environment

```bash
cd backend

# Create virtual environment
python3 -m venv venv

# Activate it
source venv/bin/activate
```

### 2. Install Dependencies

```bash
pip install -r requirements.txt
```

### 3. Configure Environment

```bash
# Copy example environment file
cp .env.example .env

# Edit .env and add your CalAI API key
nano .env
```

### 4. Run the Server

```bash
python main.py

# Server starts at: http://localhost:8000
# API docs at: http://localhost:8000/docs
```

## 📁 Project Structure

```
backend/
├── main.py                 # FastAPI application entry point
├── requirements.txt        # Python dependencies
├── .env                    # Your secrets (gitignored)
├── .env.example           # Template for .env
│
├── api/                   # API logic
│   ├── __init__.py
│   ├── calai_client.py   # CalAI API integration
│   ├── nutrition_analyzer.py
│   └── gamification.py
│
├── models/               # Data models
│   ├── __init__.py
│   └── schemas.py       # Pydantic models
│
└── tests/               # Unit tests
    └── __init__.py
```

## 🔧 Daily Workflow

```bash
# 1. Navigate to backend
cd backend

# 2. Activate virtual environment
source venv/bin/activate

# 3. Run server
python main.py

# When done
deactivate
```

## 🧪 Testing

```bash
# Run tests
pytest

# Run with coverage
pytest --cov=.
```

## 📚 API Endpoints

- `GET /` - Health check
- `POST /api/analyze-meal` - Analyze meal from image
- `GET /api/user-stats/{user_id}` - Get user statistics

Full docs: http://localhost:8000/docs (when running)

## 🔗 For Unity/XR Developers

Connect to backend at:
- **Local**: `http://localhost:8000`
- **Same WiFi (Quest)**: `http://YOUR_IP:8000`

Find your IP: `ipconfig getifaddr en0`
