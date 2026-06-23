# AAST Student Portal - Local Deployment & Configuration

This project is configured for local development and demonstration using Docker Compose and Flutter.

## 1. Quick Start with Docker Compose

To build and run Keycloak, the ASP.NET Core API backend, and the React frontend web application in one command, run:

```bash
docker compose up --build
```

### Services Map:
- **Keycloak Admin & Realms**: [http://localhost:8080](http://localhost:8080)
- **Backend Swagger API**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **Web Frontend Application**: [http://localhost:3000](http://localhost:3000)

---

## 2. Flutter Mobile Application Integration

To run the Flutter mobile application:

```bash
cd mobile
flutter run
```

### Server Endpoint Routing:
- **Android Emulator**: Set your Flutter client's base URL to point to `http://10.0.2.2:5000` (which directs to host localhost).
- **Physical Test Devices**: Replace `localhost` or `10.0.2.2` with your machine's **local LAN IP address** (e.g. `http://192.168.1.50:5000`).

---

## 3. Demo Credentials Reference Table

Below is the list of pre-seeded accounts available for interactive testing:

| Role | Username (Email) | Password | Description / Permissions |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@aast.edu` | `admin123` | Full CRUD across Courses, Sections, Policies, Periods, and Students. |
| **Instructor 1** | `instructor1@aast.edu` | `TempPassword123!` | Manages Section 1 and Section 2. Accesses attendance, roster results. |
| **Instructor 2** | `instructor2@aast.edu` | `TempPassword123!` | Manages Section 3 and Section 4. Accesses attendance, roster results. |
| **Student 1** | `student.one` / `student1@aast.edu` | `hazem123` | Enrolled in Section 1 and Section 2. Reviews GPA, checks in to sessions. |
| **Student 2** | `student2@aast.edu` | `TempPassword123!` | Enrolled in Section 1 and Section 2. Reviews GPA, checks in to sessions. |
| **Student 3** | `student3@aast.edu` | `TempPassword123!` | Enrolled in Section 1 and Section 2. Reviews GPA, checks in to sessions. |
| **Student 4** | `student4@aast.edu` | `TempPassword123!` | Enrolled in Section 1 and Section 2. Reviews GPA, checks in to sessions. |
| **Student 5** | `student5@aast.edu` | `TempPassword123!` | Enrolled in Section 1 and Section 2. Reviews GPA, checks in to sessions. |
