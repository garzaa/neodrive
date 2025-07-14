using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSounds: MonoBehaviour, IPointerDownHandler, IPointerEnterHandler {
	public AudioClip onHover;
	public AudioClip onClick;

	AudioSource audioSource;

	void Awake() {
		audioSource = gameObject.GetComponent<AudioSource>();
	}

	public void OnPointerDown(PointerEventData data) {
		audioSource.PlayOneShot(onClick);
	}

	public void OnPointerEnter(PointerEventData data) {
		audioSource.PlayOneShot(onHover);
	}
}
