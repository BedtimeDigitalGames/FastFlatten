using BedtimeCore.Reflection;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace BedtimeCore.Editor
{
	public static class TreeViewHelper
	{
		public static event Action<int[]> OnHierarchyTreeViewSelection
		{
			add { onHierarchyTreeViewSelection += value; }
			remove { onHierarchyTreeViewSelection -= value; }
		}

		public static List<object> HierarchyTreeViewControllers { get; } = new List<object>();
		public static List<object> HierarchyTreeViewControllerData { get; } = new List<object>();

		public static bool IsRevealed(int instanceID)
		{
			foreach (var data in HierarchyTreeViewControllerData)
			{
				if (data.InvokeMethod<bool, int>("IsRevealed", instanceID))
				{
					return true;
				}
			}
			return false;
		}

		private enum ProjectViewTreeView
		{
			AssetTree,
			FolderTree,
		}
		
		private const int UPDATE_INTERVAL = 10;
		private static readonly Type hierarchyType = ReflectionUtility.GetType("UnityEditor.SceneHierarchyWindow");
		private static int updateCounter;
		private static Action<int[]> onHierarchyTreeViewSelection;

		public static IEnumerable<object> GetTreeViews(EditorWindow window)
		{
			var name = window.GetType().Name;
			
			switch (name)
			{
				case "SceneHierarchyWindow":
					yield return GetHierarchyWindowTreeView(window);
					break;
				case "ProjectBrowser":
					var view = GetProjectWindowTreeView(window, ProjectViewTreeView.AssetTree);
					if (view != null){yield return view;}
					view = GetProjectWindowTreeView(window, ProjectViewTreeView.FolderTree);
					if (view != null){yield return view;}
					break;
			}
		}

		public static IEnumerable<EditorWindow> GetSceneHierarchyWindows() => hierarchyType.GetValue<List<SearchableEditorWindow>>("s_SceneHierarchyWindows");

		static TreeViewHelper()
		{
			EditorApplication.update += HandleUpdate;
		}

		private static void HandleUpdate()
		{
			if (++updateCounter > UPDATE_INTERVAL)
			{
				updateCounter = 0;
				UpdateHierarchyTreeViews();
			}
		}
		
		private static object GetProjectWindowTreeView(EditorWindow window, ProjectViewTreeView view)
		{
			switch (view)
			{
				case ProjectViewTreeView.AssetTree: 
					return window.GetValue<object>("m_AssetTree");
				case ProjectViewTreeView.FolderTree: 
					return window.GetValue<object>("m_FolderTree");
			}
			return null;
		}
		
		private static object GetHierarchyWindowTreeView(EditorWindow window)
		{
			#if UNITY_2018_3_OR_NEWER
			object hierarchy = window.GetValue<object>("m_SceneHierarchy");
			return hierarchy.GetValue<object>("m_TreeView");
			#elif UNITY_2018_3_OR_NEWER
				object stage = window.GetValue<object>("m_StageHandling");
				object hierarchy = stage.GetValue<object>("m_SceneHierarchy");
				return hierarchy.GetValue<object>("m_TreeView");
			#else
				return window.GetValue<object>("m_TreeView");
			#endif
		}

		private static void UpdateHierarchyTreeViews()
		{
			#if UNITY_2018_3_OR_NEWER
				var newHierarchyList = hierarchyType.GetValue<List<SearchableEditorWindow>>("s_SceneHierarchyWindows");
			#else
				var newHierarchyList = hierarchyType.GetValue<List<SearchableEditorWindow>>("s_SceneHierarchyWindow");
			#endif
			if (HierarchyTreeViewControllers.Count != newHierarchyList.Count)
			{
				HierarchyTreeViewControllers.Clear();
				foreach (var item in newHierarchyList)
				{
					object tree = GetHierarchyWindowTreeView(item);

					var action = tree.GetValue<Action<int[]>>("selectionChangedCallback");
					action -= onHierarchyTreeViewSelection;
					action += onHierarchyTreeViewSelection;

					HierarchyTreeViewControllers.Add(tree);
					HierarchyTreeViewControllerData.Add(tree.GetValue<object>("data"));
				}
			}
		}
	}
}
