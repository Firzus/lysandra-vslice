x## ✅ Sprint 0 – Architecture & Pipeline Robuste

- [ ] Configurer `SignalBus` global avec monitoring performance – **Core**
- [ ] Implémenter `ServiceLocator` pattern centralisé – **Core**
- [ ] Ajouter `DebugOverlay` avancé : FPS, memory, drawcalls, currentState, stamina, netcode stats – **Tools**
- [ ] Intégrer `FSMViewer` Editor avec visualisation runtime et historique de transitions – **Tools**
- [ ] Créer `WorldStreamer` avec gestion LOD et occlusion culling – **Core**
- [ ] Implémenter `MemoryManager` avec profiling et garbage collection optimisé – **Performance**
- [ ] Préparer le `PlayableDirectorManager` avec timeline blending et support localization – **Cinematics**
- [x] Ajouter pipeline GitHub Actions avec cache – **Pipeline**
- [x] Configurer Addressables : groupe `VS_FalaisesAether` – **Pipeline**
- [ ] Setup `Continuous Profiling` dans CI/CD – **Pipeline**
- [ ] Mettre en place l'architecture `DOTS` pour les systèmes à haute performance – **Architecture**

---

## 🚀 Sprint 1 – Locomotion & Persistence

- [ ] Implémenter `DashState` avec input buffering & cancelation – **Movement**
- [ ] Créer `JumpState` + `DoubleJumpState` avec physics layering – **Movement**
- [ ] Ajouter `ClimbState` avec IK hands & procedural animation – **Movement**
- [ ] Implémenter `GlideState` avec airflow simulation – **Movement**
- [ ] Setup `PlayerController` (inputs, ref FSM) avec prediction system – **Movement**
- [ ] Créer `AnimationRiggingLayer` pour animations procédurales – **Animation**
- [ ] Implémenter `FootIK` et `LookAtIK` adaptatifs – **Animation**
- [ ] Créer `CheckpointManager` avec serialization versioning – **Save**
- [ ] Intégrer système `LocalSaveProvider` + cloud sync – **Save**
- [ ] Ajouter cloud save conflict resolution – **Save**
- [ ] Créer tool `TeleportToCheckpoint` en Editor avec visualisation des états sauvegardés – **Tools**
- [ ] Setup `Crash Analytics` et rapport d'erreurs – **Tech**

---

## ⚔️ Sprint 2 – Combat & HUD

- [ ] Créer `CombatState` avec FSM dédiée – **Combat**
- [ ] Ajouter `AttackConfig` en SO (damage, anim, FX) – **Combat**
- [ ] Implémenter Light Combo 1→2→3 – **Combat**
- [ ] Ajouter Heavy Attack (input hold + anim) – **Combat**
- [ ] Implémenter `DodgeState` avec iFrames – **Combat**
- [ ] Ajouter `CounterWindow` après une esquive réussie – **Combat**
- [ ] Intégrer `StaminaSystem` (consommation + régénération) – **Combat**
- [ ] Créer HUD : PV, Stamina, Résonance, Lock-On – **UI**
- [ ] Connecter UIManager + affichage dynamique – **UI**

---

## 🤖 Sprint 3 – IA & Polish

- [ ] Créer `BT_Altéré` simple (patrouille + attaque) – **AI**
- [ ] Créer `BT_Gardien` (mêlée agressif) – **AI**
- [ ] Ajouter prefab `Écho` avec phase unique – **AI**
- [ ] Lier `EnemyStats` via ScriptableObjects – **AI**
- [ ] Ajouter SFX : dash, attaque, hit, UI confirm – **VFX/SFX**
- [ ] Ajouter VFX : slash trail, dash burst – **VFX/SFX**
- [ ] QA playtest complet de la zone – **QA**
- [ ] Fix bugs locomotion/collision/combat – **QA**
- [ ] Activer tool de warp & debug (dev build only) – **Tools**
- [ ] Setup Timeline intro/outro + trigger caméra – **Cinematics**
