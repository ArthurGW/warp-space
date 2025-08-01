using System.Collections;
using LevelGenerator;
using System.Collections.Generic;

namespace LevelGenerator.Extensions
{
    public class LevelPartIterAsEnumerator<T> : IEnumerator<T>
    {
        private LevelPartIter<T> _levelPartIter;
        
        public static explicit operator LevelPartIterAsEnumerator<T>(LevelPartIter<T> levelPartIter)
        {
            var ret = new  LevelPartIterAsEnumerator<T>();
            ret._levelPartIter = levelPartIter;
            return ret;
        }
        
        public void Dispose()
        {
            _levelPartIter =  null;
        }
        
        public T Current
        {
            get
            {
                return _levelPartIter.Current();
            }
        }
        
        public bool MoveNext()
        {
            return _levelPartIter.MoveNext();
        }

        public void Reset()
        {
            _levelPartIter.Reset();
        }

        object IEnumerator.Current => Current;
    }
    
    public static class LevelExtensions
    {
        public static IEnumerator<T> GetEnumerator<T>(this LevelPartIter<T> iter)
        {
            return (LevelPartIterAsEnumerator<T>)iter;
        }
        
        // public static unsafe List<MapSquare> MapSquaresList(this Level level)
        // {
        //     var ret = new List<MapSquare>();
        //     var current = level.MapSquares;
        //     MapSquare.__Internal* ptr = (MapSquare.__Internal*)current.__Instance;
        //     var count = level.NumMapSquares;
        //     var square = new MapSquare();
        //     var old = square.__Instance;
        //     for (var i = 0UL; i < count; i++)
        //     {
        //         var n = ptr[i].GetType().FullName;
        //         Debug.Log(n);
        //         *((MapSquare.__Internal*) square.__Instance) = ptr[i];
        //         ret.Add(new MapSquare(square));
        //     }
        //     // *((MapSquare.__Internal*) square.__Instance) =
        //     //     *((MapSquare.__Internal*)Marshal.AllocHGlobal(sizeof(MapSquare.__Internal)));
        //     return ret;
        // }
        //
        // public static IEnumerable<Room> IterRooms(this Level level)
        // {
        //     var current = level.Rooms;
        //     yield return current;
        // }
        //     while (current != null)
        //     {
        //         yield return current;
        //         current = level.NextRoom;
        //     }
        // }
        //
        // public static IEnumerable<Adjacency> IterAdjacencies(this Level level)
        // {
        //     var current = level.A;
        //     while (current != null)
        //     {
        //         yield return current;
        //         current = level.NextAdjacency;
        //     }
        // }
    }
}
