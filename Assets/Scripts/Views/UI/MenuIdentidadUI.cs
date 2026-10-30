using System.Threading.Tasks;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum ModoIdentidad { Anonimo, AnonimoConAlias, CuentaVinculada }

// Identidad ANTES de jugar: anónimo, solo alias, registro o login desde el menú principal.
// NO usa GameController: eso garantiza que el menú jamás abra sesiones de telemetría.
public class MenuIdentidadUI : MonoBehaviour
{

    [Header("Paneles")]
    public GameObject panelIdentidad; // raíz (se abre/cierra)
    public GameObject menuOpciones;
    public GameObject formAlias;     // 1 input
    public GameObject formRegistro;  // 3 inputs
    public GameObject formLogin;     // 2 inputs

    [Header("Campos")]
    public TMP_InputField inputAlias;         // Solo alias
    public TMP_InputField inputAliasRegistro; // Registro (se prellena si ya hay alias)
    public TMP_InputField inputCorreo;
    public TMP_InputField inputPassword;      // Registro
    public TMP_InputField inputUsuario;       // Login: alias O correo
    public TMP_InputField inputPasswordLogin; // Login

    [Header("Texto")]
    public TMP_Text textoEstado;

    private SaveService save;
    private AuthManager auth;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        save = FindAnyObjectByType<SaveService>();
        auth = FindAnyObjectByType<AuthManager>();
    }

    // ---- Apertura / cierre ----

    public void AbrirPanel()
    {
        if (panelIdentidad != null) panelIdentidad.SetActive(true);
        MostrarOpciones(); // siempre arranca en la vista principal
    }

    public void CerrarPanel()
    {
        if (panelIdentidad != null) panelIdentidad.SetActive(false);
    }

    public void JugarAnonimo() //Atajo: no configura nada y entra al juego
    {
        CerrarPanel();
        SceneManager.LoadScene("SampleScene");
    }


    // ---- Vistas ----
    public void MostrarOpciones()
    {
        CambiarVista(menuOpciones);
        RefrescarEstado();
    }

    public void MostrarAlias()
    {
        CambiarVista(formAlias);
        inputAlias.text = AliasActual(); // vacío si es anónimo
        if (textoEstado != null) textoEstado.text = "Escribe un alias (3 a 15 caracteres) para jugar con nombre.";
    }

    public void MostrarRegistro()
    {
        CambiarVista(formRegistro);
        inputAliasRegistro.text = AliasActual(); // prellenado SOLO si ya puso alias
        if (textoEstado != null) textoEstado.text = "Crea una cuenta (correo + contraseña) para recuperar tu progreso en otra PC.";
    }

    public void MostrarLogin()
    {
        CambiarVista(formLogin);
        if (textoEstado != null) textoEstado.text = "Inicia sesión con tu alias (o correo) y tu contraseña.";
    }

    private void CambiarVista(GameObject vista)
    {
        if (menuOpciones) menuOpciones.SetActive(vista == menuOpciones);
        if (formAlias) formAlias.SetActive(vista == formAlias);
        if (formRegistro) formRegistro.SetActive(vista == formRegistro);
        if (formLogin) formLogin.SetActive(vista == formLogin);
    }

    // ---- Estado actual ----
    public void RefrescarEstado()
    {
        if (textoEstado == null || save == null) return;

        PlayerData d = save.LoadDataLocal();

        switch (ObtenerModo(d))
        {
            case ModoIdentidad.CuentaVinculada:
                textoEstado.text = $"Cuenta vinculada ✓  «{d.playerName}» <{d.email}>"; break;
            case ModoIdentidad.AnonimoConAlias:
                textoEstado.text = $"ANÓNIMO con alias «{d.playerName}» (solo en este dispositivo)."; break;
            default:
                textoEstado.text = "ANÓNIMO (sin cuenta ni alias)."; break;
        }
    }

    private string AliasActual()
    {
        if (save == null) return "";
        PlayerData d = save.LoadDataLocal();
        if (d == null || string.IsNullOrEmpty(d.playerName) || d.playerName == "Anonimo") return "";
        return d.playerName;
    }

    // ---- 1) SOLO ALIAS (1 input) ----
    public void GuardarSoloAlias()
    {
        if (save == null || inputAlias == null) return;
        string alias = inputAlias.text.Trim();

        if (alias.Length < 3 || alias.Length > 15)
        {
            if (textoEstado) textoEstado.text = "Alias inválido: debe tener de 3 a 15 caracteres.";
            return;
        }
        if (alias.Contains("@"))   // NUEVO: @ reservado para correos
        {
            if (textoEstado) textoEstado.text = "El alias no puede contener «@» (eso es para correos).";
            return;
        }

        PlayerData d = save.LoadDataLocal();
        d.playerName = alias;
        d.isPendingSync = true; // la nube lo recibirá en la primera sincronización
        save.SaveDataLocal(d);

        if (auth != null) _ = auth.SetUserAlias(alias); // best-effort si ya hay sesión Firebase

        EtiquetaModo.RefrescarActual();
        PopUpUI.Mostrar($"Alias «{alias}» guardado ✓\nCierra el panel y pulsa JUGAR.", true);
    }

    // ---- 2) REGISTRAR (3 inputs) ----
    public async void CrearCuenta()
    {
        if (save == null || auth == null) { PopUpUI.Mostrar("Error: faltan referencias en la escena.", false); return; }

        string alias = inputAliasRegistro.text.Trim();
        string correo = inputCorreo.text.Trim();
        string pass = inputPassword.text;

        if (alias.Length < 3 || alias.Length > 15 || alias.Contains("@")) { PopUpUI.Mostrar("Alias inválido:\n3 a 15 caracteres y sin «@».", false); return; }
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(pass)) { PopUpUI.Mostrar("Ingresa correo y contraseña.", false); return; }
        if (!correo.Contains("@") || !correo.Contains(".")) { PopUpUI.Mostrar(MensajeError("EMAIL_INVALIDO"), false); return; }
        if (Application.internetReachability == NetworkReachability.NotReachable) { PopUpUI.Mostrar(MensajeError("SIN_RED"), false); return; }

        if (textoEstado) textoEstado.text = "Vinculando cuenta...";
        if (!await EsperarAuthAsync()) { PopUpUI.Mostrar("Firebase no está listo todavía. Intenta de nuevo en unos segundos.", false); return; }

        bool ok = await auth.LinkAccount(correo, pass, alias);
        if (!ok) { PopUpUI.Mostrar(MensajeError(auth.UltimoErrorAuth), false); return; }

        // Offline-first: la identidad también se guarda LOCAL (la nube la confirmará al jugar/terminar)
        PlayerData d = save.LoadDataLocal();
        d.playerName = alias;
        d.email = correo;
        d.isPendingSync = true;
        save.SaveDataLocal(d);

        EtiquetaModo.RefrescarActual();                 // NUEVO: actualiza la label del menú
        CambiarVista(menuOpciones);      // deja a la vista el botón CERRAR
        
        PopUpUI.Mostrar($"Cuenta creada ✓\n«{alias}» <{correo}>\nCierra el panel y pulsa JUGAR.", true);
    }

    // ---- 3) INICIAR SESIÓN (2 inputs: alias-o-correo + contraseña) ----
    public async void IniciarSesion()
    {
        if (auth == null) { PopUpUI.Mostrar("Error: faltan referencias en la escena.", false); return; }

        string usuario = inputUsuario.text.Trim();
        string pass = inputPasswordLogin.text;

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(pass)) { PopUpUI.Mostrar("Ingresa tu alias (o correo) y tu contraseña.", false); return; }
        if (Application.internetReachability == NetworkReachability.NotReachable) { PopUpUI.Mostrar("Sin conexión: iniciar sesión requiere internet.", false); return; }

        if (textoEstado) textoEstado.text = "Iniciando sesión...";
        if (!await EsperarAuthAsync()) { PopUpUI.Mostrar("Firebase no está listo todavía. Intenta de nuevo.", false); return; }

        bool ok = await auth.Login(usuario, pass);
        if (!ok) { PopUpUI.Mostrar(MensajeError(auth.UltimoErrorAuth), false); return; }

        // Si entró con correo, reflejarlo localmente aunque la nube aún no tenga documento
        if (usuario.Contains("@"))
        {
            PlayerData d = save.LoadDataLocal();
            d.email = usuario;
            save.SaveDataLocal(d);
        }

        EtiquetaModo.RefrescarActual();
        PopUpUI.Mostrar("Sesión iniciada ✓\nCierra el panel y pulsa JUGAR: se descargará tu progreso.", true);
    }

    // Espera a que Firebase Auth inicialice (+ sesión anónima), máx ~5 s
    private async Task<bool> EsperarAuthAsync()
    {
        for (int i = 0; i < 50; i++)
        {
            if (auth != null && !string.IsNullOrEmpty(auth.UserIdActual)) return true;
            await Task.Delay(100);
        }
        return false;
    }

    // REGLA ÚNICA DE IDENTIDAD (misma que usa la tesis: 3 estados escalonados)
    public static ModoIdentidad ObtenerModo(PlayerData d)
    {
        if (d == null) return ModoIdentidad.Anonimo;
        if (!string.IsNullOrEmpty(d.email)) return ModoIdentidad.CuentaVinculada;
        if (!string.IsNullOrEmpty(d.playerName) && d.playerName != "Anonimo")
            return ModoIdentidad.AnonimoConAlias;
        return ModoIdentidad.Anonimo;
    }



    private string MensajeError(string codigo)
    {
        switch (codigo)
        {
            case "EMAIL_EN_USO": return "Ese correo ya está en uso.\nPrueba con INICIAR SESIÓN.";
            case "EMAIL_INVALIDO": return "El correo no es válido.\nEjemplo: alumno@utp.edu.pe";
            case "CONTRASENA_DEBIL": return "La contraseña es muy corta.\nUsa al menos 6 caracteres.";
            case "SIN_RED": return "Sin conexión a internet.\nEl registro solo funciona en línea.";
            case "DEMASIADOS_INTENTOS": return "Demasiados intentos.\nEspera un minuto y vuelve a intentar.";
            case "CONTRASENA_INCORRECTA": return "Contraseña incorrecta.\nInténtalo otra vez.";
            case "USUARIO_NO_EXISTE": return "No existe una cuenta con esos datos.";
            case "ALIAS_NO_ENCONTRADO": return "Ese alias no tiene cuenta asociada.\nSi es tu primera vez, usa REGISTRAR.";
            default: return "No se pudo completar la operación.\nInténtalo de nuevo.";
            
        }
    }
}
