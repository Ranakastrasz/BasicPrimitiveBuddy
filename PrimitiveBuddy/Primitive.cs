using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Vector2Extensions;

namespace PrimitiveBuddy
{
    /// <summary>
    /// Render a simple 2D shape.
    /// </summary>
    public class Primitive: IPrimitive, IDisposable
    {
        #region Fields

        /// <summary>
        /// The position of the primitive object.
        /// </summary>
        private Vector2 _position = Vector2.Zero;

        /// <summary>
        /// The sprite batch this dude gonna render with.
        /// </summary>
        private readonly SpriteBatch _spriteBatch;

        private Texture2D _texture;

        #endregion // Fields

        #region Properties

        /// <summary>
        /// Blank 1x1 pixel that creates the shape.
        /// </summary>
        public Texture2D Texture
        {
            get => _texture;
            set
            {
                _texture = value;
                _manageTexture = false;
            }
        }

        private bool _manageTexture;

        /// <summary>
        /// Get/Set the color of the primitive object.
        /// </summary>
        private Color Color { get; set; }

        /// <summary>
        /// List of vectors.
        /// </summary>
        private List<Vector2> VectorList { get; set; }

        /// <summary>
        /// Gets or sets the position of the primitive object.
        /// </summary>
        public Vector2 Position
        {
            get => _position; set => _position = value;
        }

        /// <summary>
        /// Gets or sets the render depth of the primitive line object (0 = front, 1 = back).
        /// </summary>
        public float Depth { get; set; }

        /// <summary>
        /// Gets or sets the thickness of the shape's edge.
        /// </summary>
        public float Thickness { get; set; }

        public int NumCircleSegments { get; set; }

        #endregion // Properties

        #region Initialization

        /// <summary>
        /// Creates a new primitive object.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device object to use.</param>
        /// <param name="spriteBatch">The SpriteBatch object to use.</param>
        public Primitive(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            NumCircleSegments = 20;
            _spriteBatch = spriteBatch;
            VectorList = [];
            Color = Color.White;

            // Create the pixel texture.
            _manageTexture = true;
            _texture = new Texture2D(graphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Texture.SetData<Color>(new Color[] { Color.White });
            Thickness = 1.0f;
        }

        /// <summary>
        /// Creates a new primitive object.
        /// </summary>
        /// <param name="texture">A texture to use to draw.</param>
        /// <param name="spriteBatch">The SpriteBatch object to use.</param>
        public Primitive(Texture2D texture, SpriteBatch spriteBatch)
        {
            NumCircleSegments = 20;
            _spriteBatch = spriteBatch;
            VectorList = [];
            Color = Color.White;

            // Create the pixel texture.
            Texture = texture;
            Thickness = 1.0f;
        }

        /// <summary>
        /// Get ready to draw a new primitive.
        /// </summary>
        private void Clear()
        {
            Color = Color.White;
            Position = Vector2.Zero;
            VectorList.Clear();
        }

        public void Dispose()
        {
            if(_manageTexture && (_texture != null))
            {
                _texture.Dispose();
                _texture = null;
            }
        }

        #endregion // Initialization

        #region Creation Methods

        /// <summary>
        /// Create a line primitive.
        /// </summary>
        /// <param name="start">Start of the line, in pixels.</param>
        /// <param name="end">End of the line, in pixels.</param>
        private void CreateLine(Vector2 start, Vector2 end)
        {
            VectorList.Add(start);
            VectorList.Add(end);
        }

        /// <summary>
        /// Create a triangle primitive.
        /// </summary>
        /// <param name="p1">First point, in pixels.</param>
        /// <param name="p2">Second point, in pixels.</param>
        /// <param name="p3">Third point, in pixels.</param>
        private void CreateTriangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            VectorList.Add(p1);
            VectorList.Add(p2);
            VectorList.Add(p3);
            VectorList.Add(p1);
        }

        /// <summary>
        /// Create a square primitive.
        /// </summary>
        /// <param name="topLeft">Top lefthand corner of the square.</param>
        /// <param name="bottomRight">Bottom righthand corner of the square.</param>
        private void CreateSquare(Vector2 topLeft, Vector2 bottomRight)
        {
            VectorList.Add(topLeft);
            VectorList.Add(new Vector2(topLeft.X, bottomRight.Y));
            VectorList.Add(bottomRight);
            VectorList.Add(new Vector2(bottomRight.X, topLeft.Y));
            VectorList.Add(topLeft);
        }

        /// <summary>
        /// Create a polygon primitive.
        /// </summary>
        /// <param name="points">The polygon vertices in order.</param>
        private void CreatePolygon(IReadOnlyList<Vector2> points)
        {
            if(points == null || points.Count < 2)
                return;

            for(int i = 0; i < points.Count; i++)
                VectorList.Add(points[i]);

            // Close the polygon by connecting the last point back to the first.
            VectorList.Add(points[0]);
        }

        /// <summary>
        /// Build the vertices of a regular N-sided polygon.
        /// </summary>
        private static List<Vector2> CreateRegularPolygonPoints(Vector2 center, float radius, int sides, float rotation)
        {
            List<Vector2> points = new(sides);

            if(sides < 3 || radius <= 0f)
                return points;

            float step = MathHelper.TwoPi / sides;

            for(int i = 0; i < sides; i++)
            {
                float angle = rotation + (i * step);
                float x = center.X + (radius * (float)Math.Cos(angle));
                float y = center.Y + (radius * (float)Math.Sin(angle));
                points.Add(new Vector2(x, y));
            }

            return points;
        }

        /// <summary>
        /// Creates a circle starting from (0, 0).
        /// </summary>
        /// <param name="radius">The radius (half the width) of the circle.</param>
        /// <param name="sides">The number of sides on the circle. (64 is average).</param>
        private void CreateCircle(float radius, int sides) => CreateArc(radius, sides, 0, MathHelper.TwoPi);

        /// <summary>
        /// Creates an arc starting from (0, 0).
        /// </summary>
        /// <param name="radius">The radius of the arc.</param>
        /// <param name="sides">The number of segments in the arc.</param>
        /// <param name="startAngle">The starting angle.</param>
        /// <param name="sweepAngle">The sweep angle.</param>
        private void CreateArc(float radius, int sides, float startAngle, float sweepAngle)
        {
            float step = sweepAngle / sides;

            // Create the full circle.
            int numSteps = 0;
            float theta = startAngle;
            while(numSteps < sides + 1)
            {
                VectorList.Add(new Vector2(radius * (float)Math.Cos(theta),
                                     radius * (float)Math.Sin(theta)));
                numSteps++;
                theta += step;
            }
        }

        /// <summary>
        /// Creates an ellipse starting from (0, 0) with the given width and height.
        /// Vectors are generated using the parametric equation of an ellipse.
        /// </summary>
        /// <param name="semiMajorAxis">The width of the ellipse at its center.</param>
        /// <param name="semiMinorAxis">The height of the ellipse at its center.</param>
        /// <param name="sides">The number of sides on the ellipse. (64 is average).</param>
        private void CreateEllipse(float semiMajorAxis, float semiMinorAxis, int sides)
        {
            // Local variables.
            float fMax = MathHelper.TwoPi;
            float fStep = fMax / sides;

            // Create the full ellipse.
            for(float fTheta = fMax; fTheta >= -1; fTheta -= fStep)
            {
                VectorList.Add(new Vector2((float)(semiMajorAxis * Math.Cos(fTheta)),
                                     (float)(semiMinorAxis * Math.Sin(fTheta))));
            }
        }

        #endregion // Creation Methods

        #region Render Methods

        /// <summary>
        /// Renders the lines of the primitive.
        /// </summary>
        /// <param name="_spriteBatch">The sprite batch to use to render the primitive object.</param>
        private void RenderLinePrimitive()
        {
            // Validate.
            if(VectorList.Count < 2)
                return;

            // Local variables.
            Vector2 unused = Vector2.Zero;
            Vector2 unused1 = Vector2.Zero;
            Vector2 origin = new(0, 0.5f);

            // Run through the list of vectors.
            for(int i = VectorList.Count - 1; i >= 1; --i)
            {
                // Store positions.
                Vector2 position1 = VectorList[i - 1];
                Vector2 position2 = VectorList[i];

                // Calculate the distance between the two vectors.
                Vector2 finalPosition = Position + position1;
                float distance = Vector2.Distance(position1, position2);
                Rectangle destinationRect = new((int)finalPosition.X, (int)finalPosition.Y, (int)distance, (int)Thickness);

                // Calculate the angle between the two vectors.
                float angle = (float)Math.Atan2((double)(position2.Y - position1.Y),
                                     (double)(position2.X - position1.X));

                _spriteBatch.Draw(Texture, destinationRect, null, Color, angle, origin, SpriteEffects.None, Depth);
            }
        }

        #endregion // Render Methods

        #region Public Methods

        /// <summary>
        /// Rotate the primitive object around a pivot.
        /// </summary>
        /// <param name="angle">The counterclockwise rotation in radians. (0.0f is the default).</param>
        /// <param name="pivot">The position around which to rotate.</param>
        public void Rotate(float angle, Vector2 pivot)
        {
            // Subtract the pivot from all points.
            for(int i = VectorList.Count - 1; i >= 0; --i)
                VectorList[i] -= pivot;

            // Rotate about the origin.
            Matrix mat = Matrix.CreateRotationZ(angle);
            for(int i = VectorList.Count - 1; i >= 0; --i)
                VectorList[i] = Vector2.Transform(VectorList[i], mat);

            // Add the pivot to all points.
            for(int i = VectorList.Count - 1; i >= 0; --i)
                VectorList[i] += pivot;
        }

        /// <summary>
        /// Draw a single point.
        /// </summary>
        /// <param name="position">Where to draw the point.</param>
        /// <param name="color">The color of the point.</param>
        public void Point(Vector2 position, Color color)
        {
            Clear();
            Position = position;
            Color = color;
            CreateCircle(1.0f, NumCircleSegments);
            RenderLinePrimitive();
        }

        /// <summary>
        /// Draw a quick circle.
        /// </summary>
        /// <param name="position">Where to draw the circle.</param>
        /// <param name="radius">The radius of the desired circle.</param>
        /// <param name="color">The color of the circle to draw.</param>
        public void Circle(Vector2 position, float radius, Color color)
        {
            Clear();
            Position = position;
            Color = color;
            CreateCircle(radius, NumCircleSegments);
            RenderLinePrimitive();
        }

        /// <summary>
        /// Draw a quick line.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        /// <param name="color">The color of the line.</param>
        public void Line(Vector2 start, Vector2 end, Color color)
        {
            Clear();
            Color = color;
            CreateLine(start, end);
            RenderLinePrimitive();
        }

        /// <summary>
        /// Draw a quick box.
        /// </summary>
        /// <param name="upperLeft">The top-left point.</param>
        /// <param name="lowerRight">The bottom-right point.</param>
        /// <param name="color">The color of the box.</param>
        public void AxisAlignedBox(Vector2 upperLeft, Vector2 lowerRight, Color color)
        {
            Clear();
            Color = color;
            CreateSquare(upperLeft, lowerRight);
            RenderLinePrimitive();
        }

        /// <summary>
        /// Draw a rectangle.
        /// </summary>
        /// <param name="rect">The rectangle to draw.</param>
        /// <param name="color">The color to use.</param>
        public void Rectangle(Rectangle rect, Color color)
        {
            Clear();
            AxisAlignedBox(new Vector2(rect.Left, rect.Top),
                      new Vector2(rect.Left + rect.Width, rect.Top + rect.Height),
                      color);
        }

        /// <summary>
        /// Draw a rotated rectangle.
        /// This is the easiest way to draw a rectangle.
        /// </summary>
        /// <param name="upperLeft">The top-left point.</param>
        /// <param name="lowerRight">The bottom-right point.</param>
        /// <param name="rotation">The rotation angle in radians.</param>
        /// <param name="scale">The scale to draw the rectangle.</param>
        /// <param name="color">The color to use.</param>
        public void Rectangle(Vector2 upperLeft, Vector2 lowerRight, float rotation, float scale, Color color)
        {
            Clear();
            Color = color;
            CreateSquare(upperLeft, lowerRight);
            Rotate(rotation, upperLeft); // This probably doesn't work.
            RenderLinePrimitive();
        }

        /// <summary>
        /// Draw a polygon outline.
        /// </summary>
        /// <param name="points">The polygon vertices in order.</param>
        /// <param name="color">The color to draw the polygon.</param>
        public void Polygon(IReadOnlyList<Vector2> points, Color color)
        {
            Clear();
            Color = color;
            CreatePolygon(points);
            RenderLinePrimitive();
        }

        /// <summary>
        /// Draw a regular polygon outline centered on a point.
        /// </summary>
        /// <param name="center">The center of the polygon.</param>
        /// <param name="radius">The distance from the center to the vertices.</param>
        /// <param name="sides">The number of sides of the polygon.</param>
        /// <param name="rotation">The rotation of the polygon in radians.</param>
        /// <param name="color">The color to draw the polygon.</param>
        public void RegularPolygon(Vector2 center, float radius, int sides, float rotation, Color color)
        {
            List<Vector2> points = CreateRegularPolygonPoints(center, radius, sides, rotation);
            Polygon(points, color);
        }

        /// <summary>
        /// Draw a pie shape.
        /// </summary>
        /// <param name="position">The location to draw the pie.</param>
        /// <param name="radius">The radius of the pie.</param>
        /// <param name="startAngle">The starting angle of the pie.</param>
        /// <param name="sweepAngle">The sweep angle of the pie.</param>
        /// <param name="color">Color dat pie.</param>
        public void Pie(Vector2 position, float radius, float startAngle, float sweepAngle, Color color)
        {
            Clear();
            Position = position;
            Color = color;

            VectorList.Add(Vector2.Zero);
            CreateArc(radius, NumCircleSegments, startAngle, sweepAngle);
            VectorList.Add(Vector2.Zero);

            RenderLinePrimitive();
        }

        /// <summary>
        /// Draw a sine wave.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        /// <param name="frequency">The frequency of the wave.</param>
        /// <param name="amplitude">The amplitude of the wave.</param>
        /// <param name="color">The color to use.</param>
        public void SineWave(Vector2 start, Vector2 end, float frequency, float amplitude, Color color)
        {
            Clear();
            Color = color;

            // Get the length of the sine wave.
            float length = (end - start).Length();

            // Lay out all the points.
            float segmentLength = 5f;
            float xPos = 0f;
            float mid = length / 2f;
            while((xPos + segmentLength) < length)
            {
                float ratio = 1f - (Math.Abs(mid - xPos) / mid);
                VectorList.Add(new Vector2(xPos, (float)ratio * (float)(Math.Sin(frequency * xPos) * amplitude)));
                xPos += segmentLength;
            }

            // Add the last line segment at the end.
            VectorList.Add(new Vector2(length, 0f));

            // Create the rotation matrix.
            Matrix mat = Matrix.CreateRotationZ((end - start).Angle());

            // Multiply the rotation by the translation matrices.
            mat *= Matrix.CreateTranslation(start.X, start.Y, 0f);

            // Multiply each point by the combined matrix.
            for(int i = 0; i < VectorList.Count; i++)
            {
                VectorList[i] = MatrixExtensions.MatrixExt.Multiply(mat, VectorList[i]);
            }

            RenderLinePrimitive();
        }

        #endregion // Public Methods
    }
}