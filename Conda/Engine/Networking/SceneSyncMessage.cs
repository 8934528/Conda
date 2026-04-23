namespace Conda.Engine.Networking
{
    public class SceneSyncMessage
    {
        public string Type { get; set; } = ""; // Move, Add, Delete
        public string ObjectId { get; set; } = "";

        public double X { get; set; }
        public double Y { get; set; }
    }
}
