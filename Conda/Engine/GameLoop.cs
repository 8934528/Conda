using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace Conda.Engine
{
    public class GameLoop
    {
        private Stopwatch stopwatch = new();
        private DispatcherTimer? timer;

        public Action<float>? OnUpdate;

        public void Start()
        {
            stopwatch.Start();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            timer.Tick += Loop;
            timer.Start();
        }

        private void Loop(object? sender, EventArgs e)
        {
            float deltaTime = (float)stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();

            OnUpdate?.Invoke(deltaTime);
        }

        public void Stop()
        {
            timer?.Stop();
        }
    }
}
