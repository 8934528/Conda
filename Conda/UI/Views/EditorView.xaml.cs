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

namespace Conda.UI.Views
{
    public partial class EditorView : Page
    {
        private string projectPath;
        private string currentFilePath = string.Empty;
        private Process? currentProcess;
        private bool isWebViewReady = false;
        private bool isVenvActive = false;

        // Track open tabs
        public class OpenTab
        {
            public string FilePath { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public bool IsDirty { get; set; } = false;
        }

        private ObservableCollection<OpenTab> openTabs = new ObservableCollection<OpenTab>();
        private TreeViewItem? currentContextItem;

        public EditorView(string path)
        {
            InitializeComponent();
            projectPath = path;
            FileTabs.ItemsSource = openTabs;

            Loaded += EditorView_Loaded;
        }

        private async void EditorView_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView();
            LoadFiles();
            await CheckVenvStatus();
        }

        private async System.Threading.Tasks.Task CheckVenvStatus()
        {
            string venvPath = System.IO.Path.Combine(projectPath, "venv");
            isVenvActive = Directory.Exists(venvPath);
            UpdateVenvButton();
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void UpdateVenvButton()
        {
            ActivateVenvBtn.Content = isVenvActive ? "🐍 Deactivate Venv" : "🐍 Activate Venv";
            ActivateVenvBtn.Background = isVenvActive ?
                new SolidColorBrush(Color.FromRgb(198, 40, 40)) :
                new SolidColorBrush(Color.FromRgb(106, 27, 154));
        }

        private async void OnToggleVenvClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string venvPath = System.IO.Path.Combine(projectPath, "venv");

                if (isVenvActive)
                {
                    isVenvActive = false;
                    OutputConsole.Text += "⚠️ Virtual environment deactivated (folder remains)\n";
                }
                else
                {
                    if (!Directory.Exists(venvPath))
                    {
                        OutputConsole.Text += "📦 Creating virtual environment...\n";
                        await CreateVirtualEnvironmentAsync();
                    }
                    isVenvActive = true;
                    OutputConsole.Text += "✅ Virtual environment activated!\n";
                }
                UpdateVenvButton();
            }
            catch (Exception ex)
            {
                OutputConsole.Text += $"❌ Error: {ex.Message}\n";
            }
        }

        private async System.Threading.Tasks.Task CreateVirtualEnvironmentAsync()
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = "-m venv venv",
                        WorkingDirectory = projectPath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using Process? process = Process.Start(psi);
                    process?.WaitForExit(60000);
                }
                catch { }
            });
        }

        private async void OnInstallDependenciesClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                string requirementsPath = System.IO.Path.Combine(projectPath, "requirements.txt");
                if (!File.Exists(requirementsPath))
                {
                    OutputConsole.Text += "❌ requirements.txt not found!\n";
                    return;
                }

                OutputConsole.Text += "📦 Installing dependencies...\n";
                string pythonPath = GetPythonPath() ?? "python";

                await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = pythonPath,
                            Arguments = "-m pip install -r requirements.txt",
                            WorkingDirectory = projectPath,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        using Process? process = Process.Start(psi);
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit(120000);

                            Dispatcher.Invoke(() =>
                            {
                                OutputConsole.Text += output + "\n";
                                if (!string.IsNullOrEmpty(error))
                                    OutputConsole.Text += "⚠️ " + error + "\n";
                                OutputConsole.Text += "✅ Installation complete!\n";
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => OutputConsole.Text += $"❌ Error: {ex.Message}\n");
                    }
                });
            }
            catch (Exception ex)
            {
                OutputConsole.Text += $"❌ Error: {ex.Message}\n";
            }
        }

        private string? GetPythonPath()
        {
            string windowsPath = System.IO.Path.Combine(projectPath, "venv", "Scripts", "python.exe");
            string unixPath = System.IO.Path.Combine(projectPath, "venv", "bin", "python");

            if (File.Exists(windowsPath)) return windowsPath;
            if (File.Exists(unixPath)) return unixPath;

            return null;
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

        private async System.Threading.Tasks.Task CreateDefaultMonacoHtml(string path)
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
                var rootNodes = new ObservableCollection<FileNode>();
                rootNodes.Add(rootNode);
                FileTree.ItemsSource = rootNodes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FileNode BuildFileTree(string path)
        {
            var dirInfo = new DirectoryInfo(path);
            var node = new FileNode
            {
                Name = dirInfo.Name,
                FullPath = path,
                IsDirectory = true,
                Icon = GetFolderIcon()
            };

            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirNode = BuildFileTree(dir);
                    node.Children.Add(dirNode);
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

        private string GetFolderIcon() => "📁";
        private string GetFileIcon(string extension)
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
                if (treeViewItem?.DataContext is FileNode node)
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
                string fileName = Microsoft.VisualBasic.Interaction.InputBox("Enter file name:", "New File", "newfile.py");
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    string filePath = System.IO.Path.Combine(parentPath, fileName);
                    File.WriteAllText(filePath, "");
                    LoadFiles();
                }
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
                    if (node.IsDirectory)
                        Directory.Move(node.FullPath, newPath);
                    else
                        File.Move(node.FullPath, newPath);
                    LoadFiles();
                }
            }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (currentContextItem?.DataContext is FileNode node)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {node.Name}?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (node.IsDirectory)
                            Directory.Delete(node.FullPath, true);
                        else
                            File.Delete(node.FullPath);
                        LoadFiles();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OnTreeViewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Move;
        }

        private void OnTreeViewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                foreach (var file in files)
                {
                    string destPath = System.IO.Path.Combine(projectPath, System.IO.Path.GetFileName(file));
                    if (File.Exists(file))
                        File.Copy(file, destPath, true);
                }
                LoadFiles();
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
                Content = content
            };
            openTabs.Add(newTab);
            FileTabs.SelectedItem = newTab;
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

        private void OnCloseTabClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tab = button?.Tag as OpenTab;
            if (tab != null)
            {
                openTabs.Remove(tab);
                if (openTabs.Count > 0)
                    FileTabs.SelectedItem = openTabs.Last();
                else
                    currentFilePath = string.Empty;
            }
        }

        private async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            if (FileTabs.SelectedItem is OpenTab tab)
            {
                try
                {
                    if (!isWebViewReady || CodeWebView?.CoreWebView2 == null)
                    {
                        MessageBox.Show("Editor is not ready yet.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string code = await CodeWebView.CoreWebView2.ExecuteScriptAsync("getCode()");
                    code = code.Trim('"').Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"");
                    File.WriteAllText(tab.FilePath, code);
                    tab.Content = code;
                    OutputConsole.Text += $"✅ Saved: {System.IO.Path.GetFileName(tab.FilePath)}\n";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void OnRunClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FileTabs.SelectedItem is OpenTab tab && !string.IsNullOrEmpty(tab.FilePath))
                {
                    string code = await CodeWebView.CoreWebView2.ExecuteScriptAsync("getCode()");
                    code = code.Trim('"').Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"");
                    File.WriteAllText(tab.FilePath, code);
                    tab.Content = code;
                }

                string mainFile = System.IO.Path.Combine(projectPath, "main.py");
                if (!File.Exists(mainFile))
                {
                    MessageBox.Show("main.py not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                OutputConsole.Text += "🚀 Running...\n";
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
                };

                currentProcess.Start();
                currentProcess.BeginOutputReadLine();
                currentProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private string EscapeJs(string text)
        {
            return text.Replace("\\", "\\\\").Replace("`", "\\`").Replace("$", "\\$");
        }
    }
}
