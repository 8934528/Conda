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
    }
}
