using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Conda.Engine.Plugins
{
    public class PluginLoader
    {
        public List<IPlugin> Plugins = [];

        public void LoadPlugins(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                return;
            }

            foreach (var file in Directory.GetFiles(folder, "*.dll"))
            {
                try
                {
                    var assembly = Assembly.LoadFrom(file);

                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface)
                        {
                            var plugin = (IPlugin)Activator.CreateInstance(type)!;
                            plugin.Initialize();
                            Plugins.Add(plugin);
                            Console.WriteLine($"[PluginLoader] Loaded plugin: {plugin.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PluginLoader] Error loading plugin from {file}: {ex.Message}");
                }
            }
        }
    }
}
