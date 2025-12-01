@echo off
cd /d "%~dp0"
git add .
git commit -m "Initial commit: Convert Python Flet app to C# WPF

- Create WPF project structure with MainWindow and ViewModels
- Implement Database class for SQLite operations
- Create UI with sidebar navigation and main content area
- Implement Excel file upload and processing with loading indicator
- Create DataGrid with three-state filters and column hiding
- Add statistics display (unique count, 1s count)
- Implement Settings and Dashboard popups
- Make table scrollable both horizontally and vertically"
git push -u origin main
pause

