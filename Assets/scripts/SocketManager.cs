using SocketIOClient;
using UnityEngine;
using UnityEngine.SceneManagement; 
using System.Collections.Generic; 
using System; // Necesario para la clase Action

public class SocketManager : MonoBehaviour
{
    // COLECCIÓN ESTÁTICA PARA MANTENER ACCIONES EN COLA (Main Thread Dispatcher)
    private static readonly List<Action> _executionQueue = new List<Action>();
    private static bool _updateQueued = false; // Flag para saber si hay acciones pendientes

    // Cliente de Socket.io
    private SocketIOUnity socket;
    
    [Tooltip("ID único de esta estación VR que el Panel Angular debe conocer.")]
    public string vrStationId = "VR-STATION-01"; 

    // Mapeo local de ID de BD a Nombre de Escena de Unity
    private Dictionary<int, string> sceneMap = new Dictionary<int, string>()
    {
        // 🚨 CRÍTICO: Usa el nombre exacto que tienes en Build Settings (ej: "Scenes/casa")
        { 1, "Scenes/casa" },    // ID 1 en la BD
        { 2, "Scenes/choque" }, 
    };

    void Start()
    {
        // ... (Tu lógica de Start existente) ...
        
        // 1. Configuración de la conexión
        var uri = new System.Uri("http://pacheco.chillan.ubiobio.cl:8020/"); 
        socket = new SocketIOUnity(uri);

        // Evento de conexión
        socket.OnConnected += (sender, e) => {
            Debug.Log("✅ Conectado al Backend. ID de Socket: " + socket.Id);
            socket.Emit("register-unity", vrStationId); 
        };
        
        // 2. Activar la escucha del comando de escena inmediatamente
        ReceiveAssignedScenario();

        // Iniciar la conexión
        socket.Connect();
    }
    
    // Ejecuta las acciones en la cola en el HILO PRINCIPAL
    void Update()
    {
        // Solo ejecutamos si hay acciones pendientes
        if (_executionQueue.Count > 0)
        {
            // Bloqueamos la colección para evitar concurrencia
            lock (_executionQueue) 
            {
                // Copiamos las acciones y limpiamos la cola original
                var actionsToExecute = new List<Action>(_executionQueue);
                _executionQueue.Clear();

                // Ejecutamos en el hilo principal
                foreach (var action in actionsToExecute)
                {
                    action.Invoke();
                }
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

        socket.Emit("unity-ready", vrStationId); 
        Debug.Log($"📡 Evento 'unity-ready' enviado con ID: {vrStationId}");
    }
    
    // --- LÓGICA DE RECEPCIÓN (THREAD DE RED) ---
    private void ReceiveAssignedScenario()
    {
        socket.On("load-scenario", (response) => {
            
            int scenarioId = 0;
            string scenarioName = null;

            try
            {
                scenarioId = response.GetValue<int>(); 

                if (scenarioId == 0)
                {
                    Debug.LogError("Error: El ID de Escenario recibido es inválido (0). Payload RAW: " + response.ToString());
                    return;
                }

                // Mapear el ID al nombre de la escena
                if (sceneMap.TryGetValue(scenarioId, out scenarioName))
                {
                    Debug.Log($"🚨 Comando recibido: Cargar ID {scenarioId} -> Escena '{scenarioName}'.");

                    // 🚨 LA SOLUCIÓN: En lugar de llamar LoadScene directamente,
                    // la añadimos a la cola para ser ejecutada en el Hilo Principal (Main Thread)
                    
                    string sceneToLoad = scenarioName; // Capturamos el nombre de la escena
                    
                    lock (_executionQueue)
                    {
                        _executionQueue.Add(() => {
                             // Esto se ejecutará en el método Update() del hilo principal
                             SceneManager.LoadScene(sceneToLoad); 
                             Debug.Log($"✅ Escena '{sceneToLoad}' cargada con éxito en el Main Thread.");
                        });
                        _updateQueued = true;
                    }

                }
                else
                {
                    Debug.LogError($"Error: ID de Escenario {scenarioId} no encontrado en el mapa de escenas de Unity.");
                }

            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error de conversión en load-scenario: {ex.Message}.");
            }
        });
    }
    
    // Limpieza al salir
    private void OnDestroy()
    {
        if (socket != null && socket.Connected)
        {
            socket.Disconnect();
        }
    }
}