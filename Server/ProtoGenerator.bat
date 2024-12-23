@echo off
REM This batch file generates Go code from the Protobuf definition

REM Set the base directory to the directory of this batch file
set BASE_DIR=%~dp0

REM Change directory to the protoc folder where messages.proto is located
cd /d "%BASE_DIR%protoc"

REM Display current directory
echo Current directory: %CD%

REM Run protoc command for C# output
echo Generating C# code from Protobuf...
protoc --csharp_out="%BASE_DIR%..\Assets\Scripts\Server" messages.proto

REM Run protoc command for Go output
echo Generating Go code from Protobuf...
protoc --go_out="%BASE_DIR%messages" --go_opt=paths=source_relative messages.proto

REM Check if the command was successful
if %ERRORLEVEL% neq 0 (
    echo Error: Failed to generate Protobuf code.
    exit /b 1
)

echo Protobuf code generation completed successfully.

REM Pause to keep the command window open (optional)
pause
