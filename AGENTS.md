# Agent Guidelines for EditorDbf

This document provides instructions for agentic coding assistants working on the EditorDbf project.

## 1. Essential Commands

### Build and Execution
- **Build Project**: `dotnet build`
- **Run Application**: `dotnet run --project EditorDbf.App`
- **Clean Artifacts**: `dotnet clean`
- **Publish**: `dotnet publish`

### Testing
- **Automated Tests**: Execute `dotnet test` from the root directory to run the xUnit test suite. Ensure all tests pass before submitting changes.
- **Manual Verification**: Should still be done to verify UI behavior and theme consistency.

## 2. Code Style and Conventions

### General Architecture
- **Pattern**: Strict MVVM (Model-View-ViewModel).
- **Framework**: .NET 10 (`net10.0-windows`), C# 13.
- **UI**: WPF (Windows Presentation Foundation). Avoid external UI libraries; use custom styles in `EditorDbf.App/Themes/`.

### Naming Conventions
- **Classes/Methods**: PascalCase (Standard C#).
- **Private Fields**: camelCase (typically prefixed with `_` for class-level fields).
- **Local Variables**: camelCase.
- **UI Resources**: Brushes in theme files use descriptive keys (e.g., `AppBackgroundBrush`, `AccentBrush`).

### Imports and Formatting
- Use standard C# 13 formatting.
- Organize imports logically; avoid unnecessary namespace pollution.
- Keep XAML clean and use `DynamicResource` for all theme-related colors.

### MVVM Implementation
- **ViewModels**: Must inherit from `ObservableObject`.
- **Commands**: Use `RelayCommand`. Ensure `CanExecute` predicates are correctly implemented and `RaiseCanExecuteChanged()` is called when triggers change.
- **Views**: XAML files should contain minimal logic. Behavioral logic must reside in the ViewModel.

### Types and Data Handling
- **DBF Mapping**: Use `DotNetDBF`. Follow these mappings:
  - `Logical` -> `bool`
  - `Date` -> `DateTime`
  - `Numeric` -> `decimal`
- **Encoding**: Use ANSI code pages with fallback to `CP1252`.
- **Dates**: Hardcoded format is `dd/MM/yyyy` across the application.

### Error Handling and Localizations
- **Language**: All UI text, labels, and error messages MUST be in **Spanish**.
- **Exceptions**: Use descriptive error dialogs for users. Do not let the application crash silently.

### Theme System
- Themes are managed via `LightTheme.xaml` and `DarkTheme.xaml`.
- To add a new color:
  1. Define the key in `LightTheme.xaml`.
  2. Define the same key in `DarkTheme.xaml`.
  3. Reference via `DynamicResource` in `ControlStyles.xaml` or specific Views.
- Theme switching is handled in `MainWindow.xaml.cs::ApplyTheme()` by replacing `Application.Current.Resources.MergedDictionaries[0]`.

### Persistence
- Connection profiles are stored in JSON format at `%LOCALAPPDATA%\EditorDbf\connections.json`.
- Database saves must use a temporary file and an atomic move to preserve `.fpt` (memo) files.

## 3. Project Layers Reference

- **Infrastructure**: `ObservableObject`, `RelayCommand` (MVVM Base).
- **Models**: `ConnectionProfile`, `AppState`, `DbfTableDocument`, `DbfFieldDescriptor` (Data POCOs).
- **Services**: `DbfTableService`, `ConnectionRepository` (I/O and Logic).
- **ViewModels**: `MainViewModel`, `TableTabViewModel` (UI Logic).
- **Views**: `MainWindow.xaml`, `App.xaml` (XAML).
