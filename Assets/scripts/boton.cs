using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking; // Necesario para UnityWebRequest
using TMPro; // Necesario para usar objetos TextMeshPro

public class LoginManager : MonoBehaviour
{
    // ⚙️ Configuración
    // Reemplaza esto con la URL real de tu Backend (ej. tu IP o localhost:3000)
    private const string API_URL = "http://localhost:3000/api/v1/bomberos"; 

    // Variables públicas para las referencias de los InputFields
    public TMP_InputField inputNombre;
    public TMP_InputField inputRUT; 
    
    // Estructura para serializar los datos a JSON
    // Las propiedades deben coincidir con los campos esperados por tu Backend (modelos/servicios de Sequelize)
    [System.Serializable]
    public class BomberoData
    {
        public string NombreCompleto; // El nombre del bombero
        public string Rut;    // El RUT del bombero
    }

    public void ProcesarDatos()
    {
        string nombre = inputNombre.text;
        string rut = inputRUT.text;
        
        if (string.IsNullOrEmpty(rut) || string.IsNullOrEmpty(nombre) || rut.Length < 8) 
        {
            Debug.LogError("Error: Nombre o RUT inválido o vacío. Por favor, revisa.");
            // Opcional: Mostrar mensaje de error en pantalla al usuario
            return; 
        }

        Debug.Log("Validación OK. Preparando para enviar datos al Backend...");
        
        // Iniciar la coroutine para el envío de datos
        StartCoroutine(SendBomberoData(nombre, rut));
    }

    // Coroutine para enviar los datos del Bombero al Backend
    private IEnumerator SendBomberoData(string nombre, string rut)
    {
        // 1. Crear el objeto de datos
        BomberoData data = new BomberoData
        {
            NombreCompleto = nombre,
            Rut = rut
        };
        
        // 2. Serializar el objeto a una cadena JSON
        string json = JsonUtility.ToJson(data);
        
        // 3. Crear la petición POST
        UnityWebRequest request = new UnityWebRequest(API_URL, "POST");
        
        // Convertir el JSON a bytes y adjuntarlo a la petición
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        
        // 4. Establecer las cabeceras para indicar que el contenido es JSON
        request.SetRequestHeader("Content-Type", "application/json");

        // 5. Enviar la petición y esperar por la respuesta
        yield return request.SendWebRequest();

        // 6. Manejar la respuesta
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            // Error de conexión o error HTTP (ej. 404, 500)
            Debug.LogError("Error al registrar Bombero: " + request.error);
            Debug.LogError("Respuesta del servidor: " + request.downloadHandler.text);
        }
        else
        {
            // Éxito: El backend respondió con éxito (ej. 201 Created)
            Debug.Log("Bombero registrado exitosamente. Respuesta: " + request.downloadHandler.text);
            
            // Aquí iría la lógica para enviar el evento por WebSocket (siguiente paso)
            // Y luego pasar a la pantalla de espera de escenario
        }
    }
}