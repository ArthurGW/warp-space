using UnityEngine;

namespace Layout
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
                for (var i = transform.childCount - 1; i >= 0; --i)
                {
                    var child = transform.GetChild(i).gameObject;
                    child.SetActive(false);
#if UNITY_EDITOR
                    DestroyImmediate(child);
#else
                    Destroy(child);
#endif
                }
                _isOpen = isOpen;
                var prefab = isOpen ? openPrefab : blockedPrefab;
                var obj = Instantiate(prefab, transform, false);
                obj.transform.SetLocalPositionAndRotation(Vector3.zero, Directions.DirectionToRotation(direction));
            }
        }
    }
}
