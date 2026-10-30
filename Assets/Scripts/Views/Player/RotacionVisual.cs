using Unity.VisualScripting;
using UnityEngine;


// VISUAL ONLY: gira el modelo visual (hijo) hacia donde se mueve el player.
// NO toca PlayerMovement, ni CharacterController, ni colliders, ni telemetria.
// Lee la velocidad REAL del CharacterController -> funciona con cualquier origen
// de movimiento (input, rebote del pisoton, etc.) sin acoplarse a PlayerMovement.
[DisallowMultipleComponent]
public class RotacionVisual : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Modelo visual que debe girar. Nunca el objeto con el CharacterController.")]
    public Transform modeloVisual;

    [Header("Ajustes")]
    [Tooltip("Suavidad del giro (mayor = gira mas rapido).")]
    public float velocidadGiro = 12f;
    [Tooltip("Debajho de esta velocidad (m/s) el modelo no gira (evita temblor al estar quieto).")]
    public float umbralVelocidad = 0.5f;

    private CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    // LateUpdate: gira DESPUES de que PlayerMovement se haya movido este frame
    void LateUpdate()
    {
        if (modeloVisual == null || controller == null) return;

        Vector3 vel = controller.velocity;
        vel.y = 0f; //ignorar salto/ caida -> solo direccion horizontal
        if (vel.magnitude < umbralVelocidad) return; // quieto: mantiene la ultima orientacion

        Quaternion objetivo = Quaternion.LookRotation(vel.normalized, Vector3.up);
        modeloVisual.rotation = Quaternion.Slerp(modeloVisual.rotation, objetivo, Time.deltaTime * velocidadGiro);

    }

}
