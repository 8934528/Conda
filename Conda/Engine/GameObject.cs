using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conda.Engine.Components;

namespace Conda.Engine
{
    public class GameObject
    {
        public string Name { get; set; } = "GameObject";

        private List<Component> components = new();

        public T AddComponent<T>() where T : Component, new()
        {
            var comp = new T();
            comp.GameObject = this;
            components.Add(comp);
            return comp;
        }

        public T GetComponent<T>() where T : Component
        {
            return components.OfType<T>().FirstOrDefault();
        }

        public List<Component> GetAllComponents() => components;
    }
}
