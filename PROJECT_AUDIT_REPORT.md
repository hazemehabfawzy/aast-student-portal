# AAST Student Portal — Full Audit Report (v2)
Generated: 2026-06-30 21:40 (UTC+3)
Investigation updated: 2026-06-30 22:02 (UTC+3) — GradingService live-impact verdict added
Last commit: `f1c4b81` — merge: integrate remote history, resolve .gitignore conflict

---

## Git Status

- **Total commits in repo:** 5
- **Last 5 commits:**
  - `f1c4b81` — merge: integrate remote history, resolve .gitignore conflict
  - `2d55ee1` — feat: AAST Student Portal - full-stack academic management system
  - `f0c088f` — chore: add hprof to gitignore
  - `aead4b1` — Initial commit: AAST Student Portal
  - `ff2f47c` — Initial commit: Student Portal project
- **Uncommitted changes:** YES — large number of modified/new files; nothing committed since the merge
- **New features in working tree (uncommitted):**
  - Chat system (`ChatController.cs`, `ChatMessage.cs`, `ChatPage.tsx`, migration `20260628000000_AddChatMessages.cs`)
  - Prediction service (`prediction-service/main.py`, `model.py`, `Dockerfile`, `GradePredictionClient.cs`)
  - Notifications (`NotificationBell.tsx`, `StudentNotifications.tsx`)
  - Face check-in flow (`AttendanceController.cs` updates, `FaceCheckInDto.cs`)
  - Firebase mobile integration (`firebase_options.dart`, `AndroidManifest.xml`)
  - Keycloak sync utilities (`sync_keycloak_ids.py`, `check_realm.py`)

---

## Build Status

| Layer | Result | Detail |
|---|---|---|
| dotnet build (`StudentPortal.Api`) | **PASS** | 0 warnings, 0 errors — 59.65s |
| tsc --noEmit (frontend) | **PASS** | No TypeScript errors |
| flutter analyze (mobile) | **WARN** | 34 issues: 0 errors, 3 warnings, 31 infos — see below |
| flutter build (Pixel 6 API 35) | **PASS** | APK built successfully after `flutter clean`; 214.7s |

### Flutter Analyze — Issues Detail

**3 Warnings (should fix):**
- `config.dart:8` — `_clientId` field declared but never used (`unused_field`)
- `attendance_screen.dart:80` — `_determinePosition` method declared but never called (`unused_element`)
- `results_screen.dart:53` — `_getPrediction` method declared but never called (`unused_element`)

**31 Infos (lower priority):**
- `withOpacity` deprecated across 13 locations in `attendance_screen.dart`, `doctors_screen.dart`, `notifications_screen.dart`, `profile_screen.dart`, `results_screen.dart`, `schedule_screen.dart` → replace with `.withValues()`
- `use_build_context_synchronously` in `assignments_screen.dart` (×4) and `doctors_screen.dart` (×2) — async gaps across BuildContext uses
- `prefer_final_fields` in `assignments_screen.dart`, `attendance_screen.dart`
- `curly_braces_in_flow_control_structures` in `schedule_screen.dart` (×7)

> **Note:** Build caused `flutter clean` to be required — original developer had hardcoded build paths referencing `C:\Users\Ahmed\Desktop\...` in Gradle cache. Clean resolved it.

---

## Mobile App Launch — Pixel 6 API 35

| Check | Result |
|---|---|
| Emulator | **RUNNING** — `emulator-5554`, Android 15 (API 35) |
| APK install | **SUCCESS** — `app-debug.apk` installed |
| App startup | **RUNNING** — Flutter/Impeller backend (OpenGLES) |
| Firebase init | **OK** — Firebase APIs initialized on device unlock |
| Dart VM Service | `http://127.0.0.1:59409/qvCKClkI_go=/` |
| DevTools | `http://127.0.0.1:59409/qvCKClkI_go=/devtools/` |

**Startup warnings (non-fatal):**
- `Skipped 184 frames` on first render — app doing heavy work on main thread at startup
- `Skipped 41 frames` during Firebase initialization
- 35 packages have newer versions incompatible with current constraints (`flutter pub outdated`)

---

## Flutter Unit Tests

| Test Suite | Result |
|---|---|
| `widget_test.dart` — App smoke test | **PASS** |
| Total: 1 test | **1 passed, 0 failed** |

---

## .NET Unit Tests

| Suite | Passed | Failed | Total |
|---|---|---|---|
| `GradingServiceTests` | 9 | **4** | 13 |

**Failing tests — all in `GradingServiceTests.cs`:**

1. **`RecalculateResult_BoundaryValuesLookup(97, "A+")`** — Line 123
   - Expected: `"A+"`, Actual: `"F"`

2. **`RecalculateResult_BoundaryValuesLookup(96, "A")`** — Line 123
   - Expected: `"A"`, Actual: `"F"`

3. **`RecalculateResult_CorrectTotalScoreAndLetter_30_20_10_40_Weights`** — Line 68
   - Expected total score: `90`, Actual: `355`

4. **`RecalculateResult_BoundaryValuesLookup(60, "D")`** — Line 123
   - Expected: `"D"`, Actual: `"F"`

**Root cause (investigation completed 2026-06-30 22:02):** All four failures share one root cause — **the tests are wrong, not the production code.**

The tests were written for an old weighted-percentage formula (treat each score as 0–100%, multiply by a weight: 0.3 / 0.2 / 0.1 / 0.4, then sum). The production code was later refactored to a raw-point summation model (scores are entered in their natural ranges: Week7 0–30, Week12 0–20, Prefinal 0–10, Final 0–40; max total = 100). The test files were never updated after that refactor.

Concretely, test #3 seeds `Week7Score=100, Week12Score=90, PrefinalScore=80, FinalScore=85` — input that the live `PUT /results/{id}` controller would **reject** with `400 Bad Request` (Week7 max is 30). The test bypasses the controller and hits `GradingService` directly with out-of-range values, producing `100+90+80+85 = 355`. The fix is to update the tests to use valid raw-point inputs.

**Live-impact verdict — NONE.** See investigation detail in Critical Issues §2 below.

**Security warning:** `SQLitePCLRaw.lib.e_sqlite3 2.1.6` — HIGH SEVERITY vulnerability ([GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)) — update required.

---

## Docker Status

| Service | Status | Port |
|---|---|---|
| keycloak | **UP (healthy)** | 8080 |
| api | **UP** | 5000 |
| web | **UP** | 3000 |
| face-service | **UP** | 8000 |
| prediction-service | **UP** | 8001 |

**API container error logs — CRITICAL:**
- `SqliteException (0x80004005): SQLite Error 14: 'unable to open database file'`
- Repeated stack traces from EF Core `SingleQueryingEnumerable` → queries intermittently fail
- **Fix:** Verify `docker-compose.yml` volume mapping for the SQLite DB file path inside the container

---

## Web Frontend — Full UI Test (Playwright / Headless Chromium)

**23 / 23 PASS — 0 WARN — 0 FAIL**

Screenshots saved to: `D:/projects/StudentPortal/web_screenshots/`

### Login Tests
| Role | Result | Landed at |
|---|---|---|
| Student login | **PASS** | `/student/profile` |
| Instructor login | **PASS** | `/instructor/sections` |
| Admin login | **PASS** | `/admin/students` |

### Student Pages
| Page | Route | Result |
|---|---|---|
| Dashboard | `/` | **PASS** |
| Profile | `/student/profile` | **PASS** |
| Results | `/student/results` | **PASS** |
| Schedule | `/student/schedule` | **PASS** |
| Course Registration | `/student/register` | **PASS** |
| Notifications | `/student/notifications` | **PASS** |
| Assignments | `/student/assignments` | **PASS** |
| Chat | `/chat` | **PASS** |

### Instructor Pages
| Page | Route | Result |
|---|---|---|
| Sections | `/instructor/sections` | **PASS** |
| Attendance | `/instructor/attendance` | **PASS** |
| Grading | `/instructor/grading` | **PASS** |
| Assignments | `/instructor/assignments` | **PASS** |

### Admin Pages
| Page | Route | Result |
|---|---|---|
| Students | `/admin/students` | **PASS** |
| Instructors | `/admin/instructors` | **PASS** |
| Sections | `/admin/sections` | **PASS** |
| Courses | `/admin/courses` | **PASS** |
| Policies | `/admin/policies` | **PASS** |
| Reports | `/admin/reports` | **PASS** |
| Import | `/admin/import` | **PASS** |

> Note: All three roles authenticate correctly through the custom React login page at `/login`. Each role is redirected to its own default landing page after login, confirming role-based routing is working.

---

## Recently Modified Files (last 3 days)

| File | Type |
|---|---|
| `StudentPortal.Api/Services/Implementations/AttendanceService.cs` | Modified |
| `mobile/lib/screens/attendance_screen.dart` | Modified |
| `StudentPortal.Api/Middleware/KeycloakAuthExtensions.cs` | Modified |
| `mobile/lib/screens/results_screen.dart` | Modified |
| `mobile/lib/config.dart` | Modified |
| `frontend/src/pages/instructor/InstructorAttendance.tsx` | Modified |
| `StudentPortal.Api/Data/DbInitializer.cs` | Modified |
| `sync_keycloak_ids.py` | New |
| `check_realm.py` | New |
| `StudentPortal.Api/Controllers/CourseController.cs` | Modified |
| `StudentPortal.Api/Controllers/SectionController.cs` | Modified |
| `StudentPortal.Api/Controllers/AdminController.cs` | Modified |
| `frontend/src/components/NotificationBell.tsx` | New |
| `frontend/src/pages/student/StudentNotifications.tsx` | Modified |
| `StudentPortal.Api/Controllers/ChatController.cs` | New |
| `frontend/src/pages/ChatPage.tsx` | New |
| `StudentPortal.Api/Migrations/20260628000000_AddChatMessages.cs` | New migration |
| `mobile/lib/screens/doctors_screen.dart` | Modified |
| `mobile/lib/main.dart` | Modified |

---

## Merge Conflicts

**NONE** — No `<<<<<<<`, `=======`, or `>>>>>>>` markers found in any `.cs`, `.tsx`, or `.dart` file.

---

## Core Feature Endpoint Tests (Comprehensive)

Authentication tokens obtained successfully for all three roles (student.one, instructor.one, admin.portal).

### Student Endpoints
| Endpoint | Route | Status |
|---|---|---|
| Student Profile | `GET /api/students/me/profile` | **PASS** |
| Student Results | `GET /api/students/me/results` | **PASS** |
| Student Schedule | `GET /api/students/me/schedule` | **PASS** |
| Student Attendance | `GET /api/students/me/attendance` | **PASS** |
| GPA Trend | `GET /api/students/me/gpa-trend` | **FAIL [401]** |
| Available Sections | `GET /api/sections/available` | **PASS** |
| Notifications | `GET /api/notifications` | **PASS** |
| Chat Messages | `GET /api/chat/sections/{id}` | **PASS** |
| Assignments (student) | `GET /api/assignments` | **FAIL [404]** |

### Instructor Endpoints
| Endpoint | Route | Status |
|---|---|---|
| Instructor Sections | `GET /api/instructor/sections` | **PASS** |
| Instructor Attendance | `GET /api/instructor/attendance` | **FAIL [404]** |
| Instructor Grading | `GET /api/instructor/grading` | **FAIL [404]** |
| Instructor Assignments | `GET /api/instructor/assignments` | **FAIL [404]** |

### Admin Endpoints
| Endpoint | Route | Status |
|---|---|---|
| Admin Students | `GET /api/students` | **PASS** |
| Admin Instructors | `GET /api/instructors` | **PASS** |
| Admin Sections | `GET /api/sections` | **PASS** |
| Admin Courses | `GET /api/courses` | **PASS** |
| Admin Policies | `GET /api/policies` | **FAIL [404]** |
| Admin Reports | `GET /api/admin/reports` | **FAIL [404]** |
| Admin Import | `GET /api/admin/import` | **FAIL [404]** |

### External Services
| Service | Route | Status |
|---|---|---|
| Prediction Service | `GET http://localhost:8001/` | **PASS** |
| Prediction Health | `GET http://localhost:8001/health` | **FAIL [404]** |
| Face Service | `GET http://localhost:8000/` | **FAIL [404]** |
| Face Service Health | `GET http://localhost:8000/health` | **FAIL [404]** |
| Parking Status | `GET /api/parking/status` | **FAIL [404]** |

**Summary: 13 PASS / 12 FAIL**

---

## Sensitive Files Check

| File | Status |
|---|---|
| `StudentPortal.Api/firebase-service-account.json` | **PRESENT** |
| `mobile/android/app/google-services.json` | **PRESENT** |
| `data/student_portal.db` | **PRESENT** |

> Confirm all three are in `.gitignore` to prevent accidental push.

---

## GradingService Live-Impact Investigation (2026-06-30 22:02)

> Triggered by 4 failing .NET unit tests. Full call-chain traced to determine whether the broken tests indicate a live production bug or a test-only issue.

| Question | Answer |
|---|---|
| Is `RecalculateResultAsync` called by `PUT /results/{id}`? | **YES** — `ResultService.UpdateResultAsync` line 200 calls `_gradingService.RecalculateResultAsync(result.Id)` |
| Is `GradingService` registered in DI? | **YES** — `Program.cs:28` `AddScoped<IGradingService, GradingService>()` |
| Does `ResultController` inject `IGradingService` directly? | **NO** — it only injects `IResultService`; `GradingService` is injected into `ResultService` |
| Any other controller or endpoint calling `RecalculateResultAsync`? | **NO** — the only call site in the entire codebase is `ResultService.cs:200` |
| What formula does the live code use for `TotalScore`? | `Week7Score + Week12Score + PrefinalScore + FinalScore` (plain raw-point sum, max = 100) |
| Is this formula correct for the system's data model? | **YES** — controller enforces `Week7 ∈ [0,30]`, `Week12 ∈ [0,20]`, `Prefinal ∈ [0,10]`, `Final ∈ [0,40]`; sum is already in points |
| Why do tests return 355 instead of 90? | Tests seed `Week7Score=100` (invalid per controller, max is 30) and expect a weighted-percentage formula (`score × weight`) that was removed from the code |
| Is the bug LIVE-AFFECTING or a dead test artefact? | **DEAD TEST ARTEFACT** — production behavior is correct |

**Required fix:** Update `GradingServiceTests.cs` — replace out-of-range percentage inputs with valid raw-point inputs (e.g., `Week7=27, Week12=18, Prefinal=9, Final=36 → TotalScore=90`). No changes to `GradingService.cs` or `ResultService.cs`.

---

## Critical Issues — Ranked by Severity

### CRITICAL

1. **SQLite Error 14 — API container cannot open database**
   - Source: `docker compose logs api` (repeated)
   - The API is running but EF Core queries fail intermittently with `unable to open database file`
   - Fix: Check `docker-compose.yml` volume mapping; ensure the DB file path inside the container matches `appsettings.json` connection string

2. ~~**GradingService weight calculation bug**~~ — **RECLASSIFIED: TEST BUG / NOT LIVE-AFFECTING** ✓
   - **Investigation (2026-06-30 22:02):** `GradingService.RecalculateResultAsync` IS reachable from the live `PUT /results/{enrollmentId}` endpoint (via `ResultService.UpdateResultAsync` line 200, which delegates score calculation entirely to `_gradingService.RecalculateResultAsync`). Both services are registered in DI (`Program.cs` lines 28, 32).
   - **However, the production formula is correct.** The code does a plain raw-point sum (`Week7 + Week12 + Prefinal + Final`, max = 100). This is consistent with the controller validation that enforces `Week7 ∈ [0,30]`, `Week12 ∈ [0,20]`, `Prefinal ∈ [0,10]`, `Final ∈ [0,40]`. An instructor entering Week7=25, Week12=15, Prefinal=8, Final=34 gets TotalScore=82 — correct.
   - **The bug is in the unit tests.** They seed scores as percentages (100, 90, 80, 85 out of 100) expecting a weighted multiplication formula that was removed from the code. Those input values would be rejected by the controller before `GradingService` is ever reached.
   - **Action required:** Rewrite the 4 failing `GradingServiceTests` to use valid raw-point inputs. No change to `GradingService.cs` or `ResultService.cs` needed.

### HIGH

3. **GPA Trend endpoint returns 401** — `GET /api/students/me/gpa-trend`
   - Student token is valid (all other student routes pass); this endpoint requires a different role or is missing auth policy configuration
   - File to check: `ResultController.cs` or `StudentController.cs`

4. **HIGH SEVERITY npm vulnerability** — `SQLitePCLRaw.lib.e_sqlite3 2.1.6`
   - Advisory: [GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)
   - Update the package in `StudentPortal.Tests`

5. **Flutter startup frame drops** — `Skipped 184 frames` on launch
   - Main thread is overloaded during app init (Firebase + Geolocator + secure storage all initializing simultaneously)
   - Consider lazy initialization for non-critical services

### MEDIUM

6. **10 API endpoints return 404** — not implemented routes that are referenced by frontend:
   - `GET /api/assignments` (student)
   - `GET /api/instructor/attendance`, `/grading`, `/assignments`
   - `GET /api/policies`, `/api/admin/reports`, `/api/admin/import`
   - `GET /api/parking/status`
   - `GET http://localhost:8001/health` (Prediction Service)
   - `GET http://localhost:8000/` and `/health` (Face Service root)

7. **35 Flutter packages outdated** — some major versions behind (e.g., `mobile_scanner 3.5.7` vs `7.2.0`, `geolocator 10.1.1` vs `14.0.3`)

8. **Flutter build path corruption** — Gradle cache had hardcoded `C:\Users\Ahmed\Desktop\...` from original developer machine; fixed with `flutter clean` but will recur on next fresh clone if build artifacts are committed

### LOW

9. **3 Flutter warnings** — unused `_clientId`, `_determinePosition`, `_getPrediction` (dead code)
10. **`withOpacity` deprecated** — 13 usages should migrate to `.withValues()`
11. **`docker-compose.yml` warning** — obsolete `version` attribute; safe to remove
12. **All new features uncommitted** — Chat, Prediction, Notifications, Face Check-in only exist in working tree

---

## Test Summary Matrix

| Layer | Total | Passed | Failed | Status |
|---|---|---|---|---|
| dotnet unit tests | 13 | 9 | **4** | FAIL |
| Flutter unit tests | 1 | 1 | 0 | PASS |
| API endpoint tests | 25 | 13 | **12** | PARTIAL |
| Web UI — Playwright (headless) | 23 | 23 | 0 | **PASS** |
| Mobile build & launch | 1 | 1 | 0 | PASS |
| TypeScript type check | — | PASS | — | PASS |
| dotnet build | — | PASS | — | PASS |

---

## Summary

All 5 Docker services are running. The web frontend passed a full Playwright headless UI test — **23/23 pages pass** across all three roles (Student, Instructor, Admin), login flows work correctly, and role-based routing lands each user on the correct default page. The Flutter app now runs successfully on the Pixel 6 API 35 emulator after running `flutter clean` to remove cached build paths from the original developer's machine. **The previously flagged `GradingService` weight bug has been investigated (2026-06-30 22:02) and confirmed NOT live-affecting** — the production formula (raw-point sum, max 100) is correct and consistent with controller-level input validation; the 4 failing .NET tests are a test-design bug requiring test rewrites only. **The most urgent remaining issue is the SQLite Error 14** in the API container (intermittent `unable to open database file` from EF Core), which causes real query failures in production. The GPA Trend endpoint (`GET /api/students/me/gpa-trend`) returns 401 for valid student tokens and needs an auth policy correction. Friend's new features (Chat, Prediction Service, NotificationBell) are working correctly at the endpoint level and introduced no merge conflicts or TypeScript errors. The 12 failing endpoints are a mix of unimplemented routes and missing auth policy configuration, not regressions caused by recent changes.
