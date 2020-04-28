using System;
using System.Collections;
using UnityEngine;

namespace Voxels.Common
{
    /// <summary>
    /// In Unity only enabled <see cref="MonoBehaviour"/>s can start a coroutine.
    /// This class helps us avoid this limitation.
    /// </summary>
    public class StaticCoroutine : MonoBehaviour
    {
        public static StaticCoroutine Instance;

        void Awake() => Instance = this;

        IEnumerator Perform(IEnumerator coroutine, Action onComplete = null)
        {
            onComplete = onComplete ?? delegate { };
            yield return StartCoroutine(coroutine);
            onComplete();
        }

        public static void DoCoroutine(IEnumerator coroutine, Action onComplete = null)
            => Instance.StartCoroutine(Instance.Perform(coroutine, onComplete));
    }
}
