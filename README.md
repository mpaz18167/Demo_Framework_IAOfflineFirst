<div align="center">

# 🎮 Demo Framework IA Offline-First

**Unity 6 · C# · Firebase — Framework 3D con arquitectura Offline-First e IA Adaptativa**

!Unity
!CSharp
!Firebase
!Status

*Proyecto de tesis aprobado · Universidad Tecnológica del Perú · Lima, 2026*

</div>

---

## 🇬🇧 English Summary

A **Unity 6 framework** that keeps games fully playable **without internet** and adds **adaptive AI** that tunes difficulty in real time. Local JSON storage is the source of truth; Firebase Firestore syncs asynchronously in background threads — **no freezes, stable 60+ FPS**.

**Highlights**

- **Offline-First** — play 100% offline; sync happens silently when the network returns (`isPendingSync` + `lastModified`).
- **Adaptive Difficulty (DDA)** — an AI Director reads a flow index and emits factors `1.5 / 1.0 / 0.8` (boredom / flow / frustration) that adjust enemy speed, vision and procedural spawns.
- **Deterministic Behavior Trees** — patrol, chase, attack, hide-inspection and noise-check nodes written in plain C# (no heavy ML, zero latency).
- **Silent telemetry** — ~40 session metrics collected in the background without interrupting play.
- **MVC adapted to Unity** — Views only read; `GameController` is the single writer of player state.
- **Auth & leaderboard** — anonymous sign-in, email/password accounts, unique aliases and a categorized Top-10 ranking.

**Thesis validation (N = 40 paired sessions):** 2 of 3 hypotheses confirmed — Operability `p = 0.000`, In-game challenge `p = 0.001`; dynamic flow balance `p = 0.276` (reported honestly, with recommendations for larger samples).

**Beyond the demo:** the framework is a reusable base for async multiplayer, LiveOps remote config, ML-Agents trained on real telemetry, player-analytics dashboards and other genres — see [What can be built](#-qué-se-puede-construir-a-partir-de-este-framework). Thesis roadmap (full delta-CRDTs, larger-sample validation, DDA calibration, multi-agent AI) is listed too.

> 📖 Full documentation below in **Spanish** (the thesis language). Use GitHub's *Translate* button if needed.

---

## 🇪🇸 Descripción

Framework 3D demostrable para videojuegos que combina tres pilares que normalmente chocan:

1. **Persistencia Offline-First** — el almacenamiento **local (JSON) es la fuente de verdad**; la nube (Firebase Firestore) se sincroniza de forma **asíncrona en hilos secundarios**, sin congelar el juego.
2. **IA adaptativa (DDA)** — un *Director IA* calcula el índice de flujo del jugador y ajusta la dificultad en tiempo real.
3. **Calidad de software** — arquitectura **MVC** con flujo unidireccional, evaluada bajo **ISO/IEC 25010** (índice de operabilidad y mantenibilidad).

> 🎯 **Problema que resuelve:** las redes inestables bloquean los juegos con nube obligatoria (INEI 2026: 62.7% de hogares peruanos con internet intermitente) y el código monolítico acumula deuda técnica. Este framework prioriza la **disponibilidad local** (Teorema CAP) y separa capas para reducir deuda técnica.

---

## ✨ Características principales

| Característica | Detalle |
|---|---|
| 📴 **100% Offline** | Todo el juego funciona sin internet; al reconectar sincroniza solo lo pendiente |
| 🧠 **DDA en tiempo real** | Director IA con umbrales 0.75 / 0.35 y factores 1.5 / 1.0 / 0.8 |
| 🌳 **Behavior Trees en C#** | Nodos Selector/Secuencia genéricos + hojas: Patrullar, Perseguir, Atacar, Revisar Ruido, Inspeccionar Escondite |
| 📊 **Telemetría silente** | ~40 campos por sesión (OGD, flujo, FPS, latencia de sync) capturados en segundo plano |
| 🏗️ **MVC + 62 scripts modulares** | `Views/` solo lee · `GameController` es el único escritor del modelo · comunicación por eventos |
| 🔐 **Identidad flexible** | Sesión anónima automática, cuentas correo/clave, alias único validado, cambio de identidad |
| 🏆 **Ranking online** | Top-10 categorizado: `anonimo` / `alias` / `cuenta` — jamás rompe la partida si falla |
| ⚡ **60+ FPS estables** | Guardado y sincronización fuera del hilo principal |

---

## 🤖 IA Adaptativa (DDA)

```
Telemetría ──▶ DirectorIA ──▶ Índice de flujo (puntos ÷ tiempo × vidas)
                                │
        ┌───────────────────────┼───────────────────────┐
   > 0.75 (Aburrimiento)   0.35–0.75 (Flow)      < 0.35 (Frustración)
        │                       │                       │
   factor 1.5               factor 1.0              factor 0.8
   +spawn progresivo        comportamiento          enemigos más
   (más desafío)            normal                  tolerantes
```

- **Umbrales:** Aburrimiento `> 0.75` · Frustración `< 0.35`
- **Factores declarados en la tesis:** `1.5 / 1.0 / 0.8` — ajustan velocidad/visión del enemigo y el spawn procedural (`SpawnerDDA`)
- Los **Behavior Trees** consumen estos eventos; son deterministas y sin latencia (interpretación total, sin modelos entrenados)

---

## 🏛️ Arquitectura

```
   [Input/] PlayerControls (Input System)        ← borde de entrada
                   ↓
   ┌────────── VIEWS (46 scripts) ──────────────┐
   │  UI · Player · Consumibles · Environment   │
   │  solo LEE estado · pide acciones al ctrl.  │
   └───────┬───────────────────────▲────────────┘
           │ View → Controller     │ eventos (OnVidasCambiadas,
           ▼                       │ OnDificultadCambiada, …)
   ┌─────── CONTROLLERS ───────────┴────────────┐
   │  GameController → ÚNICO que escribe modelo │
   │  DirectorIA     → calcula factor DDA       │
   └──────┬─────────────────────┬───────────────┘
          ▼                     ▼ eventos
   ┌── MODELS ───────┐   ┌── AI/ (motor cognitivo) ──┐
   │ PlayerData      │   │ Node (Behavior Tree)      │
   │ SessionData     │   │ EnemigoIA · AdaptadorDDA  │
   └─────────────────┘   └───────────────────────────┘
          ▲
   ┌── DATA/ (servicios de persistencia) ─────────────┐
   │  SaveService (JSON local = verdad)              │
   │        ↕  CloudService (Firestore, async)        │
   │  AuthManager · TelemetryService · Validador      │
   └─────────────────────────────────────────────────┘
```

**Reglas verificables del flujo:**
- Las 24 escrituras a `playerData.*` del proyecto están **solo en `GameController.cs`**.
- Las vistas se comunican mediante **eventos** (`OnVidasCambiadas`, `OnEstadoConexionCambiado`, `OnDificultadCambiada`…).
- Sin singletons: acceso por `FindAnyObjectByType<T>()`. Sin `GetKeyDown`/`Keyboard.current`: solo **Input System** (`PlayerControls.inputactions`).

---

## 📁 Estructura del repositorio

```
Assets/Scripts/
├── AI/           (4)   Node, EnemigoIA, Escondite, AdaptadorCajaDDA
├── Controllers/  (2)   GameController, DirectorIA
├── Data/         (7)   SaveService, CloudService, AuthManager, FirebaseInit,
│                       TelemetryService, PerfilLocal, ValidadorEntradas
├── Input/        (1)   PlayerControls (generado por Input System)
├── Models/       (2)   PlayerData, SessionData
└── Views/       (46)   UI/ (12) · Consumibles/ (14) · Player/ (6)
                        Environment/ (6) · Enemies/ · Tools/ · World/

Assets/Scenes/          MenuPrincipal · SampleScene (Nivel 1)
Assets/Firebase/        Firebase Unity SDK (Auth + Firestore)
firestore.rules         Reglas de seguridad de Firestore
```

---

## 📈 Resultados de validación (Tesis)

**Diseño:** cuantitativo · aplicado · explicativo · **preexperimental** (pre-test/post-test, un solo grupo)
**Muestra:** N = **40 sesiones pareadas** (80 registros telemétricos) · **Shapiro-Wilk** → **Wilcoxon** (no paramétrica)
**Instrumentos:** validados con **V de Aiken ≥ 0.80** (3 jueces)

| Hipótesis | Dimensión | z | p-valor | Resultado |
|---|---|---|---|---|
| H1 | Operabilidad (ISO/IEC 25010) | −4.879 | **0.000** | ✅ **Confirmada** — media 0.524 → 0.471 (siempre en zona de flujo) |
| H2 | Desafío objetivo (OGD) | −3.387 | **0.001** | ✅ **Confirmada** — 45.65 → 35.22 (−22.8% de monotonía) |
| H3 | Balance dinámico (índice de flujo) | −1.089 | **0.276** | ❌ No confirmada — picos de flujo +115.6% con alta varianza |

> **2 de 3 hipótesis confirmadas.** La tercera se reporta con honestidad y deriva en recomendaciones: mayor N, calibrar los pesos de la función de aptitud y transiciones de comportamiento más suaves.

**Métricas de ingeniería:** 60+ FPS estados · inversión estimada 420 h (≈ S/18,900) · ahorro estimado ~40% en tiempo de desarrollo frente a la reconstrucción desde cero.

---

## 🚀 Puesta en marcha

### Requisitos
- **Unity 6000.5.0f1** (via Unity Hub — `Packages/manifest.json` restaura el resto)
- Paquetes clave (automáticos): Input System 1.19.0 · Cinemachine 3.1.7 · AI Navigation 2.0.14
- Firebase Unity SDK ya incluido en `Assets/Firebase/`

### Jugar (modo local — sin Firebase)
```bash
git clone https://github.com/mpaz18167/Demo_Framework_IAOfflineFirst.git
```
1. Abre el proyecto con **Unity Hub** (6000.5.0f1).
2. Abre `Assets/Scenes/MenuPrincipal.unity` o `Assets/Scenes/SampleScene.unity`.
3. ▶ Play — el juego, el DDA y la telemetría local funcionan **sin configuración** (si no hay Firebase configurado, la nube se omite sin errores: todo `CloudService` protege `db == null`).

### Habilitar la nube (opcional)
1. Crea un proyecto en [Firebase Console](https://console.firebase.google.com).
2. **Authentication** → habilita **Email/Password** y **Anonymous** (el juego inicia sesión anónima automática).
3. **Firestore Database** → crea la BD → pega el contenido de [`firestore.rules`](./firestore.rules) → **Publish**.
4. **Project settings → Tus apps** → descarga la configuración:
   - `google-services.json` → `Assets/`
   - `google-services-desktop.json` → `Assets/StreamingAssets/`
5. *(Recomendado)* Restringe la API key en Google Cloud Console → *API restrictions* → Cloud Firestore API + Identity Toolkit API + Firebase Installations API.

---

## 🎮 Controles

| Acción | Tecla |
|---|---|
| Mover | `W A S D` |
| Saltar | `Espacio` |
| Esconderse | `E` |
| Lanzar distractor | `F` |
| Atacar | `Q` / `Clic izquierdo` |
| Pausa | `Esc` |
| **Toggle red (online/offline)** | `F1` |
| **HUD de depuración (DDA/FPS)** | `F2` |

---

## 🔒 Seguridad

- **Este repositorio NO incluye credenciales de Firebase** — `google-services*.json` están excluidos con `.gitignore` (mantenidos solo en local). Si haces *fork*, añade **tu propia** configuración y nunca la subas.
- [`firestore.rules`](./firestore.rules) publicadas:
  - `players` — cada jugador solo lee/escribe su propio documento; el list es necesario para la unicidad de alias.
  - `telemetry` — solo creación; jamás edición ni borrado.
  - `scores` — lectura pública (Top-10); escritura solo del dueño; borrados denegados.

---

## 🚀 ¿Qué se puede construir a partir de este framework?

La demo es solo la prueba de concepto: el framework es una **base reutilizable** sobre la que se pueden montar sistemas más avanzados — cada uno con cimientos que **ya existen en el código**:

### 🎮 Nuevos juegos y géneros
- **Terror / supervivencia** — el DDA ya regula la tensión: escalar spawn, sonido y luces según frustración (`0.8`) o aburrimiento (`1.5`) en vez de solo velocidad/visión.
- **Serious games y educación** — la telemetría silente mide rendimiento real → contenido que se adapta al aprendizaje (misma línea que Carrión-León et al., 2025, alineado a ISO/IEC 25010).
- **Mobile para redes inestables** — el Offline-First ya está probado con el caso peruano (62.7% de hogares): runner/puzzle que nunca se bloquea por red.
- **Roguelike procedural** — ya existen semillas y spawner (`totalProceduralRoutes` en la telemetría): niveles infinitos con el DDA equilibrando la dificultad.

### 🌐 Multijugador y servicios (LiveOps)
- **Coop asíncrono** — dos jugadores dejan rastro (récords, telemetría) → desafíos "supera el registro de X" usando Firestore como mediador, **sin servidores dedicados** *(multiusuario síncrono/MMO queda fuera del alcance de la tesis, declarado)*.
- **Cross-device save** — subir/bajar la partida entre PC y móvil: punto de partida natural para los **delta-CRDTs completos**.
- **LiveOps / Remote Config** — ajustar factores DDA, eventos y contenido desde la nube **sin actualizar el build** (extensión directa de `CloudService`).
- **Temporadas y ligas** — el ranking categorizado ya existe → clasificatorias, temporadas y recompensas.

### 🧠 IA más avanzada
- **ML-Agents** — los ~40 campos de telemetría por sesión son un **dataset listo** para entrenar políticas y combinarlas con los Behavior Trees deterministas.
- **Utility AI / GOAP** — rellenar o reemplazar hojas del árbol por planificación utilitaria para metas complejas.
- **Squads multiagente** — enemigos con roles (tanque, distracción, flanqueo) sobre el motor `Node/` ya desacoplado (Conclusión 4 de la tesis).
- **Modelo de jugador** — clustering de estilos de juego desde la telemetría: personalización más allá de los 3 estados actuales (Aburrimiento/Flow/Frustración).

### 📊 Datos y plataformas
- **Dashboard de analítica** — heatmaps de muerte, curvas de aprendizaje y retención: los datos **ya fluyen** a Firestore; solo falta visualizarlos.
- **Framework de A/B testing** — dos variantes del DDA comparadas con la misma pipeline estadística de la tesis (Shapiro-Wilk → Wilcoxon).
- **Game Backend como servicio** — auth + guardado + ranking + analítica **replicables** en otros proyectos: el verdadero valor de "framework".

---

## 🔮 Trabajo futuro de la tesis y el demo

**De la tesis (recomendaciones formuladas en el documento):**

| # | Extensión | Origen |
|---|---|---|
| 1 | **Delta-CRDTs completos** — implementar el algoritmo de fusión *delta-state* (Almeida, 2018; Nair et al., 2026) para edición **multi-dispositivo concurrente**. Hoy se aplican sus principios (local-first, `isPendingSync`, `lastModified` LWW); el alcance actual (un solo jugador) no genera conflictos concurrentes que resolver. | Marco teórico §3.1.4 |
| 2 | **Mayor N muestral** — superar N = 40 para estabilizar la varianza del índice de flujo (0.120 → 0.968) y re-evaluar la H3 (`p = 0.276`). | Recomendaciones (H3) |
| 3 | **Calibración del DDA** — ajustar los pesos de la función de aptitud del Director IA y suavizar las transiciones entre estados de comportamiento. | Recomendaciones (H3) |
| 4 | **IA multiagente cooperativo** — el motor cognitivo (`Node` + Behavior Trees) está desacoplado a propósito para escalar a múltiples agentes. | Conclusión 4 |
| 5 | **Dashboard de telemetría** — visor para las sesiones de Firestore/JSON (hoy son datos crudos listos para exportar). | Propuesta propia |

**Del diseño del juego (roadmap del demo):**

- **Nivel 2 con verticalidad** — el Nivel 1 es plano (y ≈ 0.5). Blueprint listo: plataformas y miradores (requiere re-cocinado de NavMesh), escondites en la zona del jefe, checkpoints de proximidad y una ruta B alternativa.
- **Más sinergias de herramientas** — TNT/Nitro adicionales en la zona del jefe para explotar el combo distractor → cadena explosiva.
- **Builds multiplataforma** — exportación WebGL/Android para demostración pública sin instalar Unity.

**De ingeniería y seguridad:**

- **Firebase App Check** + restricción de la API key (paso pendiente documentado en [Seguridad](#-seguridad)) — endurecimiento de la capa cloud.
- **Integración continua** — workflow de GitHub Actions que compile el proyecto y detecte errores de C# en cada push.

> Todas las extensiones respetan los **factores DDA declarados (1.5 / 1.0 / 0.8)** y el principio Offline-First: el local sigue siendo la fuente de verdad.

---

## 🎓 Acerca de la investigación

> **Tesis de grado** — *Framework 3D con arquitectura Offline-First e Inteligencia Artificial adaptativa para la experiencia del jugador en videojuegos.*
> **Autor:** Marcelo Paz ([@mpaz18167](https://github.com/mpaz18167)) · **Institución:** Universidad Tecnológica del Perú · **Lima, 2026** — **aprobada en noviembre de 2026.**
>
> **Método ágil:** SCRUM + PMI-CPMAI. **Declaración de uso de IA:** Google NotebookLM empleado únicamente para búsqueda y proceso de literatura; redacción y verificación humana.
>
> Fuera de alcance (declarado): multijugador/MMO, estética/artes y pruebas de estrés de red a gran escala.
