using System;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using Buffer = SharpDX.Toolkit.Graphics.Buffer;

namespace BodyBasicsSharpDx
{
    public class CircleRenderer : IDisposable
    {
        private Buffer<VertexPositionColor> _vertices;
        private readonly GraphicsDevice _gd;
        private VertexInputLayout _layout;
        private BasicEffect _effect;
        private readonly int _segments;
        private Matrix _projection;

        public CircleRenderer(GraphicsDevice gd, int segments)
        {          
            _segments = segments;
            _gd = gd;
            
            CalculateCircleVertices();
        }

        public void CalculateCircleVertices()
        {
            float delta = MathUtil.TwoPi/_segments;
            float angle = 0;

            Vector2[] points = new Vector2[_segments];

            for (int i = 0; i < _segments; i++)
            {
                Vector2 point = new Vector2((float) Math.Cos(angle), (float) Math.Sin(angle));
                points[i] = point;
                angle += delta;
            }

            VertexPositionColor[] vertices = new VertexPositionColor[_segments*3];

            var color = Color.White;
            for (int i = 0; i < points.Length - 1; i++)
            {
                int bufferPos = i*3;
                vertices[bufferPos] = new VertexPositionColor(Vector3.Zero, color);
                vertices[bufferPos + 1] = new VertexPositionColor(new Vector3(points[i], 0f), color);
                vertices[bufferPos + 2] = new VertexPositionColor(new Vector3(points[i + 1], 0f), color);              
            }

            int lastIndex = (points.Length - 1)*3;
            vertices[lastIndex] = new VertexPositionColor(Vector3.Zero, color);
            vertices[lastIndex + 1] = new VertexPositionColor(new Vector3(points[points.Length - 1], 0f), color);
            vertices[lastIndex + 2] = new VertexPositionColor(new Vector3(points[0], 0f), color);

            _vertices = Buffer.Vertex.New(_gd, vertices);
            _layout = VertexInputLayout.FromBuffer(0, _vertices);
            _projection = Matrix.OrthoOffCenterLH(0, _gd.Viewport.Width, _gd.Viewport.Height, 0, 0, 1);
            _effect = new BasicEffect(_gd);
            _effect.VertexColorEnabled = true;
        }

        public void Update()
        {
            _projection = Matrix.OrthoOffCenterLH(0, _gd.Viewport.Width, _gd.Viewport.Height, 0, 0, 1);
        }

        public void Draw(float x, float y, float radius, Color color)
        {
            _effect.Projection =
                Matrix.Scaling(radius)
                * Matrix.Translation(-0.5f+x, -0.5f+y, 0)
                * _projection;

            _effect.DiffuseColor = color.ToVector4();
            _effect.CurrentTechnique.Passes[0].Apply();
            
            _gd.SetVertexBuffer(_vertices);
            _gd.SetVertexInputLayout(_layout);
            _gd.Draw(PrimitiveType.TriangleList, _vertices.ElementCount);
        }

        public void Dispose()
        {
            _effect.Dispose();
            _vertices.Dispose();
        }
    }
}
