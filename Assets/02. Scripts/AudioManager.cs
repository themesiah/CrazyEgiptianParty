using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
    public static AudioManager instance;

    public AudioSource musicSource;

	// Use this for initialization
	void Awake () {
        instance = this;
	}

    public void PlayMusic()
    {
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
}
