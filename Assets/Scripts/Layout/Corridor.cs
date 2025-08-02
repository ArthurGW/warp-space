using UnityEngine;

namespace Layout
{
    public class Corridor : MonoBehaviour
    {
        public Directions selected;
        
        void Awake()
        {
            UpdateEntrances();
        }

        public void UpdateEntrances()
        {
            foreach (var child in GetComponentsInChildren<CorridorEntrance>())
            {
                child.SetOpen(selected.HasFlag((Directions)child.direction));
            }
        }
    }
}
