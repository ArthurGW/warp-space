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

        public T Current => _levelPartIter.Current();
        
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

        public static string AsString(this Room room)
        {
            return $"Room({room.X},{room.Y},{room.W},{room.H},{room.IsCorridor})";
        }
        
        public static string AsString(this Adjacency adjacency)
        {
            return $"Adjacency({adjacency.First.AsString()},{adjacency.Second.AsString()})";
        }
        
        public static string AsString(this MapSquare square)
        {
            return $"MapSquare({square.X},{square.Y},{square.Type})";
        }
    }
}
