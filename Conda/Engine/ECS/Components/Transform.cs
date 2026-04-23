namespace Conda.Engine.ECS.Components
{
    public class Transform : IComponent
    {
        public double X;
        public double Y;
        public double Rotation;
        public double ScaleX = 1;
        public double ScaleY = 1;
    }
}
