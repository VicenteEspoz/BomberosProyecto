using UnityEngine;
using UnityEngine.Audio;

public class SirenController : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource sirenSource;
    public AudioMixer mixer;

    [Header("Mixer Parameters")]
    public string sirenVolParam = "SirenVol";

    [Header("Fade")]
    public float fadeTime = 1.2f;

    private bool active = false;
    private float current = 0f;

    public void ToggleSiren()
    {
        if (active) StopSiren();
        else StartSiren();
    }

    public void StartSiren()
    {
        if (active) return;
        active = true;
        sirenSource.loop = true;
        sirenSource.Play();
        StartCoroutine(FadeTo(1f));
    }

    public void StopSiren()
    {
        if (!active) return;
        active = false;
        StartCoroutine(FadeTo(0f));
    }

    private System.Collections.IEnumerator FadeTo(float target)
    {
        float t = 0f;
        float start = current;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            current = Mathf.Lerp(start, target, t / fadeTime);

            float dB = Mathf.Log10(Mathf.Clamp(current, 0.0001f, 1f)) * 20f;
            mixer.SetFloat(sirenVolParam, dB);
            yield return null;
        }

        current = target;

        if (current <= 0.01f)
            sirenSource.Stop();
    }
}
