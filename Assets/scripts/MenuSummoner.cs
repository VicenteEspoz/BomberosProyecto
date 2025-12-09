using UnityEngine;

public class MenuSummoner : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject canvasMenu; // El objeto del Canvas (ej. 'UI' o 'Canvas')
    public Transform headCamera;  // La cámara del jugador (CenterEyeAnchor)
    public float distancia = 1.5f; // A qué distancia aparecerá frente a la cara

    void Update()
    {
        // Detectar si se presiona el botón "B" en el control Derecho
        // Button.Two suele ser B en el derecho o Y en el izquierdo.
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        // 1. Si está desactivado, lo activamos primero
        if (!canvasMenu.activeSelf)
        {
            canvasMenu.SetActive(true);
        }

        // 2. Colocarlo frente al usuario
        PosicionarFrenteCamara();
    }

    void PosicionarFrenteCamara()
    {
        // Calculamos la posición: Posición de la cabeza + (Dirección hacia donde mira * distancia)
        Vector3 targetPosition = headCamera.position + (headCamera.forward * distancia);
        
        // Ajustamos la altura para que no dependa si el usuario mira al suelo o al cielo (opcional)
        // Si prefieres que siga la mirada exacta, comenta la siguiente línea:
        // targetPosition.y = headCamera.position.y; 

        canvasMenu.transform.position = targetPosition;

        // 3. Hacemos que el menú mire al usuario
        // Opción A: Que el menú mire EXACTAMENTE a la cámara
        canvasMenu.transform.LookAt(headCamera);
        // Como LookAt voltea el UI (lo pone al revés), lo giramos 180 grados
        canvasMenu.transform.Rotate(0, 180, 0);

        // Opción B (Más simple): Copiar la rotación de la cámara (el menú queda paralelo a la cara)
        // canvasMenu.transform.rotation = Quaternion.LookRotation(canvasMenu.transform.position - headCamera.position);
    }
}