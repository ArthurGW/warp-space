using System.Collections.Generic;
using System.Linq;
using Layout;
using LevelGenerator;
using UnityEngine;
using static Layout.LayoutUtils;
using static MapObjects.ObjectUtils;

namespace MapObjects
{
    public class HullFactory : MonoBehaviour
    {
        public List<HullPiece> hullPieces;

        public void ConstructHull(List<MapSquareData> squares, uint mapWidth, uint mapHeight)
        {
            Debug.Log("HullFactory.ConstructHull");

            DestroyAllChildren(transform);
            
            var mapPositions = squares.ToDictionary(sq => (sq.X, sq.Y), sq => sq.Type);
            var index = 0U;
            
            // For each hull location, try and match it against each available hull piece prefab
            foreach (var hullPos in mapPositions.Where(kv => kv.Value == SquareType.Hull))
            {
                foreach (var piece in hullPieces)
                {
                    var (go, rotation, flipped) = piece.TryGetPrefabAndRotation(hullPos.Key, mapPositions);
                    if (go == null || !rotation.HasValue) continue;
                    InstantiateHullPiece(hullPos.Key, go, rotation.Value, flipped, ++index);
                    break;
                }
            }
            Debug.Log("HullFactory.ConstructHull Done");
        }

        private void InstantiateHullPiece((uint X, uint Y) pos, GameObject prefab, Quaternion rotation, bool flipped, uint index)
        {
            var hull = Instantiate(prefab, transform);
            hull.name = hull.name.Insert(0, index.ToString());  // Make name unique
            if (flipped) hull.transform.localScale = new Vector3(1f, 1f, -1f);
            
            hull.transform.SetPositionAndRotation(
                GridToPosition(pos),
                rotation
            );
        }
    }
}
