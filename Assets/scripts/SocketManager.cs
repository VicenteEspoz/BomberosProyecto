using SocketIOClient;
using UnityEngine;
using UnityEngine.SceneManagement; 
using TMPro;
using System.Collections.Generic; 
using System; 
using System.Collections; // Necesario para IEnumerator

public class SocketManager : MonoBehaviour
{
    // ... (Tus clases SceneLoadData y variables siguen igual) ...
    [System.Serializable]
    public class ScenarioLoadData
    {
        public int idEscenario { get; set; } 
        public int? idSesion { get; set; } 
    }

    private static readonly List<Action> _executionQueue = new List<Action>();
    private static bool _updateQueued = false;
    private SocketIOUnity socket;
    
    public string vrStationId = "VR-STATION-01"; 

    [Header("Referencias de UI")] // Organiza el inspector
    [Tooltip("Arrastra aquí el texto TMP que mostrará el mensaje")]
    public TMP_Text feedbackText; // <--- NUEVO: Referencia al texto en el Canvas

    // Referencia opcional a un Fader (si tienes OVRScreenFade o uno propio)
    // Si usas el SDK de Meta, busca el script OVRScreenFade en tu cámara.
    [Tooltip("Arrastra aquí tu script de Fader si tienes uno, o déjalo vacío")]
    public VRFader screenFader; 

    private Dictionary<int, string> sceneMap = new Dictionary<int, string>()
    {
        { 1, "Scenes/casa" }, 
        { 2, "Scenes/choque" }, 
    };

    void Start()
    {
        // Intenta encontrar el Fader automáticamente si no está asignado
        // Ahora buscamos el componente VRFader, no el OVRScreenFade
        if (screenFader == null) screenFader = FindObjectOfType<VRFader>();

        var uri = new System.Uri("http://pacheco.chillan.ubiobio.cl:8020/"); 
        socket = new SocketIOUnity(uri);

        socket.OnConnected += (sender, e) => {
            Debug.Log("✅ Conectado al Backend.");
            socket.Emit("register-unity", vrStationId); 
        };
        
        socket.OnError += (sender, e) => Debug.LogError($"❌ Error: {e}");
        
        ReceiveAssignedScenario();
        socket.Connect();
    }
    
    void Update()
    {
        if (_executionQueue.Count > 0)
        {
            lock (_executionQueue) 
            {
                var actionsToExecute = new List<Action>(_executionQueue);
                _executionQueue.Clear();
                foreach (var action in actionsToExecute) action.Invoke();
                _updateQueued = false;
            }
        }
    }
    // Método para ser llamado por el botón "Iniciar Simulación"
    public void OnIniciarSimulacionButtonPressed()
    {
        if (socket == null || !socket.Connected)
        {
            Debug.LogError("❌ Socket no está conectado. No se puede enviar 'unity-ready'.");
            return;
        }

        // Notificar al backend que esta instancia VR está lista para recibir un escenario
        socket.Emit("unity-ready", vrStationId); 
        Debug.Log($"📡 Evento 'unity-ready' enviado con ID: {vrStationId}");
        if (feedbackText != null) 
        {
            feedbackText.text = "Esperando instrucciones del instructor...";
            feedbackText.color = Color.yellow; // Opcional: Cambiar color a amarillo (aviso)
        }
    }
    
    // --- LÓGICA DE RECEPCIÓN (THREAD DE RED) ---
   private void ReceiveAssignedScenario()
    {
        socket.On("load-scenario", (response) => {
            // ... (Tu lógica de deserialización y validación sigue igual) ...
            // ... (Asumiendo que ya obtuviste scenarioName e idSesion correctamente) ...

             ScenarioLoadData data = response.GetValue<ScenarioLoadData>();
             // ... [Tus validaciones aquí] ...
             int scenarioId = data.idEscenario;
             if (data.idSesion.HasValue) SessionData.CurrentSessionId = data.idSesion.Value;

            string scenarioName;
            if (sceneMap.TryGetValue(scenarioId, out scenarioName))
            {
                // AQUI ESTA EL CAMBIO IMPORTANTE
                // Encolamos el inicio de la Corrutina en el Main Thread
                lock (_executionQueue)
                {
                    _executionQueue.Add(() => {
                        // Iniciamos el proceso asíncrono
                        if (feedbackText != null) feedbackText.text = "Cargando escenario...";
                        StartCoroutine(LoadSceneAsyncRoutine(scenarioName));
                    });
                    _updateQueued = true;
                }
            }
        });
    }

    // 🔥 NUEVA CORRUTINA PARA CARGA SUAVE EN VR
    IEnumerator LoadSceneAsyncRoutine(string sceneName)
    {
        Debug.Log($"⏳ Iniciando transición a {sceneName}...");

        // 1. FADE OUT (Fundido a Negro)
        // Esto evita que el usuario vea el "congelamiento"
        if (screenFader != null)
        {
            screenFader.FadeOut(); 
            yield return new WaitForSeconds(screenFader.fadeTime + 0.5f); // Esperamos un poco más que el fade
        }
        else
        {
            // Si no tienes fader, al menos espera un frame para asegurar logs
            Debug.LogWarning("⚠️ No se detectó OVRScreenFade. El cambio será brusco.");
        }

        // 2. CARGA ASÍNCRONA
        // Esto carga la escena en memoria sin detener el juego por completo
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Opcional: Evitar que la escena se active hasta que esté lista
        asyncLoad.allowSceneActivation = false;

        // Esperar mientras carga (el progreso va de 0 a 0.9)
        while (asyncLoad.progress < 0.9f)
        {
            // Aquí podrías actualizar una barra de carga si quisieras
            Debug.Log($"Cargando: {asyncLoad.progress * 100}%");
            yield return null;
        }

        // 3. ACTIVACIÓN
        // Una vez cargado al 90%, permitimos la activación
        asyncLoad.allowSceneActivation = true;

        // Esperamos a que termine totalmente
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log("✅ Escena activada. La nueva escena debería encargarse del Fade In.");
    }

    private void OnDestroy()
    {
        if (socket != null && socket.Connected) socket.Disconnect();
    }
}