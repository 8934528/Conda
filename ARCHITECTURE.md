# Architecture and System Design

Cobra is built using a modern decoupled architecture to ensure performance and maintainability.

## Technology Stack

- **Frontend**: WPF (Windows Presentation Foundation) for a rich desktop experience.
- **Editor Core**: WebView2 with Monaco Editor for high-performance code editing.
- **Game Core**: Python integration with an internal C# ECS (Entity Component System).
- **Styling**: Vanilla XAML with modern resource dictionaries.

## Key Components

### 1. Settings System (`Cobra.Core.Settings`)

- **SettingsModel**: A serializable POCO class holding all system configurations.
- **SettingsManager**: A singleton that manages persistence (`settings.json`) and notifies the system of changes via the `SettingsUpdated` event.

### 2. Engine Core (`Cobra.Engine`)

- **ECS (Entity Component System)**: Handles game objects (Entities), data (Components), and logic (Systems).
- **Physics**: Integration with physics backends like Pymunk.
- **Visual Scripting**: A node-based graph system that serializes to JSON.

### 3. UI Framework (`Cobra.UI.Views`)

- **MainWindow**: The primary dashboard and project hub.
- **EditorView**: The main workspace containing the Code, Scene, and Scripting editors.
- **SettingsView**: A comprehensive configuration interface bound directly to the `SettingsManager`.

## Data Flow

1. User modifies a setting in `SettingsView`.
2. `SettingsView` updates `SettingsManager.Instance.CurrentSettings`.
3. Upon "Apply", `SettingsManager` saves to disk and fires `SettingsUpdated`.
4. `MainWindow` and `EditorView` receive the event and refresh their UI (e.g., swapping background colors or editor fonts).
