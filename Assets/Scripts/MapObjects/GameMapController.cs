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
    [RequireComponent(typeof(HullFactory))]
    public class GameMapController : MonoBehaviour
    {
        [SerializeField]
        private GameObject corridorPrefab;
        
        [SerializeField]
        private GameObject shipSquarePrefab;
        
        [SerializeField]
        private GameObject roomPrefab;
        
        private HullFactory _hullFactory;

        private void Awake()
        {
            _hullFactory = GetComponent<HullFactory>();
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
            
            Debug.Log("GameMapController.OnMapGenerated");
            ObjectUtils.DestroyAllChildren(transform);
            var roomById = result.Rooms.ToDictionary(rm => rm.Id);
            Debug.Log("GameMapController.OnMapGenerated Room Dict");
            var usedLocations = new HashSet<(uint, uint)>();

            _hullFactory.ConstructHull(result.Squares, result.Width, result.Height);
            
            Debug.Log("GameMapController.OnMapGenerated Parsing Corridors");
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
            
            Debug.Log("GameMapController.OnMapGenerated Parsing ship squares");
            // Now process ship squares, where we haven't already put other content, to avoid z-fighting
            foreach (var square in result.Squares
                         .Where(sq => sq.Type == SquareType.Ship && !usedLocations.Contains(SquareToGrid(sq)))
                     )
            {
                var obj = Instantiate(shipSquarePrefab, transform, false);
                obj.transform.SetLocalPositionAndRotation(SquareToPosition(square), Quaternion.identity);
            }
            Debug.Log("GameMapController.OnMapGenerated Done");
        }
    }
}