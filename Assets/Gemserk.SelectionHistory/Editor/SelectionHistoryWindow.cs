using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.ShortcutManagement;

namespace Gemserk
{
	[InitializeOnLoad]
	public static class SelectionHistoryInitialized
	{
		static SelectionHistoryInitialized()
		{
			SelectionHistoryWindow.RegisterSelectionListener();
		}
	}

	public class SelectionHistoryWindow : EditorWindow
	{

		public static readonly string HistorySizePrefKey = "Gemserk.SelectionHistory.HistorySize";
		public static readonly string HistoryAutomaticRemoveDeletedPrefKey = "Gemserk.SelectionHistory.AutomaticRemoveDeleted";
		public static readonly string HistoryAllowDuplicatedEntriesPrefKey = "Gemserk.SelectionHistory.AllowDuplicatedEntries";
		public static readonly string HistoryShowHierarchyObjectsPrefKey = "Gemserk.SelectionHistory.ShowHierarchyObjects";
		public static readonly string HistoryShowProjectViewObjectsPrefKey = "Gemserk.SelectionHistory.ShowProjectViewObjects";
		public static readonly string HistoryFavoritesPrefKey = "Gemserk.SelectionHistory.Favorites";

		static SelectionHistory selectionHistory = new SelectionHistory();

		static readonly bool debugEnabled = false;

		public static bool shouldReloadPreferences = true;

		private static Color hierarchyElementColor = new Color(0.7f, 1.0f, 0.7f);
		private static Color selectedElementColor => EditorGUIUtility.isProSkin ? Color.white : new Color(0.2f, 0.5f, 0.95f, 1.0f);
		private static Color highlightBackgroundColor => EditorGUIUtility.isProSkin ? new Color(0.17f, 0.36f, 0.56f) : Color.white * 0.8f;
		private static Color pinnedBackgroundColor => EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f, 0.7f) : new Color(0.1f, 0.1f, 0.1f, 0.1f);
		private static string iconPath => EditorGUIUtility.isProSkin ? "d_" : "";

		[MenuItem("Window/General/Selection History %#h")]
		static void Init()
		{
			// Get existing open window or if none, make a new one:
			var window = EditorWindow.GetWindow<SelectionHistoryWindow>();

			window.titleContent.text = "History";
			window.Show();
		}

		static void SelectionRecorder()
		{
			if (Selection.activeObject != null)
			{
				if (debugEnabled)
				{
					Debug.Log("Recording new selection: " + Selection.activeObject.name);
				}

				selectionHistory = EditorTemporaryMemory.Instance.selectionHistory;
				selectionHistory.UpdateSelection(Selection.activeObject);
			}
		}

		public static void RegisterSelectionListener()
		{
			Selection.selectionChanged += SelectionRecorder;
		}

		private static GUIStyle textureStyle;
		private static GUIStyle historyElementStyle => EditorStyles.label;
		private static GUILayoutOption lineHeight = GUILayout.Height(EditorGUIUtility.singleLineHeight + 2);

		MethodInfo openPreferencesWindow;

		void OnEnable()
		{
			automaticRemoveDeleted = EditorPrefs.GetBool(HistoryAutomaticRemoveDeletedPrefKey, true);

			selectionHistory = EditorTemporaryMemory.Instance.selectionHistory;
			selectionHistory.HistorySize = EditorPrefs.GetInt(HistorySizePrefKey, 10);

			Selection.selectionChanged += delegate
			{

				if (selectionHistory.IsSelected(selectionHistory.GetHistoryCount() - 1))
				{
					_historyScrollPosition.y = float.MaxValue;
				}

				Repaint();
			};

			try
			{
				var asm = Assembly.GetAssembly(typeof(EditorWindow));
				var t = asm.GetType("UnityEditor.PreferencesWindow");
				openPreferencesWindow = t.GetMethod("ShowPreferencesWindow", BindingFlags.NonPublic | BindingFlags.Static);
			}
			catch
			{
				// couldnt get preferences window...
				openPreferencesWindow = null;
			}

			if (textureStyle == null)
			{
				textureStyle = new GUIStyle { normal = new GUIStyleState { background = Texture2D.whiteTexture } };
			}
		}

		void UpdateSelection(Object obj)
		{
			selectionHistory.SetSelection(obj);
			Selection.activeObject = obj;
			// Selection.activeObject = selectionHistory.UpdateSelection(currentIndex);
		}

		private Vector2 _favoritesScrollPosition;
		private Vector2 _historyScrollPosition;

		bool automaticRemoveDeleted;
		bool allowDuplicatedEntries;

		bool showHierarchyViewObjects;
		bool showProjectViewObjects;

		void OnGUI()
		{

			if (shouldReloadPreferences)
			{
				selectionHistory.HistorySize = EditorPrefs.GetInt(SelectionHistoryWindow.HistorySizePrefKey, 10);
				automaticRemoveDeleted = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryAutomaticRemoveDeletedPrefKey, true);
				allowDuplicatedEntries = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryAllowDuplicatedEntriesPrefKey, false);

				showHierarchyViewObjects = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryShowHierarchyObjectsPrefKey, true);
				showProjectViewObjects = EditorPrefs.GetBool(SelectionHistoryWindow.HistoryShowProjectViewObjectsPrefKey, true);

				shouldReloadPreferences = false;
			}

			if (automaticRemoveDeleted)
				selectionHistory.ClearDeleted();

			if (!allowDuplicatedEntries)
				selectionHistory.RemoveDuplicated();

			// Favorites
			var favoritesEnabled = EditorPrefs.GetBool(HistoryFavoritesPrefKey, true);
			if (favoritesEnabled && selectionHistory.Favorites.Count > 0)
			{
				_favoritesScrollPosition = EditorGUILayout.BeginScrollView(_favoritesScrollPosition, GUILayout.ExpandHeight(false));
				DrawFavorites();
				EditorGUILayout.EndScrollView();

				// Draw divider.
				GUILayout.Space(6);
				EditorGUILayout.Separator();
				var original = GUI.backgroundColor;
				GUI.backgroundColor = pinnedBackgroundColor;
				GUI.Box(GUILayoutUtility.GetLastRect(), GUIContent.none);
				GUI.backgroundColor = original;
				GUILayout.Space(2);
			}

			bool changedBefore = GUI.changed;

			_historyScrollPosition = EditorGUILayout.BeginScrollView(_historyScrollPosition, GUILayout.ExpandHeight(true));

			bool changedAfter = GUI.changed;

			if (!changedBefore && changedAfter)
			{
				Debug.Log("changed");
			}

			DrawHistory();

			EditorGUILayout.EndScrollView();

			GUILayout.BeginHorizontal();
			{
				var backContent = new GUIContent(EditorGUIUtility.IconContent(iconPath + "back").image, "Back");
				if (GUILayout.Button(backContent, lineHeight))
				{
					PreviousSelection();
				}

				var forwardContent = new GUIContent(EditorGUIUtility.IconContent(iconPath + "forward").image, "Forward");
				if (GUILayout.Button(forwardContent, lineHeight))
				{
					NextSelection();
				}

				if (GUILayout.Button("Clear", GUILayout.Width(70), lineHeight))
				{
					selectionHistory.Clear();
					Repaint();
				}
			}
			GUILayout.EndHorizontal();

			if (!automaticRemoveDeleted)
			{
				if (GUILayout.Button("Remove Deleted"))
				{
					selectionHistory.ClearDeleted();
					Repaint();
				}
			}

			if (allowDuplicatedEntries)
			{
				if (GUILayout.Button("Remove Duplciated"))
				{
					selectionHistory.RemoveDuplicated();
					Repaint();
				}
			}

			DrawSettingsButton();
		}

		void DrawSettingsButton()
		{
			if (openPreferencesWindow == null)
				return;

			if (GUILayout.Button("Preferences"))
			{
				openPreferencesWindow.Invoke(null, null);
			}
		}

		[Shortcut("History/Previous Selection", KeyCode.LeftArrow, ShortcutModifiers.Shift | ShortcutModifiers.Shift)]
		public static void PreviousSelection()
		{
			selectionHistory.Previous();
			Selection.activeObject = selectionHistory.GetSelection();
		}

		[Shortcut("History/Next Selection", KeyCode.RightArrow, ShortcutModifiers.Shift | ShortcutModifiers.Shift)]
		public static void NextSelection()
		{
			selectionHistory.Next();
			Selection.activeObject = selectionHistory.GetSelection();
		}

		void DrawElement(Object obj, int i, Color originalColor)
		{
			var buttonStyle = historyElementStyle;
			var nonSelectedColor = originalColor;

			if (!EditorUtility.IsPersistent(obj))
			{
				if (!showHierarchyViewObjects)
					return;
				nonSelectedColor = hierarchyElementColor;
			}
			else
			{
				if (!showProjectViewObjects)
					return;
			}

			bool isSelected = selectionHistory.IsSelected(obj);

			if (isSelected)
			{
				GUI.contentColor = selectedElementColor;
				buttonStyle = new GUIStyle(buttonStyle);
				buttonStyle.fontStyle = FontStyle.Bold;
				buttonStyle.normal.textColor = Color.white;
			}
			else
			{
				GUI.contentColor = nonSelectedColor;
			}

			var rect = EditorGUILayout.BeginHorizontal();
			if (isSelected)
			{
				var original = GUI.backgroundColor;
				GUI.backgroundColor = highlightBackgroundColor;
				GUI.Box(rect, GUIContent.none, textureStyle);
				GUI.backgroundColor = original;
			}

			var layoutWidth = GUILayout.MinWidth(40);
			if (obj == null)
			{
				GUILayout.Label("Deleted", buttonStyle, layoutWidth, lineHeight);
			}
			else
			{
				var icon = AssetPreview.GetMiniThumbnail(obj);

				GUIContent content = new GUIContent();

				content.image = icon;
				content.text = obj.name;

				var labelRect = GUILayoutUtility.GetRect(content, buttonStyle, layoutWidth, lineHeight);
				GUI.Label(labelRect, content, buttonStyle);

				GUI.contentColor = originalColor;

				var buttonWidth = GUILayout.Width(30);
				var pingContent = new GUIContent(EditorGUIUtility.IconContent(iconPath + "scenevis_visible_hover").image, "Ping");
				if (GUILayout.Button(pingContent, lineHeight, buttonWidth))
				{
					EditorGUIUtility.PingObject(obj);
				}

				var favoritesEnabled = EditorPrefs.GetBool(HistoryFavoritesPrefKey, true);

				if (favoritesEnabled)
				{
					var pinContent = new GUIContent(EditorGUIUtility.IconContent(iconPath + "scenepicking_pickable_hover@2x").image, "Pin");
					var isFavorite = selectionHistory.IsFavorite(obj);
					Color defaultColor = GUI.backgroundColor;
					Color buttonColor = defaultColor;

					if (isFavorite)
					{
						buttonColor = Color.yellow;
						pinContent = new GUIContent(EditorGUIUtility.IconContent(iconPath + "scenepicking_notpickable_hover@2x").image, "Unpin");
					}

					GUI.backgroundColor = buttonColor;
					if (GUILayout.Button(pinContent, buttonWidth, lineHeight))
					{
						selectionHistory.ToggleFavorite(obj);
						Repaint();
					}
					GUI.backgroundColor = defaultColor;
				}

			}

			EditorGUILayout.EndHorizontal();

			ButtonLogic(rect, obj);
		}

		void DrawFavorites()
		{
			var originalColor = GUI.contentColor;

			var favorites = selectionHistory.Favorites;

			var buttonStyle = historyElementStyle;

			for (int i = 0; i < favorites.Count; i++)
			{
				var favorite = favorites[i];
				DrawElement(favorite, i, originalColor);
			}

			GUI.contentColor = originalColor;
		}

		void DrawHistory()
		{
			var originalColor = GUI.contentColor;

			var history = selectionHistory.History;

			var buttonStyle = historyElementStyle;

			var favoritesEnabled = EditorPrefs.GetBool(HistoryFavoritesPrefKey, true);

			for (int i = 0; i < history.Count; i++)
			{
				var historyElement = history[i];
				if (selectionHistory.IsFavorite(historyElement) && favoritesEnabled)
					continue;
				DrawElement(historyElement, i, originalColor);
			}

			GUI.contentColor = originalColor;
		}

		void ButtonLogic(Rect rect, Object currentObject)
		{
			var currentEvent = Event.current;

			if (currentEvent == null)
				return;

			if (!rect.Contains(currentEvent.mousePosition))
				return;

			//			Debug.Log (string.Format("event:{0}", currentEvent.ToString()));

			var eventType = currentEvent.type;

			if (eventType == EventType.MouseDrag && currentEvent.button == 0)
			{

#if !UNITY_EDITOR_OSX

				if (currentObject != null)
				{
					DragAndDrop.PrepareStartDrag();

					DragAndDrop.StartDrag(currentObject.name);

					DragAndDrop.objectReferences = new Object[] { currentObject };

					//					if (ProjectWindowUtil.IsFolder(currentObject.GetInstanceID())) {

					// fixed to use IsPersistent to work with all assets with paths.
					if (EditorUtility.IsPersistent(currentObject))
					{

						// added DragAndDrop.path in case we are dragging a folder.

						DragAndDrop.paths = new string[] {
							AssetDatabase.GetAssetPath(currentObject)
						};

						// previous test with setting generic data by looking at
						// decompiled Unity code.

						// DragAndDrop.SetGenericData ("IsFolder", "isFolder");
					}
				}

				Event.current.Use();
#endif

			}
			else if (eventType == EventType.MouseUp)
			{

				if (currentObject != null)
				{
					if (Event.current.button == 0)
					{
						UpdateSelection(currentObject);
					}
					else
					{
						EditorGUIUtility.PingObject(currentObject);
					}
				}

				Event.current.Use();
			}

		}

	}
}