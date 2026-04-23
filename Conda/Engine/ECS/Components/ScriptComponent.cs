using System;

namespace Conda.Engine.ECS.Components
{
    public class ScriptComponent : Component
    {
        public string ScriptPath { get; set; } = string.Empty;
        public Conda.Engine.VisualScripting.NodeGraph? Graph { get; set; }
    }
}
