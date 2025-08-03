using Layout;
using UnityEngine;

namespace MapObjects
{
    public class CorridorEntrance : MonoBehaviour
    {
        public CardinalDirection direction;
        private bool _isOpen;
        
        [SerializeField]
        private GameObject openPrefab;
        
        [SerializeField]
        private GameObject blockedPrefab;
        
        public void SetOpen(bool isOpen)
        {
            if (_isOpen != isOpen || transform.childCount == 0)
            {
                Utils.DestroyAllChildren(transform);
                _isOpen = isOpen;
                var prefab = isOpen ? openPrefab : blockedPrefab;
                var obj = Instantiate(prefab, transform, false);
                obj.transform.SetLocalPositionAndRotation(Vector3.zero, direction.ToRotation());
            }
        }
    }
}
