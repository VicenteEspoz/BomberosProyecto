using UnityEngine;
using System.Collections;

public class VRFader : MonoBehaviour
{
    public float fadeTime = 1.0f;
    public Color fadeColor = Color.black;
    public bool startFadedOut = false; // Si true, empieza negro y aclara (útil al cargar escena nueva)

    private Material fadeMaterial = null;
    private float currentAlpha = 0.0f;

    void Awake()
    {
        // Creamos un material simple "en el aire" para no depender de archivos
        fadeMaterial = new Material(Shader.Find("Standard")); 
        // Intentamos usar un shader UI o Unlit si Standard da problemas, pero Standard suele funcionar.
        // Mejor opción para VR simple: "Unlit/Color" si existe, o manipulamos el Standard.
        if (Shader.Find("Unlit/Color") != null)
             fadeMaterial = new Material(Shader.Find("Unlit/Color"));

        fadeColor.a = 0;
        currentAlpha = startFadedOut ? 1.0f : 0.0f;
    }

    // Dibujamos un cuadrado frente a la cámara en cada frame
    void OnPostRender()
    {
        if (currentAlpha > 0)
        {
            fadeMaterial.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, currentAlpha);
            fadeMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Color(fadeMaterial.color);
            GL.Begin(GL.QUADS);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(0, 1, 0);
            GL.Vertex3(1, 1, 0);
            GL.Vertex3(1, 0, 0);
            GL.End();
            GL.PopMatrix();
        }
    }
    
    // Método público para llamar desde SocketManager
    public void FadeOut()
    {
        StartCoroutine(FadeRoutine(1.0f)); // Ir a Negro
    }

    public void FadeIn()
    {
        StartCoroutine(FadeRoutine(0.0f)); // Ir a Transparente
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = currentAlpha;
        float timer = 0.0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeTime);
            yield return null;
        }
        currentAlpha = targetAlpha;
    }
}