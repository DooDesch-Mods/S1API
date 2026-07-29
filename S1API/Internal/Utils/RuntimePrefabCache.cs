using UnityEngine;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// Keeps runtime-created prefab templates alive without allowing their
    /// behaviours to tick in the scene.
    /// </summary>
    internal static class RuntimePrefabCache
    {
        private static GameObject? _root;

        internal static void Store(GameObject prefab)
        {
            GameObject root = GetOrCreateRoot();
            prefab.hideFlags = HideFlags.HideAndDontSave;
            prefab.transform.SetParent(root.transform, false);
            prefab.transform.localPosition = Vector3.zero;
            prefab.transform.localRotation = Quaternion.identity;

            // Native consumers instantiate the template without explicitly
            // activating the clone. Keep activeSelf true while the inactive
            // cache parent prevents the template's behaviours from ticking.
            prefab.SetActive(true);
        }

        private static GameObject GetOrCreateRoot()
        {
            if (_root != null)
                return _root;

            _root = new GameObject("S1API_RuntimePrefabCache")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _root.SetActive(false);
            Object.DontDestroyOnLoad(_root);
            return _root;
        }
    }
}
