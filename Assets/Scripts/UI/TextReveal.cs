using System.Collections;
using TMPro;
using UnityEngine;

namespace Voxels.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextReveal : MonoBehaviour
    {
        TextMeshProUGUI _message;

        void Awake() => _message = GetComponent<TextMeshProUGUI>();

        public void HideMessage()
        {
            _message.text = "";
            _message.ForceMeshUpdate(true);
        }

        public void ShowNewMessage(string text)
        {
            _message.text = text;
            _message.ForceMeshUpdate(true);
            StopAllCoroutines();
            StartCoroutine("Reveal");
        }

        /// <summary>
        /// Reveals the text and after 5 seconds clears it up
        /// </summary>
        IEnumerator Reveal()
        {
            int totalVisibleCharacters = _message.textInfo.characterCount;
            int counter = 0;

            while(true)
            {
                int visibleCount = counter % (totalVisibleCharacters + 1);

                _message.maxVisibleCharacters = visibleCount;

                if(visibleCount >= totalVisibleCharacters)
                    break;

                counter++;

                yield return new WaitForSeconds(0.09f);
            }

            yield return new WaitForSeconds(5f);

            _message.text = "";
        }
    }
}
