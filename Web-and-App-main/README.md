# AAST Student Portal — Smart Academic Management System

A full-stack graduation project for the Arab Academy for Science,
Technology and Maritime Transport (AAST) — Computer Engineering Department.

**Supervisor:** Dr. Ahmed El-Deeb  
**Year:** 2025

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend API | ASP.NET Core 8, C#, Entity Framework Core, SQLite |
| Web Frontend | React 19, TypeScript, Vite, Tailwind CSS |
| Mobile App | Flutter 3, Dart, Android |
| Authentication | Keycloak 24, OAuth2, OIDC, JWT |
| Notifications | Firebase Cloud Messaging (FCM) |
| AI Features | Face Recognition (FastAPI + face_recognition) |
| Deployment | Docker Compose |

## Features

- **3 User Roles:** Admin, Instructor, Student
- **Grade Management:** Enter, edit, and publish grades with auto FCM notification
- **Smart Attendance:** QR code, PIN, and Face Recognition check-in with geofence
- **Mobile App:** Flutter native Android app with real-time data
- **AI Grade Prediction:** Predicts final exam score from mid-semester performance
- **Admin Portal:** Section management, bulk import, account creation

## Quick Start

### Prerequisites
- Docker Desktop
- Python 3.x
- Flutter SDK (for mobile)

### Run the project
```bash
# Clone the repo
git clone https://github.com/hazemehabfawzy/aast-student-portal.git
cd aast-student-portal

# Copy and configure settings
cp StudentPortal.Api/appsettings.example.json StudentPortal.Api/appsettings.json
# Edit appsettings.json with your Keycloak and Firebase credentials

# Add your google-services.json to mobile/android/app/
# Add your firebase-service-account.json to StudentPortal.Api/

# Start all services
docker compose up -d
```

### Demo Credentials
| Role | Username | Password |
|------|----------|----------|
| Admin | admin.portal | Admin@123 |
| Instructor | instructor.one | Instructor@123 |
| Student | student.one | hazem123 |

### Access Points
- Web App: http://localhost:3000
- API Swagger: http://localhost:5000/swagger
- Keycloak Admin: http://localhost:8080

## Project Structure
```
├── StudentPortal.Api/     # ASP.NET Core 8 backend
├── frontend/              # React 19 web application
├── mobile/                # Flutter 3 Android app
├── face-service/          # Python FastAPI face recognition
├── keycloak/              # Realm configuration
├── data/                  # SQLite database (not committed)
└── docker-compose.yml     # Container orchestration
```

## Grading Schema
Week 7 (30) + Week 12 (20) + Coursework (10) + Final (40) = 100  
Auto-F rule: Final score < 12/40 → Grade = F regardless of total
