using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Conda.Engine.Physics
{
    public class Collider
    {
        public Rect Bounds { get; set; }

        public bool IsTrigger { get; set; } = false;
    }
}