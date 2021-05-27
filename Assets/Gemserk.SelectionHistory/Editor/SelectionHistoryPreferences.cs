using UnityEngine;
using UnityEditor;

namespace Gemserk {
    public static class SelectionHistoryPreferences {

        private static bool prefsLoaded = false;
        private static int historySize;
        private static bool autoRemoveDeleted;
        private static bool allowDuplicates;
        private static bool showHierarchyObjects;
        private static bool showProjectViewObjects;
        private static bool drawFavorites;

        [SettingsProvider]
        public static SettingsProvider CreateSelectionHistorySettingsProvider() {
            var provider = new SettingsProvider("Preferences/Selection History", SettingsScope.User) {
                label = "Selection History",

                guiHandler = (searchContext) => {
                    if (!prefsLoaded) {
                        historySize = EditorPrefs.GetInt(SelectionHistoryWindow.HistorySizePrefKey, 10);
                        autoRemoveDeleted = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryAutomaticRemoveDeletedPrefKey, true);
                        allowDuplicates = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryAllowDuplicatedEntriesPrefKey, false);
                        showHierarchyObjects = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryShowHierarchyObjectsPrefKey, true);
                        showProjectViewObjects = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryShowProjectViewObjectsPrefKey, true);
                        drawFavorites = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryFavoritesPrefKey, true);
                        prefsLoaded = true;
                    }

                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    EditorGUIUtility.labelWidth = 200;

                    historySize = EditorGUILayout.IntField("History Size", historySize);
                    autoRemoveDeleted = EditorGUILayout.Toggle("Auto Remove Deleted", autoRemoveDeleted);
                    allowDuplicates = EditorGUILayout.Toggle("Allow Duplicated Entries", allowDuplicates);
                    showHierarchyObjects = EditorGUILayout.Toggle("Show HierarchyView Objects", showHierarchyObjects);
                    showProjectViewObjects = EditorGUILayout.Toggle("Show ProjectView Objects", showProjectViewObjects);
                    drawFavorites = EditorGUILayout.Toggle("Favorites Enabled", drawFavorites);

                    EditorGUI.indentLevel--;

                    if (GUI.changed) {
                        EditorPrefs.SetInt(SelectionHistoryWindow.HistorySizePrefKey, historySize);
                        EditorPrefs.SetBool(SelectionHistoryWindow.HistoryAutomaticRemoveDeletedPrefKey, autoRemoveDeleted);
                        EditorPrefs.SetBool(SelectionHistoryWindow.HistoryAllowDuplicatedEntriesPrefKey, allowDuplicates);
                        EditorPrefs.SetBool(SelectionHistoryWindow.HistoryShowHierarchyObjectsPrefKey, showHierarchyObjects);
                        EditorPrefs.SetBool(SelectionHistoryWindow.HistoryShowProjectViewObjectsPrefKey, showProjectViewObjects);
                        EditorPrefs.SetBool(SelectionHistoryWindow.HistoryFavoritesPrefKey, drawFavorites);

                        SelectionHistoryWindow.shouldReloadPreferences = true;
                    }
                },
            };
            return provider;
        }
    }
}
