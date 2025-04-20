# Lysandra – Action-RPG narratif sous Unity

## 🎮 Présentation

**Lysandra** est un jeu d’action-aventure narratif en monde semi-ouvert, développé avec **Unity 6 URP**. Inspiré par _Genshin Impact_, _Zelda: BotW_ et _Nier Automata_, il propose :

- Une **exploration fluide** et verticale
- Des **combats stylisés** basés sur le timing et la réactivité
- Un univers **richement narré**, mêlant mystère, mémoire et rédemption
- Une structure modulaire pensée pour **l’évolutivité** et la **collaboration en équipe**

Le projet est conçu dès sa base pour anticiper des extensions multijoueur, du contenu en ligne et un pipeline de production robuste.

---

## 🛠️ Stack technique

| Catégorie                 | Détails                                                                                    |
| ------------------------- | ------------------------------------------------------------------------------------------ |
| **Moteur**                | Unity 6 LTS (URP)                                                                          |
| **Langage**               | C# (orienté Clean Architecture, data-driven)                                               |
| **Systèmes clés**         | `StateMachine<T>`, `CombatSystem`, `SaveSystem`, `WorldStreamer`, `UIManager`, `SignalBus` |
| **UI**                    | UI Toolkit + TextMeshPro                                                                   |
| **FX**                    | Shader Graph, VFX Graph, Timeline                                                          |
| **CI/CD**                 | GitHub Actions (build + cache Library/ optimisé)                                           |
| **Sauvegarde**            | Locale (JSON) + Cloud (prévu via Supabase)                                                 |
| **Multijoueur (à venir)** | Unity Netcode + Unity Relay                                                                |
| **Versioning**            | Git + GitHub, structure modulaire par domaine                                              |

---

## 🧠 Pour les IA et contributeurs

Pour comprendre rapidement la vision, les objectifs techniques et créatifs du projet, merci de consulter ces documents internes :

- [Instructions](Instructions.md)
  ➤ Architecture technique, outils, best practices, conventions
- [Game Design Document](https://lprieu.notion.site/lysandra-gdd?pvs=4)
  ➤ Univers, gameplay, mécaniques, progression, DA
- [Tasks](Tasks.md) _(ou lien vers GitHub Projects si applicable)_
  ➤ Tâches à faire, état d’avancement, priorités actuelles
