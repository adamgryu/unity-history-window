using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;

namespace Gemserk {

    [InitializeOnLoad]
    public static class SelectionHistoryInitialized {

        static SelectionHistoryInitialized() {
            // Initalize the selection listener, even before the window is opened.
            SelectionHistoryWindow.RegisterSelectionListener();
        }
    }

    public class SelectionHistoryWindow : EditorWindow, IHasCustomMenu {

        // Public Static Fields
        public const float LINE_HEIGHT = 16;
        public static bool shouldReloadPreferences = true;

        // Public Static Preference Keys
        public static readonly string HistorySizePrefKey = "Gemserk.SelectionHistory.HistorySize";
        public static readonly string HistoryAutomaticRemoveDeletedPrefKey = "Gemserk.SelectionHistory.AutomaticRemoveDeleted";
        public static readonly string HistoryAllowDuplicatedEntriesPrefKey = "Gemserk.SelectionHistory.AllowDuplicatedEntries";
        public static readonly string HistoryShowHierarchyObjectsPrefKey = "Gemserk.SelectionHistory.ShowHierarchyObjects";
        public static readonly string HistoryShowProjectViewObjectsPrefKey = "Gemserk.SelectionHistory.ShowProjectViewObjects";
        public static readonly string HistoryFavoritesPrefKey = "Gemserk.SelectionHistory.Favorites";

        // Private Static Fields
        private static SelectionHistory selectionHistory = new SelectionHistory(); // Start with a dummy instance to avoid null-refs.
        private static readonly bool debugEnabled = false;

        // Private Static Helper Properies
        private static Color hierarchyElementTextColor => new Color(0.7f, 1.0f, 0.7f);
        private static Color selectedElementTextColor => EditorGUIUtility.isProSkin ? Color.white : Color.white;
        private static Color selectedElementBackgroundColor => EditorGUIUtility.isProSkin ? new Color(0.17f, 0.36f, 0.56f) : new Color(0.17f, 0.36f, 0.56f);
        private static Color pinnedElementBackgroundColor => EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f, 0.7f) : new Color(0.1f, 0.1f, 0.1f, 0.1f);
        private static string iconPath => EditorGUIUtility.isProSkin ? "d_" : "";

        // Private Static Cached Fields
        private static bool cachedStyles = false;
        private static GUIStyle horizontalGroupStyle;
        private static GUIStyle historyLabelStyle;
        private static GUIStyle selectedHistoryLabelStyle;
        private static GUIStyle textureBackgroundStyle;
        private static GUIStyle miniButtonStyle;
        private static GUILayoutOption lineHeight = GUILayout.Height(LINE_HEIGHT);
        private static GUILayoutOption minWidth = GUILayout.MinWidth(40);

        #region Static Methods

        [MenuItem("Window/General/Selection History %#h")]
        public static void OpenWindow() {
            // Get existing open window or if none, make a new one:
            var window = EditorWindow.GetWindow<SelectionHistoryWindow>();
            window.titleContent.text = "History";
            window.Show();
        }

        public static void RegisterSelectionListener() {
            Selection.selectionChanged += SelectionRecorder;
        }

        public static void SelectionRecorder() {
            if (Selection.activeObject != null) {
                if (debugEnabled) {
                    Debug.Log("Recording new selection: " + Selection.activeObject.name);
                }

                selectionHistory = EditorTemporaryMemory.Instance.selectionHistory;
                selectionHistory.UpdateSelection(Selection.activeObject);
            }
        }

        #endregion

        // Private Cached Preferences
        private bool automaticRemoveDeleted;
        private bool allowDuplicatedEntries;
        private bool showHierarchyViewObjects;
        private bool showProjectViewObjects;
        private bool favoritesEnabled;

        // Private State
        private Vector2 historyScrollPosition;

        private void OnEnable() {
            selectionHistory = EditorTemporaryMemory.Instance.selectionHistory;
            Selection.selectionChanged += OnSelectionChanged;
            wantsMouseMove = true;
        }

        private void OnDisable() {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        // This must be called in OnGUI for whatever reason.
        private void InitializeStyles() {
            if (cachedStyles) {
                return;
            }
            cachedStyles = true;

            textureBackgroundStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };

            horizontalGroupStyle = new GUIStyle() { border = new RectOffset(), margin = new RectOffset(), padding = new RectOffset(), fixedHeight = 16f };

            miniButtonStyle = new GUIStyle(GUIStyle.none);
            miniButtonStyle.fixedHeight = LINE_HEIGHT;
            miniButtonStyle.fixedWidth = LINE_HEIGHT;

            selectedHistoryLabelStyle = new GUIStyle(GUIStyle.none);
            selectedHistoryLabelStyle.fontStyle = FontStyle.Bold;
            selectedHistoryLabelStyle.normal.textColor = Color.white;

            historyLabelStyle = new GUIStyle(GUIStyle.none);
            historyLabelStyle.fontStyle = FontStyle.Normal;
            historyLabelStyle.normal.textColor = EditorStyles.label.normal.textColor;
        }

        private void OnSelectionChanged() {
            if (selectionHistory.IsSelected(selectionHistory.GetHistoryCount() - 1)) {
                historyScrollPosition.y = float.MaxValue;
            }
            Repaint();
        }

        public void SetSelection(Object select) {
            selectionHistory.SetSelection(select);
            Selection.activeObject = select;
        }

        #region GUI

        private void OnGUI() {
            // Ensure styles are loaded.
            InitializeStyles();

            // Ensure preferences are loaded.
            if (shouldReloadPreferences) {
                selectionHistory.HistorySize = EditorPrefs.GetInt(SelectionHistoryWindow.HistorySizePrefKey, 10);
                allowDuplicatedEntries = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryAllowDuplicatedEntriesPrefKey, false);
                automaticRemoveDeleted = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryAutomaticRemoveDeletedPrefKey, true);
                showHierarchyViewObjects = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryShowHierarchyObjectsPrefKey, true);
                showProjectViewObjects = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryShowProjectViewObjectsPrefKey, true);
                favoritesEnabled = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryFavoritesPrefKey, true);
                shouldReloadPreferences = false;
            }

            // Clear any items that should not be shown.
            if (automaticRemoveDeleted) {
                selectionHistory.RemovedDestroyed();
            }
            if (!allowDuplicatedEntries) {
                selectionHistory.RemoveDuplicated();
            }

            // Draw the elements in a scroll view, favorites first.
            historyScrollPosition = EditorGUILayout.BeginScrollView(historyScrollPosition, GUILayout.ExpandHeight(true));
            if (favoritesEnabled && selectionHistory.Favorites.Count > 0) {
                DrawFavorites();
            }
            DrawHistory();
            EditorGUILayout.EndScrollView();

            // Draw a sidebar on the left to match the hierarchy view visually.
            if (favoritesEnabled) {
                var sidebar = GUILayoutUtility.GetLastRect();
                sidebar.width = LINE_HEIGHT;
                DrawColoredBox(sidebar, Color.black * 0.176f);
            }

            DrawButtorBar();
        }

        private void DrawFavorites() {
            var favorites = selectionHistory.Favorites;
            for (int i = 0; i < favorites.Count; i++) {
                DrawElement(favorites[i]);
            }
        }

        private void DrawHistory() {
            var history = selectionHistory.History;
            for (int i = 0; i < history.Count; i++) {
                var historyElement = history[i];
                if (selectionHistory.IsFavorite(historyElement) && favoritesEnabled) {
                    continue; // Skip elements that show up in the favorites.
                } else {
                    DrawElement(historyElement);
                }
            }
        }

        private void DrawElement(Object elementObject) {
            // Early exit if this element should be excluded.
            bool isHierarchyObject = !EditorUtility.IsPersistent(elementObject);
            if (!showHierarchyViewObjects && isHierarchyObject) {
                return;
            }
            if (!showProjectViewObjects && !isHierarchyObject) {
                return;
            }

            // Start the horizontal layout.
            Rect elementRect = EditorGUILayout.BeginHorizontal(horizontalGroupStyle);

            // Draw background box if selected.
            bool isSelected = selectionHistory.IsSelected(elementObject);
            if (isSelected) {
                DrawColoredBox(elementRect, selectedElementBackgroundColor);
            }

            // Choose the label style and tint color.
            Color? labelColor;
            GUIStyle labelStyle;
            if (isSelected) {
                labelStyle = selectedHistoryLabelStyle;
                labelColor = selectedElementTextColor;
            } else {
                labelStyle = historyLabelStyle;
                labelColor = isHierarchyObject ? hierarchyElementTextColor : (Color?)null;
            }

            // Draw the actual label for the element.
            if (elementObject == null) {
                GUILayout.Label("Deleted", labelStyle, minWidth, lineHeight);
            } else {
                bool hovered = Event.current.type == EventType.Repaint && elementRect.Contains(Event.current.mousePosition);
                DrawElementHorizontalContent(elementObject, labelStyle, labelColor, hovered);
            }

            // End the horizontal layout.
            EditorGUILayout.EndHorizontal();
            HandleSelectionElementEvents(elementRect, elementObject);

            // Only need to repaint for the favorites UI.
            if (favoritesEnabled && Event.current.type == EventType.MouseMove) {
                Repaint();
            }
        }

        private void DrawElementHorizontalContent(Object elementObject, GUIStyle labelStyle, Color? labelColor, bool hovered) {
            Color originalContentColor = GUI.contentColor;

            // Draw the favorites button.
            if (favoritesEnabled) {
                bool isFavorite = selectionHistory.IsFavorite(elementObject);
                GUIContent pinContent = GetPinButtonContent(isFavorite);
                GUI.contentColor = hovered || isFavorite ? originalContentColor : Color.clear;

                if (GUILayout.Button(pinContent, miniButtonStyle)) {
                    selectionHistory.ToggleFavorite(elementObject);
                    Repaint();
                }
            }

            GUILayout.Space(LINE_HEIGHT / 2f);

            // Setup label content.
            GUIContent labelContent = new GUIContent();
            labelContent.image = AssetPreview.GetMiniThumbnail(elementObject);
            labelContent.text = elementObject.name;

            // Draw the label.
            GUI.contentColor = labelColor.HasValue ? labelColor.Value : originalContentColor;
            Rect labelRect = GUILayoutUtility.GetRect(labelContent, labelStyle, minWidth, lineHeight);
            GUI.Label(labelRect, labelContent, labelStyle);

            GUI.contentColor = originalContentColor;
        }

        private void HandleSelectionElementEvents(Rect rect, Object currentObject) {
            // Early exit if event is not relevant.
            var currentEvent = Event.current;
            if (currentEvent == null) {
                return;
            }
            if (!rect.Contains(currentEvent.mousePosition)) {
                return;
            }
            if (currentObject == null) {
                return;
            }

            // Handle click and drag events.
            var eventType = currentEvent.type;
            if (eventType == EventType.MouseDrag && currentEvent.button == 0) {
                HandleDragEvent(currentObject);
            } else if (eventType == EventType.MouseUp) {
                if (Event.current.button == 0) {
                    if (Selection.activeObject == currentObject) {
                        EditorGUIUtility.PingObject(currentObject);
                    } else {
                        SetSelection(currentObject);
                    }
                    Event.current.Use();
                }
            }
        }

        private static void HandleDragEvent(Object currentObject) {
#if !UNITY_EDITOR_OSX
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.StartDrag(currentObject.name);
            DragAndDrop.objectReferences = new Object[] { currentObject };
            if (EditorUtility.IsPersistent(currentObject)) {
                DragAndDrop.paths = new string[] { AssetDatabase.GetAssetPath(currentObject) };
            }
            Event.current.Use();
#endif
        }

        private void DrawButtorBar() {
            GUILayout.BeginHorizontal();
            {
                var backContent = new GUIContent(EditorGUIUtility.IconContent(iconPath + "back").image, "Back");
                if (GUILayout.Button(backContent, lineHeight)) {
                    PreviousSelection();
                }

                var forwardContent = new GUIContent(EditorGUIUtility.IconContent(iconPath + "forward").image, "Forward");
                if (GUILayout.Button(forwardContent, lineHeight)) {
                    NextSelection();
                }

                if (GUILayout.Button("Clear", GUILayout.Width(70), lineHeight)) {
                    selectionHistory.Clear();
                    Repaint();
                }
            }
            GUILayout.EndHorizontal();
        }

        private static void DrawColoredBox(Rect rect, Color color) {
            var original = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUI.Box(rect, GUIContent.none, textureBackgroundStyle);
            GUI.backgroundColor = original;
        }

        private GUIContent GetPinButtonContent(bool isFavorite) {
            if (isFavorite) {
                return new GUIContent(EditorGUIUtility.IconContent(iconPath + "scenepicking_notpickable_hover@2x").image, "Unpin");
            } else {
                return new GUIContent(EditorGUIUtility.IconContent(iconPath + "scenepicking_pickable_hover@2x").image, "Pin");
            }
        }

        #endregion

        #region Menu Items

        [Shortcut("History/Previous Selection", KeyCode.LeftArrow, ShortcutModifiers.Alt | ShortcutModifiers.Action)]
        public static void PreviousSelection() {
            selectionHistory.Previous();
            Selection.activeObject = selectionHistory.GetSelection();
        }

        [Shortcut("History/Next Selection", KeyCode.RightArrow, ShortcutModifiers.Alt | ShortcutModifiers.Action)]
        public static void NextSelection() {
            selectionHistory.Next();
            Selection.activeObject = selectionHistory.GetSelection();
        }

        public void AddItemsToMenu(GenericMenu menu) {
            // Adds methods to the window context menu.
            menu.AddItem(new GUIContent("Selection History Preferences"), false, () => {
                SettingsService.OpenUserPreferences("Preferences/Selection History");
            });
        }

        #endregion
    }


}