using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
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
        {
            instance.StartCoroutine(instance.Perform(coroutine, onComplete));
        }
    }
}
