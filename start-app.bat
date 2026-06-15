@echo off
title Credit Card Rewards Optimizer
echo ===================================================
echo   Starting Credit Card Rewards Optimizer Server...
echo   Database: SQLite (rewards.db)
echo ===================================================
echo.

:: Launch default browser to show the app
echo Opening browser to http://localhost:5044 ...
start "" "http://localhost:5044"

:: Start the C# API server
cd /d "%~dp0"
dotnet run --project CreditCardRewards.Api/CreditCardRewards.Api.csproj

pause
