using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conda.Engine.VisualScripting;

namespace Conda.Engine.Components
{
    public class ScriptComponent : Component
    {
        public NodeGraph Graph { get; set; } = new();
    }
}
