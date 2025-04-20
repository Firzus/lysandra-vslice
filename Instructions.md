# Instructions - Lysandra

## Présentation du projet

**Lysandra** est un jeu d’action-aventure narratif en monde semi-ouvert développé avec **Unity 6**.

Inspiré de titres comme _Genshin Impact_, _Zelda: BotW_ et _Nier Automata_, le projet combine :

- Exploration fluide
- Systèmes de mouvement complexes (escalade, glissade, dash, etc.)
- Combats stylisés avec une emphase sur la chorégraphie
- Composantes RPG légères (évolution, compétences)
- Narration environnementale et cinématique

Le but est de fournir un socle **solide, modulaire et élégant** pour prototyper puis produire un jeu ambitieux et fluide.

---

## Objectifs du projet

- Créer un **système de mouvement riche et dynamique**, inspiré de Genshin (Idle → Walk → Run → Sprint → Dash → Jump → Glide)
- Mettre en place une **architecture de gameplay réutilisable et scalable**, orientée component-driven et state-driven
- Implémenter un **système de combat stylisé** à la fois accessible et profond (light attack, heavy, combo, reaction, cancel)
- Intégrer des outils d’édition visuelle pour fluidifier le pipeline de création de contenu (animations, effets, cinématiques)
- Concevoir un **système de streaming de monde fluide**, pour supporter l’exploration dans un monde ouvert découpé en zones
- Gérer la **sauvegarde des données localement puis dans le cloud**, avec système d’authentification
- Préparer un système de **coopératif en ligne avec Unity Relay + Netcode**
- Favoriser une organisation **modulaire, clean-code & data-driven**, adaptée au travail en équipe et à la scalabilité
- **Anticiper des extensions AAA** : stack FSM, systèmes imbriqués, débogage visuel, gestion temps, localisation, etc.

---

## Outils & Technologies

| Catégorie        | Outils utilisés                                       |
| ---------------- | ----------------------------------------------------- |
| Moteur           | **Unity 6** (LTS)                                     |
| Prototypage      | **ProBuilder 6**                                      |
| Caméra           | **Cinemachine 3** (FreeLook, virtual blends)          |
| Shader           | **Shader Graph**                                      |
| FX               | **Visual Effect Graph**, **Post-Processing (URP)**    |
| Cinématiques     | **Timeline** + **PlayableDirector**                   |
| Input            | **Input System (New)**                                |
| IA               | Unity **Behavior Trees**                              |
| FSM              | Custom `StateMachine<T>` System                       |
| FSM+             | Support future de **Hierarchical FSM / State Stack**  |
| Dialogue         | Dialogue UI + Timeline intégrée (à venir)             |
| Scène & World    | **Multi-scène additive streaming system**             |
| Persistence      | Scène `Persistent` + `DontDestroyOnLoad`              |
| Versioning       | **Git / GitHub** (branch, PR, CI/CD future)           |
| UI / HUD         | **UI Toolkit** (menus) + **TextMeshPro** (HUD)        |
| Gestion UI       | **UIManager** centralisé                              |
| Sauvegarde Cloud | **Supabase (auth, DB, stockage JSON)**                |
| Coopératif       | **Netcode for GameObjects + Unity Relay** (plus tard) |
| Temps & SlowMo   | Système à venir : Pause, HitStop, BulletTime          |
| Loading assets   | Support futur : **Unity Addressables**                |
| Localisation     | Structure prévue pour UI multi-langue                 |
| Audio Routing    | Architecture `AudioManager` modulaire (à venir)       |

---

## Architecture du projet

### Structure des dossiers

- **Scripts/**
  - `Core/` – FSM, Interfaces, Services, Signals, Managers
  - `Player/` – PlayerController, Input, FSM states, Configs
  - `Combat/` – States, Abilities, ReactionSystem, HitResolver
  - `Movement/` – Locomotion, Air, Climb, Dash, Glide, WallRun
  - `Abilities/` – Compétences spécifiques (Dash, Blink, Heal, etc.)
  - `UI/` – UIManager, Menus, HUD, Dialogue, Transitions
  - `World/` – WorldStreamer, ChunkLoader, EnvironmentInteraction
  - `DevTools/` – DebugOverlay, FSMViewer, TeleportTool, SceneDebugger
- **Data/**
  - `ScriptableObjects/` – Stats, AnimConfig, Abilities, Characters
  - `Resources/` – Prefabs ou fichiers dynamiques référencés par SO
- **Art/**
  - `Models/` – Meshes 3D (Personnages, props, environnement)
  - `Textures/` – Diffuse, Normal, Mask
  - `VFX/` – FX visuels (slash, dash, UIFX)
- **Audio/** – Musiques, SFX, dialogues, banks
- **Animations/** – Controllers, Timeline, BlendTrees, Events
- **Prefabs/** – Tous les objets du monde (players, ennemis, interactables)
- **Scenes/**
  - `Persistent/` – Système central (managers, UI, etc.)
  - `World_*/` – Scènes de zones ouvertes (additive streaming)
  - `Test/` – Scènes de prototypage ou démo technique

---

### Architecture Gameplay & Monde

- Utilisation de **`StateMachine<T>`** pour mouvement & combat
- Préparation d’un **`StateStack<T>`** (à venir) pour superpositions (ex: Glide + Aim)
- Support possible pour FSM hiérarchique si besoin
- **Abilities modulaires** injectées par SO ou code (dash, planage, etc.)
- Systèmes découplés par **EventChannel** ou **SignalBus** (architecture Event-driven)
- `MovementSystem`, `CombatSystem`, `StaminaSystem` tous isolés et testables
- Monde découpé en **scènes additives**, chargées via `WorldStreamer`
- `Addressables` prévus pour assets lourds dynamiques
- `DontDestroyOnLoad` pour Player, GameManager, UIManager
- Structure `TimeManager` (pause / slowmo / bulletTime) prévue

---

### Préparation du Multijoueur Coopératif (Netcode + Relay)

Lysandra est conçu pour **accueillir un mode coop à 2–4 joueurs** via **Unity Relay + Netcode for GameObjects**, comme dans Genshin Impact.

Objectif : ne pas l’implémenter tout de suite, mais **préparer l’architecture dès maintenant**.

### Architecture prévue :

- `PlayerManager` : liste des joueurs (locaux + distants)
- `LocalPlayer` : séparé, suivi caméra et UI
- `NetworkObject` intégré dans le prefab Player
- `NetworkManager` inactif par défaut (testable plus tard)
- Tous les systèmes conçus comme "Multiplayer-Aware" (pas de singleton rigide)
- Préparation d’un `MultiplayerAdapter` pour isoler les appels réseau (`CmdDash`, `RpcHit`, etc.)
- Prévision d’un système de **lobby avec code d’invitation**
- `InputReader` découpé pour facilement le répliquer ou désactiver à distance

---

## Système de sauvegarde

### Objectif :

Permettre une **sauvegarde des données fiable, évolutive et multiplateforme**, d’abord **locale en JSON**, avec une structure prête à accueillir un système cloud à base de **Supabase** (auth, DB, stockage JSON) pour la suite.

---

### Étapes techniques mises en place :

- `SaveData` → structure JSON sérialisée contenant :
  - position joueur
  - état de la FSM
  - stamina, stats
  - inventaire (List<string>)
  - données du monde (zone visitées, objets ouverts…)
  - version de la sauvegarde
- Interface `ISaveProvider` avec deux implémentations :
  - `LocalSaveProvider` : sauvegarde sur disque (`Application.persistentDataPath`)
  - `SupabaseSaveProvider` : POST/GET via REST API Supabase (à venir)
- `SaveManager` ou `GameSessionManager` choisit dynamiquement le bon provider :
  - Mode offline : local
  - Mode connecté : cloud Supabase

---

### Préparation du cloud (Supabase)

- Supabase utilisé pour :
  - Authentification (email/password, token)
  - Stockage PostgreSQL (`save_data` avec JSONB)
  - Synchronisation multi-device
- Communication avec Unity via `UnityWebRequest` (REST API)
- Sauvegardes au format JSON (versionnées)

---

### Avantages

| Étape     | Bénéfice                                |
| --------- | --------------------------------------- |
| Locale    | Simple, rapide, fiable, testable        |
| Interface | Extensible (slot multiple, cloud-ready) |
| Supabase  | Auth + DB + JSON intégré                |
| Future    | Multijoueur, shop, multi-device         |

---

## Extrait du GDD analysé

**Inspirations clés** :

- Mouvement fluide et vertical (grimper, glisser, planage)
- Combat stylisé, réactif (cancel, hit stop, dash iframe)
- Exploration dynamique avec loading seamless
- Mécaniques propres à chaque personnage (Ayaka, Sayu…)
- Cinématiques in-game déclenchées (finishers, story beats)

**Systèmes notables à implémenter** :

- `StaminaSystem` central
- `WallRun`, `Climb`, `Glide`, `Swim`, `Fall`, `LandingState`...
- `DebugOverlay` avec infos runtime (stamina, currentState, FPS)
- `UIManager` unifié : HUD, menus, transitions, dialogues
- `WorldStreamer` (multi scène)

---

## Best Practices (dev & architecture)

- `FixedTick()` pour la physique, `Tick()` pour la logique
- Aucune logique bloquée dans l’input → utiliser des `Events` ou `Command Layer`
- Favoriser les **ScriptableObjects** pour stats, config, feedbacks
- FSM pilotée uniquement par logique, pas par animation ni input direct
- Pas de singleton rigide (ex: `Player.Instance`) : `PlayerManager.LocalPlayer`
- Pas de hardcode dans les states : tout injectable / override
- `currentStateName` toujours exposé en debug (dev HUD)
- Animation pilotée via `PlayableDirector` ou `AnimationEventBus`
- `SignalBus` prévu pour décorréler les systèmes (combat, audio, UI...)
- `AudioManager` modulaire à prévoir (bus music, UI, sfx, voice)

---

## Liens utiles

- [Lysandra – Overview](https://lprieu.notion.site/lysandra?pvs=4)
- [Lysandra – Game Design Document (GDD)](https://lprieu.notion.site/lysandra-gdd?pvs=4)
- [Lysandra – Data / Configs](https://lprieu.notion.site/lysandra-data?pvs=4)

---

## Résumé final

> Ce document sert de référence complète pour tout assistant IA, développeur contributeur ou membre de l'équipe rejoignant le projet Lysandra.

Il définit :

- Les objectifs de design et d’ambition
- L’infrastructure technique (offline & cloud)
- Les systèmes critiques à développer (FSM, streaming, UI, save, coop)
- Les outils de production utilisés (Unity, Supabase, GitHub…)
- Les bonnes pratiques de développement (testabilité, découplage, clean code)

---

## Profil du projet

- **Type** : A-RPG solo avec option coop
- **Équipe actuelle** : 1 développeur principal (extensible)
- **Stack backend prévue** : Supabase (auth, DB, sync)
- **Multijoueur** : Unity Relay + Netcode for GameObjects (plus tard)
- **Scalabilité prévue** : Oui (cloud save, shop, extension online)

---

## À venir (Roadmap)

- Implémentation de StateStack ou Hierarchical FSM
- Setup `SignalBus` global (event-driven)
- Base de `TimeManager` (pause, hitstop, bulletTime)
- UIManager : centralisation des panels + feedbacks
- Animation System + `PlayableDirector` hooks
- Coop prototyping : Netcode + Unity Relay + Lobby
- Intégration Auth Supabase + UI login/register
- Addressables pour world zones + skins
- DevTools (debug overlay, warp, FSM viewer)

---

> Document maintenu et validé par le Lead Dev & Copilot IA.
>
> Dernière mise à jour : **Avril 2025**
