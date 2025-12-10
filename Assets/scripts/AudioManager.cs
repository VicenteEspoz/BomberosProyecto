using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking; 
using System.IO;
using System;

// Este script maneja la lógica de grabación, subida de audio y cierre de la aplicación.
public class AudioManager : MonoBehaviour
{
    // Instancia Singleton para acceso global
    public static AudioManager Instance { get; private set; }

    [Header("Configuración del Servicio Web")]
    private const string BASE_URL = "http://pacheco.chillan.ubiobio.cl:8020/api/v1/sesiones";

    [Header("Control de Grabación")]
    private AudioClip recording;
    private string micName;
    private bool isRecording = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // --- 1. INICIAR GRABACIÓN AUTOMÁTICA ---
        StartRecording();
    }

    // Método público llamado por el botón (a través de EndSimulationButtonController)
    public void EndSimulationAndQuit()
    {
        Debug.Log("🚨 Comando de cierre recibido. Procesando...");
        
        if (isRecording)
        {
            // Iniciamos la subida. La app se cerrará DENTRO de esta corrutina cuando termine.
            StartCoroutine(StopAndUploadAudio());
        }
        else
        {
            // Si no estaba grabando, cerramos inmediatamente
            Debug.Log("No se estaba grabando. Cerrando app...");
            Application.Quit();
        }
    }

    private void StartRecording()
    {
        if (Microphone.devices.Length > 0)
        {
            micName = Microphone.devices[0];
            // Grabamos hasta 300 segundos (5 minutos), a 44100Hz
            recording = Microphone.Start(micName, false, 300, 44100);
            isRecording = true;
            Debug.Log($"🎙️ Grabación iniciada con: {micName}");
        }
        else
        {
            Debug.LogWarning("⚠️ No se detectó micrófono. No se grabará audio.");
        }
    }

    // Corrutina para dar tiempo al UnityWebRequest de empezar la subida antes de cerrar
    IEnumerator QuitAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("👋 Cerrando aplicación después de intentar subir el audio.");
        Application.Quit();
    }

    IEnumerator StopAndUploadAudio()
    {
        // Detener micrófono
        int position = Microphone.GetPosition(micName);
        Microphone.End(micName);
        isRecording = false;

        Debug.Log("🎙️ Grabación detenida. Procesando audio...");

        // Verificación de seguridad
        if (!SessionData.CurrentSessionId.HasValue)
        {
            Debug.LogError("❌ Error: No hay ID de sesión. Cerrando...");
            Application.Quit(); // Cerramos si hay error
            yield break;
        }

        // --- PROCESAMIENTO (Esto puede tardar un poco) ---
        // Sugerencia: Aquí podrías activar un texto en el Canvas que diga "SUBIENDO DATOS, ESPERE..."
        yield return null; // Esperamos un frame para que la UI se actualice si pusiste un mensaje

        byte[] wavData = WavUtility.FromAudioClip(recording, position);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("audio", wavData, "grabacion_final.wav", "audio/wav"));

        string url = $"{BASE_URL}/{SessionData.CurrentSessionId.Value}/audio";

        using (UnityWebRequest www = UnityWebRequest.Post(url, formData))
        {
            www.method = "PUT";
            Debug.Log($"📤 Subiendo audio ({wavData.Length / 1024 / 1024} MB)... Por favor espere.");
            
            // --- ESPERAMOS AQUI HASTA QUE TERMINE LA SUBIDA ---
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Error al subir audio: {www.error}");
            }
            else
            {
                Debug.Log("✅ Audio subido exitosamente.");
            }
        }

        // --- 3. AHORA SÍ CERRAMOS LA APP ---
        Debug.Log("👋 Proceso terminado. Cerrando aplicación.");
        Application.Quit();
    }
}

// --- CLASE DE UTILIDAD PARA CONVERTIR A WAV ---
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

            // --- CABECERA WAV ---
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

            // --- DATOS DE AUDIO ---
            float[] data = new float[sampleCount];
            clip.GetData(data, 0); 

            // Convertir float (-1 a 1) a short (PCM 16-bit)
            foreach (var sample in data)
            {
                writer.Write((short)(sample * short.MaxValue));
            }

            return memoryStream.ToArray();
        }
    }
}