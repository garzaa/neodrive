using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSounds: MonoBehaviour, IPointerEnterHandler, ISelectHandler {
	public AudioClip onHover;
	public AudioClip onClick;

	AudioSource audioSource;

	void Awake() {
		audioSource = gameObject.GetComponent<AudioSource>();
		if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
	}

	void Start() {
		GetComponent<Button>().onClick.AddListener(PlayClickSouond);	
	}

	void PlayClickSouond() {
		if (audioSource.enabled) audioSource.PlayOneShot(onClick);
	}

	public void OnPointerEnter(PointerEventData data) {
		if (audioSource.enabled) audioSource.PlayOneShot(onHover);
	}

	public void OnSelect(BaseEventData data) {
		if (audioSource.enabled) audioSource.PlayOneShot(onHover);
	}
}
