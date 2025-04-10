using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace lab6_2
{
    public readonly struct Desk
    {
        public int SizeX { get; }
        public int SizeY { get; }

        public Desk(int sizeX, int sizeY)
        {
            SizeX = sizeX;
            SizeY = sizeY;
        }

        public bool IsWithinBounds(int x, int y) 
            => x >= 0 && x < SizeX && y >= 0 && y < SizeY;
    }

    public enum PieceType { Knight, Rook, Bishop, Queen, King, Pawn }

    public readonly struct Figure
    {
        public PieceType Type { get; }
        public bool IsWhite { get; }

        public Figure(PieceType type, bool isWhite)
        {
            Type = type;
            IsWhite = isWhite;
        }
    }
    
    public enum Weight {Empty, Single, Double}
    public interface IMoveRule
    {
        bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, Dictionary<(int, int), Figure> board);
    }
    
    public class KnightMoveRule : IMoveRule
    {
        public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, Dictionary<(int, int), Figure> board)
        {
            int dx = Math.Abs(to.x - from.x);
            int dy = Math.Abs(to.y - from.y);
            bool isValid = ((dx == 1 && dy == 2) || (dx == 2 && dy == 1)) && desk.IsWithinBounds(to.x, to.y);
            if (!isValid) return false;
            return true;
        }

        public Weight GetWeight((int x, int y) to, Dictionary<(int, int), Figure> board)
        {
            if (!board.TryGetValue(to, out Figure targetFigure))
            {
                return Weight.Empty;
            }
            if (!targetFigure.IsWhite)
            {
                return Weight.Double;
            }
            return Weight.Single;
        }
    }

    public class RookMoveRule : IMoveRule
    {
        public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, Dictionary<(int, int), Figure> board)
        {
            if (from.x != to.x && from.y != to.y) return false;
            if (!desk.IsWithinBounds(to.x, to.y)) return false;
            
            int stepX = Math.Sign(to.x - from.x);
            int stepY = Math.Sign(to.y - from.y);
            
            int x = from.x + stepX;
            int y = from.y + stepY;
            
            while (x != to.x || y != to.y)
            {
                if (board.ContainsKey((x, y))) return false;
                x += stepX;
                y += stepY;
            }
            
            // Проверяем, что в целевой клетке нет своей фигуры
            if (board.TryGetValue(to, out Figure targetFigure) && 
                targetFigure.IsWhite == board[from].IsWhite)
            {
                return false;
            }
            
            return true;
        }
    }

    public class BishopMoveRule : IMoveRule
    {
        public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, Dictionary<(int, int), Figure> board)
        {
            int dx = Math.Abs(to.x - from.x);
            int dy = Math.Abs(to.y - from.y);
            if (dx != dy || dx == 0) return false;
            if (!desk.IsWithinBounds(to.x, to.y)) return false;
            
            int stepX = Math.Sign(to.x - from.x);
            int stepY = Math.Sign(to.y - from.y);
            
            int x = from.x + stepX;
            int y = from.y + stepY;
            
            while (x != to.x && y != to.y)
            {
                if (board.ContainsKey((x, y))) return false;
                x += stepX;
                y += stepY;
            }
            
            if (board.TryGetValue(to, out Figure targetFigure) && 
                targetFigure.IsWhite == board[from].IsWhite)
            {
                return false;
            }
            
            return true;
        }
    }

    public class QueenMoveRule : IMoveRule
    {
        public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, Dictionary<(int, int), Figure> board)
        {
            // Комбинация ладьи и слона
            var rook = new RookMoveRule();
            var bishop = new BishopMoveRule();
            
            return rook.IsValidMove(desk, from, to, board) || 
                   bishop.IsValidMove(desk, from, to, board);
        }
    }

    public class KingMoveRule : IMoveRule
    {
        public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, Dictionary<(int, int), Figure> board)
        {
            int dx = Math.Abs(to.x - from.x);
            int dy = Math.Abs(to.y - from.y);
            bool isValid = (dx <= 1 && dy <= 1 && dx + dy > 0) && desk.IsWithinBounds(to.x, to.y);
            if (!isValid) return false;
            
            if (board.TryGetValue(to, out Figure targetFigure) && 
                targetFigure.IsWhite == board[from].IsWhite)
            {
                return false;
            }
            return true;
        }
    }

    public class PawnMoveRule : IMoveRule
    {
        public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, Dictionary<(int, int), Figure> board)
        {
            if (!desk.IsWithinBounds(to.x, to.y)) return false;
            
            bool isWhite = board[from].IsWhite;
            int direction = isWhite ? 1 : -1;
            int startY = isWhite ? 1 : 6;
            
            // Обычный ход вперед
            if (from.x == to.x && !board.ContainsKey(to))
            {
                // На одну клетку
                if (to.y == from.y + direction) return true;
                
                // На две клетки из начальной позиции
                if (from.y == startY && to.y == from.y + 2 * direction)
                {
                    // Проверяем, что промежуточная клетка пуста
                    (int x, int y) middle = (from.x, from.y + direction);
                    return !board.ContainsKey(middle);
                }
            }
            
            // Взятие фигуры
            if (Math.Abs(to.x - from.x) == 1 && to.y == from.y + direction)
            {
                // Обычное взятие
                if (board.TryGetValue(to, out Figure targetFigure) && 
                    targetFigure.IsWhite != isWhite)
                {
                    return true;
                }
            }
            
            return false;
        }
    }

    public interface IGame
    {
        GameState Move((int x, int y) from, (int x, int y) to, GameState state);
        GameState SwitchToFigure(Figure figure, GameState state);
    }
    
    public class Player : IGame
    {
        public bool IsWhite { get; }

        public Player(bool isWhite)
        {
            IsWhite = isWhite;
        }

        // Теперь метод принимает Figure вместо PieceType
        public GameState SwitchToFigure(Figure figure, GameState state)
        {
                return state.ToBuilder().WithActiveFigure(figure).Build();
        }

        public GameState Move((int x, int y) from, (int x, int y) to, GameState currentState)
         {
             // Просто перемещаем фигуру без проверки правил
             var newBoard = new Dictionary<(int, int), Figure>(currentState.Board);
             
             if (newBoard.TryGetValue(from, out var figure))
             {
                 newBoard.Remove(from);
                 newBoard[to] = figure;
                 Console.WriteLine($"Player moved {figure.Type} from {from} to {to}");

                 if (currentState.ActiveFigure.Type == currentState.Task.TFigure.Type)
                 {
                     return currentState.ToBuilder()
                         .WithCurrentPosition(to)
                         .WithBoard(newBoard)
                         .WithActiveFigure(currentState.ActiveFigure)
                         .WithTask(currentState.Task.WithKnightPosition(to))
                         .Build(); 
                 }
                 return currentState.ToBuilder()
                     .WithCurrentPosition(to)
                     .WithBoard(newBoard)
                     .WithActiveFigure(currentState.ActiveFigure)
                     .Build();
             }

             Console.WriteLine("No figure at position or wrong color!");
             return currentState;
         }
        
    }

    
    public class GameRules : IGame
    {
        private readonly Desk _desk;
        private readonly Dictionary<PieceType, IMoveRule> _moveRules;
        private readonly IGame _decorated;

        public GameRules(IGame decorated, Desk desk)
        {
            _desk = desk;
            _moveRules = new Dictionary<PieceType, IMoveRule>
            {
                [PieceType.Knight] = new KnightMoveRule(),
                [PieceType.Rook] = new RookMoveRule(),
                [PieceType.Bishop] = new BishopMoveRule(),
                [PieceType.Queen] = new QueenMoveRule(),
                [PieceType.King] = new KingMoveRule(),
                [PieceType.Pawn] = new PawnMoveRule()
            };
            _decorated = decorated;
        }

        public GameState Move((int x, int y) from, (int x, int y) to, GameState state)
        {
            if (!state.Board.TryGetValue(from, out var figure))
            {
                Console.WriteLine("Нет фигуры в начальной позиции!");
                return state;
            }

            if (!_moveRules.TryGetValue(figure.Type, out var rule))
            {
                Console.WriteLine($"Нет правил для фигуры типа {figure.Type}!");
                return state;
            }

            if (rule is KnightMoveRule knightRule)
            {
                Weight weight = knightRule.GetWeight(to, state.Board);
            }
            
            if (!rule.IsValidMove(_desk, from, to, state.Board))
            {
                Console.WriteLine("Неверный ход для данной фигуры!");
                return state;
            }
            
            return _decorated.Move(from, to, state);
        }

        public GameState SwitchToFigure(Figure figure, GameState state)
        {
           return _decorated.SwitchToFigure(figure, state);
        }
    }

    public sealed class GameTask
    {
        public Figure TFigure { get; }
        public (int x, int y) KnightPosition { get; }
        public (int x, int y) TargetPosition { get; }

        public GameTask(Figure targetFigure, (int x, int y) startPos, (int x, int y) targetPos)
        {
            TFigure = targetFigure;
            KnightPosition = startPos;
            TargetPosition = targetPos;
        }

        public GameTask WithKnightPosition((int x, int y) newPosition) =>
            new GameTask(TFigure, newPosition, TargetPosition);

        public bool IsCompleted => KnightPosition.Equals(TargetPosition);
    }

    public class GameState
    {
        public Figure ActiveFigure { get; }
        public (int x, int y) CurrentPosition { get; }
        public Dictionary<(int, int), Figure> Board { get; }
        public GameTask Task { get; }

        public GameState(
            Figure activeFigure,
            (int x, int y) currentPosition,
            Dictionary<(int, int), Figure> board,
            GameTask task)
        {
            ActiveFigure = activeFigure;
            CurrentPosition = currentPosition;
            Board = board;
            Task = task;
        }

        public bool IsTaskComplete => ActiveFigure.Type == PieceType.Knight && 
                                     CurrentPosition.Equals(Task.TargetPosition);

        public Builder ToBuilder() => new Builder(this);

        public class Builder
        {
            private Figure _activeFigure;
            private (int x, int y) _currentPosition;
            private Dictionary<(int, int), Figure> _board;
            private GameTask _task;

            public Builder(GameState state)
            {
                _activeFigure = state.ActiveFigure;
                _currentPosition = state.CurrentPosition;
                _board = state.Board;
                _task = state.Task;
            }

            public Builder WithActiveFigure(Figure figure)
            {
                _activeFigure = figure;
                return this;
            }

            public Builder WithCurrentPosition((int x, int y) pos)
            {
                _currentPosition = pos;
                return this;
            }

            public Builder WithBoard(Dictionary<(int, int), Figure> board)
            {
                _board = new Dictionary<(int, int), Figure>(board);
                return this;
            }

            public Builder WithTask(GameTask task)
            {
                _task = task;
                return this;
            }

            public GameState Build() => new GameState(
                _activeFigure, _currentPosition, _board, _task);
        }
    }
    
    public class PathFinder
{
    private static readonly (int dx, int dy)[] KnightMoves = 
    {
        (1, 2), (2, 1), (-1, 2), (-2, 1),
        (1, -2), (2, -1), (-1, -2), (-2, -1)
    };

    public List<(int x, int y)> FindShortestPath(
        (int x, int y) start, 
        (int x, int y) target, 
        Dictionary<(int, int), Figure> board,
        IMoveRule knightMoveRule = null)
    {
        var visited = new Dictionary<(int x, int y), (int x, int y)>();
        var queue = new Queue<(int x, int y)>();
        var distances = new Dictionary<(int x, int y), int>();
        var priorityQueue = new SortedList<int, (int x, int y)>(Comparer<int>.Create((x, y) => x.CompareTo(y)));

        queue.Enqueue(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == target)
                break;

            foreach (var move in KnightMoves)
            {
                var next = (x: current.x + move.dx, y: current.y + move.dy);
                
                // Проверяем границы доски
                if (next.x < 0 || next.x >= 8 || next.y < 0 || next.y >= 8)
                    continue;

                // Проверяем допустимость хода (если передан knightMoveRule)
                bool moveValid = true;
                if (knightMoveRule != null)
                {
                    moveValid = knightMoveRule.IsValidMove(new Desk(8, 8), current, next, board);
                }

                if (!moveValid)
                    continue;

                // Рассчитываем новый вес пути
                int weight = 1;
                if (knightMoveRule is KnightMoveRule knightRule)
                {
                    var weightType = knightRule.GetWeight(next, board);
                    weight = weightType switch
                    {
                        Weight.Empty => 1,
                        Weight.Single => 1,
                        Weight.Double => 2,
                        _ => 1
                    };
                }

                // Обновляем расстояния
                int newDistance = distances[current] + weight;
                
                if (!distances.ContainsKey(next) || newDistance < distances[next])
                {
                    distances[next] = newDistance;
                    visited[next] = current;
                    queue.Enqueue(next);
                    
                    // Для приоритезации можно использовать priorityQueue
                    // priorityQueue.Add(newDistance, next);
                }
            }
        }

        // Восстановление пути
        var path = new List<(int x, int y)>();
        if (visited.ContainsKey(target))
        {
            var current = target;
            while (current != start)
            {
                path.Add(current);
                current = visited[current];
            }
            path.Add(start);
            path.Reverse();
        }
        
        return path;
    }
}

    
    class Program
    {
        static void InputInt(string prompt, out int num)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out num))
                    return;
                Console.WriteLine("Неверный ввод, попробуйте снова!");
            }
        }

        static (int x, int y) InputCoordinates(string prompt, Desk desk)
        {
            while (true)
            {
                Console.Write(prompt);
                var parts = Console.ReadLine().Split();
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int x) &&
                    int.TryParse(parts[1], out int y) &&
                    desk.IsWithinBounds(x, y))
                {
                    return (x, y);
                }
                Console.WriteLine("Неверные координаты! Введите два числа через пробел (например: '3 4')");
            }
        }

        static GameState InputFigure(GameState game, Desk desk)
        {
            while (true)
            {
                var (x, y) = InputCoordinates("Введите координаты фигуры (x y): ", desk);
                if (game.Board.TryGetValue((x, y), out var figure))
                {
                    return game.ToBuilder().
                        WithActiveFigure(figure).
                        WithCurrentPosition((x, y)).
                        Build();
                }
                Console.WriteLine("На этой позиции нет фигуры!");
            }
        }

        static void PrintBoard(GameState state, Desk desk)
        {
            Console.WriteLine("  " + string.Join(" ", Enumerable.Range(0, desk.SizeX).Select(i => i)));
            for (int y = 0; y < desk.SizeY; y++)
            {
                Console.Write(y + " ");
                for (int x = 0; x < desk.SizeX; x++)
                {
                    if (state.Board.TryGetValue((x, y), out var figure))
                    {
                        Console.Write(GetFigureSymbol(figure));
                    }
                    else
                    {
                        Console.Write(".");
                    }
                    Console.Write(" ");
                }
                Console.WriteLine();
            }
        }

        static char GetFigureSymbol(Figure figure)
        {
            return figure.Type switch
            {
                PieceType.Knight => figure.IsWhite ? 'N' : 'n',
                PieceType.Rook => figure.IsWhite ? 'R' : 'r',
                PieceType.Bishop => figure.IsWhite ? 'B' : 'b',
                PieceType.Queen => figure.IsWhite ? 'Q' : 'q',
                PieceType.King => figure.IsWhite ? 'K' : 'k',
                PieceType.Pawn => figure.IsWhite ? 'P' : 'p',
                _ => '?'
            };
        }

        static void Main(string[] args)
        {
            var desk = new Desk(8, 8);
            var player = new GameRules(
                new Player(true),
                desk);

            // Инициализация доски
            var board = new Dictionary<(int, int), Figure>
            {
                [(0, 0)] = new Figure(PieceType.Rook, true),
                [(1, 0)] = new Figure(PieceType.Knight, true),
                [(2, 2)] = new Figure(PieceType.Bishop, true),
                [(3, 1)] = new Figure(PieceType.Queen, true),
                [(7, 0)] = new Figure(PieceType.Rook, true),
                
                // Белые пешки
                [(0, 1)] = new Figure(PieceType.Pawn, true),
                [(1, 1)] = new Figure(PieceType.Pawn, true),
                [(7, 1)] = new Figure(PieceType.Pawn, true),
                
                // Черные фаги (несколько для примера)
                [(0, 7)] = new Figure(PieceType.Rook, false),
                [(7, 7)] = new Figure(PieceType.Rook, false),
                [(4, 7)] = new Figure(PieceType.King, false),
            };
            
            var task = new GameTask(board[(1,0)],(1, 0), (5, 2));
            
            var game = new GameState(
                task.TFigure, // Начинаем с коня
                (1, 0),
                board,
                task);

            var pathFinder = new PathFinder();
            var way = pathFinder.FindShortestPath(game.Task.KnightPosition, game.Task.TargetPosition, game.Board);

            int numStep = 1;
            while (!game.IsTaskComplete)
            {
                Console.Clear();
                PrintBoard(game, desk);
                
                Console.WriteLine($"\nТекущая фигура: {game.ActiveFigure.Type} на {game.CurrentPosition}");
                Console.WriteLine($"Цель: довести коня до {task.TargetPosition}");
                
                Console.WriteLine("\nВыберите действие:");
                Console.WriteLine("1. Сделать ход");
                Console.WriteLine("2. Переключиться на другую фигуру");
                
                InputInt("Введите номер действия: ", out int choice);
                
                if (choice == 1)
                {
                    var from = game.CurrentPosition;
                    if (game.ActiveFigure.Type == task.TFigure.Type)
                    {
                        game = player.Move(from, way[numStep], game);
                        numStep++;
                    }
                    else
                    {
                        var to = InputCoordinates("Введите целевую позицию (x y): ", desk);
                        game = player.Move(from, to, game);
                    }
                }
                else if (choice == 2)
                {
                    game = InputFigure(game, desk);
                    if (game.ActiveFigure.Type == task.TFigure.Type)
                    {
                        numStep = 1;
                        way = pathFinder.FindShortestPath(game.Task.KnightPosition, game.Task.TargetPosition,
                            game.Board);
                    }
                }
            }

            Console.WriteLine("Поздравляем! Конь достиг целевой позиции!");
        }
    }
}


// using System.Diagnostics.Contracts;
//
// namespace lab6_2
// {
//     using System;
//     using System.Collections.Generic;
//     using System.Linq;
//     public readonly struct Desk
//     {
//         public int SizeX { get; }
//         public int SizeY { get; }
//
//         public Desk(int sizeX, int sizeY)
//         {
//             SizeX = sizeX;
//             SizeY = sizeY;
//         }
//
//         public bool IsWithinBounds(int x, int y) 
//             => x >= 0 && x < SizeX && y >= 0 && y < SizeY;
//     }
//
//     public enum PieceType { Knight, Rook, Bishop, Queen, King, Pawn }
//
//     public readonly struct Figure
//     {
//         public PieceType Type { get; }
//         public bool IsWhite { get; }
//
//         public Figure(PieceType type, bool isWhite)
//         {
//             Type = type;
//             IsWhite = isWhite;
//         }
//     }
//
//     public interface IMoveRule
//     {
//         bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, GameState state);
//     }
//
//     public class KnightMoveRule : IMoveRule
//     {
//         public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, GameState state)
//         {
//             int dx = Math.Abs(to.x - from.x);
//             int dy = Math.Abs(to.y - from.y);
//             return ((dx == 1 && dy == 2) || (dx == 2 && dy == 1)) && desk.IsWithinBounds(to.x, to.y);
//         }
//     }
//     public class RookMoveRule : IMoveRule
//     {
//         public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, GameState state) 
//             => from.x == to.x || from.y == to.y;
//     }
//
//     public class BishopMoveRule : IMoveRule
// {
//     public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, GameState state)
//     {
//         // Слон ходит по диагоналям
//         int dx = Math.Abs(to.x - from.x);
//         int dy = Math.Abs(to.y - from.y);
//         return dx == dy && dx != 0 && desk.IsWithinBounds(to.x, to.y);
//     }
// }
//
// public class QueenMoveRule : IMoveRule
// {
//     public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to, GameState state)
//     {
//         // Ферзь объединяет ладью и слона
//         return (from.x == to.x || from.y == to.y ||       // как ладья
//                 Math.Abs(to.x - from.x) == Math.Abs(to.y - from.y)) // как слон
//                && !(from.x == to.x && from.y == to.y) // не оставаться на месте
//                && desk.IsWithinBounds(to.x, to.y);
//     }
// }
//
// public class KingMoveRule : IMoveRule
// {
//     public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to,GameState state)
//     {
//         // Король ходит на 1 клетку в любом направлении
//         int dx = Math.Abs(to.x - from.x);
//         int dy = Math.Abs(to.y - from.y);
//         return (dx <= 1 && dy <= 1 && (dx + dy) > 0) && desk.IsWithinBounds(to.x, to.y);
//     }
// }
//
// public class PawnMoveRule : IMoveRule
// {
//     public bool IsValidMove(Desk desk, (int x, int y) from, (int x, int y) to,GameState state)
//     {
//         // Пешка ходит вперед на 1 клетку
//         int direction = from.y < to.y ? 1 : -1; // Направление зависит от цвета
//
//         // Базовый ход на 1 вперед
//         if (to.x == from.x && to.y == from.y + direction &&
//             !ContainsFigureAt(desk, to,state))
//         {
//             return true;
//         }
//
//         // Взятие фигуры по диагонали
//         if (Math.Abs(to.x - from.x) == 1 && to.y == from.y + direction &&
//             ContainsFigureAt(desk, to,state))
//         {
//             return true;
//         }
//
//         return false;
//     }
//
//     private bool ContainsFigureAt(Desk desk, (int x, int y) pos, GameState state)
//     {
//         return state.Board.TryGetValue((pos.x, pos.y), out var state);
//     
//
//     public interface IGame
//     {
//         GameState Move((int x, int y) from, (int x, int y) to, GameState state);
//     }
//     
//     public class Player : IGame
//     {
//         public bool IsWhite { get; }
//
//         public Player(bool isWhite)
//         {
//             IsWhite = isWhite;
//         }
//
//         public void SwitchFigure(PieceType pieceType)
//         {
//             
//         }
//         public GameState Move((int x, int y) from, (int x, int y) to, GameState currentState)
//         {
//             // Просто перемещаем фигуру без проверки правил
//             var newBoard = new Dictionary<(int, int), Figure>(currentState.Board);
//             
//             if (newBoard.TryGetValue(from, out var figure) && figure.IsWhite == this.IsWhite)
//             {
//                 newBoard.Remove(from);
//                 newBoard[to] = figure;
//                 Console.WriteLine($"Player moved {figure.Type} from {from} to {to}");
//
//                 return currentState.ToBuilder()
//                     .WithCurrentPosition(to)
//                     .WithBoard(newBoard)
//                     .WithActiveFigure(currentState.ActiveFigure) // Можно добавить логику смены фигуры
//                     .Build();
//             }
//
//             Console.WriteLine("No figure at position or wrong color!");
//             return currentState;
//         }
//     }
//     
//     public class GameRules : IGame
//     {
//         private readonly Desk _desk;
//         private readonly Dictionary<PieceType, IMoveRule> _moveRules;
//         private readonly IGame _decorated;
//
//         public GameRules(IGame decorated,Desk desk)
//         {
//             _desk = desk;
//             _moveRules = new Dictionary<PieceType, IMoveRule>
//             {
//                 [PieceType.Knight] = new KnightMoveRule(),
//                 [PieceType.Rook] = new RookMoveRule()
//             };
//             _decorated = decorated;
//         }
//
//         public GameState Move((int x, int y) from, (int x, int y) to, GameState state)
//         {
//             if (!state.Board.TryGetValue(from, out var figure))
//             {
//                 Console.WriteLine("No figure at this position!");
//                 return state;
//             }
//
//             if (!_moveRules.TryGetValue(figure.Type, out var rule))
//             {
//                 Console.WriteLine($"No move rules for {figure.Type}!");
//                 return state;
//             }
//
//             if (rule.IsValidMove(_desk, from, to, state))
//             {
//                 var newBoard = new Dictionary<(int, int), Figure>(state.Board);
//                 newBoard.Remove(from);
//                 newBoard[to] = figure;
//                 
//                 return state.ToBuilder()
//                     .WithCurrentPosition(to)
//                     .WithBoard(newBoard)
//                     .Build();
//             }
//
//             Console.WriteLine("Invalid move!");
//             return state;
//         }
//     }
//
//     public class KnightMovementTask
//     {
//         public (int x, int y) Start { get; }
//         public (int x, int y) Target { get; }
//
//         public KnightMovementTask((int x, int y) start, (int x, int y) target)
//         {
//             Start = start;
//             Target = target;
//         }
//     }
//
//     public class GameState
//     {
//         public Figure ActiveFigure { get; }
//         public (int x, int y) CurrentPosition { get; }
//         public Dictionary<(int, int), Figure> Board { get; }
//         public KnightMovementTask Task { get; }
//
//         public GameState(
//             Figure activeFigure,
//             (int x, int y) currentPosition,
//             Dictionary<(int, int), Figure> board,
//             KnightMovementTask task)
//         {
//             ActiveFigure = activeFigure;
//             CurrentPosition = currentPosition;
//             Board = board;
//             Task = task;
//         }
//
//         public bool IsTaskComplete => CurrentPosition.Equals(Task.Target);
//
//         public Builder ToBuilder() => new Builder(this);
//
//         public class Builder
//         {
//             private Figure _activeFigure;
//             private (int x, int y) _currentPosition;
//             private Dictionary<(int, int), Figure> _board;
//             private KnightMovementTask _task;
//
//             public Builder(GameState state)
//             {
//                 _activeFigure = state.ActiveFigure;
//                 _currentPosition = state.CurrentPosition;
//                 _board = state.Board;
//                 _task = state.Task;
//             }
//
//             public Builder WithActiveFigure(Figure figure)
//             {
//                 _activeFigure = figure;
//                 return this;
//             }
//
//             public Builder WithCurrentPosition((int x, int y) pos)
//             {
//                 _currentPosition = pos;
//                 return this;
//             }
//
//             public Builder WithBoard(Dictionary<(int, int), Figure> board)
//             {
//                 _board = new Dictionary<(int, int), Figure>(board);
//                 return this;
//             }
//
//             public Builder WithFigure(Figure figure)
//             {
//                 _activeFigure = figure;
//                 return this;
//             }
//
//             public GameState Build() => new GameState(
//                 _activeFigure, _currentPosition, _board, _task);
//         }
//     }
//
//     class Program
//     {
//         public static void InputInt(out int num)
//         {
//             while (!int.TryParse(Console.ReadLine(), out num))
//                 Console.WriteLine("Retry"); 
//         }
//         public static void InputCoordinates(out int x, out int y)
//         {
//             InputInt(out x);
//             InputInt(out y);
//         }
//     
//         public static void SelectTargetCoordinates(out KnightMovementTask task)
//         {   
//             Console.WriteLine("Select initial coordinates:");
//             InputCoordinates(out int x1, out int y1);
//             Console.WriteLine("Select final coordinates:");
//             InputCoordinates(out int x2, out int y2);
//             task = new KnightMovementTask((x1, y1), (x2, y2));
//         }
//         
//         public static Figure TempFigure(GameState game, Desk desk)
//         {
//             Console.WriteLine("Select figure by coordinates:");
//             InputInt(out int x);
//             InputInt(out int y);
//             if (!desk.IsWithinBounds(x, y))
//             {
//                 Console.WriteLine("Invalid coordinates!");
//                 return game.ActiveFigure;
//             }
//             if (!game.Board.TryGetValue((x, y), out var figure))
//             {
//                 Console.WriteLine("No figure at this position!");
//                 return game.ActiveFigure;
//             }
//             return figure;
//         }
//         
//         static void Main(string[] args)
//         {
//             var desk = new Desk(8, 8);
//
//             // Инициализация доски
//             var board = new Dictionary<(int, int), Figure>
//             {
//                 [(0, 0)] = new Figure(PieceType.Knight, true),
//                 [(0, 1)] = new Figure(PieceType.Rook, true),
//                 [(7, 7)] = new Figure(PieceType.Knight, false),
//                 // [(0, 8)] = new Figure(PieceType.Knight, false),
//             };
//
//             var task = new KnightMovementTask((0, 0), (7, 7));
//             var game = new GameState(
//                 board[(0, 0)],
//                 (0, 0),
//                 board,
//                 task);
//
//             IGame player = new GameRules(
//                 new Player(true),
//                 desk);
//
//             while (!game.IsTaskComplete)
//             {
//                 Console.WriteLine($"Current figure: {game.ActiveFigure.Type} at {game.CurrentPosition}");
//                 Console.WriteLine("Enter move (fromX fromY toX toY):");
//                 
//                 var input = Console.ReadLine()?.Split(' ');
//                 if (input?.Length == 4 && 
//                     int.TryParse(input[0], out var fromX) &&
//                     int.TryParse(input[1], out var fromY) &&
//                     int.TryParse(input[2], out var toX) &&
//                     int.TryParse(input[3], out var toY))
//                 {
//                     game = player.Move((fromX, fromY), (toX, toY), game);
//                     Console.WriteLine("Select figure");
//                     game.ToBuilder().
//                         WithFigure(TempFigure(game, desk));
//                 }
//             }
//
//             Console.WriteLine("Task completed!");
//         }
//     }
// }
