using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
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
    
    // La referencia al controlador del botón ha sido eliminada.
    
    [Header("Ajustes de Animación")]
    public float doorOpenSpeed = 2f; 
    public Vector3 doorOpenAngle = new Vector3(0, 90, 0); 
    public float disembarkSpeed = 3f; 

    private bool hasArrived = false; 

    void Start()
    {
        // Al iniciar, apagamos el movimiento del jugador
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

        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        Vector3 direction = target.position - transform.position;
        if (direction != Vector3.zero)
            transform.forward = Vector3.Lerp(transform.forward, direction.normalized, step);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentTargetIndex++;
            
            // Si llegamos al final de la lista (Comportamiento de Llegada Natural)
            if (currentTargetIndex >= targets.Count)
            {
                currentTargetIndex = targets.Count - 1;
                hasArrived = true; 
                
                // --- INICIAR SECUENCIA DE SALIDA Y ACTIVAR MOVIMIENTO DE JUGADOR ---
                StartCoroutine(DisembarkSequence());
            }
        }
    }

    // Esta corrutina maneja la salida del camión para que el jugador pueda moverse
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