using UnityEngine;
using Firebase.Auth;
using System;
using System.Threading.Tasks;

public class AuthManager : MonoBehaviour
{
    private FirebaseAuth auth;
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
            Debug.LogError($"[AuthManager] Error al iniciar sesión anónima: {ex.Message}");
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
 
    public async Task LinkAccount(string email, string password, string aliasDeseado)
    {
        try
        {
            Credential credential = EmailAuthProvider.GetCredential(email, password); // Crear credenciales con el correo y contraseña proporcionados
            AuthResult result = await auth.CurrentUser.LinkWithCredentialAsync(credential); // Vincular la cuenta anónima con la cuenta de correo electrónico
            Debug.Log($"[AuthManager] Cuenta vinculada exitosamente. UID: {result.User.UserId}");

            if (!string.IsNullOrEmpty(aliasDeseado))
            {
                await SetUserAlias(aliasDeseado); // Actualizar el alias del usuario si se proporciona uno
            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Error al vincular la cuenta: {ex.Message}");
        }
    }

    public async Task Login (string inputUsuario, string password)
    {
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
                    Debug.LogError($"[AuthManager] El nombre de usuario no existe o no tiene correo asociado.");
                    return;
                }
            }

            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(correoParaLogin, password); // Intentar iniciar sesión con el correo y contraseña proporcionados
            Debug.Log($"[AuthManager] Inicio de sesión exitoso. UID: {result.User.UserId}, Alias: {result.User.DisplayName}");
            OnUserAuthenticated?.Invoke(result.User.UserId); // Notificar que el usuario se ha autenticado exitosamente
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AuthManager] Error al iniciar sesión: {ex.Message}");
        }
    }
}
