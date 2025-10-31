using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TruckPathMover : MonoBehaviour
{
    public List<Transform> targets; // lista de puntos de destino
    public float speed = 5f;        // velocidad del camión
    private int currentTargetIndex = 0; // índice del destino actual

    void Update()
    {
        if (targets.Count == 0) return; // si no hay destinos, salir

        Transform target = targets[currentTargetIndex];
        float step = speed * Time.deltaTime;

        // mover hacia el destino actual
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        // El camión mira hacia donde se mueve
        Vector3 direction = target.position - transform.position;
        if (direction != Vector3.zero)
            transform.forward = Vector3.Lerp(transform.forward, direction.normalized, step);

        // cuando llega al destino (distancia muy pequeña)
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            // pasar al siguiente destino
            currentTargetIndex++;

            // Final del camino
            if (currentTargetIndex >= targets.Count)
            {
                // detenerse
                currentTargetIndex = targets.Count - 1;

            
            }
        }
    }
}
