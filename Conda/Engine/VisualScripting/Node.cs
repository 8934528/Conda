using System;
using System.Collections.Generic;

namespace Conda.Engine.VisualScripting
{
    public class Node
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "Start";
        public string Title { get; set; } = string.Empty;

        public double X { get; set; }
        public double Y { get; set; }

        public Dictionary<string, string> Properties { get; set; } = new();

        public List<string> Outputs { get; set; } = new();
    }
}
