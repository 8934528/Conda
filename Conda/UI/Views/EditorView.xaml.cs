using System;
using Conda.Core.Settings;
using Conda.Engine.SceneSystem;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Conda.Editor;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Linq;
using System.Windows.Input;
using Conda.Engine.VisualScripting;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Conda.UI.Views;
using MahApps.Metro.IconPacks;

using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using DataFormats = System.Windows.DataFormats;
using Cursors = System.Windows.Input.Cursors;

using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;


namespace Conda.UI.Views
{
    public partial class EditorView : Page
    {
        private readonly string projectPath;
        private string currentFilePath = string.Empty;
        private Process? currentProcess;

        private bool isWebViewReady = false;
        private bool isVenvActive = false;
        private bool isFullScreen = false;
        private WindowState previousState;
        private Window? parentWindow;

        // Play Mode & Scene Objects
        private bool isPlaying = false;
        private Scene currentScene = new();

        // Visual Scripting
        private readonly List<Node> nodes = [];
        private Node? selectedNode;
        private readonly NodeGraph currentGraph = new();

        private Border? currentSelectedNav;
        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        // Gizmo & Camera Fields
        private SceneObject? selectedGameObject;
        private bool isDragging = false;
        private Point dragOffset;

        private double zoom = 1.0;
        private double camX = 0;
        private double camY = 0;
        private bool isPanning = false;
        private Point lastPanPoint;

        private System.Windows.Threading.DispatcherTimer gameLoop = null!;
        private const int GridSize = 20;

        // GIZMO STATE SYSTEM
        private enum GizmoMode
        {
            None,
            MoveX,
            MoveY,
            Rotate
        }

        private GizmoMode currentGizmo = GizmoMode.None;
        private Point lastMousePos;

        // ECS CORE
        private readonly Conda.Engine.ECS.World world = new();
        private readonly Conda.Engine.ECS.Systems.ScriptSystem scriptSystem = new();
        private readonly Conda.Engine.Plugins.PluginLoader pluginLoader = new();





        public class OpenTab
        {
            public string FilePath { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public bool IsDirty { get; set; } = false;
            public string Icon { get; set; } = "File";
        }

        private readonly ObservableCollection<OpenTab> openTabs = [];
        private TreeViewItem? currentContextItem;

        public EditorView(string path)
        {
            InitializeComponent();
            projectPath = path;
            FileTabs.ItemsSource = openTabs;
            Loaded += EditorView_Loaded;
            currentGraph.Nodes = nodes;
            SceneCanvas.Loaded += (s, e) => DrawGrid();
            currentSelectedNav = (Border)FindName("NavCodeEditor");
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.StateChanged += ParentWindow_StateChanged;
            }
        }

        private void ParentWindow_StateChanged(object? sender, EventArgs e)
        {
            if (parentWindow != null)
            {
                isFullScreen = parentWindow.WindowState == WindowState.Maximized && parentWindow.WindowStyle == WindowStyle.None;
            }
        }

        private async void EditorView_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView();
            LoadFiles();
            CheckVenvStatus();
            OutputConsole.Text = "Conda Editor Ready!\n";
            OutputConsole.Text += $"Project: {projectPath}\n";
            OutputConsole.Text += "Python virtual environment support enabled\n";
            OutputConsole.Text += "-----------------------------------------------------------------------------\n\n";



            InitEngine();
            CreateTestEntity();
        }


        private void InitEngine()
        {
            pluginLoader.LoadPlugins("Plugins");
            StartGameLoop();
        }

        private void CreateTestEntity()
        {
            currentScene.Objects.Add(new SceneObject 
            { 
                Name = "Test Object", 
                X = 100, 
                Y = 100, 
                Width = 100, 
                Height = 100 
            });
            RedrawScene();
        }

        private void CheckVenvStatus()
        {
            string venvPath = System.IO.Path.Combine(projectPath, "venv");
            isVenvActive = Directory.Exists(venvPath);
        }

        private async System.Threading.Tasks.Task InitializeWebView()
        {
            await CodeWebView.EnsureCoreWebView2Async();

            string htmlPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets",
                "monaco.html"
            );

            if (!File.Exists(htmlPath))
            {
                await CreateDefaultMonacoHtml(htmlPath);
            }

            CodeWebView.CoreWebView2.Navigate(new Uri(htmlPath).ToString());

            CodeWebView.CoreWebView2.NavigationCompleted += (s, args) =>
            {
                isWebViewReady = true;
            };
        }

        private static async System.Threading.Tasks.Task CreateDefaultMonacoHtml(string path)
        {
            string? directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string html = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body, html { margin: 0; padding: 0; height: 100%; width: 100%; overflow: hidden; background-color: #1e1e1e; }
        #editor { height: 100%; width: 100%; }
    </style>
    <link rel=""stylesheet"" data-name=""vs/editor/editor.main"" href=""https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs/editor/editor.main.min.css"">
</head>
<body>
    <div id=""editor""></div>
    <script src=""https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs/loader.js""></script>
    <script>
        let editor;
        require.config({ paths: { vs: 'https://cdn.jsdelivr.net/npm/monaco-editor@0.45.0/min/vs' } });
        require(['vs/editor/editor.main'], function () {
            editor = monaco.editor.create(document.getElementById('editor'), {
                value: '# Welcome to Conda IDE\n\n# Write your Python code here\n',
                language: 'python',
                theme: 'vs-dark',
                automaticLayout: true,
                fontSize: 14,
                minimap: { enabled: true }
            });
        });
        function setCode(code) { if (editor) editor.setValue(code); }
        function getCode() { return editor ? editor.getValue() : ''; }
        function setFontSize(size) { if (editor) editor.updateOptions({ fontSize: size }); }
        function undo() { if (editor) editor.trigger('keyboard', 'undo'); }
        function redo() { if (editor) editor.trigger('keyboard', 'redo'); }
        function find() { if (editor) editor.trigger('keyboard', 'actions.find'); }
        function replace() { if (editor) editor.trigger('keyboard', 'editor.action.startFindReplaceAction'); }
    </script>
</body>
</html>";
            await File.WriteAllTextAsync(path, html);
        }

        private void LoadFiles()
        {
            try
            {
                var rootNode = BuildFileTree(projectPath);
                ObservableCollection<FileNode> rootNodes = [];
                rootNodes.Add(rootNode);
                FileTree.ItemsSource = rootNodes;
            }
            catch (Exception ex)
            {
                _ = CustomDialog.ShowAsync(Window.GetWindow(this), $"Error loading files: {ex.Message}", "Error", DialogIcon.Error);
            }
        }

        private static FileNode BuildFileTree(string path)
        {
            var dirInfo = new DirectoryInfo(path);
            var node = new FileNode
            {
                Name = dirInfo.Name,
                FullPath = path,
                IsDirectory = true,
                IconKind = GetFolderIcon(),
                IsExpanded = true
            };

            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirName = Path.GetFileName(dir);
                    if (dirName != "venv" && dirName != "__pycache__")
                    {
                        var dirNode = BuildFileTree(dir);
                        node.Children.Add(dirNode);
                    }
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    var fileInfo = new FileInfo(file);
                    node.Children.Add(new FileNode
                    {
                        Name = fileInfo.Name,
                        FullPath = file,
                        IsDirectory = false,
                        IconKind = GetFileIcon(fileInfo.Extension)
                    });
                }
            }
            catch (UnauthorizedAccessException) { }

            return node;
        }

        private static string GetFolderIcon() => "Folder";
        private static string GetFileIcon(string extension)

        {
            return extension.ToLower() switch
            {
                ".py" => "FilePython",
                ".txt" => "FileDocument",
                ".md" => "FileDocument",
                ".json" => "FileCode",
                ".html" => "LanguageHtml5",
                ".css" => "LanguageCss3",
                ".js" => "LanguageJavascript",
                ".png" or ".jpg" or ".jpeg" or ".gif" => "FileImage",
                _ => "File"
            };
        }

        private void OnTreeViewPreviewRightClick(object sender, MouseButtonEventArgs e)
        {
            var hitTestResult = VisualTreeHelper.HitTest(FileTree, e.GetPosition(FileTree));
            if (hitTestResult?.VisualHit != null)
            {
                var treeViewItem = FindParent<TreeViewItem>(hitTestResult.VisualHit);
                if (treeViewItem == null) return;

                if (treeViewItem.DataContext is FileNode)
                {
                    currentContextItem = treeViewItem;
                    treeViewItem.IsSelected = true;
                    var contextMenu = (ContextMenu)FindResource("FileContextMenu");
                    contextMenu.PlacementTarget = FileTree;
                    contextMenu.IsOpen = true;
                    e.Handled = true;
                }
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void OnOpenFileClick(object sender, RoutedEventArgs e)
        {
            if (currentContextItem?.DataContext is FileNode node && !node.IsDirectory)
            {
                OnShowCodeEditor(null!, null!);
                OpenFileInTab(node.FullPath);
            }
        }

        private async void OnNewFileClick(object sender, RoutedEventArgs e)
        {
            if (currentContextItem?.DataContext is FileNode node)
            {
                string parentPath = node.IsDirectory ? node.FullPath : System.IO.Path.GetDirectoryName(node.FullPath)!;
                await CreateNewFileAsync(parentPath);
            }
        }

        private async void OnNewFolderClick(object sender, RoutedEventArgs e)
        {
            if (currentContextItem?.DataContext is FileNode node)
            {
                string parentPath = node.IsDirectory ? node.FullPath : System.IO.Path.GetDirectoryName(node.FullPath)!;
                string folderName = Microsoft.VisualBasic.Interaction.InputBox("Enter folder name:", "New Folder", "NewFolder");
                if (!string.IsNullOrWhiteSpace(folderName))
                {
                    string folderPath = System.IO.Path.Combine(parentPath, folderName);
                    Directory.CreateDirectory(folderPath);
                    LoadFiles();
                    ShowToast($"Created folder: {folderName}");
                    OutputConsole.Text += $"Created folder: {folderName}\n";
                }
            }
            await Task.CompletedTask;
        }

        private async void OnRenameClick(object sender, RoutedEventArgs e)
        {
            if (currentContextItem?.DataContext is FileNode node)
            {
                string newName = Microsoft.VisualBasic.Interaction.InputBox("Enter new name:", "Rename", node.Name);
                if (!string.IsNullOrWhiteSpace(newName) && newName != node.Name)
                {
                    string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(node.FullPath)!, newName);
                    try
                    {
                        if (node.IsDirectory)
                            Directory.Move(node.FullPath, newPath);
                        else
                            File.Move(node.FullPath, newPath);
                        LoadFiles();
                        ShowToast($"Renamed: {node.Name} → {newName}");
                        OutputConsole.Text += $"Renamed: {node.Name} → {newName}\n";
                    }
                    catch (Exception ex)
                    {
                        await CustomDialog.ShowAsync(Window.GetWindow(this), $"Error renaming: {ex.Message}", "Error", DialogIcon.Error);
                    }
                }
            }
        }

        private async void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (currentContextItem?.DataContext is FileNode node)
            {
                var result = await CustomDialog.ShowAsync(
                    Window.GetWindow(this),
                    $"Are you sure you want to delete {node.Name}?",
                    "Confirm Delete",
                    DialogIcon.Warning,
                    "Yes",
                    "No");

                if (result)
                {
                    try
                    {
                        if (node.IsDirectory)
                            Directory.Delete(node.FullPath, true);
                        else
                            File.Delete(node.FullPath);
                        LoadFiles();
                        ShowToast($"Deleted: {node.Name}");
                        OutputConsole.Text += $"Deleted: {node.Name}\n";
                    }
                    catch (Exception ex)
                    {
                        await CustomDialog.ShowAsync(Window.GetWindow(this), $"Error deleting: {ex.Message}", "Error", DialogIcon.Error);
                    }
                }
            }
        }

        private void OnCopyPathClick(object sender, RoutedEventArgs e)
        {
            if (currentContextItem?.DataContext is FileNode node)
            {
                System.Windows.Clipboard.SetText(node.FullPath);
                ShowToast("Path copied to clipboard");
            }
        }

        private void OnRevealInExplorerClick(object sender, RoutedEventArgs e)
        {
            if (currentContextItem?.DataContext is FileNode node)
            {
                try
                {
                    if (node.IsDirectory)
                        Process.Start("explorer.exe", node.FullPath);
                    else
                        Process.Start("explorer.exe", $"/select,\"{node.FullPath}\"");
                }
                catch (Exception ex)
                {
                    OutputConsole.Text += $"Error opening explorer: {ex.Message}\n";
                }
            }
        }

        private void OnTreeViewDragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                e.Effects = System.Windows.DragDropEffects.Move;
        }

        private void OnTreeViewDrop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                foreach (var file in files)
                {
                    string destPath = System.IO.Path.Combine(projectPath, System.IO.Path.GetFileName(file));
                    if (File.Exists(file))
                    {
                        File.Copy(file, destPath, true);
                        OutputConsole.Text += $"Copied file: {System.IO.Path.GetFileName(file)}\n";
                    }
                }
                LoadFiles();
            }
        }

        private async Task CreateNewFileAsync(string parentPath)
        {
            string fileName = Microsoft.VisualBasic.Interaction.InputBox("Enter file name:", "New File", "newfile.py");
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string filePath = System.IO.Path.Combine(parentPath, fileName);
                File.WriteAllText(filePath, "");
                LoadFiles();
                OpenFileInTab(filePath);
                ShowToast($"Created file: {fileName}");
                OutputConsole.Text += $"Created file: {fileName}\n";
            }
            await Task.CompletedTask;
        }

        private void OnFileTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileNode node && node != null && !node.IsDirectory)
            {
                OnShowCodeEditor(null!, null!);
                OpenFileInTab(node.FullPath);
            }
        }

        private void OpenFileInTab(string filePath)
        {
            var existingTab = openTabs.FirstOrDefault(t => t.FilePath == filePath);
            if (existingTab != null)
            {
                FileTabs.SelectedItem = existingTab;
                return;
            }

            string content = File.ReadAllText(filePath);
            var newTab = new OpenTab
            {
                FilePath = filePath,
                Name = System.IO.Path.GetFileName(filePath),
                Content = content,
                Icon = GetFileIcon(System.IO.Path.GetExtension(filePath))
            };
            openTabs.Add(newTab);
            FileTabs.SelectedItem = newTab;
            OutputConsole.Text += $"Opened: {newTab.Name}\n";
        }

        private async void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileTabs.SelectedItem is OpenTab tab)
            {
                currentFilePath = tab.FilePath;
                if (isWebViewReady && CodeWebView?.CoreWebView2 != null)
                {
                    string escapedContent = EscapeJs(tab.Content);
                    await CodeWebView.CoreWebView2.ExecuteScriptAsync($"setCode(`{escapedContent}`)");
                }
            }
        }

        private async void OnCloseTabClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OpenTab tab)

            {
                if (tab.IsDirty)
                {
                    var result = await CustomDialog.ShowAsync(
                        Window.GetWindow(this),
                        $"Save changes to {tab.Name}?",
                        "Unsaved Changes",
                        DialogIcon.Question,
                        "Yes",
                        "No");

                    if (result)
                    {
                        await SaveCurrentFile();
                    }
                }
                openTabs.Remove(tab);
                OutputConsole.Text += $"Closed: {tab.Name}\n";
                if (openTabs.Count > 0)
                    FileTabs.SelectedItem = openTabs.Last();
                else
                    currentFilePath = string.Empty;
            }
        }

        private async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            await SaveCurrentFile();
        }

        private async System.Threading.Tasks.Task SaveCurrentFile()
        {
            if (FileTabs.SelectedItem is OpenTab tab)
            {
                try
                {
                    if (!isWebViewReady || CodeWebView?.CoreWebView2 == null)
                    {
                        await CustomDialog.ShowAsync(Window.GetWindow(this), "Editor is not ready yet.", "Error", DialogIcon.Error);
                        return;
                    }

                    string code = await CodeWebView.CoreWebView2.ExecuteScriptAsync("getCode()");
                    code = UnescapeJs(code);
                    File.WriteAllText(tab.FilePath, code);
                    tab.Content = code;
                    tab.IsDirty = false;
                    ShowToast($"Saved: {System.IO.Path.GetFileName(tab.FilePath)}");
                    OutputConsole.Text += $"Saved: {System.IO.Path.GetFileName(tab.FilePath)}\n";
                }
                catch (Exception ex)
                {
                    await CustomDialog.ShowAsync(Window.GetWindow(this), $"Error saving file: {ex.Message}", "Error", DialogIcon.Error);
                }
            }
        }

        private async void OnRunClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save current file first
                await SaveCurrentFile();

                string mainFile = System.IO.Path.Combine(projectPath, "main.py");
                if (!File.Exists(mainFile))
                {
                    await CustomDialog.ShowAsync(Window.GetWindow(this), "main.py not found.", "Error", DialogIcon.Error);
                    return;
                }

                OutputConsole.Text += "\nRunning game...\n";
                string pythonPath = GetPythonPath() ?? "python";

                ProcessStartInfo psi = new()
                {
                    FileName = pythonPath,
                    Arguments = $"\"{mainFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = projectPath
                };

                currentProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
                currentProcess.OutputDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        Dispatcher.Invoke(() => OutputConsole.Text += args.Data + "\n");
                };
                currentProcess.ErrorDataReceived += (s, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        Dispatcher.Invoke(() => OutputConsole.Text += "Error: " + args.Data + "\n");
                };
                currentProcess.Exited += (s, args) =>
                {
                    Dispatcher.Invoke(() => OutputConsole.Text += $"\nProcess exited with code: {currentProcess.ExitCode}\n");
                    Dispatcher.Invoke(() => OutputConsole.Text += "----------------------------------------------------------------------------------------\n\n");
                };

                currentProcess.Start();
                currentProcess.BeginOutputReadLine();
                currentProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                await CustomDialog.ShowAsync(Window.GetWindow(this), $"Error: {ex.Message}", "Error", DialogIcon.Error);
            }
        }

        private void OnStopClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentProcess != null && !currentProcess.HasExited)
                {
                    currentProcess.Kill();
                    OutputConsole.Text += "\nProcess stopped.\n";
                }
            }
            catch (Exception ex)
            {
                OutputConsole.Text += $"\nError: {ex.Message}\n";
            }
        }

        private void OnCloseFolderClicked(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window?.Close();
        }

        private string? GetPythonPath()
        {
            string windowsPath = System.IO.Path.Combine(projectPath, "venv", "Scripts", "python.exe");
            string unixPath = System.IO.Path.Combine(projectPath, "venv", "bin", "python");

            if (File.Exists(windowsPath)) return windowsPath;
            if (File.Exists(unixPath)) return unixPath;

            return null;
        }

        private static string EscapeJs(string text)
        {
            return text.Replace("\\", "\\\\").Replace("`", "\\`").Replace("$", "\\$");
        }

        private static string UnescapeJs(string text)
        {
            if (text.StartsWith('"') && text.EndsWith('"'))
                text = text[1..^1];

            return text.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"");
        }

        // Menu Handlers
        private async void OnNewFileMenuClick(object sender, RoutedEventArgs e)
        {
            await CreateNewFileAsync(projectPath);
        }

        private void OnSaveAllClicked(object sender, RoutedEventArgs e)
        {
            foreach (var tab in openTabs)
            {
                if (tab.IsDirty)
                {
                    File.WriteAllText(tab.FilePath, tab.Content);
                    tab.IsDirty = false;
                }
            }
            OutputConsole.Text += "Saved all files\n";
        }

        private void OnUndoClicked(object sender, RoutedEventArgs e)
        {
            _ = CodeWebView.CoreWebView2?.ExecuteScriptAsync("undo()");
        }

        private void OnRedoClicked(object sender, RoutedEventArgs e)
        {
            _ = CodeWebView.CoreWebView2?.ExecuteScriptAsync("redo()");
        }

        private void OnFindClicked(object sender, RoutedEventArgs e)
        {
            _ = CodeWebView.CoreWebView2?.ExecuteScriptAsync("find()");
        }

        private void OnReplaceClicked(object sender, RoutedEventArgs e)
        {
            _ = CodeWebView.CoreWebView2?.ExecuteScriptAsync("replace()");
        }

        private void OnToggleFullScreenClicked(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
        }

        private void OnZoomInClicked(object sender, RoutedEventArgs e)
        {
            _ = CodeWebView.CoreWebView2?.ExecuteScriptAsync("setFontSize(16)");
        }

        private void OnZoomOutClicked(object sender, RoutedEventArgs e)
        {
            _ = CodeWebView.CoreWebView2?.ExecuteScriptAsync("setFontSize(12)");
        }

        private void OnDebugClicked(object sender, RoutedEventArgs e)
        {
            OutputConsole.Text += "Debug mode - Add breakpoints to debug your code\n";
        }

        private void OnNewTerminalClicked(object sender, RoutedEventArgs e)
        {
            OutputConsole.Text += "\n$> Terminal ready. Use 'python main.py' to run your game\n";
        }

        private void OnClearConsoleClicked(object sender, RoutedEventArgs e)
        {
            OutputConsole.Clear();
            OutputConsole.Text = "Console cleared.\n";
        }

        private void OnDocumentationClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "#",
                UseShellExecute = true
            });
        }

        private async void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            await CustomDialog.ShowAsync(Window.GetWindow(this),
                "Conda IDE\nVersion 1.0\n\nA Python Game Development Environment\nBuilt with WPF and WebView2",
                "About Conda IDE",
                DialogIcon.Info);
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e) => OnSettingsIconClicked(sender, e);
        private void OnPreferencesClicked(object sender, RoutedEventArgs e) => OnSettingsIconClicked(sender, e);

        private async void OnNewFileToolbarClick(object sender, RoutedEventArgs e)
        {
            await CreateNewFileAsync(projectPath);
        }

        private async void OnNewFolderToolbarClick(object sender, RoutedEventArgs e)
        {
            string folderName = Microsoft.VisualBasic.Interaction.InputBox("Enter folder name:", "New Folder", "NewFolder");
            if (!string.IsNullOrWhiteSpace(folderName))
            {
                string folderPath = System.IO.Path.Combine(projectPath, folderName);
                Directory.CreateDirectory(folderPath);
                LoadFiles();
                OutputConsole.Text += $"Created folder: {folderName}\n";
            }
            await Task.CompletedTask;
        }

        private void OnRefreshClicked(object sender, RoutedEventArgs e)
        {
            LoadFiles();
            OutputConsole.Text += "File explorer refreshed\n";
        }

        // Updated Menu Handlers with CustomDialog
        private async void OnNewProjectClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "New Project dialog would open here.", "New Project", DialogIcon.Info);

        private async void OnOpenProjectClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Open Project dialog would open here.", "Open Project", DialogIcon.Info);

        private void OnExitClicked(object sender, RoutedEventArgs e)
            => System.Windows.Application.Current.Shutdown();



        private async void OnResetLayoutClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Layout reset.", "Reset Layout", DialogIcon.Info);

        private async void OnRecentProjectsClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Recent projects list would appear here.", "Recent Projects", DialogIcon.Info);

        private void OnProjectSettingsClicked(object sender, RoutedEventArgs e)
        {
            var folderSettings = new FolderSettings(projectPath);
            var settingsView = new SettingsView(folderSettings);
            var settingsWindow = new Window
            {
                Title = $"Project Settings - {Path.GetFileName(projectPath)}",
                Content = settingsView,
                Width = 1200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };
            settingsWindow.ShowDialog();
        }

        private async void OnBuildClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Build process would start here.", "Build", DialogIcon.Info);

        private async void OnExportClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Export options would appear here.", "Export", DialogIcon.Info);

        private async void OnPackageManagerClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Package manager would open here.", "Package Manager", DialogIcon.Info);

        private async void OnExtensionsClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Extensions manager would open here.", "Extensions", DialogIcon.Info);

        private async void OnOpenTerminalClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Terminal would open here.", "Terminal", DialogIcon.Info);

        private async void OnRunCommandClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Command runner would open here.", "Run Command", DialogIcon.Info);



        private void OnSettingsIconClicked(object sender, RoutedEventArgs e)
        {
            var settingsView = new SettingsView();
            var settingsWindow = new Window
            {
                Title = "Conda IDE - Settings",
                Content = settingsView,
                Width = 1200,
                Height = 800,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };
            settingsWindow.ShowDialog();
        }



        private void OnRenameGameObject(object sender, RoutedEventArgs e)
        {
            if (selectedGameObject != null)
            {
                var renamePanel = new StackPanel();
                renamePanel.Children.Add(new TextBlock
                {
                    Text = "Enter new name:",
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                var nameBox = new TextBox
                {
                    Text = selectedGameObject.Name,
                    Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(5)
                };
                renamePanel.Children.Add(nameBox);

                _ = CustomDialog.ShowCustomAsync(Window.GetWindow(this), "Rename Object", renamePanel, "Rename", "Cancel");
            }
        }



        private void ToggleFullScreen()
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                if (!isFullScreen)
                {
                    previousState = window.WindowState;
                    window.WindowStyle = WindowStyle.None;
                    window.WindowState = WindowState.Maximized;
                    isFullScreen = true;
                }
                else
                {
                    window.WindowStyle = WindowStyle.SingleBorderWindow;
                    window.WindowState = previousState;
                    isFullScreen = false;
                }
            }
        }

        // --- SCENE EDITOR LOGIC ---

        private void NavItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border clickedNav)
            {
                string? tag = clickedNav.Tag?.ToString();
                if (string.IsNullOrEmpty(tag)) return;

                // Reset previous selection
                if (currentSelectedNav != null)
                {
                    currentSelectedNav.Background = Brushes.Transparent;
                    if (currentSelectedNav.Child is StackPanel sp)
                    {
                        foreach (var child in sp.Children.OfType<StackPanel>())
                        {
                            foreach (var textBlock in child.Children.OfType<TextBlock>())
                            {
                                if (textBlock.FontSize == 14) // Title text
                                    textBlock.Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                            }
                        }
                    }
                }

                // Highlight new selection
                clickedNav.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                if (clickedNav.Child is StackPanel newSp)
                {
                    foreach (var child in newSp.Children.OfType<StackPanel>())
                    {
                        foreach (var textBlock in child.Children.OfType<TextBlock>())
                        {
                            if (textBlock.FontSize == 14) // Title text
                                textBlock.Foreground = Brushes.White;
                        }
                    }
                }
                currentSelectedNav = clickedNav;

                // Switch view based on tag
                switch (tag)
                {
                    case "CodeEditor":
                        OnShowCodeEditor(null!, null!);
                        break;
                    case "SceneEditor":
                        OnShowSceneEditor(null!, null!);
                        break;
                    case "VisualScripting":
                        OnShowVisualScripting(null!, null!);
                        break;
                }
            }
        }

        private void OnShowCodeEditor(object sender, RoutedEventArgs e)
        {
            CodeEditorContainer.Visibility = Visibility.Visible;
            SceneEditorContainer.Visibility = Visibility.Collapsed;
            VisualScriptingContainer.Visibility = Visibility.Collapsed;
            
            ExplorerContainer.Visibility = Visibility.Visible;
            InspectorContainer.Visibility = Visibility.Collapsed;
        }

        private void OnShowSceneEditor(object sender, RoutedEventArgs e)
        {
            CodeEditorContainer.Visibility = Visibility.Collapsed;
            SceneEditorContainer.Visibility = Visibility.Visible;
            VisualScriptingContainer.Visibility = Visibility.Collapsed;
            
            ExplorerContainer.Visibility = Visibility.Collapsed;
            InspectorContainer.Visibility = Visibility.Visible;

            RefreshHierarchy();
        }

        private void OnShowVisualScripting(object sender, RoutedEventArgs e)
        {
            CodeEditorContainer.Visibility = Visibility.Collapsed;
            SceneEditorContainer.Visibility = Visibility.Collapsed;
            VisualScriptingContainer.Visibility = Visibility.Visible;
            
            ExplorerContainer.Visibility = Visibility.Visible;
            InspectorContainer.Visibility = Visibility.Collapsed;
        }

        private void OnPlayToggle(object sender, RoutedEventArgs e)
        {
            isPlaying = !isPlaying;

            if (isPlaying)
            {
                PlayToggle.Content = "Stop";
                PlayToggle.Background = Brushes.Red;
                // Start logic if needed
            }
            else
            {
                PlayToggle.Content = "Play";
                PlayToggle.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // #28a745
            }
        }

        private void StartGameLoop()
        {
            gameLoop = new()
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            gameLoop.Tick += (s, e) =>
            {
                if (isPlaying)
                {
                    Conda.Engine.ECS.Systems.ScriptSystem.Update(world);
                }

                foreach (var plugin in pluginLoader.Plugins)
                {
                    plugin.Update();
                }

                RedrawScene();
            };
            gameLoop.Start();
        }

        private void OnAddGameObject(object sender, RoutedEventArgs e)
        {
            AddGameObject();
        }

        private void OnSavePrefab(object sender, RoutedEventArgs e)
        {
            // TODO: Implement prefab saving
            ShowToast("Save Prefab not implemented yet");
        }

        private void OnLoadPrefab(object sender, RoutedEventArgs e)
        {
            // TODO: Implement prefab loading
            ShowToast("Load Prefab not implemented yet");
        }

        private void GameLoop_Tick(object? sender, EventArgs e)
        {
            if (!isPlaying) return;

            foreach (var obj in currentScene.Objects)
            {
                RunPython(obj);
            }

            RedrawScene();
        }

        private static void RunPython(SceneObject obj)
        {
            if (string.IsNullOrEmpty(obj.ScriptPath) || !File.Exists(obj.ScriptPath))
                return;

            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = "python",
                    Arguments = $"\"{obj.ScriptPath}\" {obj.X} {obj.Y}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var parts = output.Split(',');

                if (parts.Length == 2)
                {
                    if (double.TryParse(parts[0], out double newX)) obj.X = newX;
                    if (double.TryParse(parts[1], out double newY)) obj.Y = newY;
                }
            }
            catch { }
        }

        private void AddGameObject()
        {
            var go = new SceneObject { Name = "GameObject_" + (currentScene.Objects.Count + 1), X = 100, Y = 100 };
            currentScene.Objects.Add(go);
            RefreshHierarchy();
            RedrawScene();
        }

        private void UpdateInspector(SceneObject obj)
        {
            InspectorPanel.Children.Clear();

            InspectorPanel.Children.Add(new TextBlock { Text = obj.Name, Foreground = Brushes.Cyan, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });

            AddField("Name", obj.Name, v => { obj.Name = v; RefreshHierarchy(); });
            AddField("X", obj.X.ToString("F1"), v => { if (double.TryParse(v, out double res)) { obj.X = res; RedrawScene(); } });
            AddField("Y", obj.Y.ToString("F1"), v => { if (double.TryParse(v, out double res)) { obj.Y = res; RedrawScene(); } });
            AddField("Width", obj.Width.ToString("F1"), v => { if (double.TryParse(v, out double res)) { obj.Width = res; RedrawScene(); } });
            AddField("Height", obj.Height.ToString("F1"), v => { if (double.TryParse(v, out double res)) { obj.Height = res; RedrawScene(); } });
            AddField("Sprite Path", obj.SpritePath, v => { obj.SpritePath = v; RedrawScene(); });
            AddField("Script Path", obj.ScriptPath, v => { obj.ScriptPath = v; });
        }

        // --- Visual Scripting Methods ---

        private void AddNode(string title, double x, double y)
        {
            var node = new Node { Title = title, X = x, Y = y };
            nodes.Add(node);

            var border = new Border
            {
                Width = 150,
                Height = 80,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                CornerRadius = new CornerRadius(5),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = title,
                    Foreground = Brushes.White,
                    Margin = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                Tag = node
            };

            Canvas.SetLeft(border, x);
            Canvas.SetTop(border, y);

            border.MouseLeftButtonDown += (s, e) => OnNodeClicked(border);
            NodeCanvas.Children.Add(border);

            EnableNodeDrag(border);
        }

        private void EnableNodeDrag(Border nodeUI)
        {
            nodeUI.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    var pos = e.GetPosition(NodeCanvas);
                    var node = (Node)nodeUI.Tag;

                    node.X = pos.X - (nodeUI.Width / 2);
                    node.Y = pos.Y - (nodeUI.Height / 2);

                    Canvas.SetLeft(nodeUI, node.X);
                    Canvas.SetTop(nodeUI, node.Y);

                    // Draw connections would go here
                }
            };
        }

        private void OnNodeClicked(Border nodeUI)
        {
            var node = (Node)nodeUI.Tag;

            if (selectedNode == null)
            {
                selectedNode = node;
                nodeUI.BorderBrush = Brushes.Yellow;
            }
            else
            {
                if (selectedNode != node)
                {
                    selectedNode.Outputs.Add(node.Id);
                    // Reset selection or handle multi-connection
                    ShowToast($"Connected {selectedNode.Title} to {node.Title}");
                }

                // Clear selection highlight
                foreach (var child in NodeCanvas.Children.OfType<Border>())
                    child.BorderBrush = Brushes.Gray;

                selectedNode = null;
            }
        }

        private string ExportNodes()
        {
            return JsonSerializer.Serialize(nodes, JsonIndentedOptions);
        }

        private void OnAssetDropped(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                if (!file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                    continue;

                CreateSpriteFromImage(file);
            }
        }

        private void CreateSpriteFromImage(string path)
        {
            var go = new SceneObject 
            { 
                Name = System.IO.Path.GetFileNameWithoutExtension(path),
                X = 200, 
                Y = 200,
                SpritePath = path
            };

            currentScene.Objects.Add(go);
            RefreshHierarchy();
            RedrawScene();
        }



        private static bool IsImageFile(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLower();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif";
        }

        private void OnHierarchySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SceneHierarchy.SelectedItem is SceneObject obj)
            {
                foreach (var item in currentScene.Objects) item.IsSelected = false;
                obj.IsSelected = true;
                selectedGameObject = obj;
                UpdateInspector(obj);
                RedrawScene();
            }
        }

        private void RedrawScene()
        {
            SceneCanvas.Children.Clear();

            // Draw ECS World
            Conda.Engine.ECS.Systems.RenderSystem.Render(world, SceneCanvas);

            // Draw Legacy SceneObjects (if any)
            foreach (var obj in currentScene.Objects)
            {
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = obj.Width * zoom,
                    Height = obj.Height * zoom,
                    Fill = Brushes.White,
                    RenderTransform = new RotateTransform(
                        obj.Rotation,
                        (obj.Width * zoom) / 2,
                        (obj.Height * zoom) / 2
                    )
                };

                Canvas.SetLeft(rect, obj.X * zoom + camX);
                Canvas.SetTop(rect, obj.Y * zoom + camY);

                SceneCanvas.Children.Add(rect);

                if (obj.IsSelected)
                {
                    DrawMoveGizmo(obj);
                    DrawRotationGizmo(obj);
                }
            }
        }

        private void SceneCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point click = e.GetPosition(SceneCanvas);
            lastMousePos = click;

            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                isPanning = true;
                lastPanPoint = click;
                return;
            }

            if (selectedGameObject != null)
            {
                double cx = selectedGameObject.X * zoom + camX + (selectedGameObject.Width * zoom) / 2;
                double cy = selectedGameObject.Y * zoom + camY + (selectedGameObject.Height * zoom) / 2;

                // X Axis hit
                if (DistanceToLine(click, cx, cy, cx + 40, cy) < 5)
                {
                    currentGizmo = GizmoMode.MoveX;
                    return;
                }

                // Y Axis hit
                if (DistanceToLine(click, cx, cy, cx, cy - 40) < 5)
                {
                    currentGizmo = GizmoMode.MoveY;
                    return;
                }

                // Rotation hit (near circle)
                double dist = Math.Sqrt(Math.Pow(click.X - cx, 2) + Math.Pow(click.Y - cy, 2));
                if (Math.Abs(dist - 40) < 5)
                {
                    currentGizmo = GizmoMode.Rotate;
                    return;
                }
            }

            // Normal selection hit detection
            foreach (var obj in currentScene.Objects)
            {
                double screenX = obj.X * zoom + camX;
                double screenY = obj.Y * zoom + camY;
                double screenW = obj.Width * zoom;
                double screenH = obj.Height * zoom;

                if (click.X >= screenX && click.X <= screenX + screenW &&
                    click.Y >= screenY && click.Y <= screenY + screenH)
                {
                    foreach (var other in currentScene.Objects) other.IsSelected = false;
                    selectedGameObject = obj;
                    obj.IsSelected = true;
                    currentGizmo = GizmoMode.None;
                    
                    dragOffset = new Point(click.X - screenX, click.Y - screenY);
                    isDragging = true;

                    RedrawScene();
                    RefreshHierarchy();
                    return;
                }
            }

            selectedGameObject = null;
            currentGizmo = GizmoMode.None;
            foreach (var obj in currentScene.Objects) obj.IsSelected = false;
            RedrawScene();
        }

        private void SceneCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point current = e.GetPosition(SceneCanvas);
            double dx = current.X - lastMousePos.X;
            double dy = current.Y - lastMousePos.Y;

            if (isPanning)
            {
                camX += dx;
                camY += dy;
                lastMousePos = current;
                RedrawScene();
                return;
            }

            if (selectedGameObject == null) return;

            switch (currentGizmo)
            {
                case GizmoMode.MoveX:
                    selectedGameObject.X += dx / zoom;
                    break;

                case GizmoMode.MoveY:
                    selectedGameObject.Y += dy / zoom;
                    break;

                case GizmoMode.Rotate:
                    RotateObject(selectedGameObject, current);
                    break;

                case GizmoMode.None:
                    if (isDragging)
                    {
                        double newScreenX = current.X - dragOffset.X;
                        double newScreenY = current.Y - dragOffset.Y;
                        selectedGameObject.X = (newScreenX - camX) / zoom;
                        selectedGameObject.Y = (newScreenY - camY) / zoom;
                    }
                    break;
            }

            lastMousePos = current;
            RedrawScene();
        }

        private void SceneCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            isPanning = false;
        }

        private void SceneCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoomFactor = 1.1;
            if (e.Delta > 0) zoom *= zoomFactor;
            else zoom /= zoomFactor;
            RedrawScene();
        }

        private static double Snap(double value) => Math.Round(value / GridSize) * GridSize;

        private static void LoadInspector() { /* replaced by UpdateInspector(GameObject) */ }

        private void OnApplyInspector(object sender, RoutedEventArgs e)
        {
            // Inspector updates are now live via the AddField callbacks
            if (selectedGameObject != null)
                UpdateInspector(selectedGameObject);
        }

        private void RefreshHierarchy()
        {
            SceneHierarchy.ItemsSource = null;
            SceneHierarchy.ItemsSource = currentScene.Objects;
        }

        private async void OnSaveScene(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Conda Scene (*.conda)|*.conda", InitialDirectory = projectPath };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    currentScene.Save(dialog.FileName);
                    OutputConsole.Text += $"Scene saved: {System.IO.Path.GetFileName(dialog.FileName)}\n";
                    await CustomDialog.ShowAsync(Window.GetWindow(this), "Scene saved successfully!", "Success", DialogIcon.Success);
                }
                catch (Exception ex)
                {
                    await CustomDialog.ShowAsync(Window.GetWindow(this), $"Error saving scene: {ex.Message}", "Error", DialogIcon.Error);
                }
            }
        }

        private async void OnLoadScene(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Conda Scene (*.conda)|*.conda", InitialDirectory = projectPath };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    currentScene = Scene.Load(dialog.FileName);
                    SceneCanvas.Children.Clear();
                    DrawGrid();
                    RefreshHierarchy();
                    OutputConsole.Text += $"Scene loaded: {System.IO.Path.GetFileName(dialog.FileName)}\n";
                    await CustomDialog.ShowAsync(Window.GetWindow(this), "Scene loaded successfully!", "Success", DialogIcon.Success);
                }
                catch (Exception ex)
                {
                    await CustomDialog.ShowAsync(Window.GetWindow(this), $"Error loading scene: {ex.Message}", "Error", DialogIcon.Error);
                }
            }
        }

        private void DrawGrid()
        {
            double width = SceneCanvas.ActualWidth > 0 ? SceneCanvas.ActualWidth : 2000;
            double height = SceneCanvas.ActualHeight > 0 ? SceneCanvas.ActualHeight : 2000;

            for (double x = 0; x < width; x += GridSize)
            {
                var line = new System.Windows.Shapes.Line
                {
                    X1 = x, Y1 = 0, X2 = x, Y2 = height,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    StrokeThickness = 0.5
                };
                SceneCanvas.Children.Add(line);
            }

            for (double y = 0; y < height; y += GridSize)
            {
                var line = new System.Windows.Shapes.Line
                {
                    X1 = 0, Y1 = y, X2 = width, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    StrokeThickness = 0.5
                };
                SceneCanvas.Children.Add(line);
            }
        }

        private void ShowToast(string message, bool isSuccess = true)
        {
            if (FindName("ToastMessage") is TextBlock toastMessage &&
                FindName("ToastIcon") is PackIconMaterial toastIcon &&
                FindName("ToastOverlay") is Grid toastOverlay &&
                FindName("ToastTranslate") is TranslateTransform toastTranslate)
            {
                toastMessage.Text = message;
                toastIcon.Kind = isSuccess ? PackIconMaterialKind.CheckCircle : PackIconMaterialKind.CloseCircle;
                toastOverlay.Visibility = Visibility.Visible;

                DoubleAnimation slideUp = new()
                {
                    From = 100,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(400),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                DoubleAnimation fadeIn = new()
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(400)
                };

                toastTranslate.BeginAnimation(TranslateTransform.YProperty, slideUp);
                toastOverlay.BeginAnimation(OpacityProperty, fadeIn);

                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(3)
                };
                timer.Tick += (s, args) =>
                {
                    DoubleAnimation slideDown = new()
                    {
                        To = 100,
                        Duration = TimeSpan.FromMilliseconds(400),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };
                    DoubleAnimation fadeOut = new()
                    {
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(400)
                    };
                    slideDown.Completed += (s2, args2) => toastOverlay.Visibility = Visibility.Collapsed;
                    toastTranslate.BeginAnimation(TranslateTransform.YProperty, slideDown);
                    toastOverlay.BeginAnimation(OpacityProperty, fadeOut);
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private void AddField(string label, string value, Action<string> onUpdate)
        {
            var stack = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };
            stack.Children.Add(new TextBlock { Text = label, Foreground = Brushes.Gray, FontSize = 14 });
            var box = new TextBox
            {
                Text = value,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5, 2, 5, 2)
            };
            box.TextChanged += (s, e) => onUpdate(box.Text);
            stack.Children.Add(box);
            InspectorPanel.Children.Add(stack);
        }

        private void DrawMoveGizmo(SceneObject obj)
        {
            double centerX = obj.X * zoom + camX + (obj.Width * zoom) / 2;
            double centerY = obj.Y * zoom + camY + (obj.Height * zoom) / 2;

            double size = 40;

            // X Axis (RED)
            var xLine = new System.Windows.Shapes.Line
            {
                X1 = centerX,
                Y1 = centerY,
                X2 = centerX + size,
                Y2 = centerY,
                Stroke = Brushes.Red,
                StrokeThickness = 3
            };

            // Y Axis (GREEN)
            var yLine = new System.Windows.Shapes.Line
            {
                X1 = centerX,
                Y1 = centerY,
                X2 = centerX,
                Y2 = centerY - size,
                Stroke = Brushes.LimeGreen,
                StrokeThickness = 3
            };

            SceneCanvas.Children.Add(xLine);
            SceneCanvas.Children.Add(yLine);
        }

        private void DrawRotationGizmo(SceneObject obj)
        {
            double centerX = obj.X * zoom + camX + (obj.Width * zoom) / 2;
            double centerY = obj.Y * zoom + camY + (obj.Height * zoom) / 2;

            var circle = new System.Windows.Shapes.Ellipse
            {
                Width = 80,
                Height = 80,
                Stroke = Brushes.Gold,
                StrokeThickness = 2
            };

            Canvas.SetLeft(circle, centerX - 40);
            Canvas.SetTop(circle, centerY - 40);

            SceneCanvas.Children.Add(circle);
        }

        private void RotateObject(SceneObject obj, Point mouse)
        {
            double cx = obj.X * zoom + camX + (obj.Width * zoom) / 2;
            double cy = obj.Y * zoom + camY + (obj.Height * zoom) / 2;

            double angle = Math.Atan2(mouse.Y - cy, mouse.X - cx);
            obj.Rotation = angle * (180 / Math.PI);
        }

        private static double DistanceToLine(Point p, double x1, double y1, double x2, double y2)
        {
            double A = p.X - x1;
            double B = p.Y - y1;
            double C = x2 - x1;
            double D = y2 - y1;

            double dot = A * C + B * D;
            double lenSq = C * C + D * D;
            double param = lenSq != 0 ? dot / lenSq : -1;

            double xx, yy;

            if (param < 0) { xx = x1; yy = y1; }
            else if (param > 1) { xx = x2; yy = y2; }
            else { xx = x1 + param * C; yy = y1 + param * D; }

            double dx = p.X - xx;
            double dy = p.Y - yy;

            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
