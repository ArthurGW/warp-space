using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MapObjects
{
    public static class ObjectUtils
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
