using UnityEngine;

namespace Layout
{
    public class CorridorEntrance : MonoBehaviour
    {
        public Direction direction;
        private bool _isOpen;

        public void SetOpen(bool isOpen)
        {
            _isOpen = isOpen;
            transform.Find("Blocked").gameObject.SetActive(!isOpen);
            transform.Find("Open").gameObject.SetActive(isOpen);
        }
    }
}
