# RFM System - Sheetil

A Windows desktop application for managing customer data with Excel file processing, built with C# WPF.

## Features

- **Excel File Processing**: Upload and process Excel files (.xlsx)
- **Data Filtering**: Three-state filters for each column (All, Only 1s, Only Empty)
- **Data Display**: Scrollable DataGrid with statistics
- **Database**: SQLite database for logging actions and search history
- **Modern UI**: Custom title bar, sidebar navigation, and popup dialogs

## Requirements

- .NET 8.0 SDK or later ([Download here](https://dotnet.microsoft.com/download))
- Windows OS
- Visual Studio 2022 or later (optional, for development)

## Installation

### Option 1: Using .NET CLI (Without Visual Studio)

1. Install .NET 8.0 SDK from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
2. Clone the repository:
   ```bash
   git clone https://github.com/mkarim1378/rfm-system.git
   cd rfm-system
   ```
3. Restore NuGet packages:
   ```bash
   dotnet restore
   ```
4. Build the project:
   ```bash
   dotnet build --configuration Release
   ```
5. Run the application:
   ```bash
   dotnet run --project RFMSystem/RFMSystem.csproj --configuration Release
   ```

### Option 2: Using Visual Studio

1. Clone the repository
2. Open `RFMSystem.sln` in Visual Studio
3. Restore NuGet packages
4. Build and run the project

### Quick Run Scripts

You can use the provided batch scripts:
- `build.bat` - Build the project in Release mode
- `run.bat` - Build and run the application
- `publish.bat` - Create a standalone executable

## Usage

1. Click "Upload/Download" button in the sidebar
2. Select an Excel file (.xlsx)
3. The file will be processed and displayed in a table
4. Use the three-state filters to filter data:
   - Left button: Show only empty values
   - Middle button: Show all values
   - Right button: Show only "1" values
5. Columns with active filters (left or right) will be hidden from the table but still filter the data

## Project Structure

- `MainWindow.xaml` - Main UI layout
- `MainWindow.xaml.cs` - Main window logic and event handlers
- `Database.cs` - SQLite database operations
- `App.xaml` - Application resources and styles

## Technologies

- C# / WPF
- EPPlus (Excel processing)
- SQLite (Database)
- Newtonsoft.Json (JSON serialization)

## License

This project is part of the RFM System.

