using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conda.Engine.Components
{
    public class Sprite : Component
    {
        public string Color { get; set; } = "#00C8FF";
        public double Width { get; set; } = 100;
        public double Height { get; set; } = 100;
    }
}
