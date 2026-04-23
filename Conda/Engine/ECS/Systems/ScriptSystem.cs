using System.Diagnostics;
using Conda.Engine.ECS.Components;

namespace Conda.Engine.ECS.Systems
{
    public class ScriptSystem
    {
        public static void Update(World world)
        {
            foreach (var (entity, transform, script) in world.Query<Transform, Script>())
            {
                if (string.IsNullOrEmpty(script.ScriptPath))
                    continue;

                ProcessStartInfo psi = new()
                {
                    FileName = "python",
                    Arguments = $"\"{script.ScriptPath}\" {transform.X} {transform.Y}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                try
                {
                    using var process = Process.Start(psi);
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        var parts = output.Split(',');

                        if (parts.Length == 2)
                        {
                            if (double.TryParse(parts[0], out double x) && double.TryParse(parts[1], out double y))
                            {
                                transform.X = x;
                                transform.Y = y;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Script error: {ex.Message}");
                }
            }
        }
    }
}
