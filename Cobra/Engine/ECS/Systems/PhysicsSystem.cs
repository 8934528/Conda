using Cobra.Engine.ECS;
using Cobra.Engine.Physics;

namespace Cobra.Engine.ECS.Systems
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
