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
                DestroyGameObject(parent.GetChild(i).gameObject);
            }
        }

        public static void DestroyGameObject(GameObject obj)
        {
            obj.SetActive(false);
#if UNITY_EDITOR
            Object.DestroyImmediate(obj);
#else
                Object.Destroy(obj);
#endif
        }
    }
}
