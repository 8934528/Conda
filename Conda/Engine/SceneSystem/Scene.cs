using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Conda.Engine.SceneSystem
{
    public class Scene
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true
        };

        public ObservableCollection<SceneObject> Objects { get; set; } = [];

        public void Save(string path)
        {
            var json = JsonSerializer.Serialize(this, _options);

            File.WriteAllText(path, json);
        }

        public static Scene Load(string path)
        {
            if (!File.Exists(path)) return new Scene();

            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<Scene>(json) ?? new Scene();
        }
    }
}
