using Layout;
using UnityEngine;

namespace MapObjects
{
    public class CorridorController : MonoBehaviour
    {
        public CardinalDirections openDirections;
        
        private void Awake()
        {
            UpdateEntrances();
        }

        private void Start()
        {
            UpdateEntrances();
        }

        private void Reset()
        {
            UpdateEntrances();
        }

        public void UpdateEntrances()
        {
            foreach (var child in GetComponentsInChildren<CorridorEntrance>())
            {
                child.SetOpen(openDirections.HasFlag((CardinalDirections)child.direction));
            }
        }
    }
}
