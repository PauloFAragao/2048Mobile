using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioClip moveSfx;
    [SerializeField] private AudioClip mergeSfx;

    [SerializeField] private AudioClip btnSfx;

    [SerializeField] private AudioSource audioSource;

    [SerializeField] private GameManager gm;

    [SerializeField] private Slider slider;

    private float _vol;

    public AudioMixer mixer;

    public void playMoveSfx()
    {
        playAudio(moveSfx);
    }

    public void playMergeSfx()
    {
        playAudio(mergeSfx);
    }

    public void PlayBtnSfx()
    {
        if (gm.soundIsOn)
            playAudio(btnSfx);
    }

    private void playAudio(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void SetMasterVol(float vol)
    {
        _vol = vol;
        mixer.SetFloat("MasterVol", vol <= 0 ? -80 : 20 * Mathf.Log10(vol));
    }

    public float GetMasterVol()
    {
        return _vol;
    }

    public void LoadVol(float vol)
    {
        slider.value = vol;

        mixer.SetFloat("MasterVol", vol <= 0 ? -80 : 20 * Mathf.Log10(vol));
    }

}
