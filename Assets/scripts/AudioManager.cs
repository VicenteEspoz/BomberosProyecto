using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking; 
using System.IO;
using System;
using TMPro; // <--- 1. IMPORTANTE: Agregamos esta librería para usar Texto TMP

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Configuración del Servicio Web")]
    private const string BASE_URL = "http://pacheco.chillan.ubiobio.cl:8020/api/v1/sesiones";

    [Header("Referencias UI")]
    public TextMeshProUGUI textoEstado; // Variable para arrastrar el texto aquí

    [Header("Control de Grabación")]
    private AudioClip recording;
    private string micName;
    private bool isRecording = false;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        StartRecording();
    }

    public void EndSimulationAndQuit()
    {
        Debug.Log("🚨 Comando de cierre recibido. Procesando...");
        
        if (isRecording)
        {
            StartCoroutine(StopAndUploadAudio());
        }
        else
        {
            Debug.Log("No se estaba grabando. Cerrando app...");
            Application.Quit();
        }
    }

    private void StartRecording()
    {
        if (Microphone.devices.Length > 0)
        {
            micName = Microphone.devices[0];
            recording = Microphone.Start(micName, false, 300, 44100);
            isRecording = true;
            Debug.Log($"🎙️ Grabación iniciada con: {micName}");
        }
        else
        {
            Debug.LogWarning("⚠️ No se detectó micrófono. No se grabará audio.");
        }
    }

    IEnumerator StopAndUploadAudio()
    {
        // --- CAMBIO DE TEXTO VISUAL ---
        if (textoEstado != null)
        {
            textoEstado.text = "Subiendo audio...\nPor favor espere.";
            // Opcional: Cambiar color a amarillo o algo que llame la atención
            textoEstado.color = Color.yellow; 
        }

        // Detener micrófono
        int position = Microphone.GetPosition(micName);
        Microphone.End(micName);
        isRecording = false;

        Debug.Log("🎙️ Grabación detenida. Procesando audio...");

        if (!SessionData.CurrentSessionId.HasValue)
        {
            Debug.LogError("❌ Error: No hay ID de sesión. Cerrando...");
            Application.Quit(); 
            yield break;
        }

        // Esperamos un frame para asegurar que el texto se actualice visualmente en el casco
        yield return null; 

        byte[] wavData = WavUtility.FromAudioClip(recording, position);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("audio", wavData, "grabacion_final.wav", "audio/wav"));

        string url = $"{BASE_URL}/{SessionData.CurrentSessionId.Value}/audio";

        using (UnityWebRequest www = UnityWebRequest.Post(url, formData))
        {
            www.method = "PUT";
            Debug.Log($"📤 Subiendo audio ({wavData.Length / 1024 / 1024} MB)... Por favor espere.");
            
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Error al subir audio: {www.error}");
                // Opcional: Avisar error en el texto
                if (textoEstado != null) textoEstado.text = "Error al subir.";
            }
            else
            {
                Debug.Log("✅ Audio subido exitosamente.");
                if (textoEstado != null) textoEstado.text = "¡Subida exitosa!";
            }
        }

        Debug.Log("👋 Proceso terminado. Cerrando aplicación.");
        
        // Damos un pequeño respiro (0.5s) para que el usuario alcance a leer "Éxito" antes de que se cierre
        yield return new WaitForSeconds(0.5f);
        Application.Quit();
    }
}

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip, int lastPos)
    {
        using (var memoryStream = new MemoryStream())
        using (var writer = new BinaryWriter(memoryStream))
        {
            int sampleCount = (lastPos > 0) ? lastPos : clip.samples;
            sampleCount *= clip.channels;
            int frequency = clip.frequency;
            short channels = (short)clip.channels;
            short bitsPerSample = 16; 

            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + sampleCount * 2); 
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16); 
            writer.Write((short)1); 
            writer.Write(channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * bitsPerSample / 8); 
            writer.Write((short)(channels * bitsPerSample / 8)); 
            writer.Write(bitsPerSample);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(sampleCount * 2); 

            float[] data = new float[sampleCount];
            clip.GetData(data, 0); 

            foreach (var sample in data)
            {
                writer.Write((short)(sample * short.MaxValue));
            }
            return memoryStream.ToArray();
        }
    }
}