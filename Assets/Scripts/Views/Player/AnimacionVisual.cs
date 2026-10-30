using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
// VISUAL ONLY: alimenta el Animator del modelo KayKit con la velocidad real
// del CharacterController. Parametro "Velocidad" normalizado 0..1.
[DisallowMultipleComponent]
public class AnimacionVisual : MonoBehaviour
{

    [Header("Referencias")]
    public CharacterController controller; //Si lo dejas vacio, se busca en el padre solo.
    public Animator animator;

    [Header("Parametros del Animator")]
    public string paramVelocidad = "Velocidad";
    public string paramEnSuelo = "EnSuelo";
    public string triggerAtacar = "Atacar";
    public string triggerLanzar = "LanzarDistractor";
    public string triggerDanio = "RecibirDanio";
    public string triggerMorir = "Morir";
    public string triggerRespawn = "Respawn";

    [Header("Ajustes")]
    [Tooltip("Velocidad del PlayerMovement (velocidadBase) para normalizar 0..1")]
    public float velocidadReferencia = 6f;
    [Tooltip("Espera extra tras el fade-in de FadeUI antes del Spawn_Ground")]
    public float margenTrasFade = 0.1f;

    private PlayerMovement player;
    private GameController gameController;
    private PlayerControls controls; // instancia PROPIA: no toca la de PlayerMovement
    private bool muertoPrevio = false;
    private int mascarasPrevias;
    private Coroutine cRespawn;


    private void Reset() // Se auto-rellena al añadir el componente
    {
        controller = GetComponentInParent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (controller == null) controller = GetComponentInParent<CharacterController>();
        if (animator == null) animator = GetComponent<Animator>();
        player = GetComponentInParent<PlayerMovement>();
        gameController = FindAnyObjectByType<GameController>();

        controls = new PlayerControls();               // instancia independiente
        controls.Player.Attack.performed += _ => Atacar();
        controls.Player.DropDistractor.performed += _ => Lanzar();
    }

    private void OnEnable() { if (controls != null) controls.Player.Enable(); }
    private void OnDisable() { if (controls != null) controls.Player.Disable(); }
    private void OnDestroy() { controls?.Dispose(); }

    private void Atacar()
    {
        if (animator == null || muertoPrevio) return; // sin golpes durante la muerte
        animator.SetTrigger(triggerAtacar);
    }

    private void Lanzar()
    {
        if (animator == null || muertoPrevio) return;
        animator.SetTrigger(triggerLanzar);
    }

    private void Start()
    {
        mascarasPrevias = player != null ? player.nivelMascara : 0; // evita trigger al cargar
    }
    // Update is called once per frame
    void Update()
    {
        if (animator == null || controller == null) return;

        Vector3 v = controller.velocity;
        v.y = 0f;
        animator.SetFloat(paramVelocidad, v.magnitude / velocidadReferencia);

        // 2) MUERTE/RESPAWN: flanco sobre el flag publico de GameController
        bool muerto = gameController != null && gameController.estaEnSecuenciaDeMuerte;
        if (muerto && !muertoPrevio)
        {
            // Death_A INMEDIATO: se ve mientras tu FadeUI oscurece la pantalla
            animator.SetTrigger(triggerMorir);
            if (cRespawn != null) { StopCoroutine(cRespawn); cRespawn = null; }
        }

        if (!muerto && muertoPrevio)
        {
            // Respawn DESPUES de que tu fade-in termine (lee duracionFade de TU FadeUI)
            if (cRespawn != null) StopCoroutine(cRespawn);
            cRespawn = StartCoroutine(RespawnTrasFade());
        }

        muertoPrevio = muerto;
        if (muerto) return; // durante la secuencia no se manda nada mas

        // 3) SALTO POR PARTES: manda el estado fisico real (cubre tambien el trampolin)
        animator.SetBool(paramEnSuelo, controller.isGrounded);

        // Perdida de mascara (nivelMascara es publico) -> animacion de dano
        if (player != null)
        {
            int mascaras = player.nivelMascara;
            if (mascaras < mascarasPrevias) animator.SetTrigger(triggerDanio);
            mascarasPrevias = mascaras;
        }
    }

    private IEnumerator RespawnTrasFade()
    {
        // Tu secuencia: EjecutarRespawn() -> FundidoHaciaClaro() (duracionFade).
        // Esperamos ese fundido + margen para que Spawn_Ground se VEA.
        float espera = margenTrasFade;
        if (FadeUI.Instancia != null) espera += FadeUI.Instancia.duracionFade; // campo PUBLICO de tu FadeUI
        yield return new WaitForSeconds(espera);
        animator.SetTrigger(triggerRespawn);
    }
}
