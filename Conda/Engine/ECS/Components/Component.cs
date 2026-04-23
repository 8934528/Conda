using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conda.Engine.ECS.Components
{
    public abstract class Component
    {
        public GameObject? GameObject { get; set; }
        public string Title { get; set; } = string.Empty;

        public virtual void Start() { }
        public virtual void Update() { }
    }
}
