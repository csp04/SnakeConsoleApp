using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
namespace SnakeConsoleApp
{
    class Program
    {
        private const string headChar = "@";
        private const string bodyChar = "o";
        private const string tailChar = "O";

        private static bool canPressKey = true;

        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            var boundary = new Rect(0, 1, 79, 24);
            Snake snake = new Snake(boundary);
            SnakeFood food = new SnakeFood();

            var fps = 45;

            food.LocationChanged += (_, __) =>
            {
                Render(food.Location, "#");
                Render(new Point(25, 25), "Food: " + food.Location.X.ToString("00") + " " + food.Location.Y.ToString("00"));
            };

            food.FoodHasBeenEaten += (_, __) =>
            {
                Render(food.Location, " ");
                food.SetRandomLocation(boundary, snake);
            };

            food.SetRandomLocation(boundary, snake);

            snake.Direction = Direction.RIGHT;

            //for rendering
            snake.LocationChanged += (_, e) =>
            {
              
                //first head
                //Render(e.OldHead, " ");
                Render(e.NewHead, headChar);

                //mid body
                /*for(int i = 0; i < e.NewBody.Count; i++)
                {
                    var oldPart = e.OldBody[i];
                    var newPart = e.NewBody[i];

                    //Render(oldPart, " ");
                    Render(newPart, bodyChar);
                }*/
                if (e.NewBody.Count > 0)
                    Render(e.NewBody[0], bodyChar);

                //last tail
                Render(e.OldTail, " ");
                Render(e.NewTail, tailChar);

                

                if (snake.IfSelfCollided())
                {
                    Render(snake.Head, " ");
                    Render(snake.Tail, " ");
                    for (int i = 0; i < snake.Body.Count; i++)
                        Render(snake.Body[i], " ");

                    snake.Reset();
                    fps = 45;
                }

                if (snake.CanEatFood(food))
                {
                    //
                    if (snake.Body.Count % 4 == 0)
                        fps -= 2;
                }

                canPressKey = true;

                Render(new Point(0, 25), e.NewHead.X.ToString("00")+ " " + e.NewHead.Y.ToString("00"));
            };

            
            Action runFrame = () =>
            {
                var startTime = DateTime.Now;

               
                while (true)
                {
                    lock(snake)
                        snake.Run();

                    Thread.Sleep(snake.Speed);
               
                    var deltaTime = DateTime.Now - startTime;
                    snake.Speed = deltaTime.Milliseconds > fps ? snake.Speed - 1 : snake.Speed + 1;
                    

                    Render(new Point(60, 25), deltaTime.Milliseconds.ToString("000") + "ms ");

                    startTime = DateTime.Now;
                }
            };

            //ThreadPool.QueueUserWorkItem(new WaitCallback(runFrame));

            Task.Run(runFrame);

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();

                if (!canPressKey) continue;
                canPressKey = false;
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow when snake.Direction != Direction.DOWN:
                        snake.Direction = Direction.UP;
                        break;
                    case ConsoleKey.DownArrow when snake.Direction != Direction.UP:
                        snake.Direction = Direction.DOWN;
                        break;
                    case ConsoleKey.LeftArrow when snake.Direction != Direction.RIGHT:
                        snake.Direction = Direction.LEFT;
                        break;
                    case ConsoleKey.RightArrow when snake.Direction != Direction.LEFT:
                        snake.Direction = Direction.RIGHT;
                        break;
                }

                
            }
            while (key.Key != ConsoleKey.Escape);
        }


        static void Render(Point loc, string txt)
        {
            var foreColor = (ConsoleColor)(GetRand(1, 14));

            Console.ForegroundColor = foreColor == ConsoleColor.Black ? ConsoleColor.White : foreColor;
            Console.SetCursorPosition(loc.X, loc.Y);
            Console.Write(txt);
        }

        public static int GetRand(int lower, int upper)
        {
            return (new Random(DateTime.Now.Millisecond).Next() % (upper - lower)) + lower;
        }
    }

    public struct Rect
    {
        public int X { get; }
        public int Y { get; }
        public int W { get; }
        public int H { get; }

        public Rect(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }
    }

    public struct Point
    {
        public int X { get; }
        public int Y { get; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public enum Direction
    {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }

    public class ChangedLocationEventArgs : EventArgs
    {
        public Point OldHead { get; }
        public Point NewHead { get; }

        public Point OldTail { get; }
        public Point NewTail { get; }

        public List<Point> OldBody { get; }
        public List<Point> NewBody { get; }

        public ChangedLocationEventArgs(Point oldHead, Point newHead,
                                        Point oldTail, Point newTail,
                                        List<Point> oldBody, List<Point> newBody)
        {
            OldHead = oldHead;
            NewHead = newHead;
            OldTail = oldTail;
            NewTail = newTail;
            OldBody = oldBody;
            NewBody = newBody;
        }
    }

    public class Snake
    {

        public event EventHandler<ChangedLocationEventArgs> LocationChanged;

        public Point Head { get; set; }
        public Point Tail { get; set; }
        public List<Point> Body { get; set; }

        public int Speed { get; set; }

        private Direction direction;

        public Direction Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        private Rect _boundary;

        public Snake(Rect boundary)
        {
            Reset();

            _boundary = boundary;
        }

        public void Reset()
        {
            Speed = 45;
            Tail = new Point(0, 1);
            Head = new Point(2, 1);
            Body = new List<Point>();
            Body.Add(new Point(1, 1));
        }

        public void Run()
        {
            switch(Direction)
            {
                case Direction.UP:
                    Up();
                    break;
                case Direction.DOWN:
                    Down();
                    break;
                case Direction.LEFT:
                    Left();
                    break;
                case Direction.RIGHT:
                    Right();
                    break;
            }
        }

        private void adjustBodyLocation(Point loc)
        {
            if (Body.Count == 0) return;
            var nextPart = Body[0];
            Body[0] = loc;

            for(int i = 1; i < Body.Count; i++)
            {
                var part = Body[i];
                Body[i] = nextPart;
                nextPart = part;
            }
        }

        private void OnDirectionChange(Point newHead)
        {
            var oldTail = Tail;
            var oldHead = Head;

            var oldBody = Body.ToList();

            if (oldBody.Count > 0)
                Tail = oldBody.Last();
            else
                Tail = Head;

            adjustBodyLocation(Head);
            

            Head = newHead;
            LocationChanged?.Invoke(this, new ChangedLocationEventArgs(oldHead, newHead, oldTail, Tail, oldBody, Body));
            
        }

        public void Up()
        {
            var up = Head.Y - 1;

            if (Head.Y <= _boundary.Y)
                up = _boundary.H;

            OnDirectionChange(new Point(Head.X, up));
        }

        public void Down()
        {
            var down = Head.Y + 1;

            if (Head.Y >= _boundary.H)
                down = _boundary.Y;

            OnDirectionChange(new Point(Head.X, down));
        }

        public void Left()
        {

            var left = Head.X - 1;

            if (Head.X <= _boundary.X)
                left = _boundary.W;

            OnDirectionChange(new Point(left, Head.Y));
        }

        public void Right()
        {


            var right = Head.X + 1;

            if (Head.X >= _boundary.W)
                right = _boundary.X;

            OnDirectionChange(new Point(right, Head.Y));
        }

        public bool IfSelfCollided()
        {
            for(int i = 0; i < Body.Count; i++)
            {
                if (Collided(Head, Body[i]))
                    return true;
            }

            if (Collided(Head, Tail))
                return true;

            return false;
        }
        private bool Collided(Point loc1, Point loc2)
        {
            return loc1.X == loc2.X && loc1.Y == loc2.Y;
        }

        public bool CanEatFood(SnakeFood food)
        {
            if(Head.X == food.Location.X && Head.Y == food.Location.Y)
            {
                Body.Insert(0, Body.Count > 0 ? Body[0] : Tail);
                food.Eated();
                return true;
            }
            return false;
        }
    }

    public class SnakeFood
    {
        public event EventHandler FoodHasBeenEaten;
        public event EventHandler LocationChanged;

        public Point Location { get; private set; }

        public SnakeFood()
        {
        }


        public void SetRandomLocation(Rect bound, Snake snake)
        {


            var x = Program.GetRand(bound.X, bound.W);
            var y = Program.GetRand(bound.Y, bound.H);

            bool again = false;
            if (snake.Head.X == x && snake.Head.Y == y)
                again = true;

            if (snake.Tail.X == x && snake.Tail.Y == y)
                again = true;

            for(int i = 0; i < snake.Body.Count; i++)
                if(snake.Body[i].X == x && snake.Body[i].Y == y)
                {
                    again = true;
                    break;
                }

            if (again)
            {
                SetRandomLocation(bound, snake);
                return;
            }

            Location = new Point(x, y);
            LocationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Eated()
        {
            FoodHasBeenEaten?.Invoke(this, EventArgs.Empty);
        }
    }
}
