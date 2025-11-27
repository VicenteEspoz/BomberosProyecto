using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class RecordingAndUpload : MonoBehaviour
{
    [Header("Backend")]
    public string BACKEND_URL = "http://pacheco.chillan.ubiobio.cl:8020"; // Ajusta según tu backend

    [Header("Recording")]
    public int maxRecordingSeconds = 60;
    public int sampleRate = 48000;

    [Header("Session")]
    [Tooltip("Si ya tienes sessionId lo puedes poner; si no, el script creará una sesión en el backend.")]
    public string sessionId = "";

    private AudioClip recordingClip;
    private bool isRecording = false;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        EnsureMicrophonePermission();
    }

    public void StartRecording()
    {
        if (isRecording) return;
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No microphone devices found.");
            return;
        }

        recordingClip = Microphone.Start(null, false, maxRecordingSeconds, sampleRate);
        isRecording = true;
        Debug.Log("Recording started...");
    }

    public void StopRecordingAndUpload()
    {
        if (!isRecording)
        {
            Debug.LogWarning("Not recording.");
            return;
        }

        int pos = Microphone.GetPosition(null);
        Microphone.End(null);
        isRecording = false;

        if (pos <= 0)
        {
            Debug.LogWarning("Recorded length is zero.");
            return;
        }

        int channels = recordingClip.channels;
        float[] samples = new float[recordingClip.samples * channels];
        recordingClip.GetData(samples, 0);

        float[] trimmed = new float[pos * channels];
        Array.Copy(samples, 0, trimmed, 0, trimmed.Length);

        AudioClip trimmedClip = AudioClip.Create("trimmed", pos, channels, recordingClip.frequency, false);
        trimmedClip.SetData(trimmed, 0);

        #if UNITY_EDITOR
        audioSource.clip = trimmedClip;
        audioSource.Play();
        #endif

        byte[] wavBytes = WavUtility.FromAudioClip(trimmedClip);
        StartCoroutine(EnsureSessionAndUpload(wavBytes));
    }

    private IEnumerator EnsureSessionAndUpload(byte[] wavBytes)
    {
        // 1) Si no hay sessionId, crear una sesión válida en el servidor.
        if (string.IsNullOrEmpty(sessionId))
        {
            string createUrl = CombineUrl(BACKEND_URL, "/api/v1/sesiones");
            Debug.Log("Creating session at: " + createUrl);

            // Construimos un JSON mínimo válido: Duracion y Fecha son campos obligatorios en el modelo
            var nowIso = DateTime.UtcNow.ToString("o"); // ISO 8601
            var json = $"{{\"Duracion\":\"00:00:00\",\"Fecha\":\"{nowIso}\"}}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            UnityWebRequest req = new UnityWebRequest(createUrl, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool error = (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError);
#else
            bool error = (req.isNetworkError || req.isHttpError);
#endif
            if (error)
            {
                Debug.LogError($"Failed to create session: {req.error} - {req.downloadHandler.text}");
                yield break;
            }

            // Parseamos el JSON: backend responde { message: '...', sesion: { ID_Sesion: 123, ... } }
            try
            {
                CreateSessionResponse resp = JsonUtility.FromJson<CreateSessionResponse>(req.downloadHandler.text);
                if (resp != null && resp.sesion != null)
                {
                    // ID_Sesion es un entero; lo convertimos a string
                    sessionId = resp.sesion.ID_Sesion.ToString();
                }
                else
                {
                    Debug.LogWarning("Respuesta inesperada al crear sesión: " + req.downloadHandler.text);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not parse create-session response: " + ex.Message + " raw:" + req.downloadHandler.text);
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                Debug.LogError("Session ID not returned by server. Aborting upload.");
                yield break;
            }

            Debug.Log("Created session: " + sessionId);
        }

        // 2) Subir el WAV (multipart/form-data). Backend acepta PUT /api/v1/sesiones/{id}/audio
        string uploadUrl = CombineUrl(BACKEND_URL, $"/api/v1/sesiones/{sessionId}/audio");
        Debug.Log("Uploading audio to: " + uploadUrl + " (size=" + wavBytes.Length + ")");

        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavBytes, $"session-{sessionId}.wav", "audio/wav");

        UnityWebRequest uwr = UnityWebRequest.Post(uploadUrl, form);
        uwr.method = UnityWebRequest.kHttpVerbPUT; // usamos PUT porque el backend está así

        // Unity crea los headers del form; cuando se cambia method a PUT, los headers se conservan en Unity 2019+/2020+
        yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        bool uploadError = (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError);
#else
        bool uploadError = (uwr.isNetworkError || uwr.isHttpError);
#endif
        if (uploadError)
        {
            Debug.LogError($"Upload failed: {uwr.error} - {uwr.downloadHandler.text}");
        }
        else
        {
            Debug.Log("Upload successful: " + uwr.downloadHandler.text);
        }
    }

    private void EnsureMicrophonePermission()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
        }
        #endif
    }

    private string CombineUrl(string baseUrl, string path)
    {
        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.TrimEnd('/');
        if (!path.StartsWith("/")) path = "/" + path;
        return baseUrl + path;
    }

    [Serializable]
    private class CreateSessionResponse
    {
        public string message;
        public SesionResponse sesion;
    }

    [Serializable]
    private class SesionResponse
    {
        public int ID_Sesion;
        public string Duracion;
        public string Fecha;
        public string Audio_Sesion;
        // puedes añadir otros campos si los necesitas
    }
}