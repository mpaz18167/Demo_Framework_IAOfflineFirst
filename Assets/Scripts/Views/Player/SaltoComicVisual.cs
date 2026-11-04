using UnityEngine;

[DisallowMultipleComponent] // Evita que se agregue más de una instancia del componente al mismo GameObject
public class SaltoComicVisual : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el modelo 3D hijo que contiene la malla del personaje")]
    public Transform modeloVisual;
    [Header("Ajustes de Deformación")]
    [Tooltip("Escala al despegar: se estira en Y y se comprime en X/Z")]
    public Vector3 escalaSalto = new Vector3(0.85f, 1.25f, 0.85f);
    [Tooltip("Escala al caer: se aplasta en Y y se ensancha en X/Z")]
    public Vector3 escalaCaida = new Vector3(1.25f, 0.75f, 1.25f);
    [Tooltip("Velocidad de recuperación elástica")]
    public float velocidadRetorno = 10f;
    private CharacterController controller;
    private bool estabaEnSuelo = true;
    private Vector3 escalaOriginal = Vector3.one;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (modeloVisual != null) escalaOriginal = modeloVisual.localScale;
    }
    private void Update()
    {
        if (controller == null || modeloVisual == null) return;
        bool enSuelo = controller.isGrounded;
        // 1. Detectar ATERRIZAJE (estaba cayendo y acaba de tocar el suelo)
        if (!estabaEnSuelo && enSuelo)
        {
            modeloVisual.localScale = escalaCaida; // Squash (aplastar)
        }
        estabaEnSuelo = enSuelo;
        // 2. Retorno elástico suave hacia la escala original (1, 1, 1)
        modeloVisual.localScale = Vector3.Lerp(modeloVisual.localScale, escalaOriginal, Time.deltaTime * velocidadRetorno);
    }
    // Se llama desde PlayerMovement.cs al presionar la tecla de salto
    public void GatillarEstiramientoSalto()
    {
        if (modeloVisual != null)
        {
            modeloVisual.localScale = escalaSalto; // Stretch (estirar)
        }
    }
}
