using System.Collections;
using UnityEngine;

//USANDO MVC: Este script es parte de la vista (View) y se encarga de manejar el movimiento del jugador en el juego.
public class PlayerMovement : MonoBehaviour, IDamage
{

    private float distanciaAcumulada = 0f;


    [Header("Ajustes de Movimiento")]
    public float velocidadBase = 6f; // Velocidad base del jugador
    public float alturaSalto = 1.5f; // Altura del salto del jugador
    public float gravedad = -15f; // Gravedad aplicada al jugador un poco mas alta para que elsalto no se sienta tan flotante

    [Header("Ajustes de Combate")]
    public float radioAtaque = 1.2f; // Radio de ataque del jugador
    public float duracionAtaque = 0.35f; // Duración del ataque del jugador
    public float fuerzaRebotePisoton = 9f; // Fuerza de rebote al pisotón del jugador
    public Transform centroAtaque; // Transform que representa el centro del ataque del jugador
    private bool estaAtacando = false; // Booleano para verificar si el jugador está atacando
    public bool esInvulnerable = false; // Booleano para verificar si el jugador es invulnerable Manejado por la caja Mascara

    [Header("Habilidades Tácticas")]
    public GameObject distractorPrefab; // Arrastra aquí tu prefab del distractor
    public Transform puntoDeLanzamiento; // Un objeto vacío hijo del jugador (frente a él)
    public float cooldownDistractor = 3f; // Tiempo de espera entre usos
    private float tiempoUltimoDistractor = -5f;

    [Header("Flash teledirigido (potenciador, no acumulable)")]
    public GameObject flashPrefab;   // proyectil home — se crea en Unity (abajo)
    public bool tieneFlash = false;  // solo 1: si no lo tiras, no puedes agarrar otro

    [Header("Drop de emergencia (jefe muy cerca)")]
    [Tooltip("Si el jefe está a esta distancia o menos, se suelta a los pies en vez de lanzarse")]
    public float radioDropEmergencia = 7f;

    private EnemigoIA jefe;
    private CharacterController characterController; // Componente CharacterController para manejar la colisión y el movimiento del jugador
    private PlayerControls controls;// Instancia de PlayerControls para manejar la entrada del jugador
    private Vector2 inputMovimiento;// Vector2 para almacenar la entrada de movimiento del jugador
    private Vector3 velocidadCaida;// Vector3 para almacenar la velocidad de caída del jugador
    private bool enElSuelo; // Booleano para verificar si el jugador está en el suelo
    private bool cabezaTecho;// Booleano para verificar si el jugador está tocando el techo
    private bool puedeMoverse = true; // Variable para controlar si el jugador puede moverse o no

    //Referencias a la capa Controller y Services para la telemetria
    private GameController gameController;
    private TelemetryService telemetryService;

    [Header("Sistema de Máscara (Estilo Crash Bandicoot)")]
    [Range(0, 3)] public int nivelMascara = 0; // 0: Sin máscara, 1: 1 golpe, 2: 2 golpes, 3: Invulnerable
    public float multiplicadorVelocidadNivel3 = 1.35f;
    public float tiempoInvencibleNivel3 = 10f;
    public float duracionParpadeoDanio = 1.0f; // Tiempo de invulnerabilidad tras perder una máscara
    private bool esInvulnerablePorDanio = false;
    private Coroutine rutinaNivel3;

    // Control EXACTO del boost nivel 3 (sin apilar, sin dejarlo pegado al morir)
    private bool boostNivel3Activo = false;
    private RotacionVisual rotacionVisual;
    private float velocidadSinBoost = 0f;
    private float finInvulnNivel3 = 0f;
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        InicializarControles();

    }
    private void InicializarControles()
    {
        if (controls != null) return;
        controls = new PlayerControls();
        controls.Player.Move.performed += ctx => inputMovimiento = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => inputMovimiento = Vector2.zero;
        controls.Player.Jump.performed += ctx => Saltar();
        controls.Player.DropDistractor.performed += ctx => LanzarDistractor();
        controls.Player.Attack.performed += ctx => EjecutarAtaque();
    }

    void OnEnable()
    {
        InicializarControles();
        controls?.Player.Enable();
    }

    void OnDisable()
    {
        controls?.Player.Disable();
    }

    void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
        telemetryService = FindAnyObjectByType<TelemetryService>();
        rotacionVisual = FindAnyObjectByType<RotacionVisual>();
        jefe = FindAnyObjectByType<EnemigoIA>();
       
    }

    // Update is called once per frame
    void Update()
    {
        if (!puedeMoverse) return; // Si el jugador no puede moverse, salir del método Update

        //Chequeo de estados (antes de calcular movimiento)
        enElSuelo = characterController.isGrounded;
        cabezaTecho = (characterController.collisionFlags & CollisionFlags.Above) != 0;

        //Logica de gravedad y Limites
        if (enElSuelo && velocidadCaida.y < 0)
        {
            velocidadCaida.y = -2f; //// Botas magnéticas para no despegarse de las rampas o el suelo
        }
        if (cabezaTecho && velocidadCaida.y > 0)
        {
            velocidadCaida.y = -2f; //Rebote al tocar el techo
        }

        //acumular gravedad normal
        velocidadCaida.y += gravedad * Time.deltaTime;

        //Calcular movimiento Horizontal
        Vector3 movimientoHorizontal = transform.right * inputMovimiento.x + transform.forward * inputMovimiento.y;
        movimientoHorizontal *= velocidadBase;

        //Sumamos el movimiento WASD y la gravedad antes de darcelo a unity
        Vector3 MovimientoFinal = movimientoHorizontal + velocidadCaida;

        characterController.Move(MovimientoFinal * Time.deltaTime);

        // =========================================================
        // NUEVO: PODÓMETRO PARA LA MATRIZ DE TELEMETRÍA
        // =========================================================

        if (enElSuelo && inputMovimiento.magnitude > 0)
        {
            //Calcular cuanta distancia fisica se movio en este frame
            float distanciaFrame = movimientoHorizontal.magnitude * Time.deltaTime;
            distanciaAcumulada += distanciaFrame;

            //Por cada 1 unidad metrica de Unity (aporx 1 metro), sumamos +1 a "M"
            if (distanciaAcumulada >= 1f)
            {
                if (telemetryService != null) telemetryService.RegistrarMovimientoM();
                distanciaAcumulada = 0f; // Reiniciamos el acumulador para el siguiente metro
            }
        }
    }



    private void Saltar()
    {
        if (!puedeMoverse) return;
        if (enElSuelo)
        {
            velocidadCaida.y = Mathf.Sqrt(alturaSalto * -2f * gravedad); // Calcular la velocidad de salto usando la fórmula de física
            SaltoComicVisual comic = GetComponent<SaltoComicVisual>();
            if (comic != null) comic.GatillarEstiramientoSalto(); // Gatillar el estiramiento visual del salto
            if (telemetryService != null) telemetryService.RegistrarAccionJugador();
        }
    }

    private void EjecutarAtaque()
    {
        if (!puedeMoverse || estaAtacando) return;
        StartCoroutine(RutinaGiro());
    }

    private IEnumerator RutinaGiro()
    {
        estaAtacando = true;
        if (telemetryService != null) telemetryService.RegistrarAccionJugador();

        float tiempoTranscurrido = 0f;
        Vector3 punto = centroAtaque != null ? centroAtaque.position : transform.position; // Usar el centro del ataque si está definido, de lo contrario usar la posición del jugador

        while (tiempoTranscurrido < duracionAtaque)
        {
            Collider[] impactados = Physics.OverlapSphere(punto, radioAtaque);
            foreach (Collider col in impactados)
            {
                if (col == null) continue; // NUEVO: objetivo destruido en este mismo bucle → ignorarlo (evita excepción que traba estaAtacando)
                if (col.gameObject != this.gameObject)
                {
                    IDamage objetivo = col.GetComponent<IDamage>();
                    if (objetivo != null)
                    {
                        objetivo.RecibirDaño(true, false, transform.position); // Llamar al método RecibirDaño del objetivo
                    }
                }
            }

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        estaAtacando = false;
    }

    // Acción de ataque (Giro estilo Crash)
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {

        // =========================================================
        // 1. MÁSCARA NIVEL 3 (EFECTO ESTRELLA / TRACTOR)
        // =========================================================
        if (nivelMascara == 3)
        {
            // Matar enemigos básicos al tocarlos (El Jefe es inmortal porque no tiene IDamage)
            IDamage enemigo = hit.gameObject.GetComponent<IDamage>();
            if (enemigo != null)
            {
                enemigo.RecibirDaño(true, false, hit.point);
            }

            // Romper cajas al tocarlas (Asegúrate de que tus cajas tengan el tag "Caja")
            if (hit.gameObject.CompareTag("Caja"))
            {
                hit.gameObject.SendMessage("RomperCaja", true, SendMessageOptions.DontRequireReceiver);
            }
        }

        // 2. Pisotón (Cayendo sobre la caja o enemigo)
        else if (velocidadCaida.y < -0.1f && hit.normal.y > 0.3f || transform.position.y > hit.collider.bounds.center.y)
        {
            IDamage objetivo = hit.collider.GetComponent<IDamage>();
            if (objetivo != null)
            {
                velocidadCaida.y = fuerzaRebotePisoton;
                objetivo.RecibirDaño(false, true, hit.point);
                
                if (telemetryService != null) telemetryService.RegistrarAccionJugador();
            }
        }

        // 3. Cabezazo desde abajo (Subiendo y chocando contra el techo de una caja)
        else if (velocidadCaida.y > 0.1f && hit.normal.y < -0.6f)
        {
            IDamage objetivo = hit.collider.GetComponent<IDamage>();
            if (objetivo != null)
            {
                //un cabezazo no hace daño (no es giro ni pisoton), pero puede activar un evento en la caja (como abrirla o rebotar)
                objetivo.RecibirDaño(false, false, hit.point);
                velocidadCaida.y = -2f; // Fuerza el rebote del jugador hacia abajo
                if (telemetryService !=null) telemetryService.RegistrarAccionJugador();
            }
        }
    }

    public void ActivarMascara(float duracion)
    {
        StartCoroutine(RutinaMascara(duracion));
    }

    private IEnumerator RutinaMascara(float duracion)
    {
        esInvulnerable = true;
        Debug.Log("[PlayerMovement] Escudo de Máscara activado.");
        yield return new WaitForSeconds(duracion);
        esInvulnerable = false;
        Debug.Log("[PlayerMovement] Escudo de Máscara expirado.");
    }

    // NUEVO: expuesto para que los enemigos sepan si el jugador les está cayendo encima
    public bool EstaCayendo => !enElSuelo && velocidadCaida.y < -0.1f;

    public void PermitirControles(bool estado)
    {
        puedeMoverse = estado;
        if (!estado)
        {
            inputMovimiento = Vector2.zero; // Detener el movimiento del jugador
        }
        else
        {
            estaAtacando = false; // NUEVO: red de seguridad para que el giro nunca quede trabado tras respawn/pausa
        }
    }

    public void Teletransportar(Vector3 nuevaPosicion)
    {
        characterController.enabled = false; // Desactivar el CharacterController para evitar colisiones durante la teletransportación
        transform.position = nuevaPosicion; // Cambiar la posición del jugador a la nueva posición

        velocidadCaida = Vector3.zero; //reiniciar la gravedad acumulada para no caer como meteorito al reaparecer.

        characterController.enabled = true; // Reactivar el CharacterController después de la teletransportación
    }

    private void LanzarDistractor()
    {
        if (!puedeMoverse) return;

        if (distractorPrefab != null && puntoDeLanzamiento != null)
        {
            //Verifica el tiempo de recarga
            if (Time.time >= tiempoUltimoDistractor + cooldownDistractor)
            {
                bool dropEmergencia = jefe != null
                    && Vector3.Distance(transform.position, jefe.transform.position) <= radioDropEmergencia;

                Vector3 posicionSalida;
                Quaternion rotacionSalida;

                if (dropEmergencia)
                {
                    posicionSalida = puntoDeLanzamiento.position;
                    rotacionSalida = Quaternion.identity; // sin arco: cae y se activa a tus pies
                    Debug.Log("[PlayerMovement] ¡Jefe muy cerca! Suelto el objeto a los pies.");
                }
                else
                {
                    // Dirección = donde mira el MODELO visual (tu dirección de movimiento)
                    Vector3 direccion = characterController.transform.forward;
                    if (rotacionVisual != null && rotacionVisual.modeloVisual != null)
                        direccion = rotacionVisual.modeloVisual.forward;
                    direccion.y = 0f;
                    if (direccion.sqrMagnitude < 0.001f) direccion = characterController.transform.forward;
                    direccion.Normalize();

                    rotacionSalida = Quaternion.LookRotation(direccion)
                                   * Quaternion.Euler(0f, Random.Range(-8f, 8f), 0f);
                    posicionSalida = puntoDeLanzamiento.position + direccion * 1.2f;
                }

                // NUEVO: si llevas el FLASH TELEDIRIGIDO, este lanzamiento lo gasta (no acumulable)
                GameObject prefabLanzar = distractorPrefab;
                if (tieneFlash && flashPrefab != null)
                {
                    prefabLanzar = flashPrefab;
                    tieneFlash = false;
                    Debug.Log("[PlayerMovement] ¡Flash teledirigido lanzado!");
                }

                Instantiate(prefabLanzar, posicionSalida, rotacionSalida);
                tiempoUltimoDistractor = Time.time;

                if (telemetryService != null) telemetryService.RegistrarAccionJugador();

                Debug.Log("[PlayerMovement] Distractor lanzado con éxito.");
            }

            else
            {
                Debug.Log("[PlayerMovement] Habilidad en recarga...");
            }
        }
    }

    public bool TomarFlash()
    {
        if (tieneFlash) return false;
        tieneFlash = true;
        Debug.Log("[PlayerMovement] Flash teledirigido obtenido: tíralo con el botón del distractor.");
        return true;
    }



    public void RecolectarMascara()
    {
        if (nivelMascara < 2)
        {
            nivelMascara++;
            Debug.Log($"[PlayerMovement] Máscara obtenida. Nivel actual: {nivelMascara}");
            return;
        }

        if (nivelMascara == 2)
        {
            // Sube a 3: invulnerabilidad + boost de velocidad (UNA sola vez, sin apilar)
            nivelMascara = 3;
            Debug.Log("[PlayerMovement] ¡Nivel 3 alcanzado! Invulnerabilidad y velocidad activadas.");
            if (!boostNivel3Activo)
            {
                velocidadSinBoost = velocidadBase;
                velocidadBase *= multiplicadorVelocidadNivel3;
                boostNivel3Activo = true;
            }
            finInvulnNivel3 = Time.time + tiempoInvencibleNivel3;
        }
        else
        {
            // Ya era nivel 3: NO se vuelve más rápido; solo se alarga la invencibilidad ~1 s
            finInvulnNivel3 = Mathf.Max(finInvulnNivel3, Time.time) + 1f;
            Debug.Log("[PlayerMovement] Nivel 3 confirmado: +1 s de invencibilidad (sin boost extra).");
        }

        if (rutinaNivel3 != null) StopCoroutine(rutinaNivel3);
        rutinaNivel3 = StartCoroutine(RutinaMascaraNivel3());
    }

    private IEnumerator RutinaMascaraNivel3()
    {
        while (Time.time < finInvulnNivel3)
            yield return null;

        RestaurarVelocidadNivel3(); // devuelve la velocidad EXACTA (multiplicativo, sin acumular)
        nivelMascara = 2;
        rutinaNivel3 = null;
        Debug.Log("[PlayerMovement] Invulnerabilidad nivel 3 terminada. Máscara regresa a nivel 2.");
    }
    private void RestaurarVelocidadNivel3()
    {
        if (!boostNivel3Activo) return;
        velocidadBase = velocidadSinBoost;
        boostNivel3Activo = false;
    }
    // Implementación de daño sobre el jugador (Llamado por enemigos, TNT o Nitro)
    public void RecibirDaño(bool esAtaque, bool esPisoton, Vector3 puntoContacto)
    {
        // 1. Si el jugador realizó un pisotón exitoso, no recibe daño
        if (esPisoton) return;

        // 2. Si está en Nivel 3 (Aku Aku dorado) o en tiempo de gracia tras un impacto, ignora el daño
        if (nivelMascara == 3 || esInvulnerablePorDanio)
        {
            return;
        }

        if (nivelMascara > 0)
        {
            nivelMascara--;
            CamaraShake.Sacudir(0.25f, 0.15f);
            Debug.Log($"[PlayerMovement] Impacto absorbido por la máscara. Nivel restante: {nivelMascara}");
            StartCoroutine(RutinaInvulnerabilidadTemporal());
            // Aquí puedes agregar un parpadeo visual de invulnerabilidad corta (0.5s)
            return;
        }

        // 4. Si no tiene máscaras y no está en secuencia de muerte, ejecuta el flujo formal
        if (gameController != null && !gameController.estaEnSecuenciaDeMuerte) 
        {
            gameController.IniciarMuerte();
            CamaraShake.Sacudir(0.25f, 0.15f);
        }
    }
    private IEnumerator RutinaInvulnerabilidadTemporal()
    {
        esInvulnerablePorDanio = true;
        // Aquí puedes alternar la visibilidad de tu mesh/renderer para el parpadeo visual clásico
        yield return new WaitForSeconds(duracionParpadeoDanio);
        esInvulnerablePorDanio = false;
    }

    //Metodo para que el trampolin nos empuje hacia arriba
    public void AplicarImpulso(float fuerza)
    {
        velocidadCaida.y = fuerza;
    }

    public void ResetearMascaras()
    {
        nivelMascara = 0;
        if (rutinaNivel3 != null)
        {
            StopCoroutine(rutinaNivel3);
            rutinaNivel3 = null;
        }
        RestaurarVelocidadNivel3(); //Morir con nivel 3 no deja el speed-hack permanente
    }
}
