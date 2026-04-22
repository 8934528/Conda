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
        │   ├── SceneSystem/
        │   │   ├── Scene.cs
        │   │   └── SceneObject.cs
        │   ├── VisualScripting/
        │   │   └── Node.cs
        │   ├── Components/
        │   │   ├── Transform.cs
        │   │   ├── Sprite.cs
        │   │   ├── ScriptComponent.cs
        │   │   └── Component.cs
        |   |
        │   ├── Runtimes/                  # Pygame/Arcade runners
        │   ├── Builders/                  # Build/export logic
        │   ├── Bridges/                   # C# ↔ Python communication
        |   |
        │   └── GameObject.cs
        │
        ├── Editor/
        │   ├── FileItem.cs
        │   └── FileNode.cs
        │
        ├── Docs/
        ├── Utils/
        ├── Config/
        ├── Logs/
        ├── Projects/

        ├── .env/                           # must be created in this dir
        │
        ├── UI/
        │   ├── Converters/
        │   │   ├── Converters.xaml
        │   │   └── BoolToVisibilityConverter.cs
        │   ├── Views/
        │   │   ├── AnimatedModal.xaml
        │   │   ├── AnimatedModal.xaml.cs
        │   │   ├── CustomDialog.xaml
        │   │   ├── CustomDialog.xaml.cs
        │   │   ├── EditorView.xaml
        │   │   ├── EditorView.xaml.cs
        │   │   ├── MainWindow.xaml
        │   │   ├── MainWindow.xaml.cs
        │   │   ├── SettingsView.xaml
        │   │   └── SettingsView.xaml.cs
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
