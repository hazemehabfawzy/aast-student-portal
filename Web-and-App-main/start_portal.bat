@echo off
echo Starting AAST Student Portal...
cd /d D:\projects\StudentPortal1\Web-and-App-main
docker compose up -d
timeout /t 60 /nobreak
echo.
echo Ready:
echo   Web:      http://localhost:3000
echo   API:      http://localhost:5000/swagger
echo   Keycloak: http://localhost:8080
echo   student.one / hazem123
pause
