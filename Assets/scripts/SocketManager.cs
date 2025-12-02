using SocketIOClient;
using UnityEngine;
using UnityEngine.SceneManagement; 
using System.Collections.Generic; 
using System; 

public class SocketManager : MonoBehaviour
{
    // 💡 ESTRUCTURA PARA DESERIALIZAR EL PAYLOAD JSON DEL BACKEND
    // CORRECCIÓN: Usamos Propiedades { get; set; } para asegurar que el deserializador JSON
    // pueda escribir los datos. Los campos simples a veces son ignorados.
    [System.Serializable]
    public class ScenarioLoadData
    {
        public int idEscenario { get; set; } 
        // CORRECCIÓN FINAL: Cambiado a int? (nullable int).
        // El JSON trae un número (ej: 10), el deserializador fallaba al intentar meter un número en un string.
        public int? idSesion { get; set; } 
    }

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
        // 🚨 CRÍTICO: Usa el nombre exacto que tienes en Build Settings
        { 1, "Scenes/casa" }, 
        { 2, "Scenes/choque" }, 
        // Añade más aquí según tu BD
    };

    void Start()
    {
        // 1. Configuración de la conexión
        var uri = new System.Uri("http://pacheco.chillan.ubiobio.cl:8020/"); 
        socket = new SocketIOUnity(uri);

        // Evento de conexión
        socket.OnConnected += (sender, e) => {
            Debug.Log("✅ Conectado al Backend. ID de Socket: " + socket.Id);
            // Registrar esta estación VR para que el backend sepa dónde enviar el comando
            socket.Emit("register-unity", vrStationId); 
        };
        
        // Manejo de errores de conexión (importante para depuración)
        socket.OnError += (sender, e) => {
            Debug.LogError($"❌ Error de Socket: {e}");
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
            lock (_executionQueue) 
            {
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

        // Notificar al backend que esta instancia VR está lista para recibir un escenario
        socket.Emit("unity-ready", vrStationId); 
        Debug.Log($"📡 Evento 'unity-ready' enviado con ID: {vrStationId}");
    }
    
    // --- LÓGICA DE RECEPCIÓN (THREAD DE RED) ---
    private void ReceiveAssignedScenario()
    {
        socket.On("load-scenario", (response) => {
            
            // 🔍 DEBUG IMPORTANTE: Ver qué llega exactamente antes de intentar leerlo
            Debug.Log($"📩 RAW JSON recibido del servidor: {response}");

            ScenarioLoadData data; // Objeto a deserializar
            string scenarioName = null;
            int scenarioId = 0;

            try
            {
                // Intentamos deserializar con las propiedades corregidas (int?)
                data = response.GetValue<ScenarioLoadData>(); 
                
                // Si la deserialización falla silenciosamente, data podría no ser null pero tener valores 0
                if (data == null)
                {
                     Debug.LogError("❌ Error: El objeto 'data' es NULL tras deserializar.");
                     return;
                }

                scenarioId = data.idEscenario;

                if (scenarioId == 0)
                {
                    Debug.LogError($"Error: El ID de Escenario es 0. Revisar si el JSON RAW coincide con 'idEscenario'.");
                    return;
                }
                
                // 💡 PASO CRÍTICO: Guardar el ID de Sesión en la clase estática
                // Ahora idSesion es del tipo int? (nullable), verificamos si tiene valor directamente
                if (data.idSesion.HasValue)
                {
                    SessionData.CurrentSessionId = data.idSesion.Value; 
                    Debug.Log($"✅ ID de Sesión recibido y guardado en SessionData: {SessionData.CurrentSessionId.Value}");
                }
                else
                {
                    SessionData.CurrentSessionId = null;
                    // Esto no es un error crítico si solo estamos probando escena, pero es warning
                    Debug.LogWarning($"⚠️ ID de Sesión es nulo en el payload.");
                }

                // Mapear el ID al nombre de la escena
                if (sceneMap.TryGetValue(scenarioId, out scenarioName))
                {
                    Debug.Log($"🚨 Comando recibido: Cargar ID {scenarioId} -> Escena '{scenarioName}'.");
                    
                    // Se añade a la cola para ser ejecutada en el Hilo Principal
                    string sceneToLoad = scenarioName; 
                    
                    lock (_executionQueue)
                    {
                        _executionQueue.Add(() => {
                             // Esto se ejecuta en el Main Thread y carga la nueva escena
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
                Debug.LogError($"❌ Error de deserialización en load-scenario: {ex.Message}. Payload RAW: {response}");
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