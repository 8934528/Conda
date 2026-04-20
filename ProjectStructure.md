# Project Structure

Conda
├── Conda.sln
│
└── Conda
    ├── Core/                     # Core logic of Conda (non-UI)
    │   ├── ProjectSystem/        # Create/open/manage projects
    │   ├── FileSystem/           # File + folder operations
    │   ├── Environment/          # Python venv handling
    │   ├── Templates/            # Project templates (base files)
    │   └── Settings/             # App-level settings
    │
    ├── Engine/                   # Python game engine support
    │   ├── Runtimes/             # Pygame/Arcade runners
    │   ├── Builders/             # Build/export logic
    │   └── Bridges/              # C# ↔ Python communication
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
    │   │
    │   ├── Views/
    │   │   ├── EditorView.xaml
    │   │   └── ProjectView.xaml
    │   │
    │   ├── Controls/
    │   └── Services/
    │
    ├── Assets/
    │   ├── monaco.html
    │   └── logo/
    │
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs
    ├── App.xaml
    └── App.xaml.cs

## Future-proof (without complexity)

Plugins
AI tools
Visual scripting
Export systems

Without breaking structure.
