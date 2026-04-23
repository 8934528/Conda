namespace Conda.Engine.ECS.Components
{
    public class Sprite : Component
    {
        public string ImagePath { get; set; } = string.Empty;
        public double Width { get; set; } = 64;
        public double Height { get; set; } = 64;
        public string Color { get; set; } = "#00C8FF";
    }
}
