using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conda.Engine.Physics
{
    public static class Collision
    {
        public static bool Check(Collider a, Collider b)
        {
            return a.Bounds.IntersectsWith(b.Bounds);
        }
    }
}
