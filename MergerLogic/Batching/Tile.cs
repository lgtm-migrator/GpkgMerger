using MergerLogic.DataTypes;
using MergerLogic.ImageProcessing;

namespace MergerLogic.Batching
{
    public delegate Tile? CorrespondingTileBuilder();

    public class Tile
    {
        public int Z
        {
            get;
            private set;
        }

        public int X
        {
            get;
            private set;
        }

        public int Y
        {
            get;
            internal set;
        }

        public TileFormat? Format { get; internal set; }

        private byte[] _data;

        public Tile(int z, int x, int y, byte[] data, TileFormat? format = null)
        {
            this.Z = z;
            this.X = x;
            this.Y = y;
            this._data = data;
            this.Format = format;
        }

        public Tile(Coord cords, byte[] data, TileFormat? format = null)
        {
            this.Z = cords.Z;
            this.X = cords.X;
            this.Y = cords.Y;
            this._data = data;
            this.Format = format;
        }

        public bool HasCoords(int z, int x, int y)
        {
            return z == this.Z && x == this.X && y == this.Y;
        }

        public Coord GetCoord()
        {
            return new Coord(this.Z, this.X, this.Y);
        }

        public virtual void Print()
        {
            Console.WriteLine($"z: {this.Z}");
            Console.WriteLine($"x: {this.X}");
            Console.WriteLine($"y: {this.Y}");
            // Console.WriteLine($"blob: {this.Blob}");
            Console.WriteLine($"data Size: {this._data.Length}");
        }

        public virtual byte[] GetImageBytes()
        {
            return this._data;
        }

        public void SetCoords(Coord cords)
        {
            this.X = cords.X;
            this.Y = cords.Y;
            this.Z = cords.Z;
        }

        public void SetCoords(int z, int x, int y)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public override string ToString()
        {
            return $"z: {this.Z}, x: {this.X}, y: {this.Y}, data size: {this._data.Length}";
        }
    }
}
