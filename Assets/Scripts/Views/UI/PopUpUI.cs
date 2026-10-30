using System;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine;

// Popup reutilizable por escena. Cualquier script lo llama:  PopupUI.Mostrar("...", true)
// Adjuntalo SIEMPRE en un objeto ACTIVO de la escena (p. ej. la raíz del prefab).

public class PopUpUI : MonoBehaviour
{
    public static PopUpUI Actual;          // instancia de ESTA escena

    [Header("Wiring (todo interno al prefab)")]
    public GameObject panel;               // PanelFondo (INACTIVO al arrancar)
    public TMP_Text texto;
    public GameObject botonCancelar;       // se oculta en mensajes simples

    private Action alConfirmar;

    void Awake()
    {
        Actual = this;
        if (panel) panel.SetActive(false);
    }

    // --- Mensaje simple (1 botón) ---
    public static void Mostrar(string mensaje, bool exito = true)
    {
        if (Actual == null) { Debug.Log($"[Popup ausente] {(exito ? "OK" : "ERROR")}: {mensaje}"); return; }
        Actual.alConfirmar = null;
        Actual.Presentar((exito ? "✓ " : "✗ ") + mensaje, exito, false);
    }

    // --- Confirmación (2 botones): SOLO ejecuta si confirma ---
    public static void MostrarConfirmacion(string mensaje, Action alConfirmar)
    {
        if (Actual == null) { Debug.LogError($"[PopupUI] Escena sin popup; NO se ejecuta: {mensaje}"); return; }
        Actual.alConfirmar = alConfirmar;
        Actual.Presentar(mensaje, false, true);
    }

    private void Presentar(string mensaje, bool exito, bool esConfirmacion)
    {
        if (texto)
        {
            texto.text = mensaje;
            texto.color = exito ? new Color(0.35f, 1f, 0.45f) : Color.white;
        }
        if (botonCancelar) botonCancelar.SetActive(esConfirmacion);
        if (panel) panel.SetActive(true);
    }

    public void Aceptar()   // conecta al botón ACEPTAR
    {
        Action a = alConfirmar;
        alConfirmar = null;
        if (panel) panel.SetActive(false);
        a?.Invoke();        // en confirmaciones, la acción corre al cerrar
    }

    public void Cancelar()  // conecta al botón CANCELAR
    {
        alConfirmar = null;
        if (panel) panel.SetActive(false);
    }
}
