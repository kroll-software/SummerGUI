using OpenTK;
using OpenTK.Graphics;

namespace SummerGUI
{
    struct ColouredVertex
    {
        public const int Size = (3 + 4) * 4; // size of struct in bytes

        public readonly Vector3 Position;
		public readonly Color4 Color;

        public ColouredVertex(Vector3 position, Color4 color)
        {
            Position = position;
            Color = color;
        }
    }
}
