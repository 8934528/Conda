using System;
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

using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using DataFormats = System.Windows.DataFormats;
using Cursors = System.Windows.Input.Cursors;

using Button = System.Windows.Controls.Button;


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




        private Scene currentScene = new();
        private SceneObject? selectedObject;
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
                System.Windows.MessageBox.Show($"Error loading files: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                if (treeViewItem?.DataContext is FileNode)
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

        private void OnNewFileClick(object sender, RoutedEventArgs e)
        {
            if (currentContextItem?.DataContext is FileNode node)
            {
                string parentPath = node.IsDirectory ? node.FullPath : System.IO.Path.GetDirectoryName(node.FullPath)!;
                CreateNewFile(parentPath);
            }
        }

        private void OnNewFolderClick(object sender, RoutedEventArgs e)
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
                    OutputConsole.Text += $"📁 Created folder: {folderName}\n";
                }
            }
        }

        private void OnRenameClick(object sender, RoutedEventArgs e)
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
                        OutputConsole.Text += $"✏️ Renamed: {node.Name} → {newName}\n";
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error renaming: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (currentContextItem?.DataContext is FileNode node)
            {
                var result = System.Windows.MessageBox.Show($"Are you sure you want to delete {node.Name}?", "Confirm Delete",
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        if (node.IsDirectory)
                            Directory.Delete(node.FullPath, true);
                        else
                            File.Delete(node.FullPath);
                        LoadFiles();
                        OutputConsole.Text += $"🗑️ Deleted: {node.Name}\n";
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error deleting: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OnCopyPathClick(object sender, RoutedEventArgs e)
        {
            if (currentContextItem?.DataContext is FileNode node)
            {
                System.Windows.Clipboard.SetText(node.FullPath);
                OutputConsole.Text += $"📋 Copied path: {node.FullPath}\n";
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

        private void CreateNewFile(string parentPath)
        {
            string fileName = Microsoft.VisualBasic.Interaction.InputBox("Enter file name:", "New File", "newfile.py");
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                string filePath = System.IO.Path.Combine(parentPath, fileName);
                File.WriteAllText(filePath, "");
                LoadFiles();
                OpenFileInTab(filePath);
                OutputConsole.Text += $"📄 Created file: {fileName}\n";
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
                    var result = System.Windows.MessageBox.Show($"Save changes to {tab.Name}?", "Unsaved Changes", 
                        System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Question);
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        await SaveCurrentFile();
                    }
                    else if (result == System.Windows.MessageBoxResult.Cancel)
                    {
                        return;
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
                        System.Windows.MessageBox.Show("Editor is not ready yet.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }

                    string code = await CodeWebView.CoreWebView2.ExecuteScriptAsync("getCode()");
                    code = UnescapeJs(code);
                    File.WriteAllText(tab.FilePath, code);
                    tab.Content = code;
                    tab.IsDirty = false;
                    OutputConsole.Text += $"✅ Saved: {System.IO.Path.GetFileName(tab.FilePath)}\n";
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error saving file: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                    System.Windows.MessageBox.Show("main.py not found.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
        private void OnNewFileMenuClick(object sender, RoutedEventArgs e)
        {
            CreateNewFile(projectPath);
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
                FileName = "https://www.pygame.org/docs/",
                UseShellExecute = true
            });
        }

        private void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Conda IDE\nVersion 1.0\n\nA Python Game Development Environment\nBuilt with WPF and WebView2", 
                "About Conda IDE", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Settings panel would appear here.\n\nFuture features:\n- Theme customization\n- Font settings\n- Keybindings\n- Python interpreter path", 
                "Settings", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void OnNewFileToolbarClick(object sender, RoutedEventArgs e)
        {
            CreateNewFile(projectPath);
        }

        private void OnNewFolderToolbarClick(object sender, RoutedEventArgs e)
        {
            string folderName = Microsoft.VisualBasic.Interaction.InputBox("Enter folder name:", "New Folder", "NewFolder");
            if (!string.IsNullOrWhiteSpace(folderName))
            {
                string folderPath = System.IO.Path.Combine(projectPath, folderName);
                Directory.CreateDirectory(folderPath);
                LoadFiles();
                OutputConsole.Text += $"📁 Created folder: {folderName}\n";
            }
        }

        private void OnRefreshClicked(object sender, RoutedEventArgs e)
        {
            LoadFiles();
            OutputConsole.Text += "🔄 File explorer refreshed\n";
        }

        // New Missing Handlers
        private void OnNewProjectClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("New Project dialog would open here.", "New Project", System.Windows.MessageBoxButton.OK);
        private void OnOpenProjectClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Open Project dialog would open here.", "Open Project", System.Windows.MessageBoxButton.OK);
        private void OnExitClicked(object sender, RoutedEventArgs e) => System.Windows.Application.Current.Shutdown();
        private void OnPreferencesClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Preferences dialog would open here.", "Preferences", System.Windows.MessageBoxButton.OK);
        private void OnResetLayoutClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Layout reset.", "Reset Layout", System.Windows.MessageBoxButton.OK);
        private void OnRecentProjectsClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Recent projects list would appear here.", "Recent Projects", System.Windows.MessageBoxButton.OK);
        private void OnProjectSettingsClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Project settings dialog would open here.", "Project Settings", System.Windows.MessageBoxButton.OK);
        private void OnBuildClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Build process would start here.", "Build", System.Windows.MessageBoxButton.OK);
        private void OnExportClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Export options would appear here.", "Export", System.Windows.MessageBoxButton.OK);
        private void OnPackageManagerClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Package manager would open here.", "Package Manager", System.Windows.MessageBoxButton.OK);
        private void OnExtensionsClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Extensions manager would open here.", "Extensions", System.Windows.MessageBoxButton.OK);
        private void OnOpenTerminalClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Terminal would open here.", "Terminal", System.Windows.MessageBoxButton.OK);
        private void OnRunCommandClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Command runner would open here.", "Run Command", System.Windows.MessageBoxButton.OK);
        private void OnSettingsIconClicked(object sender, RoutedEventArgs e) => System.Windows.MessageBox.Show("Settings panel would open here.", "Settings", System.Windows.MessageBoxButton.OK);

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

            CodeEditorToggle.Background = Brushes.Transparent;
            SceneEditorToggle.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));

            RefreshHierarchy();
        }

        private void OnSceneDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                var obj = new SceneObject
                {
                    Name = System.IO.Path.GetFileName(file),
                    Type = IsImageFile(file) ? "Image" : "Rectangle",
                    X = 50,
                    Y = 50,
                    Width = 100,
                    Height = 100,
                    AssetPath = file
                };

                currentScene.Objects.Add(obj);
                DrawObject(obj);
            }

            RefreshHierarchy();
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

        private Border CreatePlaceholderBorder(SceneObject obj)
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
            selectedObject = selectedElement?.Tag as SceneObject;

            if (selectedObject == null) return;

            HighlightSelection();
            RemoveHandles();
            CreateResizeHandles();
            CreateRotationHandle();
            LoadInspector();

            isDragging = true;
            lastMousePos = e.GetPosition(SceneCanvas);

            selectedElement?.CaptureMouse();
            e.Handled = true;
        }

        private void OnObjectMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || selectedObject == null || selectedElement == null) return;

            var pos = e.GetPosition(SceneCanvas);

            double dx = pos.X - lastMousePos.X;
            double dy = pos.Y - lastMousePos.Y;

            selectedObject.X = Snap(selectedObject.X + dx);
            selectedObject.Y = Snap(selectedObject.Y + dy);

            Canvas.SetLeft(selectedElement, selectedObject.X);
            Canvas.SetTop(selectedElement, selectedObject.Y);

            lastMousePos = pos;
            UpdateHandlePositions();
            UpdateRotationHandle();
            LoadInspector();
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
            if (!isResizing || selectedObject == null || selectedElement == null) return;

            var pos = e.GetPosition(SceneCanvas);
            var handle = sender as FrameworkElement;
            var handlePos = (Point)handle!.Tag;

            double dx = pos.X - resizeStart.X;
            double dy = pos.Y - resizeStart.Y;

            if (handlePos.X == 1) selectedObject.Width = Math.Max(10, Snap(selectedObject.Width + dx));
            if (handlePos.Y == 1) selectedObject.Height = Math.Max(10, Snap(selectedObject.Height + dy));

            selectedElement.Width = selectedObject.Width;
            selectedElement.Height = selectedObject.Height;

            resizeStart = pos;
            UpdateHandlePositions();
            UpdateRotationHandle();
            LoadInspector();
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
            if (!isRotating || selectedElement == null || selectedObject == null) return;

            var pos = e.GetPosition(SceneCanvas);
            double centerX = Canvas.GetLeft(selectedElement) + selectedElement.Width / 2;
            double centerY = Canvas.GetTop(selectedElement) + selectedElement.Height / 2;

            double angle = Math.Atan2(pos.Y - centerY, pos.X - centerX) * (180 / Math.PI);
            angle += 90; // Offset to make 0 up

            selectedObject.Rotation = angle;
            selectedElement.RenderTransform = new RotateTransform(angle);
            LoadInspector();
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
                    X1 = x, Y1 = 0, X2 = x, Y2 = height,
                    Stroke = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                    StrokeThickness = 1
                };
                SceneCanvas.Children.Add(line);
            }

            for (int y = 0; y < height; y += GridSize)
            {
                var line = new System.Windows.Shapes.Line
                {
                    X1 = 0, Y1 = y, X2 = width, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                    StrokeThickness = 1
                };
                SceneCanvas.Children.Add(line);
            }

            foreach (var obj in currentScene.Objects) DrawObject(obj);
        }

        private static double Snap(double value) => Math.Round(value / GridSize) * GridSize;

        private void LoadInspector()
        {
            if (selectedObject == null) return;

            NameBox.Text = selectedObject.Name;
            PosXBox.Text = selectedObject.X.ToString();
            PosYBox.Text = selectedObject.Y.ToString();
            WidthBox.Text = selectedObject.Width.ToString();
            HeightBox.Text = selectedObject.Height.ToString();
            RotationBox.Text = Math.Round(selectedObject.Rotation, 2).ToString();
        }

        private void OnApplyInspector(object sender, RoutedEventArgs e)
        {
            if (selectedObject == null || selectedElement == null) return;

            selectedObject.Name = NameBox.Text;

            if (double.TryParse(PosXBox.Text, out double x)) selectedObject.X = x;
            if (double.TryParse(PosYBox.Text, out double y)) selectedObject.Y = y;
            if (double.TryParse(WidthBox.Text, out double w)) selectedObject.Width = w;
            if (double.TryParse(HeightBox.Text, out double h)) selectedObject.Height = h;
            if (double.TryParse(RotationBox.Text, out double r)) selectedObject.Rotation = r;

            selectedElement.Width = selectedObject.Width;
            selectedElement.Height = selectedObject.Height;
            Canvas.SetLeft(selectedElement, selectedObject.X);
            Canvas.SetTop(selectedElement, selectedObject.Y);
            selectedElement.RenderTransform = new RotateTransform(selectedObject.Rotation);

            UpdateHandlePositions();
            UpdateRotationHandle();
            RefreshHierarchy();
        }

        private void RefreshHierarchy()
        {
            SceneHierarchy.ItemsSource = null;
            SceneHierarchy.ItemsSource = currentScene.Objects;
        }

        private void OnSaveScene(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Conda Scene (*.conda)|*.conda", InitialDirectory = projectPath };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    currentScene.Save(dialog.FileName);
                    OutputConsole.Text += $"💾 Scene saved: {System.IO.Path.GetFileName(dialog.FileName)}\n";
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error saving scene: {ex.Message}", "Error");
                }
            }
        }

        private void OnLoadScene(object sender, RoutedEventArgs e)
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
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error loading scene: {ex.Message}", "Error");
                }
            }
        }
    }
}
