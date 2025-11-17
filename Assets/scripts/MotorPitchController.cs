using UnityEngine;
using UnityEngine.Audio;

public class MotorPitchController : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource engineSource;
    public AudioMixer mixer;

    [Header("Mixer Parameters")]
    public string motorVolParam = "MotorVol";
    public string motorPitchParam = "MotorPitch";

    [Header("Config")]
    public float minPitch = 0.8f;
    public float maxPitch = 2.2f;
    public float minVolume = 0.2f;
    public float maxVolume = 1f;

    public Rigidbody truckRb;

    void Update()
    {
        float speed = truckRb.velocity.magnitude;

        float pitch = Mathf.Lerp(minPitch, maxPitch, speed / 30f);
        engineSource.pitch = pitch;
        mixer.SetFloat(motorPitchParam, pitch);

        float vol = Mathf.Lerp(minVolume, maxVolume, speed / 30f);
        float dB = Mathf.Log10(Mathf.Clamp(vol, 0.0001f, 1f)) * 20f;
        mixer.SetFloat(motorVolParam, dB);
    }
}
