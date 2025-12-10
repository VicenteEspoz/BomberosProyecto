using UnityEngine;

public class MenuSummoner : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject canvasMenu;
    public Transform headCamera;
    public float distancia = 1.5f;

    void Update()
    {
        // PRUEBA DE DIAGNÓSTICO:
        // Si presionas CUALQUIER botón, debería salir un mensaje.
        if (OVRInput.GetDown(OVRInput.Button.Any))
        {
            Debug.Log("LOG DE BOMBEROS: Se presionó algún botón.");
        }

        // Intenta con Button.Two (Suele ser B o Y)
        // O prueba Button.SecondaryThumbstick (si presionas la palanca) para testear.
        if (OVRInput.GetDown(OVRInput.Button.Two)) 
        {
            Debug.Log("LOG DE BOMBEROS: Botón B detectado! Moviendo Canvas...");
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        if (canvasMenu == null || headCamera == null)
        {
            Debug.LogError("LOG DE BOMBEROS: Error - Faltan asignar variables en el Inspector.");
            return;
        }

        if (!canvasMenu.activeSelf)
        {
            canvasMenu.SetActive(true);
        }

        Vector3 targetPosition = headCamera.position + (headCamera.forward * distancia);
        // Opcional: Mantener la altura fija para que no se vaya al cielo si miras arriba
        // targetPosition.y = headCamera.position.y; 

        canvasMenu.transform.position = targetPosition;

        // Hacemos que mire a la cámara y corregimos la rotación
        canvasMenu.transform.LookAt(headCamera);
        canvasMenu.transform.Rotate(0, 180, 0);
    }
}