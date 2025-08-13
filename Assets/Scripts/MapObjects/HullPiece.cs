using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Layout;
using LevelGenerator;
using Unity.VisualScripting;


namespace MapObjects
{
    public enum WindowSize : byte
    {
        One = 1,
        Three = 3,
		Five = 5,
    }

    public enum MatchSquareType : byte
    {
        Unknown = 0,
        Space = 1,  // In space
        Hull = 2,  // Part of the hull
        Internal = 3,  // Inside the ship
        AlienBreach = 4,  // Part of a breach
    }
    
    /// <summary>
    ///     Data on the orientation and local neighbourhood of a hull piece, for matching against
    /// </summary>
    /// 
    /// This consists of a window of a given width and height (centred on the piece in question), within which to match
    /// a given collection of squares. Unspecified squares are ignored.
    ///
    /// The window is always attempted to match in 4 different rotations (0, 90, 180, 270 degrees), and optionally may
    /// also be matched reflected in the x-axis, again with 4 rotations.
    public class HullPiece : MonoBehaviour
    {
        [EnumButtons] public WindowSize matchWindowWidth = WindowSize.Three;
        [EnumButtons] public WindowSize matchWindowHeight = WindowSize.Three;
        public bool flippable;

        [SerializeField]
        private Vector2Int[] hullSquaresToMatch = { new(0, 0) };
        
        [SerializeField]
        private Vector2Int[] spaceSquaresToMatch = { };
        
        [SerializeField]
        private Vector2Int[] internalSquaresToMatch = { };
        
        [SerializeField]
        private Vector2Int[] breachSquaresToMatch = { };

        private ILookup<(int, int), MatchSquareType> _matchMap;
        private ILookup<(int, int), MatchSquareType> _flippedMatchMap;

        private void InitMatchData()
        {
            if (_matchMap != null) return;

            var hullSquares = hullSquaresToMatch
                .ToLookup(sq => (sq.x, sq.y), sq => MatchSquareType.Hull);
            var spaceSquares = spaceSquaresToMatch
                .ToLookup(sq => (sq.x, sq.y), sq => MatchSquareType.Space);
            var internalSquares = internalSquaresToMatch
                .ToLookup(sq => (sq.x, sq.y), sq => MatchSquareType.Internal);
            var breachSquares = breachSquaresToMatch
                .ToLookup(sq => (sq.x, sq.y), sq => MatchSquareType.AlienBreach);
            _matchMap = hullSquares
                .Union(internalSquares)
                .Union(spaceSquares)
                .Union(breachSquares)
                .ToLookup(grp => grp.Key, grp => grp.ElementAt(0));
            
            if (!flippable)
                return;
            
            // Same, but invert the relative y, to flip in the grid x-axis
            _flippedMatchMap = _matchMap.ToLookup(grp => (grp.Key.Item1, -grp.Key.Item2), grp => grp.ElementAt(0));
        }

        private void OnValidate()
        {
            _matchMap = null;  // Clear, to regenerate later in InitMatchData
            _flippedMatchMap = null;
        }

        /// <summary>
        ///     Try to match the map at the given location against the pattern stored here
        /// </summary>
        /// 
        /// This checks against the original pattern, and its four 90 degree rotations.
        /// 
        /// <param name="mapPos">position to check</param>
        /// <param name="map">all map positions</param>
        /// <returns>prefab to instantiate if matching, or null, an optional rotation to apply,
        /// and whether the mesh should be flipped across the x-axis before rotation</returns>
        public (GameObject prefab, Quaternion? rotation, bool flip) TryGetPrefabAndRotation(
            (uint X, uint Y) mapPos, Dictionary<(uint X, uint Y), SquareType> map)
        {
            InitMatchData();  // Only runs the first time
            
            var result = CheckMatchMap(mapPos, map, _matchMap);
            if (result.HasValue)
            {
                return (gameObject, result.Value.ToRotation(), false);
            }

            if (_flippedMatchMap == null) return (null, null, false);
            
            result = CheckMatchMap(mapPos, map, _flippedMatchMap);
            if (result.HasValue)
            {
                return (gameObject, result.Value.ToRotation(), true);
            }

            return (null, null, false);
        }
        
        private CardinalDirection? CheckMatchMap(
            (uint X, uint Y) mapPos,
            Dictionary<(uint X, uint Y), SquareType> map,
            ILookup<(int, int), MatchSquareType> matchMap
        )
        {
            foreach (CardinalDirection upDirection in Enum.GetValues(typeof(CardinalDirection)))
            {
                var doesNotFitOrDoesNotMatch = false;

                foreach (var (offset, matchOffset) in OffsetSource(upDirection))
                {
                    try
                    {
                        var offsetPos = mapPos.Offset(offset);
                        if (map.ContainsKey(offsetPos) &&
                            (!matchMap.Contains(matchOffset)
                             || matchMap[matchOffset].ElementAt(0) == ConvertSquareType(map[offsetPos])))
                            continue;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // Ignore, this means outside the map, so hit the lines below
                    }
                    
                    doesNotFitOrDoesNotMatch = true;
                    break;
                }

                if (doesNotFitOrDoesNotMatch)
                {
                    continue;  // Try next rotation
                }
                
                // No non-matching or non-fitting squares found, we have a hit
                return upDirection;
            }

            // Nothing matched in any direction
            return null;
        }

        private static MatchSquareType ConvertSquareType(SquareType squareType) => squareType switch
        {
            SquareType.Hull => MatchSquareType.Hull,
            SquareType.Space => MatchSquareType.Space,
            SquareType.AlienBreach => MatchSquareType.AlienBreach,
            SquareType.Corridor or SquareType.Room or SquareType.Ship => MatchSquareType.Internal,
            SquareType.Unknown => MatchSquareType.Unknown,
            _ => throw new ArgumentException()
        };

        private IEnumerable<((int, int), (int, int))> OffsetSource(CardinalDirection upDirection)
        {
            Func<int, int, (int, int)> windowPosToMapOffset = upDirection switch 
            {
                CardinalDirection.North => (i, j) => (i, j),
                CardinalDirection.East => (i, j) => (-j, i),
                CardinalDirection.South => (i, j) => (-i, -j),
                CardinalDirection.West => (i, j) => (j, -i),
                _ => throw new ArgumentException()
            };
            for (var j = -(byte)matchWindowHeight / 2; j <= (byte)matchWindowHeight / 2; ++j)
            {
                for (var i = -(byte)matchWindowWidth / 2; i <= (byte)matchWindowWidth / 2; ++i)
                {
                    yield return (windowPosToMapOffset(i, j), (i, j));
                }
            }
        }
    }
}