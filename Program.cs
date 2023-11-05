namespace Packman
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var mazeBuilder = new MazeBuilder();
            mazeBuilder.Width = 7;
            mazeBuilder.Height = 7;
            var maze = mazeBuilder.CreateMaze();

            var level = mazeBuilder.GetLevel(maze);

            var game = new Game(level);
            game.Play();
        }
    }
}