using Layout;
using UnityEngine;

namespace MapObjects
{
    public class CorridorEntrance : MonoBehaviour
    {
        public CardinalDirection direction;
        
        [SerializeField, HideInInspector]
        private bool isOpen;
        
        [SerializeField]
        private GameObject openPrefab;
        
        [SerializeField]
        private GameObject blockedPrefab;
        
        public void SetOpen(bool newOpen)
        {
            if (newOpen != isOpen || transform.childCount == 0)
            {
                ObjectUtils.DestroyAllChildren(transform);
                isOpen = newOpen;
                var prefab = isOpen ? openPrefab : blockedPrefab;
                var obj = Instantiate(prefab, transform, false);
                obj.transform.SetLocalPositionAndRotation(Vector3.zero, direction.ToRotation());
            }
        }
    }
}
