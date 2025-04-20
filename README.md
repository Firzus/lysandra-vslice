# Lysandra – Action-RPG narratif sous Unity 6

## 🎮 Vision AAA

**Lysandra** est un jeu d'action-aventure narratif en monde semi-ouvert, développé avec **Unity 6 URP**. Inspiré par _Genshin Impact_, _Zelda: BotW_ et _Nier Automata_, il propose :

- Une **exploration fluide** avec système de mouvement verticalement ambitieux (escalade, glissade, plané)
- Des **combats stylisés** avec un système d'impact, timing précis et animations procédurales
- Un **monde dynamique** avec streaming de zones, LOD avancé et occlusion culling
- Une **narration environnementale** soutenue par des cinématiques in-engine et performances d'acteurs
- Une **architecture technique évolutive** conçue pour le travail en équipe et la scalabilité

Le projet est structuré selon les standards de l'industrie AAA avec pipeline CI/CD, analytics intégrés, et support cross-platform.

---

## 🛠️ Stack technique de classe AAA

| Catégorie         | Détails                                                                                  |
| ----------------- | ---------------------------------------------------------------------------------------- |
| **Moteur**        | Unity 6 LTS (URP) avec optimisations DOTS pour les systèmes critiques                    |
| **Architecture**  | Clean Architecture, SOLID, Data-Driven Design avec CI/CD                                 |
| **Systèmes Core** | `StateMachine<T>`, `ServiceLocator`, `MemoryManager`, `Profiler`, `CrashAnalytics`       |
| **Gameplay**      | `CombatSystem`, `MovementSystem`, `AIBehaviorTrees`, `PhysicsLayer`, `InputBuffering`    |
| **Visuel**        | PBR Workflow, HDRP-ready, Shader Graph, VFX Graph, Timeline, Animation Rigging           |
| **Performances**  | Occlusion Culling, GPU Instancing, LOD Groups, Addressables, Memory Profiling            |
| **UI/UX**         | UI Toolkit, TextMeshPro, Localization, Accessibility Features, UI Animation              |
| **Pipeline**      | GitHub Actions, Automated Testing, Asset Validation, Continuous Profiling                |
| **Sauvegarde**    | Locale (JSON) + Cloud (Supabase) avec synchronisation et résolution de conflits          |
| **Multijoueur**   | Unity Netcode + Unity Relay avec prédiction/compensation de lag et netcode visualization |
| **Monitoring**    | Application Insights, Performance Monitoring, Player Metrics, Crash Reports              |

---

## 🏆 Standards de qualité

- Tous les systèmes critiques sont couverts par des **tests unitaires et d'intégration**
- Chaque PR subit une **validation automatique** via pipeline CI/CD
- **60 FPS constants** sur les plateformes cibles, mesurés via profiling continu
- Suivi des métriques de **mémoire et performance** en temps réel
- Documentation technique et artistique approfondie, générée et mise à jour automatiquement

---

## 🧠 Documentation technique

Pour comprendre rapidement la vision, les objectifs techniques et créatifs du projet, consultez ces documents internes :

- [Instructions](Instructions.md)
  ➤ Architecture technique, outils, best practices, conventions
- [Game Design Document](https://lprieu.notion.site/lysandra-gdd?pvs=4)
  ➤ Univers, gameplay, mécaniques, progression, DA
- [Tasks](Tasks.md)
  ➤ Tâches à faire, état d'avancement, priorités actuelles
- [API Documentation](https://lysandra-docs.example.com) - pas encoré implémenté
  ➤ Documentation technique auto-générée
- [Art Bible](https://lysandra-art.example.com) - pas encoré implémenté
  ➤ Direction artistique, guidelines, palettes et références

---

## 🚀 Roadmap & Planning

La production de Lysandra suit une méthodologie Agile adaptée, avec développement itératif et playtests réguliers :

- **Sprint 0** (Terminé) : Architecture technique fondamentale et pipeline
- **Sprint 1** (En cours) : Système de mouvement, sauvegarde et checkpoints
- **Sprint 2** (À venir) : Combat et feedback visuel
- **Sprint 3** (Planifié) : IA des ennemis et polish de la première zone jouable
- **Milestone 1** : Vertical Slice jouable (Zone des Falaises d'Aether)
- **[Voir planning complet](https://github.com/users/Firzus/projects/11)**

---

## 📊 Playtest & Métriques

Le développement est guidé par des données concrètes :

- Collecte de métriques joueurs via notre système d'analytics intégré
- Sessions de playtest hebdomadaires avec rapports d'analyses
- Suivi des KPI de performance technique (framerate, mémoire, chargement)
- Heatmaps de zones de jeu et analyse de progression

---

## 🌐 Vision d'expansion

Lysandra est conçu comme une plateforme évolutive :

- Base pour un live service avec mises à jour saisonnières
- Support multijoueur coopératif sans compromis sur l'expérience solo
- Infrastructure cloud robuste et scalable pour contenus additionnels et événements
