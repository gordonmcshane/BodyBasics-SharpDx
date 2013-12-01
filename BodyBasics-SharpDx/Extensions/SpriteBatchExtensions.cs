using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Toolkit.Graphics;

namespace BodyBasicsSharpDx.Extensions
{
    public static class SpriteBatchExtensions
    {
        private static readonly Dictionary<GraphicsDevice, Texture2D> Textures = new Dictionary<GraphicsDevice, Texture2D>();

        public static void DrawLine(this SpriteBatch spriteBatch, Vector2 begin, Vector2 end, Color color, int width = 1)
        {
            Rectangle rect = new Rectangle((int)begin.X, (int)begin.Y, (int)(end - begin).Length() + width, width);
            Vector2 v = Vector2.Normalize(begin - end);
            
            float angle = (float)Math.Acos(Vector2.Dot(v, -Vector2.UnitX));
            
            if (begin.Y > end.Y) 
                angle = MathUtil.TwoPi - angle;
            
            spriteBatch.Draw(GetLineTexture(spriteBatch.GraphicsDevice), rect, null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
        }

        private static Texture2D GetLineTexture(GraphicsDevice graphicsDevice)
        {
            if (Textures.ContainsKey(graphicsDevice)) 
                return Textures[graphicsDevice];

            Texture2D texture = CreateLineTexture(graphicsDevice);
            Textures.Add(graphicsDevice, texture);

            return texture;
        }

        private static Texture2D CreateLineTexture(GraphicsDevice graphicsDevice)
        {
            Color[] colorBuffer = { Color.White };
            return Texture2D.New(graphicsDevice, 1, 1, PixelFormat.B8G8R8A8.UNorm, colorBuffer);
        }
    }
}
