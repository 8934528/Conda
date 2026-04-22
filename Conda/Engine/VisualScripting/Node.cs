using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conda.Engine.VisualScripting
{
    public class Node
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }

        public double X { get; set; }
        public double Y { get; set; }

        public List<Node> Outputs { get; set; } = new();
    }
}
