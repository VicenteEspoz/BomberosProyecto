using UnityEngine;
using TMPro;
using Unity.Muse.Speech;   // O Unity.Sentis.Speech según tu paquete
using UnityEngine.EventSystems;

public class VoiceInputToFields : MonoBehaviour
{
    public SpeechToText dictation;

    public TMP_InputField inputUsuario;
    public TMP_InputField inputRUT;

    private TMP_InputField currentField;

    void Start()
    {
        dictation.OnFinalResult += OnFinalResult;
        dictation.OnPartialResult += OnPartialResult;
    }

    void Update()
    {
        // Detectar qué input está seleccionado (VR UI)
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            currentField = EventSystem.current.currentSelectedGameObject
                .GetComponent<TMP_InputField>();
        }
    }

    // Texto final reconocido
    void OnFinalResult(string text)
    {
        if (currentField == null) return;

        currentField.text = text;
    }

    // Texto parcial mientras hablas (modo opcional)
    void OnPartialResult(string text)
    {
        if (currentField == null) return;

        currentField.text = text;
    }

    // Botón para iniciar dictación
    public void StartDictation()
    {
        dictation.StartRecording();
    }

    // Botón para detener dictación
    public void StopDictation()
    {
        dictation.StopRecording();
    }
}
