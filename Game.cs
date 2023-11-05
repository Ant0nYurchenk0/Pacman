
using System.Reflection.Metadata.Ecma335;

namespace Packman
{
    internal class Game
    {
        public List<List<LevelCell>> Level { get; set; }
        private List<LevelCell> flattenLevel;
        private bool gameOver => candies == 0 || pacman.Item3 == false;
        private int candies;

        private (int, int, bool) pacman;
        private Action playerAction = Action.Stop;
        private Action previousPlayerAction = Action.Stop;

        private List<Ghost> ghosts;

        public Game(List<List<int>> level)
        {
            Level = new();
            for (int i = 0; i < level.Count; i++)
            {
                Level.Add(new List<LevelCell>());
                for (int j = 0; j < level[i].Count; j++)
                {
                    if (level[i][j] == 0) Level[i].Add(new LevelCell()
                    {
                        CellStatus = CellStatus.Wall,
                        Width = i,
                        Height = j,
                    });
                    else Level[i].Add(new LevelCell()
                    {
                        CellStatus = CellStatus.Candy,
                        Width = i,
                        Height = j,
                    });
                }
            }
            flattenLevel = Level.SelectMany(x => x).ToList();
            candies = flattenLevel.Where(c => c.CellStatus == CellStatus.Candy).Count();
            var rand = new Random();
            initPlayer(rand);
            initGhosts(rand, candies / 50 + 1);
        }
        public void Play()
        {
            Task.Run(() => draw());
            Task.Run(() => readUserInput());
            Task.Run(()=>moveGhosts());
            while (!gameOver)
            {
                movePacman(playerAction);
            }
            if(candies == 0)
            {
                Console.WriteLine("You won!!!");
            }
            else
            {
                Console.Write("You lost :(");
            }
        }

        private void readUserInput()
        {
            while (!gameOver)
            {

                var keyInfo = Console.ReadKey();

                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        playerAction = Action.Up;
                        break;
                    case ConsoleKey.DownArrow:
                        playerAction = Action.Down;
                        break;
                    case ConsoleKey.LeftArrow:
                        playerAction = Action.Left;
                        break;
                    case ConsoleKey.RightArrow:
                        playerAction = Action.Right;
                        break;

                }
            }
        }

        private void movePacman(Action playerAction, bool error = false)
        {
            try
            {
                LevelCell cell = null;
                switch (playerAction)
                {
                    case Action.Stop:
                        cell = flattenLevel.SingleOrDefault(c => c.Width == pacman.Item1 && c.Height == pacman.Item2 && c.CellStatus != CellStatus.Wall);
                        break;
                    case Action.Left:
                        cell = flattenLevel.SingleOrDefault(c => c.Width == pacman.Item1 && c.Height == pacman.Item2 - 1 && c.CellStatus != CellStatus.Wall);
                        break;
                    case Action.Right:
                        cell = flattenLevel.SingleOrDefault(c => c.Width == pacman.Item1 && c.Height == pacman.Item2 + 1 && c.CellStatus != CellStatus.Wall);

                        break;
                    case Action.Up:
                        cell = flattenLevel.SingleOrDefault(c => c.Width == pacman.Item1 - 1 && c.Height == pacman.Item2 && c.CellStatus != CellStatus.Wall);

                        break;
                    case Action.Down:
                        cell = flattenLevel.SingleOrDefault(c => c.Width == pacman.Item1 + 1 && c.Height == pacman.Item2 && c.CellStatus != CellStatus.Wall);

                        break;
                    default:
                        break;
                }
                if (cell != null)
                {
                    processPacmanMovement(cell);
                }
                else
                {
                    throw new ArgumentException();
                }
                Thread.Sleep(200);
                previousPlayerAction = playerAction;
            }
            catch (ArgumentException)
            {
                if (!error) movePacman(previousPlayerAction, true);
            }


        }

        private void processPacmanMovement(LevelCell cell)
        {
            if (cell.CellStatus == CellStatus.Candy || cell.CellStatus == CellStatus.GhostWCandy)
            {
                candies--;
            }
            if (cell.CellStatus == CellStatus.Ghost || cell.CellStatus == CellStatus.GhostWCandy)
            {
                pacman.Item3 = false;
            }
            var oldCell = flattenLevel.SingleOrDefault(c => c.CellStatus == CellStatus.Pacman);
            if (oldCell != null) oldCell.CellStatus = CellStatus.Empty;
            cell.CellStatus = CellStatus.Pacman;
            pacman = (cell.Width, cell.Height, pacman.Item3);
        }

        private void initPlayer(Random rand)
        {
            var possibleCells = flattenLevel.Where(c => c.CellStatus != CellStatus.Wall);
            var cell = possibleCells.ElementAt(rand.Next(possibleCells.Count()));
            pacman = (cell.Width, cell.Height, true);
        }

        private void initGhosts(Random rand, int numOfGhosts)
        {
            ghosts = new();
            for (int i = 0; i < numOfGhosts; i++)
            {
                var possibleCells = flattenLevel.Where(c => c.CellStatus != CellStatus.Wall && c.CellStatus != CellStatus.Pacman && c.CellStatus != CellStatus.Ghost && isFarFromPacman(c));
                var cell = possibleCells.ElementAt(rand.Next(possibleCells.Count()));
                ghosts.Add(new Ghost()
                {
                    Height = cell.Height,
                    Width = cell.Width,
                });
            }
        }

        private bool isFarFromPacman(LevelCell levelCell)
        {
            if (Math.Pow(levelCell.Width - pacman.Item1, 2) + Math.Pow(levelCell.Height - pacman.Item2, 2) < 25) return false;
            return true;
        }

        private void moveGhosts()
        {
            Thread.Sleep(500);
            while(!gameOver)
            {
                foreach (var ghost in ghosts.AsParallel())
                {

                    var previousCell = flattenLevel.Single(c => c.Width == ghost.Width && c.Height == ghost.Height);
                    if(previousCell.CellStatus == CellStatus.Ghost) previousCell.CellStatus = CellStatus.Empty;
                    if(previousCell.CellStatus == CellStatus.GhostWCandy) previousCell.CellStatus = CellStatus.Candy;

                    var nextCell = getCellClosestToPacman(previousCell);
                    if (nextCell.CellStatus == CellStatus.Candy) nextCell.CellStatus = CellStatus.GhostWCandy;
                    if(nextCell.CellStatus == CellStatus.Empty) nextCell.CellStatus = CellStatus.Ghost;
                    if (nextCell.CellStatus == CellStatus.Pacman) pacman.Item3 = false;
                    

                    ghost.Width = nextCell.Width;
                    ghost.Height = nextCell.Height;

                }
                Thread.Sleep(500);

            }
        }

        private LevelCell getCellClosestToPacman (LevelCell cell)
        {

            var neighbors = new List<LevelCell>()
                {
                    flattenLevel.SingleOrDefault(c=>c.Width == cell.Width-1 && c.Height == cell.Height),
                    flattenLevel.SingleOrDefault(c=>c.Width == cell.Width+1 && c.Height == cell.Height),
                    flattenLevel.SingleOrDefault(c=>c.Width == cell.Width && c.Height -1== cell.Height),
                    flattenLevel.SingleOrDefault(c=>c.Width == cell.Width && c.Height +1== cell.Height),
                };
            neighbors = neighbors.Where(n => n != null && n.CellStatus != CellStatus.Wall).ToList();
            var results = new List<double>();
            foreach (var neighbor in neighbors)
            {
                results.Add( Math.Pow(neighbor.Width-pacman.Item1, 2) + Math.Pow(neighbor.Height-pacman.Item2, 2));
            }
            return neighbors.ElementAt(results.IndexOf(results.OrderBy(r => r).First()));
        }

        private void draw()
        {
            while (!gameOver)
            {

                Console.Clear();

                foreach (var row in Level)
                {
                    foreach (var cell in row)
                        switch (cell.CellStatus)
                        {
                            case CellStatus.Wall:
                                Console.ForegroundColor = ConsoleColor.Blue;
                                Console.Write("\u25a0");
                                Console.ForegroundColor = ConsoleColor.White;
                                break;
                            case CellStatus.Candy:
                                Console.Write(".");
                                break;
                            case CellStatus.Empty:
                                Console.Write(" ");
                                break;
                            case CellStatus.Pacman:
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.Write("@");
                                Console.ForegroundColor = ConsoleColor.White;
                                break;
                            case CellStatus.Ghost:
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("&");
                                Console.ForegroundColor = ConsoleColor.White;
                                break;
                            case CellStatus.GhostWCandy:
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("&");
                                Console.ForegroundColor = ConsoleColor.White;
                                break;
                            default:
                                break;
                        }
                    Console.Write("\n");
                }
                Thread.Sleep(200);
            }
        }
    }
}