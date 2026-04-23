using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System;
using Conda.Engine.ECS;
using Conda.Engine.ECS.Components;

namespace Conda.Engine.ECS.Systems
{
    public class RenderSystem(Canvas canvas)
    {
        private readonly Canvas canvas = canvas;

        public void Render(World world)
        {
            canvas.Children.Clear();

            foreach (var entity in world.Entities)
            {
                if (!entity.Has<Transform>() || !entity.Has<Sprite>())
                    continue;

                var transform = entity.Get<Transform>();
                var sprite = entity.Get<Sprite>();

                var img = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri(sprite.ImagePath, UriKind.RelativeOrAbsolute)),
                    Width = sprite.Width,
                    Height = sprite.Height
                };

                Canvas.SetLeft(img, transform.X);
                Canvas.SetTop(img, transform.Y);

                canvas.Children.Add(img);
            }
        }
    }
}
