namespace Conda.Engine.ECS.Components
{
    public class Sprite : IComponent
    {
        public string Path = "";
        public double Width = 100;
        public double Height = 100;
        public string Color { get; set; } = "";
    }
}
