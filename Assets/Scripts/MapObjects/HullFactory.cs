using System.Collections.Generic;
using System.Linq;
using Layout;
using LevelGenerator;
using UnityEngine;
using static Layout.LayoutUtils;

namespace MapObjects
{
    public class HullFactory : MonoBehaviour
    {

        public List<HullPiece> hullPieces;
        
        public void ConstructHull(List<MapSquareData> squares, uint mapWidth, uint mapHeight)
        {
            Debug.Log("HullFactory.ConstructHull");
            
            var mapPositions = squares.ToDictionary(sq => (sq.X, sq.Y), sq => sq.Type);
            foreach (var hullPos in mapPositions.Where(kv => kv.Value == SquareType.Hull))
            {
                foreach (var piece in hullPieces)
                {
                    var (go, rotation, flipped) = piece.TryGetPrefabAndRotation(hullPos.Key, mapPositions);
                    if (go == null || !rotation.HasValue) continue;
                    InstantiateHullPiece(hullPos.Key, go, rotation.Value, flipped);
                    break;
                }
            }
            Debug.Log("HullFactory.ConstructHull Done");
        }

        private void InstantiateHullPiece((uint X, uint Y) pos, GameObject prefab, Quaternion rotation, bool flipped)
        {
            var hull = Instantiate(prefab, transform);
            if (flipped) hull.transform.localScale = new Vector3(1f, 1f, -1f);
            
            hull.transform.SetPositionAndRotation(
                GridToPosition(pos),
                rotation
            );
        }
    }
}
