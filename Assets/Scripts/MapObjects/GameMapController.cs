using System;
using System.Collections.Generic;
using System.Linq;
using Layout;
using LevelGenerator;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MapObjects
{
    /// <summary>
    /// Class to convert an abstract generated map into a collection of managed GameObjects
    /// </summary>
    public class GameMapController : MonoBehaviour
    {
        [SerializeField]
        private GameObject corridorPrefab;
        
        [SerializeField]
        private GameObject hullPrefab;
        
        [SerializeField]
        private GameObject shipSquarePrefab;
        
        [SerializeField]
        private GameObject roomPrefab;

        private static Vector3 SquareToPosition(MapSquareData square)
        {
            // Note - we are choosing -x in map coordinates as +x in *abstract* map coordinates
            // This is so that +y == +z, and moves "down" the viewport based on our camera angle
            return new Vector3(-SquareSize.X * square.X, 0f, SquareSize.Y * square.Y);
        }

        private static Tuple<uint, uint> SquareToGrid(MapSquareData square)
        {
            return new Tuple<uint, uint>(square.X, square.Y);
        }

        public void OnMapGenerated(MapResult result)
        {
            Utils.DestroyAllChildren(transform);
            var roomById = result.Rooms.ToDictionary(rm => rm.Id);
            var usedLocations = new HashSet<Tuple<uint, uint>>();

            foreach (var square in result.Squares.Where(sq => sq.Type == SquareType.Hull))
            {
                var obj = Instantiate(hullPrefab, transform, false);
                obj.transform.SetLocalPositionAndRotation(SquareToPosition(square), Quaternion.identity);
                usedLocations.Add(SquareToGrid(square));
                var i = Random.Range(0, roomById.Count);
                var hullController = obj.GetComponent<HullController>();
                hullController.direction = CompassDirection.SouthEast;
                hullController.UpdateStructure();
            }

            foreach (var rm in result.Rooms.Where(rm => rm.IsCorridor))
            {
                var obj = Instantiate(corridorPrefab, transform, false);
                var asSquare = (MapSquareData)rm;
                obj.transform.SetLocalPositionAndRotation(SquareToPosition(asSquare), Quaternion.identity);
                usedLocations.Add(SquareToGrid(asSquare));
                var corridorController = obj.GetComponent<CorridorController>();
                corridorController.openDirections = CardinalDirections.North | CardinalDirections.East | CardinalDirections.South;
                corridorController.UpdateEntrances();
            }
            
            // Now process ship squares, where we haven't already put other content, to avoid z-fighting
            foreach (var square in result.Squares
                         .Where(sq => sq.Type == SquareType.Ship && !usedLocations.Contains(SquareToGrid(sq)))
                     )
            {
                var obj = Instantiate(shipSquarePrefab, transform, false);
                obj.transform.SetLocalPositionAndRotation(SquareToPosition(square), Quaternion.identity);
            }
        }
    }
}