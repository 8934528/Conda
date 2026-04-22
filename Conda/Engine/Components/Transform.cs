using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conda.Engine.Components
{
    public class Transform : Component
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Rotation { get; set; }
        public double ScaleX { get; set; } = 1;
        public double ScaleY { get; set; } = 1;
    }
}
