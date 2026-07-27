using UnityEngine;

namespace S1API.Internal.Rendering
{
    internal static class PresentationWorkbenchVisualResolver
    {
        internal static GameObject? FindVisibleRoot(GameObject? source)
        {
            if (source == null)
                return null;

            Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);
            Transform? common = null;

            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (!renderer.enabled)
                    continue;

                common = common == null
                    ? renderer.transform
                    : FindCommonAncestor(common, renderer.transform, source.transform);
            }

            return common?.gameObject;
        }

        private static Transform FindCommonAncestor(
            Transform left,
            Transform right,
            Transform boundary)
        {
            Transform candidate = left;
            while (candidate != boundary && !IsAncestorOf(candidate, right))
                candidate = candidate.parent;

            return IsAncestorOf(candidate, right) ? candidate : boundary;
        }

        private static bool IsAncestorOf(Transform ancestor, Transform target)
        {
            Transform? current = target;
            while (current != null)
            {
                if (current == ancestor)
                    return true;
                current = current.parent;
            }

            return false;
        }
    }
}
