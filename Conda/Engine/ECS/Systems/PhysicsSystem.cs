using Conda.Engine.ECS;
using Conda.Engine.Physics;

namespace Conda.Engine.ECS.Systems
{
    public class PhysicsSystem(PhysicsEngine physics)
    {
        private readonly PhysicsEngine physics = physics;

        public void Update(float deltaTime)
        {
            physics.Update(deltaTime);
        }
    }
}
