@echo off
echo Starting AAST Student Portal...
cd /d D:\projects\StudentPortal\Web-and-App-main
docker compose up -d
echo Waiting for services to start...
timeout /t 30 /nobreak
echo Done! Services running:
echo   Web:      http://localhost:3000
echo   API:      http://localhost:5000
echo   Keycloak: http://localhost:8080
echo.
echo Demo credentials:
echo   Student:    student.one / hazem123
echo   Instructor: instructor.one / Instructor@123
echo   Admin:      admin.portal / Admin@123
pause
