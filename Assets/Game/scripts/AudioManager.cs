using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioClip moveSfx;
    [SerializeField] private AudioClip mergeSfx;

    [SerializeField] private AudioClip btnSfx;

    [SerializeField] private AudioSource audioSource;

    [SerializeField] private GameManager gm;

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

}
