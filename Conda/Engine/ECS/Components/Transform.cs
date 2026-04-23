namespace Conda.Engine.ECS.Components
{
    public class Transform : Component
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Rotation { get; set; }
        public double Scale { get; set; } = 1;
    }
}
