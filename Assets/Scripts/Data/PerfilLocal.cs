using UnityEngine;

public class PerfilLocal
{
    private const string ClaveAlias = "perfil_local_alias";
    public static string ObtenerAlias() => PlayerPrefs.GetString(ClaveAlias, "");

    public static void GuardarAlias(string alias)
    {

        alias = alias.Trim();
        if (alias.Length < 3 || alias.Length > 20)
        {
            Debug.LogWarning("[PerfilLocal] Alias inválido. Debe tener entre 3 y 20 caracteres.");
            return;
        }
        PlayerPrefs.SetString(ClaveAlias, alias);
        PlayerPrefs.Save();
    }
}
