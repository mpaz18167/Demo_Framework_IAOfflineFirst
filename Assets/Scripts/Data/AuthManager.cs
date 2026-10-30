using Firebase;
using Firebase.Auth;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    private FirebaseAuth auth;

    // NUEVO El UserIdActual devuelve el UID del usuario autenticado, o null si no hay usuario autenticado
    public string UserIdActual => (auth != null && auth.CurrentUser != null) ? auth.CurrentUser.UserId : null; 
   
    // NUEVO: código corto del último error, para el popup de la UI
    public string UltimoErrorAuth { get; private set; } = "";

    public bool SesionActiva => auth != null && auth.CurrentUser != null;
    public bool SesionAnonima => SesionActiva && auth.CurrentUser.IsAnonymous;
    public bool CuentaRegistrada => SesionActiva && !string.IsNullOrEmpty(auth.CurrentUser.Email);

    public static event Action<string> OnUserAuthenticated;// Evento para notificar cuando un usuario se autentica exitosamente

    void OnEnable()
    {
        FirebaseInit.OnFirebaseInitialized += HandleFirebaseInitialized; // Suscribirse al evento de inicialización de Firebase
    }

    void OnDisable()
    {
        FirebaseInit.OnFirebaseInitialized -= HandleFirebaseInitialized; // Desuscribirse del evento de inicialización de Firebase
    }

    private void HandleFirebaseInitialized()
    {
        _ = InitializeAuthSync(); //El operador (_) permite correr la tarea sin bloquear
    }

    private async Task InitializeAuthSync()
    {
        try
        {
            auth = FirebaseAuth.DefaultInstance; // Obtener la instancia de autenticación de Firebase
            Debug.Log("[AuthManager] Firebase Auth inicializado.");
            
            if (auth.CurrentUser == null)
            {
                Debug.Log("[AuthManager] No hay usuario autenticado. Iniciando sesión anónima...");
                await SignInAnonymously();// Intentar iniciar sesión de forma anónima si no hay un usuario autenticado
            }

            else
            {
                string alias = string.IsNullOrEmpty(auth.CurrentUser.DisplayName) ? "Usuario" : auth.CurrentUser.DisplayName; // Obtener el alias del usuario, si no tiene, asignar "Usuario"
                Debug.Log($"[AuthManager] Usuario detectado. Alias: {alias}, UID: {auth.CurrentUser.UserId}");
                OnUserAuthenticated?.Invoke(auth.CurrentUser.UserId); // Notificar que el usuario ya está autenticado
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Error durante la inicializacioón: {ex.Message}");
        }

        
    }

    private async Task SignInAnonymously()
    {
        try
        {
            AuthResult result = await auth.SignInAnonymouslyAsync(); // Intentar iniciar sesión de forma anónima
            Debug.Log($"[AuthManager] Sesion Anonima iniciada. UID: {result.User.UserId}");
            OnUserAuthenticated?.Invoke(result.User.UserId); // Notificar que el usuario se ha autenticado exitosamente
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AuthManager] No se pudo iniciar sesión anónima: {ex.Message}. Se continúa en modo local con el ID del jugador (Offline-First).");

            // FALLBACK OFFLINE-FIRST: el juego no depende de la nube para arrancar.
            // Se usa el ID local ya generado en PlayerData para disparar el mismo flujo de PrepararPartida.
            SaveService saveService = GetComponent<SaveService>(); // Está en el mismo GameObject (GameManager)
            PlayerData datosLocales = saveService != null ? saveService.LoadDataLocal() : null;
            string uidLocal = (datosLocales != null && !string.IsNullOrEmpty(datosLocales.playerId))
                ? datosLocales.playerId
                : Guid.NewGuid().ToString();

            OnUserAuthenticated?.Invoke(uidLocal);
        }
    }

    // Método para actualizar el alias del usuario autenticado
    public async Task SetUserAlias(string newAlias)
    {
        if(auth.CurrentUser != null)
        {
            try
            {
                UserProfile profile = new UserProfile { DisplayName = newAlias }; // Crear un nuevo perfil de usuario con el alias proporcionado
                await auth.CurrentUser.UpdateUserProfileAsync(profile); // Actualizar el perfil del usuario en Firebase
                Debug.Log($"[AuthManager] Alias actualizado a: {auth.CurrentUser.DisplayName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthManager] Error al actualizar el alias: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[AuthManager] No hay usuario autenticado para actualizar el alias.");
        }
    }

    // Método para obtener el alias del usuario autenticado
    public string GetUserAlias()
    {
        if (auth.CurrentUser != null && !string.IsNullOrEmpty(auth.CurrentUser.DisplayName))
        {
            return auth.CurrentUser.DisplayName;
        }
        else
        {
            Debug.LogWarning("[AuthManager] No hay usuario autenticado para obtener el alias.");
            return "Usuario";
        }
    }
 
    public async Task<bool> LinkAccount(string email, string password, string aliasDeseado)
    {
        UltimoErrorAuth = "";
        try
        {

            if (auth == null || auth.CurrentUser == null)
            {
                Debug.LogWarning("[AuthManager] No hay usuario anónimo activo para vincular.");
                return false;
            }

            Credential credential = EmailAuthProvider.GetCredential(email, password); // Crear credenciales con el correo y contraseña proporcionados
            AuthResult result = await auth.CurrentUser.LinkWithCredentialAsync(credential); // Vincular la cuenta anónima con la cuenta de correo electrónico
            Debug.Log($"[AuthManager] Cuenta vinculada exitosamente. UID: {result.User.UserId}");

            if (!string.IsNullOrEmpty(aliasDeseado))
            {
                await SetUserAlias(aliasDeseado); // Actualizar el alias del usuario si se proporciona uno
            }
            return true;    

        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Error al vincular la cuenta: {ex.Message}");
            UltimoErrorAuth = ClasificarError(ex);   // NUEVO
            return false;
        }
    }

    public async Task<bool> Login (string inputUsuario, string password)
    {
        UltimoErrorAuth = "";

        try
        {
            string correoParaLogin = inputUsuario;
            // Si el input del usuario no contiene un "@", se asume que es un alias y se busca el correo asociado
            if (!inputUsuario.Contains("@"))
            {
                Debug.Log($"[AuthManager] Alias Detectado. Buscando correo asociado al alias: {inputUsuario} ....");
                CloudService cloudService = FindAnyObjectByType<CloudService>();
                correoParaLogin = await cloudService.BuscarCorreoPorAlias(inputUsuario); // Buscar el correo asociado al alias en la base de datos

                if (string.IsNullOrEmpty(correoParaLogin))
                {
                    UltimoErrorAuth = "ALIAS_NO_ENCONTRADO";
                    return false;
                }
            }

            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(correoParaLogin, password); // Intentar iniciar sesión con el correo y contraseña proporcionados
            Debug.Log($"[AuthManager] Inicio de sesión exitoso. UID: {result.User.UserId}, Alias: {result.User.DisplayName}");
            OnUserAuthenticated?.Invoke(result.User.UserId); // Notificar que el usuario se ha autenticado exitosamente
            return true;

        }
        catch (Exception ex)
        {
            UltimoErrorAuth = ClasificarError(ex);
            return false;
        }
    }

    // NUEVO: traduce la excepción de Firebase a un código corto que la UI traduce a texto amigable.
    private static string ClasificarError(Exception ex)
    {
        Exception e = ex;
        while (e != null)
        {
            if (e is FirebaseException fe)   // Firebase.FirebaseException (namespace Firebase)
            {
                AuthError err = (AuthError)fe.ErrorCode;
                switch (err)
                {
                    case AuthError.EmailAlreadyInUse:        // = 8
                    case AuthError.CredentialAlreadyInUse:   // = 10
                        return "EMAIL_EN_USO";
                    case AuthError.InvalidEmail:             // = 11
                        return "EMAIL_INVALIDO";
                    case AuthError.WeakPassword:             // = 23
                        return "CONTRASENA_DEBIL";
                    case AuthError.NetworkRequestFailed:     // = 19
                        return "SIN_RED";
                    case AuthError.TooManyRequests:          // = 13
                        return "DEMASIADOS_INTENTOS";
                    case AuthError.WrongPassword: 
                        return "CONTRASENA_INCORRECTA";      // = 12
                    case AuthError.UserNotFound: 
                        return "USUARIO_NO_EXISTE";          // = 14
                }
                break;
            }
            e = e.InnerException;
        }

        // Respaldo por si la versión del SDK no envía FirebaseException (mensajes en inglés)
        string m = (ex.GetBaseException().Message ?? "").ToLowerInvariant();
        if (m.Contains("already") && m.Contains("use")) return "EMAIL_EN_USO";
        if (m.Contains("password") && (m.Contains("least") || m.Contains("weak"))) return "CONTRASENA_DEBIL";
        if (m.Contains("email") && m.Contains("invalid")) return "EMAIL_INVALIDO";
        if (m.Contains("network")) return "SIN_RED";
        return "OTRO";
    }
}
