using System.Windows.Controls;
using Game.Config;
using Game.Models;

namespace Game.Services
{
    public class TileMapRenderer
    {
        private readonly Canvas _canvas;

        public TileMapRenderer(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void Render(LevelMap map, Image playerImage)
        {
            _canvas.Children.Clear();

            // Dit gedeelte bepaalt hoe groot de map is 'automatisch'. Zonder dit werkt het scrollen niet.
            _canvas.Width = map.Width * GameConfig.TileSize;
            _canvas.Height = map.Height * GameConfig.TileSize;

            for (int r = 0; r < map.Height; r++)
            {
                for (int c = 0; c < map.Width; c++)
                {
                    int tileId = map.Layout[r, c];

                    if (!map.TileRegistry.TryGetValue(tileId, out var tileType))
                        continue;

                    Image tile = new Image
                    {
                        Width = GameConfig.TileSize,
                        Height = GameConfig.TileSize,
                        Source = AssetCache.GetImage(tileType.AssetName),
                        Stretch = System.Windows.Media.Stretch.Fill
                    };

                    Canvas.SetLeft(tile, c * GameConfig.TileSize);
                    Canvas.SetTop(tile, r * GameConfig.TileSize);

                    _canvas.Children.Add(tile);
                }
            }

            if (!_canvas.Children.Contains(playerImage))
            {
                _canvas.Children.Add(playerImage);
            }
        }
    }
}