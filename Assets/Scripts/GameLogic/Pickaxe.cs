using UnityEngine;

namespace Voxels.GameLogic
{
    [RequireComponent(typeof(AudioSource))]
    public class Pickaxe : MonoBehaviour
    {
#pragma warning disable CS0649 // suppress "Field is never assigned to, and will always have its default value null"
        [SerializeField] AudioClip _wooshSound;
#pragma warning restore CS0649
        AudioSource _audioSource;

        void Start() => _audioSource = GetComponent<AudioSource>();

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _audioSource.clip = _wooshSound;
                _audioSource.PlayOneShot(_wooshSound);
            }
        }
    }
}
