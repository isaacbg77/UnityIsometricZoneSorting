using UnityEngine;

namespace IsometricZoneSorting
{
    internal static class SceneUtils
    {
        /// <summary>
        /// Finds the first active MonoBehaviour in the scene that implements <typeparamref name="T"/>.
        /// Returns null if none is found.
        /// </summary>
        public static T? FindInterfaceOfType<T>() where T : class
        {
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour is T match) return match;
            }
            return null;
        }
    }
}
