using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conda.Engine.SceneSystem
{
    public class SceneObject
    {
        public string Name { get; set; } = "Object";
        public string Type { get; set; } = "Rectangle"; // Image later
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 100;
        public double Height { get; set; } = 100;
        public double Rotation { get; set; } = 0;
        public string AssetPath { get; set; } = "";
    }
}
