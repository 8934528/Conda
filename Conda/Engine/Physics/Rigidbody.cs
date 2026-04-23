using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conda.Engine.Physics
{
    public class Rigidbody
    {
        public Vector Velocity { get; set; } = new Vector(0, 0);
        public Vector Acceleration { get; set; } = new Vector(0, 0);

        public float Mass { get; set; } = 1f;
        public bool UseGravity { get; set; } = true;

        public void Update(float deltaTime)
        {
            Velocity += Acceleration * deltaTime;

            if (UseGravity)
                Velocity += new Vector(0, 9.8f) * deltaTime;
        }
    }
}
