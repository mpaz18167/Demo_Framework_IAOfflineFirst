using UnityEngine;

public class EnemigoBasico : MonoBehaviour, IDamage
{

    public enum TipoVulnerabilidad { VulnerableATodo, ImnuneAtaque, InmunePisoton }
    public TipoVulnerabilidad vulnerabilidad = TipoVulnerabilidad.VulnerableATodo;

    [Header("Patrullaje Automático (Sin Puntos)")]
    public float distanciaPatrulla = 3f;
    public float velocidadBase = 2f;
    [Header("Agresividad")]
    public bool persigueAlJugador = true;
    public float radioDeteccion = 4.5f;
    public float velocidadPersecucion = 3.5f;

    private Vector3 posicionInicial;
    private float temporizador;
    private Transform jugador;
    private GameController gameController;

    // Candado para evitar que se ejecute la muerte varias veces en un mismo frame
    private bool estaMuerto = false;

    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
        posicionInicial = transform.position; // Guardamos su posición de nacimiento

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null) jugador = objJugador.transform;
    }

    // NUEVO: Cuando el GameController revive al enemigo tras una muerte del jugador
    void OnEnable()
    {
        estaMuerto = false;
        temporizador = 0f;

        // Lo devolvemos a su lugar original para que vuelva a patrullar de forma natural
        if (posicionInicial != Vector3.zero)
        {
            transform.position = posicionInicial;
        }
    }

    void Update()
    {
        if (estaMuerto) return;

        if (persigueAlJugador && jugador != null && Vector3.Distance(transform.position, jugador.position) <= radioDeteccion)
        {
            // Abandona la patrulla y persigue
            transform.position = Vector3.MoveTowards(transform.position, jugador.position, velocidadPersecucion * Time.deltaTime);
            transform.LookAt(new Vector3(jugador.position.x, transform.position.y, jugador.position.z));
        }
        else
        {
            // Patrulla pendular natural (Eje X local)
            temporizador += Time.deltaTime * velocidadBase;
            float offsetX = Mathf.Sin(temporizador) * distanciaPatrulla;
            transform.position = posicionInicial + (transform.right * offsetX);

            float dirX = Mathf.Cos(temporizador);
            if (dirX > 0.1f) transform.forward = transform.right;
            else if (dirX < -0.1f) transform.forward = -transform.right;
        }
    }


    public void RecibirDaño(bool esAtaque, bool esPisoton, Vector3 puntoContacto)
    {
        if (esAtaque && vulnerabilidad != TipoVulnerabilidad.ImnuneAtaque) Morir();
        else if (esPisoton && vulnerabilidad != TipoVulnerabilidad.InmunePisoton) Morir();
        else CastigarJugador();
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            CastigarJugador();
        }
    }

    private void CastigarJugador()
    {
        PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
        if (player!= null)
        {
           if (player.nivelMascara == 3)
            {
                Morir();
                return;
            }
        }

        // Delega el impacto para que la máscara absorba el daño
        player.RecibirDaño(false, false, transform.position);
    }

    private void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;

        if (gameController != null)
        {
            // NUEVO: En lugar de destruirlo, lo enviamos a la "Memoria del Checkpoint"
            // El controlador lo apaga (SetActive false) y decidirá si lo revive o lo destruye luego
            gameController.RegistrarObjetoRecolectado(gameObject);
        }
        else
        {
            Destroy(gameObject); // Respaldo por si se prueba la escena sin controlador
        }
    }


}
