using System;

namespace Cobra.Engine.ECS.Components
{
    public class ScriptComponent : Component
    {
        public string ScriptPath { get; set; } = string.Empty;
        public Cobra.Engine.VisualScripting.NodeGraph? Graph { get; set; }
    }
}
