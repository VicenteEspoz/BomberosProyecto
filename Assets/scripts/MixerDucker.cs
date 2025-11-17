using UnityEngine;
using UnityEngine.Audio;

public class MixerDucker : MonoBehaviour
{
    public AudioMixer mixer;

    [Header("Parameters")]
    public string targetVolumeParam = "MotorVol";  
    public string radioVolumeParam = "RadioVol";

    [Header("Ducking Settings")]
    public float duckAmountDB = -15f; 
    public float thresholdDB = -20f; 
    public float fadeTime = 0.3f;

    private float originalDB;

    void Start()
    {
        mixer.GetFloat(targetVolumeParam, out originalDB);
    }

    void Update()
    {
        mixer.GetFloat(radioVolumeParam, out float radioDB);

        bool shouldDuck = radioDB > thresholdDB;

        StopAllCoroutines();
        if (shouldDuck)
            StartCoroutine(FadeTo(originalDB + duckAmountDB));
        else
            StartCoroutine(FadeTo(originalDB));
    }

    private System.Collections.IEnumerator FadeTo(float target)
    {
        mixer.GetFloat(targetVolumeParam, out float start);
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float v = Mathf.Lerp(start, target, t / fadeTime);
            mixer.SetFloat(targetVolumeParam, v);
            yield return null;
        }
    }
}
