using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSounds: MonoBehaviour, IPointerEnterHandler, ISelectHandler, ISubmitHandler {
	public AudioClip onHover;
	public AudioClip onClick;

	AudioSource audioSource;

	void Awake() {
		audioSource = gameObject.GetComponent<AudioSource>();
		if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
	}

	public void OnSubmit(BaseEventData data) {
		// if it's a button that closes its parent UI
		if (gameObject.activeSelf) audioSource.PlayOneShot(onClick);
	}

	public void OnPointerEnter(PointerEventData data) {
		if (gameObject.activeSelf) audioSource.PlayOneShot(onHover);
	}

	public void OnSelect(BaseEventData data) {
		if (gameObject.activeSelf) audioSource.PlayOneShot(onHover);
	}
}
