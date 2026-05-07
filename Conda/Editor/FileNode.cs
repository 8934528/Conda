using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conda.Editor
{
    public class FileNode : System.ComponentModel.INotifyPropertyChanged
    {
        private string name = string.Empty;
        private string iconKind = "File";
        private bool isExpanded;
        private bool isSelected;

        public string Name 
        { 
            get => name; 
            set { name = value; OnPropertyChanged(); } 
        }

        public string IconKind 
        { 
            get => iconKind; 
            set { iconKind = value; OnPropertyChanged(); } 
        }

        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public ObservableCollection<FileNode> Children { get; set; } = [];

        public bool IsExpanded 
        { 
            get => isExpanded; 
            set { isExpanded = value; OnPropertyChanged(); } 
        }

        public bool IsSelected 
        { 
            get => isSelected; 
            set { isSelected = value; OnPropertyChanged(); } 
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
