using System;
using Layout;
using UnityEngine;

namespace MapObjects
{
    public class HullController : MonoBehaviour
    {
        [SerializeField]
        private GameObject straightPrefab;
        private readonly Quaternion _straightStartingRotation = Quaternion.identity;
    
        [SerializeField]
        private GameObject diagonalPrefab;
        private readonly Quaternion _diagonalStartingRotation = CompassDirection.NorthEast.ToRotation();

        public CompassDirection direction;

        private void Awake()
        {
            UpdateStructure();
        }

        public void UpdateStructure()
        {
            Utils.DestroyAllChildren(transform);
            
            GameObject prefab;
            Quaternion startingRotation;
            if (direction.IsDiagonal())
            {
                prefab = diagonalPrefab;
                startingRotation = _diagonalStartingRotation;
            }
            else
            {
                prefab = straightPrefab;
                startingRotation = _straightStartingRotation;
            }
            var hull = Instantiate(prefab, transform, false);
            hull.transform.SetLocalPositionAndRotation(
                Vector3.zero,
                direction.ToRotation() * Quaternion.Inverse(startingRotation)
            );
        }
    }
}
