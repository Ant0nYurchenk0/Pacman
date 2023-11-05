namespace Packman
{
    internal class Cell
    {
        public bool[] Walls { get; set; } = new bool[4];
        public bool Visited { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

    }
}