using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Project.Core.Signals;

namespace Project.Editor
{
    /// <summary>
    /// Fenêtre d'éditeur pour visualiser et déboguer le système SignalBus
    /// </summary>
    public class SignalBusDebugWindow : EditorWindow
    {
        // Référence au SignalBus
        private SignalBus _signalBus;

        // Liste des ScriptableObjects de type SignalChannel trouvés
        private List<SignalChannelBase> _allChannels = new List<SignalChannelBase>();

        // Filtres et options d'affichage
        private string _searchFilter = "";
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Overview", "Channels", "Performance", "Recent Activity" };

        private Dictionary<int, bool> _expandedStackTraces = new Dictionary<int, bool>();

        [MenuItem("Tools/Debug/Signal Bus Monitor")] // Chemin modifié
        public static void ShowWindow()
        {
            var window = GetWindow<SignalBusDebugWindow>("SignalBus Monitor");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            // Rechercher tous les ScriptableObjects de type SignalChannel
            RefreshChannelsList();

            // S'assurer que la fenêtre se rafraîchit régulièrement
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // Rafraîchir la fenêtre si le monitoring en temps réel est activé
            if (_autoRefresh && _selectedTab == 3) // Tab "Recent Activity"
            {
                Repaint();
            }
        }

        private void RefreshChannelsList()
        {
            // Rechercher tous les canaux de signaux dans le projet
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");
            _allChannels.Clear();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset is SignalChannelBase channel)
                {
                    _allChannels.Add(channel);
                }
            }
        }

        private void OnGUI()
        {
            // En-tête et outils généraux
            DrawToolbar();

            // Chercher le SignalBus dans la scène s'il n'est pas déjà référencé
            if (_signalBus == null)
            {
                _signalBus = FindFirstObjectByType<SignalBus>();
            }

            // Indicateur si le SignalBus est trouvé
            if (_signalBus != null)
            {
                EditorGUILayout.HelpBox("SignalBus trouvé et connecté.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("SignalBus non trouvé dans la scène. Certaines fonctionnalités sont limitées.", MessageType.Warning);
                if (GUILayout.Button("Créer un SignalBus"))
                {
                    CreateSignalBusGameObject();
                }
            }

            // Afficher l'interface correspondant à l'onglet sélectionné
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0: // Overview
                    DrawOverview();
                    break;
                case 1: // Channels
                    DrawChannelsTab();
                    break;
                case 2: // Performance
                    DrawPerformanceTab();
                    break;
                case 3: // Recent Activity
                    DrawRecentActivityTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            // Barre d'outils principale
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Bouton Rafraîchir
            if (GUILayout.Button("Rafraîchir", EditorStyles.toolbarButton))
            {
                RefreshChannelsList();
            }

            // Toggle Auto Refresh
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);

            // Espace flexible
            GUILayout.FlexibleSpace();

            // Bouton Reset Stats - Toujours présent mais désactivé si _signalBus est null
            EditorGUI.BeginDisabledGroup(_signalBus == null);
            if (GUILayout.Button("Reset Stats", EditorStyles.toolbarButton))
            {
                SignalPerformanceTracker.Reset();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            // Barre de recherche (séparée)
            GUILayout.BeginHorizontal();

            GUILayout.Label("Filtre:", GUILayout.Width(40));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                _searchFilter = "";
                GUI.FocusControl(null);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawOverview()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Vue d'ensemble du système SignalBus", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            using (new EditorGUI.DisabledScope(_signalBus == null))
            {
                EditorGUILayout.LabelField($"Canaux enregistrés: {_allChannels.Count}");
                EditorGUILayout.LabelField($"Total des signaux émis: {SignalPerformanceTracker.TotalSignalsEmitted}");
                EditorGUILayout.LabelField($"Types de signaux: {SignalPerformanceTracker.SignalStatsCollection.Count}");

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Informations sur le système:", EditorStyles.boldLabel);

                EditorGUILayout.HelpBox(
                    "Le système SignalBus permet une communication découplée entre les différents systèmes du jeu.\n\n" +
                    "Utilisez les onglets pour explorer les canaux disponibles, surveiller les performances et l'activité récente.",
                    MessageType.Info);

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Actions rapides:", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Créer un nouveau canal"))
                    {
                        ShowNewChannelPopup();
                    }

                    if (GUILayout.Button("Générer rapport de performance"))
                    {
                        Debug.Log(SignalPerformanceTracker.GenerateReport(true));
                    }
                }
            }
        }

        private void DrawChannelsTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Canaux de signaux dans le projet", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            if (GUILayout.Button("Rafraîchir la liste des canaux"))
            {
                RefreshChannelsList();
            }

            EditorGUILayout.Space(5);

            if (_allChannels.Count == 0)
            {
                EditorGUILayout.HelpBox("Aucun canal de signal trouvé dans le projet.", MessageType.Info);

                if (GUILayout.Button("Créer des canaux pour les signaux communs"))
                {
                    CreateCommonSignalChannels();
                }
            }
            else
            {
                // Afficher la liste des canaux
                foreach (var channel in _allChannels)
                {
                    // Appliquer le filtre si nécessaire
                    if (!string.IsNullOrEmpty(_searchFilter) &&
                        !channel.name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) &&
                        !channel.Category.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        // En-tête du canal avec un style coloré
                        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
                        headerStyle.normal.textColor = EditorGUIUtility.isProSkin ?
                            channel.ChannelColor :
                            Color.Lerp(channel.ChannelColor, Color.black, 0.5f);

                        EditorGUILayout.LabelField(channel.name, headerStyle);
                        EditorGUILayout.LabelField($"Type: {channel.SignalType.Name}");
                        EditorGUILayout.LabelField($"Catégorie: {channel.Category}");

                        if (!string.IsNullOrEmpty(channel.Description))
                        {
                            EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField(channel.Description, EditorStyles.wordWrappedLabel);
                        }

                        // Actions pour ce canal
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Sélectionner"))
                            {
                                Selection.activeObject = channel;
                            }

                            if (_signalBus != null && GUILayout.Button("Émettre signal vide"))
                            {
                                EditorApplication.delayCall += () =>
                                {
                                    channel.EmitEmpty();
                                };
                            }

                            if (GUILayout.Button("Ping"))
                            {
                                EditorGUIUtility.PingObject(channel);
                            }
                        }
                    }

                    EditorGUILayout.Space(5);
                }
            }
        }

        private void DrawPerformanceTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Performance du système de signaux", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            if (SignalPerformanceTracker.TotalSignalsEmitted == 0)
            {
                EditorGUILayout.HelpBox("Aucune donnée de performance disponible. Émettez quelques signaux pour voir les statistiques.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Reset statistiques"))
            {
                SignalPerformanceTracker.Reset();
            }

            EditorGUILayout.Space(10);

            // Données de performance générales
            EditorGUILayout.LabelField($"Total des signaux émis: {SignalPerformanceTracker.TotalSignalsEmitted}");
            EditorGUILayout.LabelField($"Types de signaux: {SignalPerformanceTracker.SignalStatsCollection.Count}");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("TOP 5 des signaux les plus fréquents:", EditorStyles.boldLabel);

            var sortedByFreq = new List<SignalPerformanceTracker.SignalStats>(
                SignalPerformanceTracker.SignalStatsCollection.Values
                    .OrderByDescending(s => s.EmissionCount)
                    .Take(5));

            foreach (var stat in sortedByFreq)
            {
                DrawSignalStatBar(
                    stat.SignalType.Name,
                    stat.EmissionCount,
                    SignalPerformanceTracker.TotalSignalsEmitted,
                    $"{stat.EmissionCount} émissions, moy: {stat.AverageTimeMs:F3}ms"
                );
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("TOP 5 des signaux les plus lents:", EditorStyles.boldLabel);

            var sortedBySlowest = new List<SignalPerformanceTracker.SignalStats>(
                SignalPerformanceTracker.SignalStatsCollection.Values
                    .Where(s => s.EmissionCount > 0)
                    .OrderByDescending(s => s.AverageTimeMs)
                    .Take(5));

            foreach (var stat in sortedBySlowest)
            {
                float maxTime = 5.0f; // Max 5ms comme référence
                DrawSignalStatBar(
                    stat.SignalType.Name,
                    Mathf.Min(stat.AverageTimeMs, maxTime),
                    maxTime,
                    $"Temps: {stat.AverageTimeMs:F3}ms, {stat.EmissionCount} émissions",
                    Color.Lerp(Color.green, Color.red, Mathf.Min(stat.AverageTimeMs / 2.0f, 1.0f))
                );
            }
        }

        private void DrawRecentActivityTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Activité récente des signaux", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Activer/désactiver le tracking en temps réel
            bool trackingEnabled = SignalPerformanceTracker.TrackingEnabled;
            trackingEnabled = EditorGUILayout.Toggle("Tracking activé", trackingEnabled);
            if (trackingEnabled != SignalPerformanceTracker.TrackingEnabled)
            {
                SignalPerformanceTracker.TrackingEnabled = trackingEnabled;
            }

            if (!trackingEnabled)
            {
                EditorGUILayout.HelpBox("Le tracking des signaux est désactivé. Activez-le pour voir l'activité en temps réel.", MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            if (SignalPerformanceTracker.RecentSignals.Count == 0)
            {
                EditorGUILayout.HelpBox("Aucune activité récente enregistrée.", MessageType.Info);
                return;
            }

            // Afficher les 20 derniers signaux
            EditorGUILayout.LabelField("Derniers signaux émis:", EditorStyles.boldLabel);

            int count = 0;
            for (int i = SignalPerformanceTracker.RecentSignals.Count - 1; i >= 0; i--)
            {
                if (count >= 20) break; // Limiter à 20 signaux

                var record = SignalPerformanceTracker.RecentSignals[i];

                // Appliquer le filtre si nécessaire
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !record.SignalType.Name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                count++;

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    // Colorer les signaux lents en orange/rouge
                    var typeStyle = new GUIStyle(EditorStyles.boldLabel);
                    if (record.EmissionTimeMs > 1.0f)
                    {
                        typeStyle.normal.textColor = record.EmissionTimeMs > 5.0f ?
                            Color.red : new Color(1.0f, 0.5f, 0.0f); // Orange or Red
                    }

                    EditorGUILayout.LabelField(record.SignalType.Name, typeStyle);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Via: Channel", GUILayout.Width(200));
                        EditorGUILayout.LabelField("Time: " + record.Timestamp.ToString("HH:mm:ss"), GUILayout.Width(100));
                    }

                    EditorGUILayout.LabelField($"Listeners: {record.ListenersCount}");

                    // Identifiant unique pour ce signal dans cette session
                    int signalId = i;

                    // Initialiser l'état si nécessaire
                    if (!_expandedStackTraces.ContainsKey(signalId))
                    {
                        _expandedStackTraces[signalId] = false;
                    }

                    // Afficher la stack trace si disponible
                    if (!string.IsNullOrEmpty(record.CallStack))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(10); // Indentation

                            string buttonText = _expandedStackTraces[signalId] ? "Call Stack ▲" : "Call Stack ▼";
                            if (GUILayout.Button(buttonText, EditorStyles.miniButton))
                            {
                                // Inverser l'état du fold
                                _expandedStackTraces[signalId] = !_expandedStackTraces[signalId];
                            }
                        }

                        // Si la stack trace est dépliée, l'afficher
                        if (_expandedStackTraces[signalId])
                        {
                            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                            {
                                GUIStyle stackStyle = new GUIStyle(EditorStyles.miniLabel);
                                stackStyle.wordWrap = true;
                                stackStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

                                EditorGUILayout.LabelField(record.CallStack, stackStyle);
                            }
                        }
                    }
                }

                EditorGUILayout.Space(2);
            }
        }

        private void DrawSignalStatBar(string label, float value, float maxValue, string tooltip, Color barColor = default)
        {
            if (barColor == default)
            {
                barColor = new Color(0.2f, 0.6f, 1.0f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(150));

                Rect r = EditorGUILayout.GetControlRect(false, 18, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, r.height), new Color(0.1f, 0.1f, 0.1f));

                float fillWidth = maxValue > 0 ? (value / maxValue) * r.width : 0;
                EditorGUI.DrawRect(new Rect(r.x, r.y, fillWidth, r.height), barColor);

                // Dessiner le texte par-dessus
                var style = new GUIStyle(EditorStyles.miniLabel);
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = Color.white;
                EditorGUI.LabelField(r, tooltip, style);
            }
        }

        private void ShowNewChannelPopup()
        {
            // Créer une popup pour entrer le nom du nouveau canal
            var popup = CreateInstance<CreateSignalChannelPopup>();
            popup.ShowUtility();
        }

        private void CreateCommonSignalChannels()
        {
            // Créer des canaux pour tous les signaux communs
            CreateChannelForType(typeof(CommonSignals.GameReady), "Core");
            CreateChannelForType(typeof(CommonSignals.SceneLoadStarted), "Core");
            CreateChannelForType(typeof(CommonSignals.SceneLoadCompleted), "Core");
            CreateChannelForType(typeof(CommonSignals.GamePaused), "Core");

            CreateChannelForType(typeof(CommonSignals.PlayerStateChanged), "Player");
            CreateChannelForType(typeof(CommonSignals.PlayerStatsChanged), "Player");
            CreateChannelForType(typeof(CommonSignals.PlayerDamaged), "Player");
            CreateChannelForType(typeof(CommonSignals.PlayerDied), "Player");

            CreateChannelForType(typeof(CommonSignals.AttackStarted), "Combat");
            CreateChannelForType(typeof(CommonSignals.AttackHit), "Combat");
            CreateChannelForType(typeof(CommonSignals.HitStop), "Combat");

            CreateChannelForType(typeof(CommonSignals.ShowMessage), "UI");
            CreateChannelForType(typeof(CommonSignals.ToggleMenu), "UI");
            CreateChannelForType(typeof(CommonSignals.UpdateHealthUI), "UI");
            CreateChannelForType(typeof(CommonSignals.UpdateStaminaUI), "UI");

            CreateChannelForType(typeof(CommonSignals.LogDebugEvent), "Debug");
            CreateChannelForType(typeof(CommonSignals.RequestPerformanceReport), "Debug");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshChannelsList();
        }

        private void CreateChannelForType(Type signalType, string category)
        {
            // Vérifier si le dossier existe, sinon le créer
            string folderPath = "Assets/Data/SignalChannels";

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentPath = "Assets/Data";

                if (!AssetDatabase.IsValidFolder(parentPath))
                {
                    parentPath = "Assets";
                    AssetDatabase.CreateFolder(parentPath, "Data");
                }

                AssetDatabase.CreateFolder(parentPath, "SignalChannels");
            }

            // Créer un canal pour ce type de signal
            var channelTypeName = typeof(SignalChannel<>).MakeGenericType(signalType);
            var channel = CreateInstance(channelTypeName) as ScriptableObject;

            if (channel != null)
            {
                // Définir quelques propriétés de base via la réflexion
                var nameProperty = channelTypeName.GetProperty("Description");
                if (nameProperty != null)
                {
                    nameProperty.SetValue(channel, $"Canal pour les signaux de type {signalType.Name}");
                }

                var categoryProperty = channelTypeName.GetProperty("Category");
                if (categoryProperty != null)
                {
                    categoryProperty.SetValue(channel, category);
                }

                // Sauvegarder l'asset
                string assetPath = $"{folderPath}/{signalType.Name}Channel.asset";
                AssetDatabase.CreateAsset(channel, assetPath);
                Debug.Log($"Canal créé pour {signalType.Name} à {assetPath}");
            }
        }

        private void CreateSignalBusGameObject()
        {
            // Créer un GameObject avec le composant SignalBus
            GameObject go = new GameObject("SignalBus");
            _signalBus = go.AddComponent<SignalBus>();
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }

    /// <summary>
    /// Popup pour créer un nouveau canal de signal
    /// </summary>
    public class CreateSignalChannelPopup : EditorWindow
    {
        private string _signalTypeName = "MySignal";
        private string _category = "Custom";
        private string _description = "Description du signal";
        private Color _channelColor = Color.white;
        private bool _isValid = false;

        private void OnEnable()
        {
            titleContent = new GUIContent("Créer un canal de signal");
            minSize = new Vector2(400, 250);
            maxSize = new Vector2(400, 250);

            ValidateTypeName();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Créer un nouveau canal de signal", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Nom du type de signal:");
            _signalTypeName = EditorGUILayout.TextField(_signalTypeName);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Catégorie:");
            _category = EditorGUILayout.TextField(_category);

            EditorGUILayout.LabelField("Description:");
            _description = EditorGUILayout.TextArea(_description, GUILayout.Height(60));

            EditorGUILayout.LabelField("Couleur:");
            _channelColor = EditorGUILayout.ColorField(_channelColor);

            EditorGUILayout.Space(10);

            ValidateTypeName();

            using (new EditorGUI.DisabledScope(!_isValid))
            {
                if (GUILayout.Button("Créer"))
                {
                    CreateSignalAndChannel();
                    Close();
                }
            }

            if (!_isValid)
            {
                EditorGUILayout.HelpBox("Le nom du type doit être valide et ne pas déjà exister.", MessageType.Error);
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Annuler"))
            {
                Close();
            }
        }

        private void ValidateTypeName()
        {
            _isValid = !string.IsNullOrEmpty(_signalTypeName) &&
                      IsValidCSharpIdentifier(_signalTypeName) &&
                      !TypeAlreadyExists(_signalTypeName);
        }

        private bool IsValidCSharpIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (!char.IsLetter(name[0]) && name[0] != '_')
                return false;

            for (int i = 1; i < name.Length; i++)
            {
                if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                    return false;
            }

            return true;
        }

        private bool TypeAlreadyExists(string typeName)
        {
            // Vérifier dans les assemblies si le type existe déjà
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Any(t => t.Name == typeName);
        }

        private void CreateSignalAndChannel()
        {
            // Créer le fichier de code pour le signal
            CreateSignalTypeFile();

            // Lancer l'opération de compilation Unity
            AssetDatabase.Refresh();

            // Attendre que la compilation soit terminée pour créer le canal
            EditorApplication.delayCall += () =>
            {
                // Tenter de charger le nouveau type
                var signalType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == _signalTypeName && typeof(ISignal).IsAssignableFrom(t));

                if (signalType != null)
                {
                    CreateChannelForSignalType(signalType);
                }
                else
                {
                    Debug.LogError($"Impossible de trouver le type {_signalTypeName} après compilation.");
                }
            };
        }

        private void CreateSignalTypeFile()
        {
            // Vérifier si le dossier existe, sinon le créer
            string folderPath = "Assets/Code/Signals";

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentPath = "Assets/Code";

                if (!AssetDatabase.IsValidFolder(parentPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Code");
                }

                AssetDatabase.CreateFolder(parentPath, "Signals");
            }

            // Créer le contenu du fichier
            string code = $@"using Project.Core.Signals;

namespace Project.Signals
{{
    /// <summary>
    /// {_description}
    /// </summary>
    public struct {_signalTypeName} : ISignal
    {{
        // TODO: Ajouter des propriétés de données pour ce signal
    }}
}}";

            // Écrire le fichier
            string filePath = $"{folderPath}/{_signalTypeName}.cs";
            System.IO.File.WriteAllText(filePath, code);

            Debug.Log($"Fichier signal créé à {filePath}");
            AssetDatabase.ImportAsset(filePath);
        }

        private void CreateChannelForSignalType(Type signalType)
        {
            // Vérifier si le dossier existe, sinon le créer
            string folderPath = "Assets/Data/SignalChannels";

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentPath = "Assets/Data";

                if (!AssetDatabase.IsValidFolder(parentPath))
                {
                    AssetDatabase.CreateFolder("Assets", "Data");
                }

                AssetDatabase.CreateFolder(parentPath, "SignalChannels");
            }

            // Créer un canal pour ce type de signal
            var channelTypeName = typeof(SignalChannel<>).MakeGenericType(signalType);
            var channel = CreateInstance(channelTypeName) as ScriptableObject;

            if (channel != null)
            {
                // Définir les propriétés
                var baseChannel = channel as SignalChannelBase;

                if (baseChannel != null)
                {
                    // Utiliser la réflexion car les propriétés internes peuvent être protégées
                    var descField = typeof(SignalChannelBase).GetField("description",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                    var catField = typeof(SignalChannelBase).GetField("category",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                    var colorField = typeof(SignalChannelBase).GetField("channelColor",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                    if (descField != null) descField.SetValue(channel, _description);
                    if (catField != null) catField.SetValue(channel, _category);
                    if (colorField != null) colorField.SetValue(channel, _channelColor);
                }

                // Sauvegarder l'asset
                string assetPath = $"{folderPath}/{signalType.Name}Channel.asset";
                AssetDatabase.CreateAsset(channel, assetPath);
                Debug.Log($"Canal créé pour {signalType.Name} à {assetPath}");

                EditorGUIUtility.PingObject(channel);
                Selection.activeObject = channel;
            }
        }
    }
}