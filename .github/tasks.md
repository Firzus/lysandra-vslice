## ✅ Sprint 0 – Architecture & Pipeline Robuste

| Tâche                                                                                        | Priorité | Dépendances | Critère de validation                      |
| -------------------------------------------------------------------------------------------- | -------- | ----------- | ------------------------------------------ |
| Configurer `SignalBus` global avec monitoring performance                                    | Haute    | -           | SignalBus opérationnel, monitoring visible |
| Implémenter `ServiceLocator` pattern centralisé                                              | Haute    | -           | Services accessibles via ServiceLocator    |
| Ajouter `DebugOverlay` avancé : FPS, memory, drawcalls, currentState, stamina, netcode stats | Haute    | -           | Overlay affiche toutes les infos           |
| Intégrer `FSMViewer` Editor avec visualisation runtime et historique de transitions          | Moyenne  | -           | FSMViewer fonctionnel en Editor            |
| Créer `WorldStreamer` avec gestion LOD et occlusion culling                                  | Haute    | -           | Streaming de scènes, LOD et culling testés |
| Implémenter `MemoryManager` avec profiling et garbage collection optimisé                    | Moyenne  | -           | Profiling mémoire visible, GC optimisé     |
| Préparer le `PlayableDirectorManager` avec timeline blending et support localization         | Basse    | -           | Blending timeline et localisations testés  |
| Ajouter pipeline GitHub Actions avec cache                                                   | Haute    | -           | Pipeline CI fonctionne, cache actif        |
| Configurer Addressables : groupe `VS_FalaisesAether`                                         | Haute    | -           | Groupe Addressables créé, assets chargés   |
| Setup `Continuous Profiling` dans CI/CD                                                      | Moyenne  | -           | Profiling auto dans pipeline               |
| Mettre en place l'architecture `DOTS` pour les systèmes à haute performance                  | Basse    | -           | Système DOTS prototype prêt                |

---

## 🚀 Sprint 1 – Locomotion & Persistence

| Tâche                                                                                | Priorité | Dépendances           | Critère de validation                                 |
| ------------------------------------------------------------------------------------ | -------- | --------------------- | ----------------------------------------------------- |
| Setup `PlayerController` (inputs, ref FSM) avec prediction system                    | Haute    | -                     | Le joueur peut se déplacer, inputs mappés, FSM réagit |
| Implémenter `DashState` avec input buffering & cancelation                           | Haute    | PlayerController      | Dash responsive, cancel possible, buffer testé        |
| Créer `JumpState` + `DoubleJumpState` avec physics layering                          | Haute    | PlayerController      | Sauts fonctionnels, double saut, collisions correctes |
| Ajouter `ClimbState` avec IK hands & procedural animation                            | Moyenne  | PlayerController      | Grimper possible, mains suivent surface               |
| Implémenter `GlideState` avec airflow simulation                                     | Moyenne  | PlayerController      | Plané fluide, feedback airflow visible                |
| Créer `AnimationRiggingLayer` pour animations procédurales                           | Moyenne  | PlayerController      | Animation procédurale visible sur le perso            |
| Implémenter `FootIK` et `LookAtIK` adaptatifs                                        | Basse    | AnimationRiggingLayer | Pieds et regard s’adaptent au sol/cible               |
| Créer `CheckpointManager` avec serialization versioning                              | Haute    | -                     | Checkpoints créés, sauvegarde/restaure état           |
| Intégrer système `LocalSaveProvider` + cloud sync                                    | Haute    | CheckpointManager     | Sauvegarde locale et cloud fonctionnelles             |
| Ajouter cloud save conflict resolution                                               | Moyenne  | LocalSaveProvider     | Conflits détectés et résolus                          |
| Créer tool `TeleportToCheckpoint` en Editor avec visualisation des états sauvegardés | Basse    | CheckpointManager     | Outil Editor fonctionnel, visualisation OK            |
| Setup `Crash Analytics` et rapport d'erreurs                                         | Basse    | -                     | Crashs remontés, rapports générés                     |

---

## ⚔️ Sprint 2 – Combat & HUD

| Tâche                                                  | Priorité | Dépendances      | Critère de validation                    |
| ------------------------------------------------------ | -------- | ---------------- | ---------------------------------------- |
| Créer `CombatState` avec FSM dédiée                    | Haute    | PlayerController | CombatState intégré à la FSM             |
| Ajouter `AttackConfig` en SO (damage, anim, FX)        | Haute    | CombatState      | Attaques configurables via SO            |
| Implémenter Light Combo 1→2→3                          | Haute    | AttackConfig     | Combo fluide, transitions correctes      |
| Ajouter Heavy Attack (input hold + anim)               | Moyenne  | AttackConfig     | Heavy attack déclenchée par input hold   |
| Implémenter `DodgeState` avec iFrames                  | Haute    | PlayerController | Dodge avec iFrames, feedback visuel      |
| Ajouter `CounterWindow` après une esquive réussie      | Moyenne  | DodgeState       | Counter possible après dodge             |
| Intégrer `StaminaSystem` (consommation + régénération) | Haute    | PlayerController | Stamina visible, consommée et régénérée  |
| Créer HUD : PV, Stamina, Résonance, Lock-On            | Haute    | StaminaSystem    | HUD affiche toutes les infos             |
| Connecter UIManager + affichage dynamique              | Haute    | HUD              | UIManager contrôle l’affichage dynamique |

---

## 🤖 Sprint 3 – IA & Polish

| Tâche                                           | Priorité | Dépendances   | Critère de validation                      |
| ----------------------------------------------- | -------- | ------------- | ------------------------------------------ |
| Créer `BT_Altéré` simple (patrouille + attaque) | Haute    | -             | Ennemi patrouille et attaque               |
| Créer `BT_Gardien` (mêlée agressif)             | Moyenne  | BT_Altéré     | Gardien attaque en mêlée                   |
| Ajouter prefab `Écho` avec phase unique         | Basse    | -             | Prefab Écho présent et jouable             |
| Lier `EnemyStats` via ScriptableObjects         | Moyenne  | BT_Altéré     | Stats modifiables via SO                   |
| Ajouter SFX : dash, attaque, hit, UI confirm    | Basse    | -             | SFX audibles sur actions                   |
| Ajouter VFX : slash trail, dash burst           | Basse    | -             | VFX visibles sur actions                   |
| QA playtest complet de la zone                  | Haute    | Tous systèmes | Playtest réalisé, feedback collecté        |
| Fix bugs locomotion/collision/combat            | Haute    | QA playtest   | Bugs critiques corrigés                    |
| Activer tool de warp & debug (dev build only)   | Basse    | -             | Outil accessible en dev build              |
| Setup Timeline intro/outro + trigger caméra     | Basse    | -             | Timeline intro/outro jouée, caméra trigger |

---

> Ce backlog est organisé par priorité, dépendances et critères de validation pour faciliter le pilotage du projet et la livraison d’un vertical slice jouable.
