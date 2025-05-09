# Cloud Save Conflict Resolution – Sauvegarde

## Objectif

Gérer les conflits de sauvegarde entre plusieurs devices ou sessions cloud.

## Sous-tâches

- [ ] Définir les scénarios de conflit (timestamp, version, device)
- [ ] Implémenter la détection de conflit lors de la synchronisation
- [ ] Proposer une résolution automatique (merge, priorité, rollback)
- [ ] Ajouter une UI de résolution manuelle (optionnel)
- [ ] Tester les cas limites (sauvegarde offline, multi-device)

## Critères d’acceptation

- Conflits détectés et résolus

## Dépendances

- LocalSaveProvider opérationnel
