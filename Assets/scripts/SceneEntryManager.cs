using UnityEngine;
using System.Collections;

public class SceneEntryManager : MonoBehaviour
{
    // Arrastra aquí el CenterEyeAnchor que tiene el VRFader
    public VRFader screenFader; 

    void Start()
    {
        if (screenFader == null) 
            screenFader = FindObjectOfType<VRFader>();

        // Iniciamos la rutina de "Entrada Suave"
        StartCoroutine(SmoothEntryRoutine());
    }

    IEnumerator SmoothEntryRoutine()
    {
        // 1. ESPERA DE SEGURIDAD (El "Colchón")
        // Dejamos 2 o 3 frames para que Unity termine de procesar todos los 'Awake' y 'Start' pesados.
        // Mientras tanto, la pantalla sigue negra gracias al "Start Faded Out".
        yield return new WaitForEndOfFrame(); 
        yield return new WaitForEndOfFrame(); 
        yield return new WaitForSeconds(0.5f); // Opcional: espera medio segundo extra por si acaso.

        // 2. AHORA SÍ, FADE IN
        // El lag ya pasó, ahora mostramos la escena suavemente.
        if (screenFader != null)
        {
            screenFader.FadeIn();
        }
    }
}