@echo off
powershell -Command "Copy-Item 'C:\Project\DataTable\DataTable\ExcelTool.cs' 'C:\Project\DataTable\fmt_test\' -Force"
cd /d C:\Project\DataTable\fmt_test
dotnet build > nul 2>&1
dotnet run --no-build 2>&1 | findstr /V "warning cs"
