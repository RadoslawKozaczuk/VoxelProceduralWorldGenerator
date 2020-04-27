using System.Collections;
using TMPro;
using UnityEngine;

namespace Voxels.UI
{
    public class TextReveal : MonoBehaviour
    {
        public TextMeshProUGUI Message;

        public void HideMessage()
        {
            Message.text = "";
            Message.ForceMeshUpdate(true);
        }

        public void ShowNewMessage(string text)
        {
            Message.text = text;
            Message.ForceMeshUpdate(true);
            StopAllCoroutines();
            StartCoroutine("Reveal");
        }

        /// <summary>
        /// Reveals the text and after 5 seconds clears it up
        /// </summary>
        /// <returns></returns>
        IEnumerator Reveal()
        {
            int totalVisibleCharacters = Message.textInfo.characterCount;
            int counter = 0;

            while(true)
            {
                int visibleCount = counter % (totalVisibleCharacters + 1);

                Message.maxVisibleCharacters = visibleCount;

                if(visibleCount >= totalVisibleCharacters)
                    break;

                counter++;

                yield return new WaitForSeconds(0.09f);
            }

            yield return new WaitForSeconds(5f);

            Message.text = "";
        }
    }
}
