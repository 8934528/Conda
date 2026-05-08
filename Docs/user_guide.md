# User Guide: Getting Started with Cobra

This guide will walk you through the primary workflows in the Cobra IDE.

## 1. Creating a New Project

- Launch Cobra and click **New Project** on the Dashboard.
- Choose a project type: **Python (Pygame)** or **JavaScript (Phaser/NPM)**.
- Cobra will automatically set up the appropriate structure, including `.gitignore` and template files.

## 2. Dependency Management

- **Python**: When `requirements.txt` is selected in the explorer, click the **Download** icon in the console toolbar to install dependencies.
- **JavaScript/NPM**: When `package.json` is selected, click the **NPM** icon to run `npm install` automatically.

## 3. Running Your Game

- Click the **Run** (Play) button in the editor.
- **Python**: Launches the game window immediately.
- **JS/NPM**: Starts a Vite dev server. Cobra will detect the server URL and prompt you to launch the game in your browser.

## 4. Expanding with Backends

- If your project needs a server, click the **Add Backend** button in the left sidebar.
- Select from **Python (Flask)**, **Node.js (Express)**, or **C# (.NET)**.
- Cobra will generate a `backend/` folder with starter code and matching configuration.

## 5. Visual Editor & Scripting

- Switch to the **Scene** tab for drag-and-drop level design.
- Use the **Inspector** to modify object properties.
- Use **Visual Scripting** (Scripting tab) for node-based logic.

## 6. Configuring the IDE

- Access **Settings** via the gear icon.
- Customize **Themes**, **Editor Fonts**, and **Game Engine** parameters like resolution and FPS.
