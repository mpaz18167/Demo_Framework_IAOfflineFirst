<div align="center">

# 🎮 Demo Framework IA Offline-First

**Unity 6 · C# · Firebase — 3D Offline-First Framework with Adaptive AI**
**Framework 3D con arquitectura Offline-First e Inteligencia Artificial adaptativa**

!Unity
!CSharp
!Firebase
!Status

*Approved thesis project · Proyecto de tesis aprobado · Universidad Tecnológica del Perú · Lima, 2026*

**[🇬🇧 English](#english) · [🇪🇸 Español](#espanol)**

</div>

---

<a id="english"></a>

## 🇬🇧 English

### Description

A **Unity 6 framework** that keeps games fully playable **without internet** and adds **adaptive AI** that tunes difficulty in real time. Three pillars that usually clash:

1. **Offline-First persistence** — the **local storage (JSON) is the source of truth**; the cloud (Firebase Firestore) syncs **asynchronously on background threads**, never freezing the game.
2. **Adaptive AI (DDA)** — an *AI Director* computes the player's flow index and adjusts difficulty in real time.
3. **Software quality** — **MVC** architecture with unidirectional flow, evaluated under **ISO/IEC 25010** (operability index and maintainability).

> 🎯 **Problem solved:** unstable networks block cloud-required games (INEI 2026: 62.7% of Peruvian households have intermittent internet) and monolithic code accumulates technical debt. This framework prioritizes **local availability** (CAP Theorem) and separates layers to reduce technical debt.

### Key features

| Feature | Details |
|---|---|
| 📴 **100% Offline** | The whole game works without internet; on reconnect it syncs only pending data |
| 🧠 **Real-time DDA** | AI Director with 0.75 / 0.35 thresholds and 1.5 / 1.0 / 0.8 factors |
| 🌳 **Behavior Trees in C#** | Generic Selector/Sequence nodes + leaves: Patrol, Chase, Attack, Check Noise, Inspect Hiding Spots |
| 📊 **Silent telemetry** | ~40 fields per session (OGD, flow, FPS, sync latency) captured in the background |
| 🏗️ **MVC + 62 modular scripts** | `Views/` only reads · `GameController` is the model's single writer · event-driven communication |
| 🔐 **Flexible identity** | Automatic anonymous session, email/password accounts, validated unique alias, identity switching |
| 🏆 **Online leaderboard** | Categorized Top-10: `anonimo` / `alias` / `cuenta` — never crashes the game on failure |
| ⚡ **Stable 60+ FPS** | Saving and syncing run off the main thread |

### Adaptive AI (DDA)

```
Telemetry ──▶ AI Director ──▶ Flow index (points ÷ time × lives)
                                │
        ┌───────────────────────┼───────────────────────┐
   > 0.75 (Boredom)       0.35–0.75 (Flow)       < 0.35 (Frustration)
        │                       │                       │
   factor 1.5               factor 1.0              factor 0.8
   + progressive spawn      normal behavior         more lenient
   (more challenge)                                 enemies
```

- **Thresholds:** Boredom `> 0.75` · Frustration `< 0.35`
- **Factors declared in the thesis:** `1.5 / 1.0 / 0.8` — they adjust enemy speed/vision and procedural spawning (`SpawnerDDA`)
- **Behavior Trees** consume these events; they are deterministic and zero-latency (full interpretation, no trained models)

### Architecture

```
   [Input/] PlayerControls (Input System)        ← input edge
                   ↓
   ┌────────── VIEWS (46 scripts) ──────────────┐
   │  UI · Player · Consumibles · Environment   │
   │  only READS state · asks the controller    │
   └───────┬───────────────────────▲────────────┘
           │ View → Controller     │ events (OnVidasCambiadas,
           ▼                       │ OnDificultadCambiada, ...)
   ┌─────── CONTROLLERS ───────────┴────────────┐
   │  GameController → the ONLY model writer    │
   │  DirectorIA     → computes the DDA factor  │
   └──────┬─────────────────────┬───────────────┘
          ▼                     ▼ events
   ┌── MODELS ───────┐   ┌── AI/ (cognitive engine) ─┐
   │ PlayerData      │   │ Node (Behavior Tree)      │
   │ SessionData     │   │ EnemigoIA · DDA Adapter   │
   └─────────────────┘   └───────────────────────────┘
          ▲
   ┌── DATA/ (persistence services) ────────────────┐
   │  SaveService (local JSON = truth)             │
   │        ↕  CloudService (Firestore, async)      │
   │  AuthManager · TelemetryService · Validator   │
   └───────────────────────────────────────────────┘
```

**Verifiable flow rules:**
- All 24 writes to `playerData.*` live **only in `GameController.cs`**.
- Views communicate through **events** (`OnVidasCambiadas`, `OnEstadoConexionCambiado`, `OnDificultadCambiada`, ...).
- No singletons: access via `FindAnyObjectByType<T>()`. No `GetKeyDown`/`Keyboard.current`: **Input System** only (`PlayerControls.inputactions`).

### Repository structure

```
Assets/Scripts/
├── AI/           (4)   Node, EnemigoIA, Escondite, AdaptadorCajaDDA
├── Controllers/  (2)   GameController, DirectorIA
├── Data/         (7)   SaveService, CloudService, AuthManager, FirebaseInit,
│                       TelemetryService, PerfilLocal, ValidadorEntradas
├── Input/        (1)   PlayerControls (generated by Input System)
├── Models/       (2)   PlayerData, SessionData
└── Views/       (46)   UI/ (12) · Consumibles/ (14) · Player/ (6)
                        Environment/ (6) · Enemies/ · Tools/ · World/

Assets/Scenes/          MenuPrincipal · SampleScene (Level 1)
Assets/Firebase/        Firebase Unity SDK (Auth + Firestore)
firestore.rules         Firestore security rules
```

### Validation results (Thesis)

**Design:** quantitative · applied · explanatory · **preexperimental** (pre-test/post-test, single group)
**Sample:** N = **40 paired sessions** (80 telemetry records) · **Shapiro-Wilk** → **Wilcoxon** (non-parametric)
**Instruments:** validated with **Aiken's V ≥ 0.80** (3 judges)

| Hypothesis | Dimension | z | p-value | Result |
|---|---|---|---|---|
| H1 | Operability (ISO/IEC 25010) | −4.879 | **0.000** | ✅ **Confirmed** — mean 0.524 → 0.471 (always in the flow zone) |
| H2 | In-game challenge (OGD) | −3.387 | **0.001** | ✅ **Confirmed** — 45.65 → 35.22 (−22.8% monotony) |
| H3 | Dynamic balance (flow index) | −1.089 | **0.276** | ❌ Not confirmed — flow peaks +115.6% with high variance |

> **2 of 3 hypotheses confirmed.** The third is reported honestly and leads to recommendations: larger N, fitness-weight calibration and smoother behavior transitions.

**Engineering metrics:** stable 60+ FPS · estimated investment 420 h (≈ S/18,900) · estimated ~40% savings in development time vs. rebuilding from scratch.

### Getting started

**Requirements**
- **Unity 6000.5.0f1** (via Unity Hub — `Packages/manifest.json` restores the rest)
- Key packages (automatic): Input System 1.19.0 · Cinemachine 3.1.7 · AI Navigation 2.0.14
- Firebase Unity SDK already included in `Assets/Firebase/`

**Play locally (no Firebase needed)**
```bash
git clone https://github.com/mpaz18167/Demo_Framework_IAOfflineFirst.git
```
1. Open the project with **Unity Hub** (6000.5.0f1).
2. Open `Assets/Scenes/MenuPrincipal.unity` or `Assets/Scenes/SampleScene.unity`.
3. ▶ Play — the game, the DDA and local telemetry work **without any setup** (if Firebase is not configured, the cloud is skipped without errors: every `CloudService` method guards `db == null`).

**Enable the cloud (optional)**
1. Create a project in the [Firebase Console](https://console.firebase.google.com).
2. **Authentication** → enable **Email/Password** and **Anonymous** (the game auto-starts an anonymous session).
3. **Firestore Database** → create the DB → paste the contents of [`firestore.rules`](./firestore.rules) → **Publish**.
4. **Project settings → Your apps** → download the configuration:
   - `google-services.json` → `Assets/`
   - `google-services-desktop.json` → `Assets/StreamingAssets/`
5. *(Recommended)* Restrict the API key in Google Cloud Console → *API restrictions* → Cloud Firestore API + Identity Toolkit API + Firebase Installations API.

### Controls

| Action | Key |
|---|---|
| Move | `W A S D` |
| Jump | `Space` |
| Hide | `E` |
| Throw distractor | `F` |
| Attack | `Q` / `Left click` |
| Pause | `Esc` |
| **Network toggle (online/offline)** | `F1` |
| **Debug HUD (DDA/FPS)** | `F2` |

### Security

- **This repository does NOT include Firebase credentials** — `google-services*.json` files are excluded via `.gitignore` (kept locally only). If you fork, add **your own** configuration and never commit it.
- [`firestore.rules`](./firestore.rules) published:
  - `players` — each player only reads/writes their own document; the list permission is required for alias uniqueness.
  - `telemetry` — create-only; never edited nor deleted.
  - `scores` — public reads (Top-10); owner-only writes; deletes denied.

### What can you build with this framework?

The demo is just the proof of concept: the framework is a **reusable base** for more advanced systems — each one grounded in code that **already exists**:

**🎮 New games and genres**
- **Horror / survival** — the DDA already regulates tension: scale spawning, sounds and lights according to frustration (`0.8`) or boredom (`1.5`) instead of only speed/vision.
- **Serious games and education** — silent telemetry measures real performance → content that adapts to learning (same line as Carrión-León et al., 2025, aligned with ISO/IEC 25010).
- **Mobile on unstable networks** — the Offline-First approach is proven with the Peruvian case (62.7% of households): a runner/puzzle that never blocks on connectivity.
- **Procedural roguelike** — seeds and spawner already exist (`totalProceduralRoutes` in telemetry): endless levels with the DDA balancing difficulty.

**🌐 Multiplayer and services (LiveOps)**
- **Asynchronous co-op** — two players leave a trace (records, telemetry) → "beat X's score" challenges using Firestore as a mediator, **without dedicated servers** *(synchronous multi-user/MMO remains out of the thesis scope, as declared)*.
- **Cross-device save** — upload/download progress between PC and mobile: the natural starting point for **full delta-CRDTs**.
- **LiveOps / Remote Config** — tune DDA factors, events and content from the cloud **without updating the build** (a direct extension of `CloudService`).
- **Seasons and leagues** — the categorized leaderboard already exists → qualifiers, seasons and rewards.

**🧠 More advanced AI**
- **ML-Agents** — the ~40 telemetry fields per session are a **ready dataset** to train policies and combine them with the deterministic Behavior Trees.
- **Utility AI / GOAP** — fill or replace tree leaves with utility planning for complex goals.
- **Cooperative multi-agent squads** — enemies with roles (tank, distraction, flanker) on the already decoupled `Node/` engine (Thesis Conclusion 4).
- **Player model** — clustering play styles from telemetry: personalization beyond the current three states (Boredom/Flow/Frustration).

**📊 Data and platforms**
- **Analytics dashboard** — death heatmaps, learning curves and retention: the data **already flows** to Firestore; it only needs visualization.
- **A/B testing framework** — two DDA variants compared with the thesis' own statistical pipeline (Shapiro-Wilk → Wilcoxon).
- **Game backend as a service** — auth + save + leaderboard + analytics **replicable** across projects: the real value of a "framework".

### About the research

> **Thesis** — *A 3D Framework with Offline-First architecture and Adaptive Artificial Intelligence for the player experience in video games.*
> **Author:** Marcelo Paz ([@mpaz18167](https://github.com/mpaz18167)) · **Institution:** Universidad Tecnológica del Perú · **Lima, 2026** — **approved in November 2026.**
>
> **Agile method:** SCRUM + PMI-CPMAI. **AI usage declaration:** Google NotebookLM used only for literature search and processing; writing and verification done by humans.
>
> Declared out of scope: MMO/synchronous multiplayer, aesthetics/art assets and large-scale network stress testing.

---

<a id="espanol"></a>

## 🇪🇸 Español

### Descripción

Framework 3D demostrable para videojuegos que combina tres pilares que normalmente chocan:

1. **Persistencia Offline-First** — el almacenamiento **local (JSON) es la fuente de verdad**; la nube (Firebase Firestore) se sincroniza de forma **asíncrona en hilos secundarios**, sin congelar el juego.
2. **IA adaptativa (DDA)** — un *Director IA* calcula el índice de flujo del jugador y ajusta la dificultad en tiempo real.
3. **Calidad de software** — arquitectura **MVC** con flujo unidireccional, evaluada bajo **ISO/IEC 25010** (índice de operabilidad y mantenibilidad).

> 🎯 **Problema que resuelve:** las redes inestables bloquean los juegos con nube obligatoria (INEI 2026: 62.7% de hogares peruanos con internet intermitente) y el código monolítico acumula deuda técnica. Este framework prioriza la **disponibilidad local** (Teorema CAP) y separa capas para reducir deuda técnica.

### Características principales

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

### IA Adaptativa (DDA)

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

### Arquitectura

```
   [Input/] PlayerControls (Input System)        ← borde de entrada
                   ↓
   ┌────────── VIEWS (46 scripts) ──────────────┐
   │  UI · Player · Consumibles · Environment   │
   │  solo LEE estado · pide acciones al ctrl.  │
   └───────┬───────────────────────▲────────────┘
           │ View → Controller     │ eventos (OnVidasCambiadas,
           ▼                       │ OnDificultadCambiada, ...)
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
- Las vistas se comunican mediante **eventos** (`OnVidasCambiadas`, `OnEstadoConexionCambiado`, `OnDificultadCambiada`, ...).
- Sin singletons: acceso por `FindAnyObjectByType<T>()`. Sin `GetKeyDown`/`Keyboard.current`: solo **Input System** (`PlayerControls.inputactions`).

### Estructura del repositorio

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

### Resultados de validación (Tesis)

**Diseño:** cuantitativo · aplicado · explicativo · **preexperimental** (pre-test/post-test, un solo grupo)
**Muestra:** N = **40 sesiones pareadas** (80 registros telemétricos) · **Shapiro-Wilk** → **Wilcoxon** (no paramétrica)
**Instrumentos:** validados con **V de Aiken ≥ 0.80** (3 jueces)

| Hipótesis | Dimensión | z | p-valor | Resultado |
|---|---|---|---|---|
| H1 | Operabilidad (ISO/IEC 25010) | −4.879 | **0.000** | ✅ **Confirmada** — media 0.524 → 0.471 (siempre en zona de flujo) |
| H2 | Desafío objetivo (OGD) | −3.387 | **0.001** | ✅ **Confirmada** — 45.65 → 35.22 (−22.8% de monotonía) |
| H3 | Balance dinámico (índice de flujo) | −1.089 | **0.276** | ❌ No confirmada — picos de flujo +115.6% con alta varianza |

> **2 de 3 hipótesis confirmadas.** La tercera se reporta con honestidad y deriva en recomendaciones: mayor N, calibrar los pesos de la función de aptitud y transiciones de comportamiento más suaves.

**Métricas de ingeniería:** 60+ FPS estables · inversión estimada 420 h (≈ S/18,900) · ahorro estimado ~40% en tiempo de desarrollo frente a la reconstrucción desde cero.

### Puesta en marcha

**Requisitos**
- **Unity 6000.5.0f1** (via Unity Hub — `Packages/manifest.json` restaura el resto)
- Paquetes clave (automáticos): Input System 1.19.0 · Cinemachine 3.1.7 · AI Navigation 2.0.14
- Firebase Unity SDK ya incluido en `Assets/Firebase/`

**Jugar (modo local — sin Firebase)**
```bash
git clone https://github.com/mpaz18167/Demo_Framework_IAOfflineFirst.git
```
1. Abre el proyecto con **Unity Hub** (6000.5.0f1).
2. Abre `Assets/Scenes/MenuPrincipal.unity` o `Assets/Scenes/SampleScene.unity`.
3. ▶ Play — el juego, el DDA y la telemetría local funcionan **sin configuración** (si no hay Firebase configurado, la nube se omite sin errores: todo `CloudService` protege `db == null`).

**Habilitar la nube (opcional)**
1. Crea un proyecto en [Firebase Console](https://console.firebase.google.com).
2. **Authentication** → habilita **Email/Password** y **Anonymous** (el juego inicia sesión anónima automática).
3. **Firestore Database** → crea la BD → pega el contenido de [`firestore.rules`](./firestore.rules) → **Publish**.
4. **Project settings → Tus apps** → descarga la configuración:
   - `google-services.json` → `Assets/`
   - `google-services-desktop.json` → `Assets/StreamingAssets/`
5. *(Recomendado)* Restringe la API key en Google Cloud Console → *API restrictions* → Cloud Firestore API + Identity Toolkit API + Firebase Installations API.

### Controles

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

### Seguridad

- **Este repositorio NO incluye credenciales de Firebase** — `google-services*.json` están excluidos con `.gitignore` (mantenidos solo en local). Si haces *fork*, añade **tu propia** configuración y nunca la subas.
- [`firestore.rules`](./firestore.rules) publicadas:
  - `players` — cada jugador solo lee/escribe su propio documento; el list es necesario para la unicidad de alias.
  - `telemetry` — solo creación; jamás edición ni borrado.
  - `scores` — lectura pública (Top-10); escritura solo del dueño; borrados denegados.

### ¿Qué se puede construir a partir de este framework?

La demo es solo la prueba de concepto: el framework es una **base reutilizable** sobre la que se pueden montar sistemas más avanzados — cada uno con cimientos que **ya existen en el código**:

**🎮 Nuevos juegos y géneros**
- **Terror / supervivencia** — el DDA ya regula la tensión: escalar spawn, sonido y luces según frustración (`0.8`) o aburrimiento (`1.5`) en vez de solo velocidad/visión.
- **Serious games y educación** — la telemetría silente mide rendimiento real → contenido que se adapta al aprendizaje (misma línea que Carrión-León et al., 2025, alineado a ISO/IEC 25010).
- **Mobile para redes inestables** — el Offline-First ya está probado con el caso peruano (62.7% de hogares): runner/puzzle que nunca se bloquea por red.
- **Roguelike procedural** — ya existen semillas y spawner (`totalProceduralRoutes` en la telemetría): niveles infinitos con el DDA equilibrando la dificultad.

**🌐 Multijugador y servicios (LiveOps)**
- **Coop asíncrono** — dos jugadores dejan rastro (récords, telemetría) → desafíos "supera el registro de X" usando Firestore como mediador, **sin servidores dedicados** *(multiusuario síncrono/MMO queda fuera del alcance de la tesis, declarado)*.
- **Cross-device save** — subir/bajar la partida entre PC y móvil: punto de partida natural para los **delta-CRDTs completos**.
- **LiveOps / Remote Config** — ajustar factores DDA, eventos y contenido desde la nube **sin actualizar el build** (extensión directa de `CloudService`).
- **Temporadas y ligas** — el ranking categorizado ya existe → clasificatorias, temporadas y recompensas.

**🧠 IA más avanzada**
- **ML-Agents** — los ~40 campos de telemetría por sesión son un **dataset listo** para entrenar políticas y combinarlas con los Behavior Trees deterministas.
- **Utility AI / GOAP** — rellenar o reemplazar hojas del árbol por planificación utilitaria para metas complejas.
- **Squads multiagente** — enemigos con roles (tanque, distracción, flanqueo) sobre el motor `Node/` ya desacoplado (Conclusión 4 de la tesis).
- **Modelo de jugador** — clustering de estilos de juego desde la telemetría: personalización más allá de los 3 estados actuales (Aburrimiento/Flow/Frustración).

**📊 Datos y plataformas**
- **Dashboard de analítica** — heatmaps de muerte, curvas de aprendizaje y retención: los datos **ya fluyen** a Firestore; solo falta visualizarlos.
- **Framework de A/B testing** — dos variantes del DDA comparadas con la misma pipeline estadística de la tesis (Shapiro-Wilk → Wilcoxon).
- **Game Backend como servicio** — auth + guardado + ranking + analítica **replicables** en otros proyectos: el verdadero valor de "framework".

> Todas las extensiones respetan los **factores DDA declarados (1.5 / 1.0 / 0.8)** y el principio Offline-First: el local sigue siendo la fuente de verdad.

### Acerca de la investigación

> **Tesis de grado** — *Framework 3D con arquitectura Offline-First e Inteligencia Artificial adaptativa para la experiencia del jugador en videojuegos.*
> **Autor:** Marcelo Paz ([@mpaz18167](https://github.com/mpaz18167)) · **Institución:** Universidad Tecnológica del Perú · **Lima, 2026** — **aprobada en noviembre de 2026.**
>
> **Método ágil:** SCRUM + PMI-CPMAI. **Declaración de uso de IA:** Google NotebookLM empleado únicamente para búsqueda y proceso de literatura; redacción y verificación humana.
>
> Fuera de alcance (declarado): multijugador/MMO, estética/artes y pruebas de estrés de red a gran escala.
