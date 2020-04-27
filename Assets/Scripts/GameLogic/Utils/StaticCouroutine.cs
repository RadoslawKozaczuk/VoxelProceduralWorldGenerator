using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    /// <summary>
    /// In Unity only enabled <see cref="MonoBehaviour"/>s can start a coroutine.
    /// This class helps us avoid this limitation.
    /// </summary>
    public class StaticCoroutine : MonoBehaviour
    {
        static public StaticCoroutine instance;

        void Awake() => instance = this;

        IEnumerator Perform(IEnumerator coroutine, Action onComplete = null)
        {
            onComplete = onComplete ?? delegate { };
            yield return StartCoroutine(coroutine);
            onComplete();
        }

        static public void DoCoroutine(IEnumerator coroutine, Action onComplete = null)
            => instance.StartCoroutine(instance.Perform(coroutine, onComplete));
    }
}
