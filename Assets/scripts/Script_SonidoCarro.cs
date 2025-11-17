using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_SonidoCarro : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip MotorEncendido;
    public AudioClip MotorApagado;
    public AudioClip Sirena;
    public AudioClip Bocina;
    public AudioClip Radio;    

    public void playMotorEncendido()
    {
        audioSource.PlayLoop(MotorEncendido);
    }

    public void playMotorApagado()
    {
        udioSource.stop();
    }
    public void playSirena()
    {
        audioSource.PlayLoop(Sirena);
    }
    public void playRadio()
    {
        audioSource.PlayLoop(Radio);
    }
}
