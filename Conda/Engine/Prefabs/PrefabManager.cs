using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Conda.Engine.Prefabs
{
    public class PrefabManager
    {
        private readonly string prefabFolder;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public PrefabManager(string projectPath)
        {
            prefabFolder = Path.Combine(projectPath, "prefabs");

            if (!Directory.Exists(prefabFolder))
                Directory.CreateDirectory(prefabFolder);
        }

        public void SavePrefab(string name, object gameObject)
        {
            string json = JsonSerializer.Serialize(gameObject, JsonOptions);

            File.WriteAllText(Path.Combine(prefabFolder, name + ".json"), json);
        }

        public T LoadPrefab<T>(string name)
        {
            string path = Path.Combine(prefabFolder, name + ".json");

            if (!File.Exists(path))
                return default!;

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json)!;
        }
    }
}
