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
        controls = new PlayerControls();
        //Escuchamos el evento de Interaccion (Tecla E)
        controls.Player.Hide.performed += ctx => IntentarEsconderse();
    }

    void OnEnable() { controls.Player.Enable(); }
    void OnDisable() { controls.Player.Disable(); }

    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
        
    }

    private void IntentarEsconderse()
    {
        if (jugadorCerca && jugadorRef != null)
        {
            // REGLA: Si intentas esconderte mientras te persigue de cerca, te mata.
            EnemigoIA enemigo = FindAnyObjectByType<EnemigoIA>();
            if (enemigo != null && !estaEscondido)
            {
                float distanciaAlEnemigo = Vector3.Distance(transform.position, enemigo.transform.position);

                if (distanciaAlEnemigo < 5.0f)
                {
                    Debug.Log("[Escondite] ¡Te escondiste en su cara! El enemigo te atrapó.");
                    if (gameController != null) gameController.IniciarMuerte();
                    return; // Abortamos la acción de esconderse
                }
            }

            estaEscondido = !estaEscondido;

            //Bloquear / Desbloquear el movimiento del jugador
            PlayerMovement movimiento = jugadorRef.GetComponent<PlayerMovement>();
            if (movimiento != null)
            {
                movimiento.PermitirControles(!estaEscondido);
            }

            //Avisar a la IA global si somos invisibles
            if (gameController != null) gameController.jugadorEscondido = estaEscondido;

            if (estaEscondido)
            {
                Debug.Log($"[Escondite] Jugador escondido en {etiquetaEscondite}. Controles Bloqueados");
                if ( gameController != null)
                {
                    gameController.RegistrarUsoDeEscondite(etiquetaEscondite);
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

}
