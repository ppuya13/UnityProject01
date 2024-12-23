@echo off
set SOURCE_DIR=I:\Popol\Popol Project 01\Server
set BUILD_DIR=I:\Popol\Build\Server

cd %SOURCE_DIR%
go build -o main.exe main.go

if not exist "%BUILD_DIR%" (
    mkdir "%BUILD_DIR%"
)

copy main.exe "%BUILD_DIR%\main.exe"
echo Build completed and copied to %BUILD_DIR%
pause