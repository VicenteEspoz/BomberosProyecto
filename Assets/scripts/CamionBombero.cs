using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamionBombero : MonoBehaviour
{
    [Header("Configuración del Camino")]
    public List<Transform> targets; 
    public float speed = 5f;
    private int currentTargetIndex = 0;

    [Header("Configuración de Salida")]
    public Transform rightDoor;      // Arrastra aquí el objeto de la puerta derecha
    public Transform cameraRig;      // El objeto padre del jugador (PlayerController o Camera Rig)
    public Transform disembarkPoint; // Un Empty GameObject fuera del camión a donde irá el jugador
    
    [Header("Configuración de Movimiento")]
    public GameObject smoothControls; // ARRASTRA AQUÍ TU OBJETO "SmoothControls"
    
    [Header("Ajustes de Animación")]
    public float doorOpenSpeed = 2f; // Qué tan rápido abre la puerta
    public Vector3 doorOpenAngle = new Vector3(0, 90, 0); // Ángulo de apertura
    public float disembarkSpeed = 3f; // Velocidad a la que sale el jugador

    private bool hasArrived = false; 

    void Start()
    {
        // Al iniciar el juego, apagamos el movimiento para que no camines dentro del camión
        if (smoothControls != null)
        {
            smoothControls.SetActive(false);
        }
    }

    void Update()
    {
        // Si no hay destinos o ya llegamos, no hacemos nada
        if (targets.Count == 0 || hasArrived) return;

        MoveTruck();
    }

    void MoveTruck()
    {
        Transform target = targets[currentTargetIndex];
        float step = speed * Time.deltaTime;

        // Mover camión
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        // Rotar camión
        Vector3 direction = target.position - transform.position;
        if (direction != Vector3.zero)
            transform.forward = Vector3.Lerp(transform.forward, direction.normalized, step);

        // Chequear distancia
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentTargetIndex++;

            // Si llegamos al final de la lista
            if (currentTargetIndex >= targets.Count)
            {
                currentTargetIndex = targets.Count - 1;
                hasArrived = true; // Marcamos que llegó
                
                // INICIAR SECUENCIA DE SALIDA
                StartCoroutine(DisembarkSequence());
            }
        }
    }

    // Esta corrutina maneja el orden de los eventos
    IEnumerator DisembarkSequence()
    {
        // 1. Esperar un momento pequeño para que se sienta que frenó
        yield return new WaitForSeconds(0.5f);

        // 2. Abrir la puerta
        Quaternion initialRotation = rightDoor.localRotation;
        Quaternion targetRotation = initialRotation * Quaternion.Euler(doorOpenAngle);
        float time = 0;

        while (time < 1)
        {
            time += Time.deltaTime * doorOpenSpeed;
            rightDoor.localRotation = Quaternion.Slerp(initialRotation, targetRotation, time);
            yield return null; 
        }

        // 3. Esperar un momento con la puerta abierta
        yield return new WaitForSeconds(0.2f);

        // 4. Mover la Camera Rig hacia afuera
        if (cameraRig != null && disembarkPoint != null)
        {
            cameraRig.parent = null; // Desemparentar para que sea independiente

            // Movemos al jugador hasta el punto de bajada
            while (Vector3.Distance(cameraRig.position, disembarkPoint.position) > 0.05f)
            {
                cameraRig.position = Vector3.MoveTowards(cameraRig.position, disembarkPoint.position, disembarkSpeed * Time.deltaTime);
                yield return null;
            }
        }

        // 5. ACTIVAR MOVIMIENTO DEL JUGADOR
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