using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// Clones Unity objects without allowing Awake or Start to observe donor runtime state.
    /// </summary>
    internal static class InactiveObjectCloner
    {
        internal static T CloneComponent<T>(T source, Transform? parent) where T : Component
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            GameObject stagingRoot = CreateInactiveStagingRoot();
            try
            {
                GameObject clone = Object.Instantiate(source.gameObject, stagingRoot.transform, false);
                clone.SetActive(false);
                clone.transform.SetParent(parent, false);
                T? component = clone.GetComponent<T>();
                if (component != null)
                    return component;

                Object.DestroyImmediate(clone);
                throw new InvalidOperationException($"Cloned object has no {typeof(T).Name} component.");
            }
            finally
            {
                Object.DestroyImmediate(stagingRoot);
            }
        }

        internal static GameObject CloneGameObject(GameObject source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            GameObject stagingRoot = CreateInactiveStagingRoot();
            try
            {
                GameObject clone = Object.Instantiate(source, stagingRoot.transform, false);
                clone.SetActive(false);
                clone.transform.SetParent(null, false);
                return clone;
            }
            finally
            {
                Object.DestroyImmediate(stagingRoot);
            }
        }

        private static GameObject CreateInactiveStagingRoot()
        {
            var root = new GameObject("S1API_InactiveCloneStaging");
            root.SetActive(false);
            return root;
        }
    }
}
