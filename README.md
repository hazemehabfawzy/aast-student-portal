# AAST Student Portal

A graduation project for the Arab Academy for Science, Technology and Maritime Transport (AAST) — Computer Engineering Department.

A full-stack student portal system with a React web app, Flutter mobile app, ASP.NET Core API, and Keycloak authentication.

---

## System Architecture

```
React Web App (localhost:3000)     ──────┐
                                         ├──► ASP.NET Core API (localhost:5000)
Flutter Mobile App (Android)       ──────┘         │
                                               SQLite DB
                                               Keycloak (localhost:8080)
                                               Firebase FCM (push notifications)
```

## Features

### Student (Web + Mobile)
- View academic profile (GPA, credits, department)
- View results per course (Week 7, Week 12, Course Work, Final)
- Weekly class timetable
- Attendance check-in via QR code or PIN (mobile only)
- Notifications and attendance warnings

### Instructor (Web)
- View and manage enrolled students per section
- Enter and edit student scores (Week7/30, Week12/20, Prefinal/10, Final/40)
- Publish grades (notifies students via push notification)
- Start attendance sessions (QR or PIN)
- View real-time attendance reports

### Admin (Web)
- Manage students, instructors, courses, sections
- Bulk import students via CSV
- Manage grading policies and grade scales
- View system-wide reports and export to Excel/PDF

---

## Tech Stack

| Component | Technology |
|---|---|
| Web App | React 19, Vite, TypeScript, TailwindCSS |
| Mobile App | Flutter 3.x, Dart |
| Backend API | ASP.NET Core 8, Entity Framework Core |
| Database | SQLite |
| Authentication | Keycloak 24 (OAuth2/OIDC) |
| Push Notifications | Firebase Cloud Messaging (FCM) |
| Containerization | Docker Compose |

---

## Getting Started

### Prerequisites
- Docker Desktop
- Flutter SDK
- Node.js 18+
- .NET 8 SDK (for local development)

### Quick Start

1. Clone the repository:
```bash
git clone https://github.com/hazemehabfawzy/aast-student-portal.git
cd aast-student-portal
```

2. Start all services:
```bash
docker compose up -d
```

3. Wait for services to start (about 60 seconds), then sync Keycloak IDs:
```bash
python sync_keycloak_ids.py
```

4. Access the portal:
- **Web App**: http://localhost:3000
- **API Swagger**: http://localhost:5000/swagger
- **Keycloak Admin**: http://localhost:8080 (admin/admin)

### Demo Credentials

| Role | Username | Password |
|---|---|---|
| Admin | admin.portal | Admin@123 |
| Instructor | instructor.one | Instructor@123 |
| Student | student.one | hazem123 |

---

## Mobile App Setup

1. Navigate to the mobile directory:
```bash
cd mobile
flutter pub get
```

2. Run on Android emulator or device:
```bash
flutter run
```

> The mobile app connects to the API via `10.0.2.2:5000` (Android emulator alias for localhost).

---

## Project Structure

```text
├── frontend/              # React web application
├── mobile/                # Flutter mobile application  
├── StudentPortal.Api/     # ASP.NET Core 8 Web API
├── keycloak/              # Keycloak realm configuration
├── data/                  # SQLite database (gitignored)
├── docker-compose.yml     # Docker Compose configuration
├── sync_keycloak_ids.py   # Run after every docker restart
└── start_portal.bat       # Windows startup script
```

---

## Firebase Setup (Optional)

Push notifications require Firebase configuration:

1. Create a Firebase project at https://console.firebase.google.com
2. Add an Android app with package name `com.studentportal.app`
3. Download `google-services.json` → place in `mobile/android/app/`
4. Generate a service account key → place in `StudentPortal.Api/firebase-service-account.json`

---

## Course Catalog

The system is seeded with the real AAST Computer Engineering course catalog — 66 courses across 10 semesters.

---

## Team

- **Hazem Ehab Fawzy** — Full-Stack Developer
- Student ID: 19104097
- Department: Computer Engineering, AAST

---

## License

This project is submitted as a graduation project to AAST.
