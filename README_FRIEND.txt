================================================
  AAST Student Portal - Quick Start Guide
  Given to you by: Hazem Ehab Fawzy
================================================

REQUIREMENTS (install these first):
  1. Docker Desktop
     https://www.docker.com/products/docker-desktop/

  2. Start Docker Desktop and wait until the whale
     icon in the taskbar stops animating.

FILES YOU NEED FROM HAZEM (place them here):
  - Web-and-App-main\StudentPortal.Api\firebase-service-account.json
  - Web-and-App-main\mobile\android\app\google-services.json
  - Web-and-App-main\data\student_portal.db

------------------------------------------------
  HOW TO RUN
------------------------------------------------

  START EVERYTHING (web + emulator):
    Double-click: FULL_START.bat
    Wait 2-3 minutes. Browser opens automatically.

  STOP:
    Double-click: STOP_PORTAL.bat

  RESTART (if something goes wrong):
    Double-click: RESTART_PORTAL.bat

  EMULATOR ONLY (if already started):
    Double-click: RUN_EMULATOR_ONLY.bat

  INSTALL ON REAL PHONE:
    1. Enable USB Debugging on phone:
       Settings > About Phone >
       tap Build Number 7 times >
       Settings > Developer Options >
       enable USB Debugging
    2. Connect phone via USB cable
    3. Double-click: INSTALL_MOBILE.bat

------------------------------------------------
  CREDENTIALS
------------------------------------------------

  Role        Username         Password
  -----------------------------------------
  Student     student.one      hazem123
  Instructor  instructor.one   Instructor@123
  Admin       admin.portal     Admin@123

------------------------------------------------
  ACCESS POINTS
------------------------------------------------

  Web App:   http://localhost:3000
  API Docs:  http://localhost:5000/swagger
  Keycloak:  http://localhost:8080

------------------------------------------------
  IF SOMETHING IS WRONG
------------------------------------------------

  1. Make sure Docker Desktop is running
  2. Run RESTART_PORTAL.bat
  3. If still broken, call Hazem

================================================
