using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Pickaxe : MonoBehaviour
{
    [SerializeField] AudioClip _wooshSound;
    AudioSource _audioSource;
    
    // Use this for initialization
    void Start() => _audioSource = GetComponent<AudioSource>();

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _audioSource.clip = _wooshSound;
            _audioSource.PlayOneShot(_wooshSound);
        }
    }
}
