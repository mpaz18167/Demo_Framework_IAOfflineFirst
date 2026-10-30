using System.Runtime.ConstrainedExecution;
using UnityEngine;
using static UnityEngine.InputManagerEntry;

[DisallowMultipleComponent]
public class AnimacionEnemigoVisual : MonoBehaviour
{
    [Header("Referencias (auto-relleno si se deja vacio)")]
    public Animator animator;

    [Header("Parametros del Animator")]
    public string paramVelocidad = "Velocidad";
    public string triggerAtacar = "Atacar";
    public string triggerMorir = "Morir";

    [Header("Ajustes")]
    [Tooltip("m/s que mapea a Velocidad=1.0 (correr)")]
    public float velocidadReferencia = 3.5f;

    private Vector3 posicionPrevia;
    private float velocidadSuavizada;

    private void Reset() { animator = GetComponentInChildren<Animator>(); }

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        posicionPrevia = transform.position;
    }

    // Metodos publicos: los llaman los hooks de UNA linea en los scripts de IA
    public void Atacar()
    {
        if (animator != null) animator.SetTrigger(triggerAtacar);
    }

    public void Morir()
    {
        if (animator != null) animator.SetTrigger(triggerMorir);
    }

    private void Update()
    {
        if (animator == null) return;

        Vector3 delta = transform.position - posicionPrevia;
        posicionPrevia = transform.position;
        delta.y = 0f;
        float velocidadInstantanea = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        velocidadSuavizada = Mathf.Lerp(velocidadSuavizada, velocidadInstantanea, 12f * Time.deltaTime);

        animator.SetFloat(paramVelocidad, velocidadSuavizada / velocidadReferencia);
    }
}
