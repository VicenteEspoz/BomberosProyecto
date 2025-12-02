using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking; // Necesario para la petición HTTP
using System.IO;
using System;

public class CamionBombero : MonoBehaviour
{
    [Header("Configuración del Camino")]
    public List<Transform> targets; 
    public float speed = 5f;
    private int currentTargetIndex = 0;

    [Header("Configuración de Salida")]
    public Transform rightDoor;      
    public Transform cameraRig;      
    public Transform disembarkPoint; 
    
    [Header("Configuración de Movimiento")]
    public GameObject smoothControls; 
    
    [Header("Ajustes de Animación")]
    public float doorOpenSpeed = 2f; 
    public Vector3 doorOpenAngle = new Vector3(0, 90, 0); 
    public float disembarkSpeed = 3f; 

    [Header("Configuración de Audio")]
    private AudioClip recording;
    private string micName;
    private bool isRecording = false;
    private const string BASE_URL = "http://pacheco.chillan.ubiobio.cl:8020/api/v1/sesiones";

    private bool hasArrived = false; 

    void Start()
    {
        // Al iniciar, apagamos el movimiento del jugador
        if (smoothControls != null)
        {
            smoothControls.SetActive(false);
        }

        // --- 1. INICIAR GRABACIÓN ---
        StartRecording();
    }

    void Update()
    {
        if (targets.Count == 0 || hasArrived) return;

        MoveTruck();
    }

    void StartRecording()
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

    void MoveTruck()
    {
        Transform target = targets[currentTargetIndex];
        float step = speed * Time.deltaTime;

        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        Vector3 direction = target.position - transform.position;
        if (direction != Vector3.zero)
            transform.forward = Vector3.Lerp(transform.forward, direction.normalized, step);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentTargetIndex++;

            if (currentTargetIndex >= targets.Count)
            {
                currentTargetIndex = targets.Count - 1;
                hasArrived = true; 
                
                // --- 2. DETENER GRABACIÓN Y SUBIR ---
                if (isRecording)
                {
                    StartCoroutine(StopAndUploadAudio());
                }
                
                StartCoroutine(DisembarkSequence());
            }
        }
    }

    IEnumerator StopAndUploadAudio()
    {
        if (!isRecording) yield break;

        // Detener micrófono
        int position = Microphone.GetPosition(micName);
        Microphone.End(micName);
        isRecording = false;

        Debug.Log("🎙️ Grabación detenida. Procesando audio...");

        // Verificar si tenemos un ID de sesión válido (del script anterior)
        if (!SessionData.CurrentSessionId.HasValue)
        {
            Debug.LogError("❌ No hay SessionData.CurrentSessionId. No se puede subir el audio.");
            yield break;
        }

        // Convertir AudioClip a WAV (bytes)
        byte[] wavData = WavUtility.FromAudioClip(recording, position);

        // Preparar el formulario Multipart
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        // "audio" es el nombre del campo que espera tu backend (uploadAudio.single('audio'))
        // "grabacion_camion.wav" es el nombre del archivo
        // "audio/wav" es el mime type
        formData.Add(new MultipartFormFileSection("audio", wavData, "grabacion_camion.wav", "audio/wav"));

        // Construir URL: /api/v1/sesiones/:id/audio
        string url = $"{BASE_URL}/{SessionData.CurrentSessionId.Value}/audio";

        // Crear la petición. Usamos POST inicialmente para configurar el form-data, luego cambiamos a PUT
        using (UnityWebRequest www = UnityWebRequest.Post(url, formData))
        {
            // 🚨 TRUCO: UnityWebRequest.Post configura el Content-Type correctamente para multipart.
            // Cambiamos el método a PUT manualmente para satisfacer tu backend.
            www.method = "PUT"; 

            Debug.Log($"📤 Subiendo audio a: {url}");
            
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Error al subir audio: {www.error} - {www.downloadHandler.text}");
            }
            else
            {
                Debug.Log("✅ Audio subido correctamente al servidor.");
            }
        }
    }

    IEnumerator DisembarkSequence()
    {
        yield return new WaitForSeconds(0.5f);

        Quaternion initialRotation = rightDoor.localRotation;
        Quaternion targetRotation = initialRotation * Quaternion.Euler(doorOpenAngle);
        float time = 0;

        while (time < 1)
        {
            time += Time.deltaTime * doorOpenSpeed;
            rightDoor.localRotation = Quaternion.Slerp(initialRotation, targetRotation, time);
            yield return null; 
        }

        yield return new WaitForSeconds(0.2f);

        if (cameraRig != null && disembarkPoint != null)
        {
            cameraRig.parent = null; 

            while (Vector3.Distance(cameraRig.position, disembarkPoint.position) > 0.05f)
            {
                cameraRig.position = Vector3.MoveTowards(cameraRig.position, disembarkPoint.position, disembarkSpeed * Time.deltaTime);
                yield return null;
            }
        }

        if (smoothControls != null)
        {
            smoothControls.SetActive(true);
            Debug.Log("Jugador ha bajado. Movimiento activado.");
        }
        else
        {
            Debug.LogWarning("No asignaste 'SmoothControls' en el inspector del camión.");
        }

        Debug.Log("Secuencia de llegada completada.");
    }
}

// --- CLASE DE UTILIDAD PARA CONVERTIR A WAV ---
// Unity no trae esto por defecto, es necesario para que el backend entienda el archivo.
public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip, int lastPos)
    {
        using (var memoryStream = new MemoryStream())
        using (var writer = new BinaryWriter(memoryStream))
        {
            // Si lastPos es 0 (o muy pequeño), usaremos todo el clip, sino recortamos hasta donde grabó
            int sampleCount = (lastPos > 0) ? lastPos : clip.samples;
            sampleCount *= clip.channels;
            
            int frequency = clip.frequency;
            short channels = (short)clip.channels;
            short bitsPerSample = 16; 

            // --- CABECERA WAV ---
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + sampleCount * 2); // File size - 8
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16); // Chunk size
            writer.Write((short)1); // Audio format (1 = PCM)
            writer.Write(channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * bitsPerSample / 8); // Byte rate
            writer.Write((short)(channels * bitsPerSample / 8)); // Block align
            writer.Write(bitsPerSample);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(sampleCount * 2); // Data chunk size

            // --- DATOS DE AUDIO ---
            float[] data = new float[sampleCount];
            // Extraer solo la parte grabada
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