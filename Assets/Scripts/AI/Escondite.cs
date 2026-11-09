using UnityEngine;

public class Escondite : MonoBehaviour
{
    [Header("Configuración del Escondite")]
    public string etiquetaEscondite = "Casillero"; //puede ser "MESA","SOMBRA,etc.

    private bool jugadorCerca = false;
    private bool estaEscondido = false;
    private GameController gameController;
    private PlayerControls controls;

    private GameObject jugadorRef; //Referencia directa al jugador para congelarlo


    void Awake()
    {
        InicializarControles();
    }
    private void InicializarControles()
    {
        if (controls != null) return;
        controls = new PlayerControls();
        controls.Player.Hide.performed += ctx => IntentarEsconderse();
    }

    void OnEnable()
    {
        InicializarControles();
        controls?.Player.Enable();
        GameController.OnReconfiguracionEntorno += ResetearEscondite;
    }
    void OnDisable()
    {
        controls?.Player.Disable();
        GameController.OnReconfiguracionEntorno -= ResetearEscondite;
    }

    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
        
    }

    private void ResetearEscondite()
    {
        estaEscondido = false;   // si el jugador murió escondido, el casillero se limpia
        jugadorCerca = false;
        jugadorRef = null;
    }

    private void IntentarEsconderse()
    {
        if (jugadorCerca && jugadorRef != null)
        {
            // Si intentas esconderte mientras te persigue de cerca, te mata.
            EnemigoIA enemigo = FindAnyObjectByType<EnemigoIA>();

            // REGLA JUSTA: Solo te prohíbe esconderte si el enemigo está PEGADO a ti (< 2m) y viéndote de frente
            if (enemigo != null && !estaEscondido)
            {
                float distanciaAlEnemigo = Vector3.Distance(transform.position, enemigo.transform.position);
                if (distanciaAlEnemigo < 2.2f && enemigo.PuedeVerAlJugador(gameController))
                {
                    Debug.Log("[Escondite] ¡El enemigo está pegado a ti! Te atrapó en la puerta.");
                    if (gameController != null) gameController.IniciarMuerte();
                    return;
                }
            }

            estaEscondido = !estaEscondido;

            //Bloquear / Desbloquear el movimiento del jugador
            PlayerMovement movimiento = jugadorRef.GetComponent<PlayerMovement>();

            if (movimiento != null) movimiento.PermitirControles(!estaEscondido);

            //Avisar a la IA global si somos invisibles
            if (gameController != null) gameController.jugadorEscondido = estaEscondido;

            if (estaEscondido)
            {
                if (gameController != null) gameController.RegistrarUsoDeEscondite(etiquetaEscondite);

                // HIDE & SEEK DINÁMICO (Regulado por DDA):

                if (enemigo != null)
                {
                    float factorDDA = gameController != null ? gameController.ObtenerFactorDificultad() : 1.0f;

                    float radioEfectivo = 5.0f * factorDDA; // 4m en frustración, 7.5m en aburrimiento

                    float dist = Vector3.Distance(enemigo.transform.position, transform.position);

                    if (dist <= radioEfectivo)
                    {
                        Vector3 ojo = enemigo.transform.position + Vector3.up * enemigo.alturaOjosJefe;
                        Vector3 pechoJugador = jugadorRef.transform.position + Vector3.up * 0.6f;
                        // Solo se da cuenta si hay línea de visión directa (sin paredes en medio)
                        if (!Physics.Linecast(ojo, pechoJugador, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore) ||
                            hit.collider.CompareTag("Player") || hit.transform == this.transform)
                        {
                            enemigo.ConocerEsconditeVisto(transform);
                        }
                    }
                }
            }
            else
            {
                Debug.Log("[Escondite] Jugador Salio del escondite. Controles reactivos");
            }
        }
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            jugadorRef=other.gameObject; //Guardamos al jugador
        }
    }
        

    private void OnTriggerExit(Collider other) 
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            if (!estaEscondido) jugadorRef = null;
        }
    }


    public bool JugadorEstaAqui() { return estaEscondido; }

    public void ForzarSalida()
    {
        if (estaEscondido)
        {
            estaEscondido = false;
            if (gameController != null) gameController.jugadorEscondido = false;
            if (jugadorRef != null)
            {
                PlayerMovement mov = jugadorRef.GetComponent<PlayerMovement>();
                // NUEVO: si hay secuencia de muerte activa, la salida forzada NO toca los controles
                if (mov != null && (gameController == null || !gameController.estaEnSecuenciaDeMuerte))
                    mov.PermitirControles(true);
            }
            Debug.Log("[Escondite] ¡El enemigo te sacó a la fuerza del escondite!");
        }
    }


}
