using UnityEngine;

namespace Voxels.GameLogic
{
	[RequireComponent(typeof(AudioSource))]
	public class Pickaxe : MonoBehaviour
	{
		[SerializeField] AudioClip _wooshSound;
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
