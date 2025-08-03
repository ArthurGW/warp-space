using UnityEngine;

namespace MapObjects
{
    public static class SquareSize
    {
        public static readonly float X = 10f;
        public static readonly float Y = 10f;
    }
    
    public static class Utils
    {
        public static void DestroyAllChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; --i)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
#if UNITY_EDITOR
                Object.DestroyImmediate(child);
#else
                Object.Destroy(child);
#endif
            }
        }

        public static void DestroyAllChildren(GameObject parent)
        {
            DestroyAllChildren(parent.transform);
        }
    }
}
