using SocketIOClient;
using UnityEngine;

public class SocketManager : MonoBehaviour
{
    private SocketIOUnity socket;
    
    // Identificador único de esta estación o visor VR
    public string vrStationId = "VR-STATION-01"; 

    void Start()
    {
        var uri = new System.Uri("http://localhost:3000"); // URL de tu backend
        socket = new SocketIOUnity(uri);

        socket.OnConnected += (sender, e) => {
            Debug.Log("Conectado al Backend.");
            // Enviar el ID de la estación al conectarse para que el servidor lo registre
            socket.Emit("register-unity-station", vrStationId); 
        };

        socket.Connect();
    }

    public void OnIniciarSimulacionButtonPressed()
    {
        // 1. Envía el evento 'unity-ready' con el ID de la estación
        var payload = new { stationId = vrStationId };
        socket.Emit("unity-ready", JsonUtility.ToJson(payload)); 
        Debug.Log("Evento 'unity-ready' enviado.");
    }
    
    // 2. Método para recibir el Escenario asignado desde el Backend
    public void ReceiveAssignedScenario()
    {
        socket.On("load-scenario", (response) => {
            string scenarioName = response.GetValue<string>(); // o un objeto JSON completo
            Debug.Log($"Escenario recibido: {scenarioName}. Cargando escena...");
            // Aquí iría la lógica para cargar la escena de Unity
            // SceneManager.LoadScene(scenarioName); 
        });
    }

    // Llama a este método en el evento del botón "Iniciar Simulación"
}