using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Layout;
using LevelGenerator;
using UnityEngine;
using static Layout.LayoutUtils;

namespace MapObjects
{
    /// <summary>
    /// Class to convert an abstract generated map into a collection of managed GameObjects
    /// </summary>
    [RequireComponent(typeof(HullFactory), typeof(RoomFactory))]
    public class GameMapController : MonoBehaviour
    {
        [SerializeField]
        private GameObject shipSquarePrefab;
        
        private HullFactory _hullFactory;
        private RoomFactory _roomFactory;

        private void Awake()
        {
            _hullFactory = GetComponent<HullFactory>();
            _roomFactory = GetComponent<RoomFactory>();
        }

        public void OnMapGenerationFailed()
        {
            Debug.Log("GameMapController.OnMapGenerationFailed");
            ObjectUtils.DestroyAllChildren(transform);
            Debug.Log("GameMapController.OnMapGenerationFailed Done");
        }

        public void OnMapGenerated(MapResult result)
        {
            _hullFactory ??= GetComponent<HullFactory>();
            _roomFactory ??= GetComponent<RoomFactory>();
            
            Debug.Log("GameMapController.OnMapGenerated");
            ObjectUtils.DestroyAllChildren(transform);
            var roomsById = result.Rooms.ToDictionary(rm => rm.Id);
            Debug.Log("GameMapController.OnMapGenerated Room Dict");
            // var usedLocations = new HashSet<(uint, uint)>();

            _hullFactory.ConstructHull(result.Squares,
                result.Width,
                result.Height);
            
            Debug.Log("GameMapController.OnMapGenerated Parsing Corridors");
            
            _roomFactory.ConstructRooms(result.Rooms,
                roomsById,
                result.Adjacencies);
            Debug.Log("GameMapController.OnMapGenerated Parsing ship squares");
            // Finally, process ship squares to fill in the gaps
            // We could work out the gaps here, but we have Ship squares so we may as well use them
            foreach (var square in result.Squares
                         .Where(sq => sq.Type == SquareType.Ship)
                     )
            {
                var obj = Instantiate(shipSquarePrefab,
                    transform,
                    false);
                obj.transform.SetLocalPositionAndRotation(
                    square.ToPosition(),
                    Quaternion.identity);
            }
            Debug.Log("GameMapController.OnMapGenerated Done");
        }
    }
}