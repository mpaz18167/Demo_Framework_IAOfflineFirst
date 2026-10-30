using UnityEngine;
using System.Collections;

public class EnemigoBasico : MonoBehaviour, IDamage
{

    public enum TipoVulnerabilidad { VulnerableATodo, ImnuneAtaque, InmunePisoton }
    public TipoVulnerabilidad vulnerabilidad = TipoVulnerabilidad.VulnerableATodo;

    // Máquina de estados estilo Crash Bandicoot
    private enum Estado { Patrulla, Persecucion, Carga, Aturdido, Regreso }
    private Estado estado = Estado.Patrulla;
    private AnimacionEnemigoVisual anim;

    public enum EjeDePatrulla { EjeX, EjeZ }

    [Header("Patrullaje Automático (Sin Puntos)")]
    [Tooltip("Eje (local del enemigo) por el que se desplaza va y viene")]
    public EjeDePatrulla ejeDePatrulla = EjeDePatrulla.EjeX;
    public float distanciaPatrulla = 3f;
    public float velocidadBase = 2f;

    [Header("Agresividad")]
    public bool persigueAlJugador = true;
    public float radioDeteccion = 4.5f;
    public float velocidadPersecucion = 3.5f;
    [Tooltip("Zona máxima desde su origen: si el jugador la cruza, vuelve a su ruta")]
    public float distanciaMaximaDePersecucion = 10f;
    [Tooltip("Distancia a la que deja de acercarse (el daño lo hace el trigger)")]
    public float distanciaDeContacto = 1.4f;
    public bool verificarLineaDeVista = true;

    [Header("Carga estilo Crash (carga recta → se aturde → vuelve)")]
    public float distanciaInicioCarga = 4.5f;
    public float velocidadCarga = 7f;
    [Range(0.2f, 2f)] public float duracionCarga = 0.7f;
    [Range(0.2f, 3f)] public float tiempoAturdido = 1.2f;

    [Header("Regulación DDA (velocidad de caza + campo de visión)")]
    public bool reguladaPorDDA = true;
    [Range(0.5f, 1f)] public float factorMinimoDDA = 0.7f;
    [Range(1f, 2f)] public float factorMaximoDDA = 1.4f;

    private float factorDDA = 1f;

    private Vector3 posicionInicial;
    private Quaternion orientacionInicial;
    private float alturaPivotSobreSuelo = 0.5f;
    private bool posicionInicialConfigurada = false;

    private float temporizador;
    private float tiempoEnEstado;
    private Vector3 direccionDeCarga;

    private Transform jugador;
    private PlayerMovement playerMov;
    private GameController gameController;
    private bool estaMuerto = false;

    void Awake()
    {
        gameController = FindAnyObjectByType<GameController>();

        anim = GetComponentInChildren<AnimacionEnemigoVisual>();

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
        {
            jugador = objJugador.transform;
            playerMov = objJugador.GetComponent<PlayerMovement>();
        }

        if (gameController != null)
            factorDDA = Mathf.Clamp(gameController.ObtenerFactorDificultad(), factorMinimoDDA, factorMaximoDDA);
    }

    void Start()
    {
        if (!posicionInicialConfigurada)
        {
            posicionInicial = transform.position;
            orientacionInicial = transform.rotation;
            posicionInicialConfigurada = true;
        }
        CalcularAlturaSobreSuelo();
    }


    // NUEVO: Cuando el GameController revive al enemigo tras una muerte del jugador
    void OnEnable()
    {
        estaMuerto = false;
        temporizador = 0f;
        tiempoEnEstado = 0f;
        estado = Estado.Patrulla;

        if (posicionInicialConfigurada)
        {
            transform.position = posicionInicial;   // vuelve a su ruta tras el respawn del jugador
            transform.rotation = orientacionInicial;
        }

        GameController.OnDificultadCambiada += AplicarFactorDDA; // DDA regula velocidad y visión
    }

    void OnDisable()
    {
        GameController.OnDificultadCambiada -= AplicarFactorDDA;
    }

    private void AplicarFactorDDA(float factor)
    {
        if (!reguladaPorDDA) return;
        factorDDA = Mathf.Clamp(factor, factorMinimoDDA, factorMaximoDDA);
    }

    void Update()
    {
        if (estaMuerto || jugador == null) return;

        tiempoEnEstado += Time.deltaTime;

        bool jugadorNoDisponible = gameController != null &&
            (gameController.jugadorEscondido || gameController.estaEnSecuenciaDeMuerte);

        float distAJugador = Vector3.Distance(transform.position, jugador.position);

        switch (estado)
        {
            case Estado.Patrulla:
                Patrullar();
                if (persigueAlJugador && !jugadorNoDisponible &&
                    distAJugador <= RadioDeteccionEfectivo() && HayLineaDeVista())
                {
                    CambiarEstado(Estado.Persecucion);
                }
                break;

            case Estado.Persecucion:
                // Si se esconde: pierde el rastro y vuelve a su ruta (NUNCA va al escondite)
                if (jugadorNoDisponible) { CambiarEstado(Estado.Regreso); break; }

                // Límite de persecución: "hasta cierto punto"
                if (Vector3.Distance(transform.position, posicionInicial) > distanciaMaximaDePersecucion)
                { CambiarEstado(Estado.Regreso); break; }

                // Pierde el interés si se aleja demasiado
                if (distAJugador > RadioDeteccionEfectivo() * 1.5f)
                { CambiarEstado(Estado.Regreso); break; }

                if (distAJugador > distanciaDeContacto)
                {
                    transform.position = Vector3.MoveTowards(transform.position,
                        ProyectarAlSuelo(jugador.position), VelocidadPersecucionEfectiva() * Time.deltaTime);
                    MirarA(jugador.position);
                }

                if (distAJugador <= distanciaInicioCarga && HayLineaDeVista())
                {
                    direccionDeCarga = DireccionPlanaHacia(jugador.position);
                    CambiarEstado(Estado.Carga);
                }
                break;

            case Estado.Carga: // Carga recta estilo Crash
                transform.position = ProyectarAlSuelo(
                    transform.position + direccionDeCarga * velocidadCarga * Time.deltaTime);

                if (Vector3.Distance(transform.position, jugador.position) <= distanciaDeContacto)
                    CastigarJugador(); // el trigger también lo hace al solaparse

                if (tiempoEnEstado >= duracionCarga) CambiarEstado(Estado.Aturdido);
                break;

            case Estado.Aturdido: // Ventana para que el jugador ataque/pise sin represalias
                if (tiempoEnEstado >= tiempoAturdido) CambiarEstado(Estado.Regreso);
                break;

            case Estado.Regreso:
                transform.position = Vector3.MoveTowards(transform.position,
                    ProyectarAlSuelo(posicionInicial), velocidadBase * Time.deltaTime);
                MirarA(posicionInicial);

                if (Vector3.Distance(transform.position, posicionInicial) <= 0.2f)
                {
                    transform.rotation = orientacionInicial;
                    temporizador = 0f;
                    CambiarEstado(Estado.Patrulla);
                }
                break;
        }
    }

    private void CambiarEstado(Estado nuevo)
    {
        estado = nuevo;
        tiempoEnEstado = 0f;
    }

    private void Patrullar()
    {
        temporizador += Time.deltaTime * velocidadBase;
        float offset = Mathf.Sin(temporizador) * distanciaPatrulla;

        Vector3 ejeLocal = (ejeDePatrulla == EjeDePatrulla.EjeX) ? Vector3.right : Vector3.forward;
        Vector3 ejePatrulla = orientacionInicial * ejeLocal; // eje FIJO relativo a su rotación inicial: no se corrompe tras perseguir

        transform.position = ProyectarAlSuelo(posicionInicial + ejePatrulla * offset);

        float dir = Mathf.Cos(temporizador);
        if (dir > 0.1f) MirarA(transform.position + ejePatrulla);
        else if (dir < -0.1f) MirarA(transform.position - ejePatrulla);
    }

    // ====== SUELO: proyecta SIEMPRE la altura real del terreno (adiós vuelos) ======
    private void CalcularAlturaSobreSuelo()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down,
                out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            alturaPivotSobreSuelo = transform.position.y - hit.point.y;
        else
            alturaPivotSobreSuelo = 0.5f;
    }

    private Vector3 ProyectarAlSuelo(Vector3 pos)
    {
        if (Physics.Raycast(pos + Vector3.up * 2f, Vector3.down,
                out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            pos.y = hit.point.y + alturaPivotSobreSuelo;
        return pos;
    }

    private bool HayLineaDeVista()
    {
        if (!verificarLineaDeVista) return true;
        Vector3 origen = transform.position + Vector3.up;
        Vector3 destino = jugador.position + Vector3.up;
        if (Physics.Linecast(origen, destino, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore))
            return hit.transform.root == jugador.root; // golpear al jugador = vista libre; muro = bloqueado
        return true;
    }

    private float RadioDeteccionEfectivo()
    {
        return radioDeteccion * (reguladaPorDDA ? factorDDA : 1f);
    }

    private float VelocidadPersecucionEfectiva()
    {
        return velocidadPersecucion * (reguladaPorDDA ? factorDDA : 1f);
    }

    private Vector3 DireccionPlanaHacia(Vector3 punto)
    {
        Vector3 dir = punto - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
    }

    private void MirarA(Vector3 punto)
    {
        Vector3 dir = punto - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    // ====== DAÑO POR TRIGGER (sin empujes físicos) ======
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) CastigarJugador();
    }

    public void RecibirDaño(bool esAtaque, bool esPisoton, Vector3 puntoContacto)
    {
        if (esAtaque && vulnerabilidad != TipoVulnerabilidad.ImnuneAtaque) Morir();
        else if (esPisoton && vulnerabilidad != TipoVulnerabilidad.InmunePisoton) Morir();
        else CastigarJugador();
    }

    private void CastigarJugador()
    {
        if (anim != null) anim.Atacar();
        if (playerMov == null && jugador != null) playerMov = jugador.GetComponent<PlayerMovement>();
        if (playerMov == null) return;

        if (playerMov.nivelMascara == 3)
        {
            Morir(); // Máscara invencible: contacto lo destruye (diseño, estilo Aku Aku)
            return;
        }

        // Si el jugador nos está cayendo encima, NO castigamos aquí:
        // el pisotón se resuelve en PlayerMovement.OnControllerColliderHit
        // (rebote + RecibirDaño esPisoton). Evita daño doble/race al aterrizar.
        bool cayendoDesdeArriba = playerMov.EstaCayendo && jugador.position.y > transform.position.y + 0.3f;
        if (cayendoDesdeArriba) return;

        playerMov.RecibirDaño(false, false, transform.position);
    }

    private void Morir()
    {
        if (estaMuerto) return;
        estaMuerto = true;

        if (anim != null) anim.Morir(); // NUEVO: dispara Death_A (la lógica ya esta "muerta": Update hace return)

        StartCoroutine(MorirConRetraso());
    }

    private IEnumerator MorirConRetraso() // NUEVO: deja terminar la anim antes de registrar/apagar
    {
        yield return new WaitForSeconds(0.8f); // duracion aproximada de Death_A
        if (gameController != null)
            gameController.RegistrarObjetoRecolectado(gameObject);
        else
            Destroy(gameObject);
    }

    // ====== AYUDA VISUAL EN EL EDITOR: selecciona el enemigo para ver sus rangos ======
    private void OnDrawGizmosSelected()
    {
        float factor = (Application.isPlaying && reguladaPorDDA) ? factorDDA : 1f;
        Vector3 centro = posicionInicialConfigurada ? posicionInicial : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion * factor);        // campo de visión (DDA)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centro, distanciaMaximaDePersecucion);               // zona máxima de persecución
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaInicioCarga);           // distancia de carga
    }
}
