# LocalSaveProvider & Cloud Sync – Sauvegarde

## Objectif

Permettre la sauvegarde locale et la synchronisation cloud des données de jeu.

## Sous-tâches

- [ ] Créer le script `LocalSaveProvider.cs`
- [ ] Implémenter la sauvegarde/chargement JSON sur disque
- [ ] Créer le script `CloudSaveProvider.cs` (Supabase ou mock)
- [ ] Implémenter la synchronisation cloud (upload/download, REST API)
- [ ] Gérer la sélection dynamique du provider (offline/online)
- [ ] Tester la robustesse (déconnexion, conflits, multi-device)

## Critères d’acceptation

- Sauvegarde locale et cloud fonctionnelles

## Dépendances

- CheckpointManager opérationnel
