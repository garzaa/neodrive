using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSounds: MonoBehaviour, IPointerEnterHandler, ISelectHandler, ISubmitHandler {
	public AudioClip onHover;
	public AudioClip onClick;

	AudioSource audioSource;

	void Awake() {
		audioSource = gameObject.GetComponent<AudioSource>();
	}

	public void OnSubmit(BaseEventData data) {
		audioSource.PlayOneShot(onClick);
	}

	public void OnPointerEnter(PointerEventData data) {
		audioSource.PlayOneShot(onHover);
	}

	public void OnSelect(BaseEventData data) {
		audioSource.PlayOneShot(onHover);
	}
}
