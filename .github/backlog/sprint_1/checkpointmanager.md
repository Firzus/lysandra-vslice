# CheckpointManager – Sauvegarde

## Objectif

Permettre la création, la sauvegarde et la restauration de checkpoints avec gestion de version.

## Sous-tâches

- [ ] Créer le script `CheckpointManager.cs`
- [ ] Définir la structure de données de checkpoint (position, état FSM, stats, etc.)
- [ ] Implémenter la serialization/deserialization JSON
- [ ] Gérer la version des checkpoints
- [ ] Intégrer la sauvegarde/restauration dans le flow de jeu
- [ ] Tester la robustesse (reload, edge cases)

## Critères d’acceptation

- Checkpoints créés, sauvegarde/restaure état

## Dépendances

- PlayerController opérationnel
