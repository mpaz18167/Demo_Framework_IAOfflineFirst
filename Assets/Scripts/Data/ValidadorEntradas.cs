using System.Text;
using System.Text.RegularExpressions;

// NUEVO: validación y sanitización de TODAS las entradas de texto del usuario
// (alias, correo, contraseña). Enfoque WHITELIST: solo entra lo que está permitido;
// comillas, <, >, ;, --, ../, emojis de control, etc. se rechazan de raíz → bloquea
// inyección de scripts/malware (XSS, payloads SQL-like, path traversal) sin importar
// dónde se guarde el dato (Firestore, ranking, HUD, telemetría).
public static class ValidadorEntradas
{
    // Alias: letras con acentos, números, espacio, guion, guion bajo y punto. 3-15.
    private static readonly Regex _regexAlias =
        new Regex(@"^[\p{L}\p{N} _\-\.]{3,15}$", RegexOptions.Compiled);

    // Correo: usuario@dominio.tld sin caracteres peligrosos.
    private static readonly Regex _regexCorreo =
        new Regex(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$", RegexOptions.Compiled);

    // Limpia caracteres de control (0x00-0x1F, 0x7F), recorta y limita el largo.
    public static string SanearTexto(string texto, int largoMaximo = 64)
    {
        if (string.IsNullOrEmpty(texto)) return "";

        StringBuilder sb = new StringBuilder(texto.Length);
        foreach (char c in texto.Trim())
            if (!char.IsControl(c)) sb.Append(c);

        if (sb.Length > largoMaximo) sb.Length = largoMaximo;
        return sb.ToString();
    }

    // Alias válido → true y aliasLimpio listo para guardar. "motivo" = texto para la UI.
    public static bool AliasValido(string alias, out string aliasLimpio, out string motivo)
    {
        aliasLimpio = SanearTexto(alias, 40);

        if (aliasLimpio.Length < 3) { motivo = "mínimo 3 caracteres"; return false; }
        if (aliasLimpio.Length > 15) { motivo = "máximo 15 caracteres"; return false; }
        if (!_regexAlias.IsMatch(aliasLimpio))
        {
            motivo = "solo letras (con acentos), números, espacio, «-», «_» y «.»";
            return false;
        }

        // Al menos una letra o número (prohíbe "...", "---", "   ", etc.)
        bool tieneAlfanumerico = false;
        foreach (char c in aliasLimpio)
            if (char.IsLetterOrDigit(c)) { tieneAlfanumerico = true; break; }
        if (!tieneAlfanumerico) { motivo = "debe incluir al menos una letra o número"; return false; }

        motivo = "";
        return true;
    }

    // Correo válido (también rechaza comillas, <, >, espacios y secuencias raras).
    public static bool CorreoValido(string correo, out string correoLimpio)
    {
        correoLimpio = SanearTexto(correo, 100);
        return correoLimpio.Length >= 5 && _regexCorreo.IsMatch(correoLimpio);
    }

    // Contraseña: NO se altera (Firebase exige la cadena exacta); solo se bloquean
    // caracteres de control y longitudes fuera de rango.
    public static bool ClaveValida(string clave, out string motivo)
    {
        if (string.IsNullOrEmpty(clave) || clave.Length < 6) { motivo = "mínimo 6 caracteres"; return false; }
        if (clave.Length > 64) { motivo = "máximo 64 caracteres"; return false; }
        foreach (char c in clave)
            if (char.IsControl(c)) { motivo = "no puede contener caracteres de control"; return false; }
        motivo = "";
        return true;
    }
}