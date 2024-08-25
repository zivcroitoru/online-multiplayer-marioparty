using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MixLevels : MonoBehaviour
{
    public AudioMixer masterMixer;
    public void SetSfxLvl(float sfxLvl)
    {
        // Adjust the value to be within a practical range for volume
        masterMixer.SetFloat("sfxVol", Mathf.Clamp(sfxLvl, -80f, 5f));
    }

    public void SetMusicLvl(float musicLvl)
    {
        // Adjust the value to be within a practical range for volume
        masterMixer.SetFloat("musicVol", Mathf.Clamp(musicLvl, -80f, 5f));
    }
}