using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conda.Engine.ECS
{
    public class Entity
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        private Dictionary<Type, object> components = new();

        public void Add<T>(T component)
        {
            components[typeof(T)] = component;
        }

        public T Get<T>()
        {
            return components.ContainsKey(typeof(T))
                ? (T)components[typeof(T)]
                : default;
        }

        public bool Has<T>()
        {
            return components.ContainsKey(typeof(T));
        }
    }
}
