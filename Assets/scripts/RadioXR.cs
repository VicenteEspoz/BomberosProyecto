using UnityEngine;
using UnityEngine.Audio;

// HEMOS ELIMINADO: using UnityEngine.XR.Interaction.Toolkit;

public class RadioXR : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource radioSource;
    public AudioSource clickSource;
    public AudioClip[] radioMessages;
    public AudioClip clickOn;
    public AudioClip clickOff;
    public AudioMixer mixer;

    [Header("Mixer Parameters")]
    public string radioVolParam = "RadioVol";

    // HEMOS ELIMINADO: La variable 'interactable' y la función 'Awake()'

    private bool transmitting = false;

    // --- ¡NUEVAS FUNCIONES PÚBLICAS! ---
    // Llama a estas desde tus Meta Building Blocks (HandGrabInstallationRoutine)

    /// <summary>
    /// Llama a esta función desde el evento "On Grab" (Agarrar) de tu Building Block.
    /// </summary>
    public void AgarrarRadio()
    {
        clickSource.PlayOneShot(clickOn);
        StartTransmission();
        
        // Esto activa el "Audio Ducking" para bajar el motor
        SetRadioVolume(1.0f);
    }

    /// <summary>
    /// Llama a esta función desde el evento "On Release" (Soltar) de tu Building Block.
    /// </summary>
    public void SoltarRadio()
    {
        clickSource.PlayOneShot(clickOff);
        StopTransmission();
        
        // Esto restaura el sonido del motor
        SetRadioVolume(0.0f);
    }

    // --- El resto del script se mantiene igual ---

    public void StartTransmission()
    {
        if (transmitting) return;
        transmitting = true;
        StartCoroutine(RadioRoutine());
    }

    public void StopTransmission()
    {
        transmitting = false;
    }

    private System.Collections.IEnumerator RadioRoutine()
    {
        while (transmitting)
        {
            AudioClip msg = radioMessages[Random.Range(0, radioMessages.Length)];
            radioSource.PlayOneShot(msg);
            yield return new WaitForSeconds(Random.Range(3f, 7f));
        }
    }

    // Esta función controla el parámetro del Mixer
    public void SetRadioVolume(float linear)
    {
        float dB = Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f;
        mixer.SetFloat(radioVolParam, dB);
    }
}