using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conda.Editor
{
    public class FileNode
    {
        public string Name { get; set; } = string.Empty;
        public string IconKind { get; set; } = "File";
        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public ObservableCollection<FileNode> Children { get; set; } = [];
        public bool IsExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;
    }
}
