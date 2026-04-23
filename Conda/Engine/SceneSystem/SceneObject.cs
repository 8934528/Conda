using System;

namespace Conda.Engine.SceneSystem
{
    public class SceneObject
    {
        public string Name { get; set; } = "GameObject";

        public double X { get; set; }
        public double Y { get; set; }

        public double Width { get; set; } = 100;
        public double Height { get; set; } = 100;

        public double Rotation { get; set; } = 0; // NEW

        public string SpritePath { get; set; } = "";
        public string ScriptPath { get; set; } = "";

        public bool IsSelected { get; set; }
    }
}
