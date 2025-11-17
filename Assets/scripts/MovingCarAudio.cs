using UnityEngine;
using UnityEngine.Audio;

public class MovingCarAudio : MonoBehaviour
{
    public AudioSource engineSource;
    public AudioMixer mixer;
    public string trafficVolParam = "TrafficVol";

    [Header("Distance Settings")]
    public float maxDistance = 45f;
    public float minVolume = 0.05f;
    public float maxVolume = 1f;

    private Transform listener;

    void Start()
    {
        listener = Camera.main.transform;
        engineSource.loop = true;
        engineSource.dopplerLevel = 1.6f;
        engineSource.Play();
    }

    void Update()
    {
        if (!listener) return;

        float dist = Vector3.Distance(transform.position, listener.position);
        float v = Mathf.InverseLerp(maxDistance, 0f, dist);
        v = Mathf.Lerp(minVolume, maxVolume, v);

        float dB = Mathf.Log10(Mathf.Clamp(v, 0.0001f, 1f)) * 20f;
        mixer.SetFloat(trafficVolParam, dB);
    }
}
