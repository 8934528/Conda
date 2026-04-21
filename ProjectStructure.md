# Project Structure

    Conda/
    ├── Conda.sln
    │
    └── Conda/
        ├── Core/
        │   ├── ProjectSystem/             # Create/open/manage projects
        │   │   ├── ProjectCreator.cs
        │   │   ├── ProjectManager.cs
        │   │   ├── ProjectModel.cs
        │   │   └── ProjectTemplate.cs
        │   ├── FileSystem/                # File + folder operations
        │   ├── Environment/               # Python venv handling
        │   │   └── PythonService.cs
        │   ├── Templates/                 # Project templates (base files)
        │   │   └── BasicPythonGame/
        │   │       ├── assets/
        │   │       ├── .gitignore
        │   │       ├── main.py
        │   │       ├── README.md
        │   │       └── requirements.txt
        │   └── Settings/                  # App-level settings
        │
        ├── Engine/                        # Python game engine support
        │   ├── Runtimes/                  # Pygame/Arcade runners
        │   ├── Builders/                  # Build/export logic
        │   └── Bridges/                   # C# ↔ Python communication
        │
        ├── Editor/
        │   ├── FileItem.cs
        │   └── FileNode.cs
        │
        ├── Utils/
        ├── Config/
        ├── Logs/
        ├── Projects/
        │
        ├── UI/
        │   ├── Converters/
        │   │   ├── Converters.xaml
        │   │   └── BoolToVisibilityConverter.cs
        │   ├── Views/
        │   │   ├── EditorView.xaml
        │   │   ├── EditorView.xaml.cs
        │   │   ├── MainWindow.xaml
        │   │   ├── MainWindow.xaml.cs
        │   │   ├── ProjectView.xaml
        │   │   └── ProjectView.xaml.cs
        │   ├── Controls/
        │   └── Services/
        │
        ├── Assets/
        │   ├── monaco.html
        │   └── logo/
        │       └── FirstIcon.png
        │
        ├── App.xaml
        └── App.xaml.cs

## Future-proof (without complexity)

Plugins
AI tools
Visual scripting
Export systems

Without breaking structure.
