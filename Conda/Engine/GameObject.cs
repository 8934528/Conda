using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conda.Engine.Components;
using Conda.Engine.Physics;

namespace Conda.Engine
{
    public class GameObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "GameObject";

        public Rigidbody? Rigidbody { get; set; }
        public Collider? Collider { get; set; }

        private readonly List<Component> components = [];

        public T AddComponent<T>() where T : Component, new()
        {
            var comp = new T
            {
                GameObject = this
            };
            components.Add(comp);
            return comp;
        }

        public T? GetComponent<T>() where T : Component
        {
            return components.OfType<T>().FirstOrDefault();
        }

        public List<Component> GetAllComponents() => components;
    }
}
