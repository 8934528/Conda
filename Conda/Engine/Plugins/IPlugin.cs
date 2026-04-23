namespace Conda.Engine.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        void Initialize();
        void Update();
    }
}
