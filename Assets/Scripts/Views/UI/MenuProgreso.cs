using System.Text;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine;

public class MenuProgreso : MonoBehaviour
{

    [Header("Visualizacion (TMP)")]
    public TMP_Text textoIdentidad; // alias + correo 
    public TMP_Text textoResumen;   //nivel desbloqueado, récord global, % progreso, vidas.
    public TMP_Text textoRecords;   //Tabla por nivel
    public TMP_Text textoSync;      //¿Pendiente de subir?

    [Header("Botones")]
    public GameObject panelProgreso; // se activa/desactiva con AlternarPanel()

    private GameController gc;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gc = FindAnyObjectByType<GameController>();
        Refrescar();
    }

    public void AlternarPanel() //Conectalo a un boton del Menu/Pausa para mostrar/ocultar el panel de progreso
    {
        panelProgreso.SetActive(!panelProgreso.activeSelf);
        if (panelProgreso.activeSelf) Refrescar();
    }

    public async void BotonRecuperarDeLaNube()
    {
        if (gc == null) return;
        if (Application.internetReachability == NetworkReachability.NotReachable)
        { PopUpUI.Mostrar("Sin conexión:\nno se puede consultar la nube.", false); return; }
        if (string.IsNullOrEmpty(gc.DatosJugador.email))
        { PopUpUI.Mostrar("Sin cuenta vinculada:\nno hay nada en la nube.", false); return; }

        PopUpUI.Mostrar("Consultando la nube...", true);
        PlayerData nube = await gc.DescargarNubeSoloLectura();
        if (nube == null)
        { PopUpUI.Mostrar("La nube no tiene progreso\npara esta cuenta todavía.", false); return; }

        PlayerData local = gc.DatosJugador;
        int cmp = CompararAvance(local, nube);

        string veredicto =
            cmp < 0 ? "▲ La NUBE está más adelante:\n   recuperar te hará AVANZAR." :
            cmp > 0 ? "⚠ Tu LOCAL está más adelante:\n   recuperar hará RETROCEDER." :
                      "✓ Están al mismo nivel.";
        string pend = local.isPendingSync ? "\n⚠ Cambios locales PENDIENTES de subir." : "";

        PopUpUI.MostrarConfirmacion(
            ConstruirComparacion(local, nube) + pend + "\n" + veredicto +
            "\n\n¿Descargar la nube y REEMPLAZAR\ntu progreso local?",
            async () =>
            {
                bool ok = await gc.RecuperarProgresoDeLaNube();   // método validado: NO se toca
                Refrescar();
                EtiquetaModo.RefrescarActual();
                PopUpUI.Mostrar(ok ? "Progreso recuperado de la nube ✓"
                                   : "No se pudo recuperar.", ok);
            });
    }

    public async void BotonSubirAhora()
    {
        if (gc == null) return;
        if (string.IsNullOrEmpty(gc.DatosJugador.email))
        { PopUpUI.Mostrar("Sin cuenta vinculada:\nvincula una cuenta desde el menú\npara respaldar tu progreso.", false); return; }
        if (Application.internetReachability == NetworkReachability.NotReachable)
        { PopUpUI.Mostrar("Sin conexión:\ntus datos se subirán solos\nal reconectar.", false); return; }

        PopUpUI.Mostrar("Consultando la nube...", true);
        PlayerData nube = await gc.DescargarNubeSoloLectura();   // null = sin documento en la nube

        PlayerData local = gc.DatosJugador;
        int cmp = CompararAvance(local, nube);

        string veredicto =
            nube == null ? "○ La nube está vacía:\n   subir crea tu primer respaldo." :
            cmp > 0 ? "▲ Tu LOCAL está más adelante:\n   subir hará AVANZAR la nube." :
            cmp < 0 ? "⚠ La NUBE está más adelante:\n   subir la SOBRESCRIBIRÁ y\n   perderás ese avance." :
                           "✓ Están al mismo nivel.";
        string pend = local.isPendingSync ? "\n⚠ Cambios locales PENDIENTES de subir." : "";

        PopUpUI.MostrarConfirmacion(
            ConstruirComparacion(local, nube) + pend + "\n" + veredicto +
            "\n\n¿Subir tu progreso LOCAL\na la nube?",
            () =>
            {
                gc.SincronizarAhora();
                Refrescar();
                PopUpUI.Mostrar("Sincronización solicitada ✓\nMira el estado más abajo.", true);
            });
    }

    public void Refrescar()
    {
        if (gc == null || gc.DatosJugador == null) return;

        PlayerData d = gc.DatosJugador;

        bool tieneCuenta = !string.IsNullOrEmpty(d.email);
        textoIdentidad.text = $"Alias: {d.playerName}\nCorreo: {(string.IsNullOrEmpty(d.email) ? "(sin cuenta vinculada)" : d.email)}";
        textoResumen.text = $"Nivel desbloqueado: {d.highestLevelUnlocked}\nRécord global: {d.score}\nProgreso total: {d.totalProgressPercentage:0.#}%\nVidas: {d.currentLives}";
        textoSync.text = d.isPendingSync ? "Estado: datos PENDIENTES de subir" : "Estado: sincronizado con la nube";
        textoSync.text += tieneCuenta
            ? "\nCuenta vinculada ✓ — progreso recuperable en otra PC."
            : "\n⚠ Sin cuenta: tu progreso existe solo en este dispositivo.";

        StringBuilder sb = new StringBuilder("Records por nivel:\n");

        foreach (LevelRecord r in d.levelRecords)
        {
            string tiempo = r.bestTime < float.MaxValue ? $"{r.bestTime:0.0} s" : "—";
            sb.AppendLine($"Nivel {r.levelId}: record {r.bestScore} | mejor tiempo {tiempo} | veces jugado {r.timesPlayed} | completado {r.completionCount}x");
        }
        textoRecords.text = sb.ToString();
    }

    // ---------- Comparación LOCAL vs NUBE (compartida por Recuperar y Subir) ----------
    // <0 → la nube va adelante | 0 → igualados | >0 → el local va adelante
    private static int CompararAvance(PlayerData local, PlayerData nube)
    {
        if (nube == null) return 1;                       // nube sin documento → el local "adelante"
        if (local.highestLevelUnlocked != nube.highestLevelUnlocked)
            return local.highestLevelUnlocked.CompareTo(nube.highestLevelUnlocked);
        if (local.score != nube.score)
            return local.score.CompareTo(nube.score);
        return local.totalProgressPercentage.CompareTo(nube.totalProgressPercentage);
    }

    private static string ConstruirComparacion(PlayerData local, PlayerData nube)
    {
        string nivelN = nube != null ? nube.highestLevelUnlocked.ToString() : "—";
        string recN = nube != null ? nube.score.ToString() : "—";
        string progN = nube != null ? $"{nube.totalProgressPercentage:0.#}%" : "—";
        string aliasN = nube != null ? nube.playerName : "(sin datos)";

        return "          LOCAL   |   NUBE\n" +
               $"Nivel    {local.highestLevelUnlocked,-7}|   {nivelN}\n" +
               $"Récord   {local.score,-7}|   {recN}\n" +
               $"Progreso {local.totalProgressPercentage,6:0.#}%|   {progN}\n" +
               $"Alias    {local.playerName,-7}|   {aliasN}";
    }
}

