using TMPro;
using UnityEngine;

// Etiqueta "MODO / USER" — una por escena, adjunta directamente en el TMP del label.
public class EtiquetaModo : MonoBehaviour
{
    public static EtiquetaModo Actual;
    public TMP_Text texto;   // vacío = usa el TMP del mismo objeto

    void Awake()
    {
        Actual = this;
        if (texto == null) texto = GetComponent<TMP_Text>();
    }

    void OnEnable() => Refrescar();

    public static void RefrescarActual()
    {
        if (Actual != null) Actual.Refrescar();
    }

    public void Refrescar()
    {
        if (texto == null) return;
        SaveService save = FindAnyObjectByType<SaveService>();   // existe en AMBAS escenas (verificado)
        if (save == null) return;
        PlayerData d = save.LoadDataLocal();
        if (d == null) return;

        string alias = (!string.IsNullOrEmpty(d.playerName) && d.playerName != "Anonimo") ? d.playerName : "Anonimo";

        if (!string.IsNullOrEmpty(d.email))
            texto.text = $"MODO: INICIO DE SESIÓN / VINCULADO\nUser: {alias} / {d.email}";
        else if (alias != "Anonimo")
            texto.text = $"MODO: ALIAS\nUser: {alias}";
        else
            texto.text = "MODO: ANÓNIMO\nUser: Anonimo";
    }
}