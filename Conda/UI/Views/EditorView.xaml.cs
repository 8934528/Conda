using System;
using System.Net.WebSockets;
using Conda.Engine.Prefabs;
using Conda.Engine.Networking;
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
using Conda.Engine.SceneSystem;
using Conda.Engine;
using Conda.Engine.VisualScripting;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Conda.UI.Views; // for customdialog and animatedmodal
using Conda.Engine.ECS;
using Conda.Engine.ECS.Components;
using Conda.Engine.ECS.Systems;

using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using DataFormats = System.Windows.DataFormats;
using Cursors = System.Windows.Input.Cursors;

using Button = System.Windows.Controls.Button;
using EngineTransform = Conda.Engine.ECS.Components.Transform;
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
        private readonly ClientWebSocket syncSocket = new();
        private bool isWebViewReady = false;
        private bool isVenvActive = false;
        private bool isFullScreen = false;
        private WindowState previousState;
        private Window? parentWindow;

        // Play Mode & Scene Objects
        private bool isPlaying = false;
        private readonly List<GameObject> sceneObjects = [];

        // Visual Scripting
        private readonly List<Node> nodes = [];
        private Node? selectedNode;
        private NodeGraph currentGraph = new NodeGraph();

        private static readonly JsonSerializerOptions JsonIndentedOptions = new() { WriteIndented = true };

        private World world = new();
        private GameLoop loop = null!;
        private RenderSystem renderer = null!;

        private Scene currentScene = new();
        private GameObject? selectedGameObject;
        private bool isDragging = false;
        private Point lastMousePos;

        private FrameworkElement? selectedElement;
        private readonly List<System.Windows.Shapes.Rectangle> resizeHandles = [];
        private System.Windows.Shapes.Ellipse? rotationHandle;
        private bool isResizing = false;
        private bool isRotating = false;
        private Point resizeStart;
        private const int GridSize = 20;





        public class OpenTab
        {
            public string FilePath { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public bool IsDirty { get; set; } = false;
            public string Icon { get; set; } = "📄";
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
            OutputConsole.Text += $"📁 Project: {projectPath}\n";
            OutputConsole.Text += "🐍 Python virtual environment support enabled\n";
            OutputConsole.Text += "-----------------------------------------------------------------------------\n\n";

            try
            {
                await syncSocket.ConnectAsync(new Uri("ws://localhost:5000/ws"), System.Threading.CancellationToken.None);
                _ = ReceiveUpdatesLoop();
                OutputConsole.Text += "🌐 Connected to Multiplayer Scene Sync!\n";
            }
            catch
            {
                OutputConsole.Text += "⚠️ Multiplayer Scene Sync server not running (ws://localhost:5000/ws).\n";
            }

            InitEngine();
            CreateTestEntity();
        }

        private void InitEngine()
        {
            renderer = new RenderSystem(GameCanvas);

            loop = new();

            loop.OnUpdate = (dt) =>
            {
                Dispatcher.Invoke(() =>
                {
                    renderer.Render(world);
                });
            };

            loop.Start();
        }

        private void CreateTestEntity()
        {
            var entity = new Entity();

            entity.Add(new Conda.Engine.ECS.Components.Transform
            {
                X = 100,
                Y = 100
            });

            entity.Add(new Sprite
            {
                ImagePath = "Assets/logo/test.png",
                Width = 100,
                Height = 100
            });

            world.Entities.Add(entity);
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
                Icon = GetFolderIcon(),
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
                        Icon = GetFileIcon(fileInfo.Extension)
                    });
                }
            }
            catch (UnauthorizedAccessException) { }

            return node;
        }

        private static string GetFolderIcon() => "📁";
        private static string GetFileIcon(string extension)

        {
            return extension.ToLower() switch
            {
                ".py" => "🐍",
                ".txt" => "📄",
                ".md" => "📝",
                ".json" => "📋",
                ".html" => "🌐",
                ".css" => "🎨",
                ".js" => "⚡",
                ".png" or ".jpg" or ".jpeg" or ".gif" => "🖼️",
                _ => "📄"
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
                    OutputConsole.Text += $"📁 Created folder: {folderName}\n";
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
                        OutputConsole.Text += $"✏️ Renamed: {node.Name} → {newName}\n";
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
                        OutputConsole.Text += $"🗑️ Deleted: {node.Name}\n";
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
                    OutputConsole.Text += $"❌ Error opening explorer: {ex.Message}\n";
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
                        OutputConsole.Text += $"📄 Copied file: {System.IO.Path.GetFileName(file)}\n";
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
                OutputConsole.Text += $"📄 Created file: {fileName}\n";
            }
            await Task.CompletedTask;
        }

        private void OnFileTreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileNode node && node != null && !node.IsDirectory)
            {
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
            OutputConsole.Text += $"📂 Opened: {newTab.Name}\n";
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
                OutputConsole.Text += $"❌ Closed: {tab.Name}\n";
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
                    OutputConsole.Text += $"✅ Saved: {System.IO.Path.GetFileName(tab.FilePath)}\n";
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

                OutputConsole.Text += "\n🚀 Running game...\n";
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
                        Dispatcher.Invoke(() => OutputConsole.Text += "❌ " + args.Data + "\n");
                };
                currentProcess.Exited += (s, args) =>
                {
                    Dispatcher.Invoke(() => OutputConsole.Text += $"\n✅ Process exited with code: {currentProcess.ExitCode}\n");
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
                    OutputConsole.Text += "\n⛔ Process stopped.\n";
                }
            }
            catch (Exception ex)
            {
                OutputConsole.Text += $"\n❌ Error: {ex.Message}\n";
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
            OutputConsole.Text += "💾 Saved all files\n";
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
            OutputConsole.Text += "🐛 Debug mode - Add breakpoints to debug your code\n";
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

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
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
                OutputConsole.Text += $"📁 Created folder: {folderName}\n";
            }
            await Task.CompletedTask;
        }

        private void OnRefreshClicked(object sender, RoutedEventArgs e)
        {
            LoadFiles();
            OutputConsole.Text += "🔄 File explorer refreshed\n";
        }

        // Updated Menu Handlers with CustomDialog
        private async void OnNewProjectClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "New Project dialog would open here.", "New Project", DialogIcon.Info);

        private async void OnOpenProjectClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Open Project dialog would open here.", "Open Project", DialogIcon.Info);

        private void OnExitClicked(object sender, RoutedEventArgs e)
            => System.Windows.Application.Current.Shutdown();

        private async void OnPreferencesClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Preferences dialog would open here.", "Preferences", DialogIcon.Info);

        private async void OnResetLayoutClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Layout reset.", "Reset Layout", DialogIcon.Info);

        private async void OnRecentProjectsClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Recent projects list would appear here.", "Recent Projects", DialogIcon.Info);

        private async void OnProjectSettingsClicked(object sender, RoutedEventArgs e)
            => await CustomDialog.ShowAsync(Window.GetWindow(this), "Project settings dialog would open here.", "Project Settings", DialogIcon.Info);

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

        private void OnShowCodeEditor(object sender, RoutedEventArgs e)
        {
            CodeEditorContainer.Visibility = Visibility.Visible;
            SceneEditorContainer.Visibility = Visibility.Collapsed;
            ExplorerContainer.Visibility = Visibility.Visible;
            InspectorContainer.Visibility = Visibility.Collapsed;

            CodeEditorToggle.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            SceneEditorToggle.Background = Brushes.Transparent;
        }

        private void OnShowSceneEditor(object sender, RoutedEventArgs e)
        {
            CodeEditorContainer.Visibility = Visibility.Collapsed;
            SceneEditorContainer.Visibility = Visibility.Visible;
            ExplorerContainer.Visibility = Visibility.Collapsed;
            InspectorContainer.Visibility = Visibility.Visible;
            VisualScriptingContainer.Visibility = Visibility.Collapsed;

            CodeEditorToggle.Background = Brushes.Transparent;
            SceneEditorToggle.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            VisualScriptingToggle.Background = Brushes.Transparent;

            RefreshHierarchy();
        }

        private void OnShowVisualScripting(object sender, RoutedEventArgs e)
        {
            CodeEditorContainer.Visibility = Visibility.Collapsed;
            SceneEditorContainer.Visibility = Visibility.Collapsed;
            VisualScriptingContainer.Visibility = Visibility.Visible;
            ExplorerContainer.Visibility = Visibility.Visible;
            InspectorContainer.Visibility = Visibility.Collapsed;

            CodeEditorToggle.Background = Brushes.Transparent;
            SceneEditorToggle.Background = Brushes.Transparent;
            VisualScriptingToggle.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        }

        private async void OnPlayToggle(object sender, RoutedEventArgs e)
        {
            isPlaying = !isPlaying;

            if (isPlaying)
            {
                PlayToggle.Content = "⏹ Stop";
                PlayToggle.Background = Brushes.Red;
                await StartPlayMode();
            }
            else
            {
                PlayToggle.Content = "▶ Play";
                PlayToggle.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // #28a745
                StopPlayMode();
            }
        }

        private static string ExportNodeGraph(NodeGraph graph)
        {
            return JsonSerializer.Serialize(graph, JsonIndentedOptions);
        }

        private string ExportSceneToJson()
        {
            var objects = SceneCanvas.Children.OfType<Border>()
                .Where(b => b.Tag is GameObject)
                .Select(obj =>
                {
                    var go = (GameObject)obj.Tag;
                    var t = go.GetComponent<EngineTransform>()!;
                    var s = go.GetComponent<Sprite>()!;
                    var script = go.GetComponent<ScriptComponent>();

                    return new
                    {
                        name = go.Name,
                        x = t.X,
                        y = t.Y,
                        width = s.Width,
                        height = s.Height,
                        color = s.Color,
                        rotation = t.Rotation,
                        ImagePath = s.ImagePath,
                        graph = script?.Graph
                    };
                });

            return JsonSerializer.Serialize(objects, JsonIndentedOptions);
        }

        private async Task StartPlayMode()
        {
            try
            {
                string json = ExportSceneToJson();
                string sceneFile = System.IO.Path.Combine(projectPath, "scene_runtime.json");
                File.WriteAllText(sceneFile, json);

                OutputConsole.Text += "▶ Running scene...\n";

                string pythonPath = GetPythonPath() ?? "python";
                string mainScript = System.IO.Path.Combine(projectPath, "main.py");

                // Ensure main.py exists
                if (!File.Exists(mainScript))
                {
                    await CreateDefaultRuntimeScript(mainScript);
                }

                ProcessStartInfo psi = new()
                {
                    FileName = pythonPath,
                    Arguments = $"\"{mainScript}\"",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                currentProcess = new Process { StartInfo = psi };

                currentProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Dispatcher.Invoke(() => OutputConsole.Text += e.Data + "\n");
                };

                currentProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Dispatcher.Invoke(() => OutputConsole.Text += "❌ " + e.Data + "\n");
                };

                currentProcess.Start();
                currentProcess.BeginOutputReadLine();
                currentProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                await CustomDialog.ShowAsync(Window.GetWindow(this), ex.Message, "Error", DialogIcon.Error);
            }
        }

        private void StopPlayMode()
        {
            if (currentProcess != null && !currentProcess.HasExited)
            {
                currentProcess.Kill();
                OutputConsole.Text += "\n[Stopped]\n";
            }
        }

        private void SyncSceneWhilePlaying()
        {
            if (!isPlaying) return;

            string json = ExportSceneToJson();
            string sceneFile = System.IO.Path.Combine(projectPath, "scene_runtime.json");
            File.WriteAllText(sceneFile, json);
        }

        private async Task CreateDefaultRuntimeScript(string path)
        {
            string script = @"import pygame
import json
import os

pygame.init()

WIDTH, HEIGHT = 800, 600
screen = pygame.display.set_mode((WIDTH, HEIGHT))
pygame.display.set_caption(""Conda Runtime"")

clock = pygame.time.Clock()

# Load scene
scene = []
if os.path.exists(""scene_runtime.json""):
    with open(""scene_runtime.json"") as f:
        scene = json.load(f)

# Load node graph
graph = {}
if os.path.exists(""node_graph.json""):
    with open(""node_graph.json"") as f:
        graph = json.load(f)

def execute_node(node_id, obj, nodes):
    node = nodes.get(node_id)
    if not node:
        return

    node_type = node[""Type""]
    props = node.get(""Properties"", {})

    # === NODE TYPES ===
    if node_type == ""Move"":
        obj[""x""] += float(props.get(""dx"", 0))
        obj[""y""] += float(props.get(""dy"", 0))

    elif node_type == ""Rotate"":
        obj[""rotation""] += float(props.get(""angle"", 1))

    elif node_type == ""Print"":
        print(props.get(""text"", ""Hello""))

    # === FLOW ===
    for out in node.get(""Outputs"", []):
        execute_node(out, obj, nodes)

image_cache = {}

def load_image(path):
    if path not in image_cache:
        image_cache[path] = pygame.image.load(path)
    return image_cache[path]

running = True

while running:
    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            running = False

    screen.fill((30, 30, 30))

    for obj in scene:
        obj_graph = obj.get(""graph"")
        if obj_graph:
            obj_nodes = {n[""Id""]: n for n in obj_graph.get(""Nodes"", [])}
        else:
            obj_nodes = {n[""Id""]: n for n in graph.get(""Nodes"", [])}

        start_nodes = [n for n in obj_nodes.values() if n[""Type""] == ""Start""]

        for start in start_nodes:
            execute_node(start[""Id""], obj, obj_nodes)

        # Rendering
        if ""ImagePath"" in obj and obj[""ImagePath""]:
            img = load_image(obj[""ImagePath""])
            # Apply rotation if needed (simplified here based on tutorial)
            rotated_img = pygame.transform.rotate(img, -obj.get(""rotation"", 0))
            new_rect = rotated_img.get_rect(center=(obj[""x""] + obj[""width""]/2, obj[""y""] + obj[""height""]/2))
            screen.blit(rotated_img, new_rect.topleft)
        else:
            s = pygame.Surface((obj[""width""], obj[""height""]), pygame.SRCALPHA)
            color = pygame.Color(obj.get(""color"", ""#00C8FF""))
            pygame.draw.rect(s, color, (0, 0, obj[""width""], obj[""height""]))
            
            # Rotate
            rotated_s = pygame.transform.rotate(s, -obj.get(""rotation"", 0))
            new_rect = rotated_s.get_rect(center=(obj[""x""] + obj[""width""]/2, obj[""y""] + obj[""height""]/2))
            
            screen.blit(rotated_s, new_rect.topleft)

    pygame.display.flip()
    clock.tick(60)

pygame.quit()
";
            await File.WriteAllTextAsync(path, script);
            LoadFiles(); // Refresh explorer
        }

        private void AddGameObject()
        {
            var go = new GameObject { Name = "GameObject" };

            var transform = go.AddComponent<EngineTransform>();
            transform.X = 100;
            transform.Y = 100;

            _ = go.AddComponent<Sprite>();
            var script = go.AddComponent<ScriptComponent>();
            script.Graph = currentGraph;

            sceneObjects.Add(go);
            RenderGameObject(go);
            RefreshHierarchy();
        }

        private void RenderGameObject(GameObject go)
        {
            var transform = go.GetComponent<EngineTransform>()!;
            var sprite = go.GetComponent<Sprite>()!;

            var rect = new Border
            {
                Width = sprite.Width,
                Height = sprite.Height,
                Background = new BrushConverter().ConvertFromString(sprite.Color) as SolidColorBrush ?? Brushes.Transparent,
                Tag = go,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1)
            };

            Canvas.SetLeft(rect, transform.X);
            Canvas.SetTop(rect, transform.Y);

            rect.MouseLeftButtonDown += OnObjectSelected;
            rect.MouseMove += OnObjectMouseMove;
            rect.MouseLeftButtonUp += OnObjectMouseUp;

            SceneCanvas.Children.Add(rect);
        }

        private void AddField(string label, string value, Action<string> onUpdate)
        {
            var stack = new StackPanel { Margin = new Thickness(0, 5, 0, 5) };
            stack.Children.Add(new TextBlock { Text = label, Foreground = Brushes.Gray, FontSize = 11 });
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

        private void OnAddGameObject(object sender, RoutedEventArgs e)
        {
            AddGameObject();
        }

        private void OnSavePrefab(object sender, RoutedEventArgs e)
        {
            if (selectedGameObject == null)
            {
                ShowToast("Select an object to save as Prefab", false);
                return;
            }
            var prefabManager = new PrefabManager(projectPath);
            prefabManager.SavePrefab(selectedGameObject.Name, selectedGameObject);
            ShowToast($"Saved Prefab: {selectedGameObject.Name}");
        }

        private void OnLoadPrefab(object sender, RoutedEventArgs e)
        {
            string prefabName = Microsoft.VisualBasic.Interaction.InputBox("Enter Prefab name to load:", "Load Prefab", "GameObject");
            if (!string.IsNullOrWhiteSpace(prefabName))
            {
                var prefabManager = new PrefabManager(projectPath);
                var loaded = prefabManager.LoadPrefab<GameObject>(prefabName);
                if (loaded != null)
                {
                    loaded.Id = Guid.NewGuid().ToString(); // New ID for instance
                    sceneObjects.Add(loaded);
                    
                    if (loaded.GetComponent<Sprite>()?.ImagePath != null)
                        RenderSprite(loaded);
                    else
                        RenderGameObject(loaded);
                        
                    RefreshHierarchy();
                    ShowToast($"Loaded Prefab: {prefabName}");
                }
                else
                {
                    ShowToast($"Failed to load Prefab: {prefabName}", false);
                }
            }
        }

        private void UpdateInspector(GameObject go)
        {
            InspectorPanel.Children.Clear();

            // Show name
            InspectorPanel.Children.Add(new TextBlock { Text = go.Name, Foreground = Brushes.Cyan, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });

            foreach (var comp in go.GetAllComponents())
            {
                var sectionHeader = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(50, 50, 60)),
                    CornerRadius = new CornerRadius(3),
                    Margin = new Thickness(0, 8, 0, 4),
                    Padding = new Thickness(6, 4, 6, 4),
                    Child = new TextBlock
                    {
                        Text = "▸ " + comp.GetType().Name.ToUpper(),
                        Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                        FontWeight = FontWeights.Bold,
                        FontSize = 11
                    }
                };
                InspectorPanel.Children.Add(sectionHeader);

                if (comp is EngineTransform t)
                {
                    AddField("X", t.X.ToString("F1"), v => { if (double.TryParse(v, out double res)) { t.X = res; UpdateElementPosition(go); SyncSceneWhilePlaying(); } });
                    AddField("Y", t.Y.ToString("F1"), v => { if (double.TryParse(v, out double res)) { t.Y = res; UpdateElementPosition(go); SyncSceneWhilePlaying(); } });
                    AddField("Rotation", t.Rotation.ToString("F1"), v => { if (double.TryParse(v, out double res)) { t.Rotation = res; UpdateElementRotation(go); SyncSceneWhilePlaying(); } });
                }

                if (comp is Sprite s)
                {
                    AddField("Width", s.Width.ToString("F0"), v => { if (double.TryParse(v, out double res)) { s.Width = res; UpdateElementSize(go); SyncSceneWhilePlaying(); } });
                    AddField("Height", s.Height.ToString("F0"), v => { if (double.TryParse(v, out double res)) { s.Height = res; UpdateElementSize(go); SyncSceneWhilePlaying(); } });
                    AddField("Color", s.Color, v => { s.Color = v; UpdateElementColor(go); SyncSceneWhilePlaying(); });
                }
            }
        }

        private void UpdateElementPosition(GameObject go)
        {
            var element = SceneCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == go);
            if (element != null)
            {
                var t = go.GetComponent<EngineTransform>()!;
                Canvas.SetLeft(element, t.X);
                Canvas.SetTop(element, t.Y);
            }
        }

        private void UpdateElementRotation(GameObject go)
        {
            var element = SceneCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == go);
            if (element != null)
            {
                var t = go.GetComponent<EngineTransform>()!;
                element.RenderTransform = new RotateTransform(t.Rotation);
            }
        }

        private void UpdateElementSize(GameObject go)
        {
            var element = SceneCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == go);
            if (element != null)
            {
                var s = go.GetComponent<Sprite>()!;
                element.Width = s.Width;
                element.Height = s.Height;
            }
        }

        private void UpdateElementColor(GameObject go)
        {
            var element = SceneCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == go);
            if (element != null)
            {
                var s = go.GetComponent<Sprite>()!;
                try { element.Background = new BrushConverter().ConvertFromString(s.Color) as SolidColorBrush ?? Brushes.Transparent; } catch { }
            }
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
            var go = new GameObject { Name = "Sprite" };

            var t = go.AddComponent<EngineTransform>();
            t.X = 200;
            t.Y = 200;

            var s = go.AddComponent<Sprite>();
            s.ImagePath = path;

            var script = go.AddComponent<ScriptComponent>();
            script.Graph = currentGraph;

            sceneObjects.Add(go);
            RenderSprite(go);
            RefreshHierarchy();
        }

        private void RenderSprite(GameObject go)
        {
            var t = go.GetComponent<EngineTransform>()!;
            var s = go.GetComponent<Sprite>()!;

            var img = new System.Windows.Controls.Image
            {
                Width = s.Width,
                Height = s.Height,
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(s.ImagePath)),
                Tag = go
            };

            // wrap in a border for standard selection highlighting
            var border = new Border
            {
                Width = s.Width,
                Height = s.Height,
                Background = Brushes.Transparent,
                Tag = go,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                Child = img
            };

            Canvas.SetLeft(border, t.X);
            Canvas.SetTop(border, t.Y);

            border.MouseLeftButtonDown += OnObjectSelected;
            border.MouseMove += OnObjectMouseMove;
            border.MouseLeftButtonUp += OnObjectMouseUp;

            SceneCanvas.Children.Add(border);
        }

        private static bool IsImageFile(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLower();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif";
        }

        private void DrawObject(SceneObject obj)
        {
            FrameworkElement element;

            if (obj.Type == "Image" && File.Exists(obj.AssetPath))
            {
                try
                {
                    element = new System.Windows.Controls.Image
                    {
                        Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(obj.AssetPath)),
                        Width = obj.Width,
                        Height = obj.Height,
                        Stretch = Stretch.Fill
                    };
                }
                catch
                {
                    element = CreatePlaceholderBorder(obj);
                }
            }
            else
            {
                element = CreatePlaceholderBorder(obj);
            }

            Canvas.SetLeft(element, obj.X);
            Canvas.SetTop(element, obj.Y);

            element.Tag = obj;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = new RotateTransform(obj.Rotation);

            element.MouseLeftButtonDown += OnObjectSelected;
            element.MouseMove += OnObjectMouseMove;
            element.MouseLeftButtonUp += OnObjectMouseUp;

            SceneCanvas.Children.Add(element);
        }

        private static Border CreatePlaceholderBorder(SceneObject obj)
        {
            return new Border
            {
                Width = obj.Width,
                Height = obj.Height,
                Background = Brushes.Blue,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1)
            };
        }

        private void OnObjectSelected(object sender, MouseButtonEventArgs e)
        {
            selectedElement = sender as FrameworkElement;
            selectedGameObject = selectedElement?.Tag as GameObject;

            if (selectedGameObject == null) return;

            HighlightSelection();
            RemoveHandles();
            CreateResizeHandles();
            CreateRotationHandle();
            UpdateInspector(selectedGameObject);

            isDragging = true;
            lastMousePos = e.GetPosition(SceneCanvas);

            selectedElement?.CaptureMouse();
            e.Handled = true;
        }

        private void OnObjectMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || selectedGameObject == null || selectedElement == null) return;

            var pos = e.GetPosition(SceneCanvas);

            double dx = pos.X - lastMousePos.X;
            double dy = pos.Y - lastMousePos.Y;

            var transform = selectedGameObject.GetComponent<EngineTransform>()!;
            transform.X = Snap(transform.X + dx);
            transform.Y = Snap(transform.Y + dy);

            Canvas.SetLeft(selectedElement, transform.X);
            Canvas.SetTop(selectedElement, transform.Y);

            lastMousePos = pos;
            UpdateHandlePositions();
            UpdateRotationHandle();
            UpdateInspector(selectedGameObject);
            SyncSceneWhilePlaying();
            SendSyncUpdate("Move", selectedGameObject);
        }

        private void OnObjectMouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            selectedElement?.ReleaseMouseCapture();
        }

        private void HighlightSelection()
        {
            foreach (UIElement child in SceneCanvas.Children)
            {
                if (child is Border b && b.Tag != null)
                {
                    b.BorderThickness = new Thickness(1);
                    b.BorderBrush = Brushes.White;
                }
            }

            if (selectedElement is Border border)
            {
                border.BorderBrush = Brushes.Yellow;
                border.BorderThickness = new Thickness(2);
            }
        }

        private void RemoveHandles()
        {
            foreach (var h in resizeHandles)
                SceneCanvas.Children.Remove(h);

            resizeHandles.Clear();

            if (rotationHandle != null)
            {
                SceneCanvas.Children.Remove(rotationHandle);
                rotationHandle = null;
            }
        }

        private void CreateResizeHandles()
        {
            if (selectedElement == null) return;

            double size = 8;
            var positions = new[] { new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1) };

            foreach (var pos in positions)
            {
                var handle = new System.Windows.Shapes.Rectangle
                {
                    Width = size,
                    Height = size,
                    Fill = Brushes.White,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Cursor = Cursors.SizeAll,
                    Tag = pos
                };

                handle.MouseDown += OnResizeStart;
                handle.MouseMove += OnResizeMove;
                handle.MouseUp += OnResizeEnd;

                resizeHandles.Add(handle);
                SceneCanvas.Children.Add(handle);
            }

            UpdateHandlePositions();
        }

        private void UpdateHandlePositions()
        {
            if (selectedElement == null) return;

            double x = Canvas.GetLeft(selectedElement);
            double y = Canvas.GetTop(selectedElement);
            double w = selectedElement.Width;
            double h = selectedElement.Height;

            foreach (var handle in resizeHandles)
            {
                var pos = (Point)handle.Tag;
                double hx = x + (pos.X * w);
                double hy = y + (pos.Y * h);

                Canvas.SetLeft(handle, hx - 4);
                Canvas.SetTop(handle, hy - 4);
            }
        }

        private void OnResizeStart(object sender, MouseButtonEventArgs e)
        {
            isResizing = true;
            resizeStart = e.GetPosition(SceneCanvas);
            (sender as UIElement)?.CaptureMouse();
            e.Handled = true;
        }

        private void OnResizeMove(object sender, MouseEventArgs e)
        {
            if (!isResizing || selectedGameObject == null || selectedElement == null) return;

            var pos = e.GetPosition(SceneCanvas);
            var handle = sender as FrameworkElement;
            var handlePos = (Point)handle!.Tag;

            double dx = pos.X - resizeStart.X;
            double dy = pos.Y - resizeStart.Y;

            var sprite = selectedGameObject.GetComponent<Sprite>()!;
            if (handlePos.X == 1) sprite.Width = Math.Max(10, Snap(sprite.Width + dx));
            if (handlePos.Y == 1) sprite.Height = Math.Max(10, Snap(sprite.Height + dy));

            selectedElement.Width = sprite.Width;
            selectedElement.Height = sprite.Height;

            resizeStart = pos;
            UpdateHandlePositions();
            UpdateRotationHandle();
            UpdateInspector(selectedGameObject);
            SyncSceneWhilePlaying();
        }

        private void OnResizeEnd(object sender, MouseButtonEventArgs e)
        {
            isResizing = false;
            (sender as UIElement)?.ReleaseMouseCapture();
        }

        private void CreateRotationHandle()
        {
            if (selectedElement == null) return;

            rotationHandle = new System.Windows.Shapes.Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.Orange,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Cursor = Cursors.Hand
            };

            rotationHandle.MouseDown += OnRotateStart;
            rotationHandle.MouseMove += OnRotateMove;
            rotationHandle.MouseUp += OnRotateEnd;

            SceneCanvas.Children.Add(rotationHandle);
            UpdateRotationHandle();
        }

        private void UpdateRotationHandle()
        {
            if (selectedElement == null || rotationHandle == null) return;

            double x = Canvas.GetLeft(selectedElement);
            double y = Canvas.GetTop(selectedElement);

            Canvas.SetLeft(rotationHandle, x + selectedElement.Width / 2 - 6);
            Canvas.SetTop(rotationHandle, y - 25);
        }

        private void OnRotateStart(object sender, MouseButtonEventArgs e)
        {
            isRotating = true;
            (sender as UIElement)?.CaptureMouse();
            e.Handled = true;
        }

        private void OnRotateMove(object sender, MouseEventArgs e)
        {
            if (!isRotating || selectedElement == null || selectedGameObject == null) return;

            var pos = e.GetPosition(SceneCanvas);
            double centerX = Canvas.GetLeft(selectedElement) + selectedElement.Width / 2;
            double centerY = Canvas.GetTop(selectedElement) + selectedElement.Height / 2;

            double angle = Math.Atan2(pos.Y - centerY, pos.X - centerX) * (180 / Math.PI);
            angle += 90; // Offset to make 0 up

            var transform = selectedGameObject.GetComponent<EngineTransform>()!;
            transform.Rotation = angle;
            selectedElement.RenderTransform = new RotateTransform(angle);
            UpdateInspector(selectedGameObject);
            SyncSceneWhilePlaying();
        }

        private void OnRotateEnd(object sender, MouseButtonEventArgs e)
        {
            isRotating = false;
            (sender as UIElement)?.ReleaseMouseCapture();
        }

        private void DrawGrid()
        {
            SceneCanvas.Children.Clear();
            double width = Math.Max(SceneCanvas.ActualWidth, 2000);
            double height = Math.Max(SceneCanvas.ActualHeight, 2000);

            for (int x = 0; x < width; x += GridSize)
            {
                var line = new System.Windows.Shapes.Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                    StrokeThickness = 1
                };
                SceneCanvas.Children.Add(line);
            }

            for (int y = 0; y < height; y += GridSize)
            {
                var line = new System.Windows.Shapes.Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                    StrokeThickness = 1
                };
                SceneCanvas.Children.Add(line);
            }

            foreach (var obj in currentScene.Objects) DrawObject(obj);
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
            SceneHierarchy.ItemsSource = sceneObjects;
        }

        private async void OnSaveScene(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Conda Scene (*.conda)|*.conda", InitialDirectory = projectPath };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    currentScene.Save(dialog.FileName);
                    OutputConsole.Text += $"💾 Scene saved: {System.IO.Path.GetFileName(dialog.FileName)}\n";
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
                    OutputConsole.Text += $"📂 Scene loaded: {System.IO.Path.GetFileName(dialog.FileName)}\n";
                    await CustomDialog.ShowAsync(Window.GetWindow(this), "Scene loaded successfully!", "Success", DialogIcon.Success);
                }
                catch (Exception ex)
                {
                    await CustomDialog.ShowAsync(Window.GetWindow(this), $"Error loading scene: {ex.Message}", "Error", DialogIcon.Error);
                }
            }
        }

        private void ShowToast(string message, bool isSuccess = true)
        {
            if (FindName("ToastMessage") is not TextBlock toastMessage ||
                FindName("ToastIcon") is not TextBlock toastIcon ||
                FindName("ToastOverlay") is not Grid toastOverlay ||
                FindName("ToastTranslate") is not TranslateTransform toastTranslate)
            {
                return;
            }

            toastMessage.Text = message;
            toastIcon.Text = isSuccess ? "✅" : "❌";
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

        // --- MULTIPLAYER SYNC METHODS ---
        private async void SendSyncUpdate(string type, GameObject obj)
        {
            if (syncSocket.State != WebSocketState.Open) return;
            var transform = obj.GetComponent<EngineTransform>();
            if (transform == null) return;
            
            var msg = JsonSerializer.Serialize(new SceneSyncMessage
            {
                Type = type,
                ObjectId = obj.Id,
                X = transform.X,
                Y = transform.Y
            });
            var bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            await syncSocket.SendAsync(bytes, WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }

        private async Task ReceiveUpdatesLoop()
        {
            var buffer = new byte[1024];
            while (syncSocket.State == WebSocketState.Open)
            {
                var result = await syncSocket.ReceiveAsync(buffer, System.Threading.CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;
                string json = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                var update = JsonSerializer.Deserialize<SceneSyncMessage>(json);
                if (update != null) Dispatcher.Invoke(() => ApplySyncUpdate(update));
            }
        }

        private void ApplySyncUpdate(SceneSyncMessage msg)
        {
            var obj = sceneObjects.FirstOrDefault(o => o.Id == msg.ObjectId);
            if (obj != null)
            {
                var t = obj.GetComponent<EngineTransform>();
                if (t != null)
                {
                    t.X = msg.X;
                    t.Y = msg.Y;
                    UpdateElementPosition(obj);
                }
            }
        }
    }
}
