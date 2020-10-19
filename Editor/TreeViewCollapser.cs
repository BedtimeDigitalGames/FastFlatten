using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BedtimeCore.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BedtimeCore.Editor
{
	[InitializeOnLoad]
	public static class TreeViewCollapser
	{
		private static readonly Type hierarchyType = ReflectionUtility.GetType("UnityEditor.SceneHierarchyWindow");
		private static readonly HashSet<int> selectionParents = new HashSet<int>();

		private static Color HiddenSelectionColor => EditorGUIUtility.isProSkin ? new Color(0.9f, 0.3f, 0.3f, 0.1f) : new Color(0.9f, 0.2f, 0.6f, 0.2f);

		static TreeViewCollapser()
		{
			EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyItemGUI;
			Selection.selectionChanged += HandleSelectionChanged;
			HandleSelectionChanged();
		}

		public static void CollapseSelection()
		{
			CollapseTransform(Selection.transforms);
		}

		public static void CollapseHierarchy()
		{
			CollapseHierarchy(EditorWindow.focusedWindow);
		}

		public static void CollapseHierarchy(EditorWindow window)
		{
			if (window == null)
			{
				return;
			}
			
			try
			{
				CollapseTreeView(window);
			}
			catch (MemberAccessException)
			{
			}
		}

		public static void CollapseTransform(params Transform[] toCollapse)
		{
			CollapseID(toCollapse.Select(t => t.gameObject.GetInstanceID()).ToArray());
		}

		public static void CollapseID(params int[] toCollapse)
		{
			foreach (object view in GetHierarchyViews())
			{
				var data = view.GetValue<object>("data");

				foreach (int item in toCollapse)
				{
					TreeViewItem treeItem = data.InvokeMethod<TreeViewItem, int>("FindItem", item);
					if (treeItem != null)
					{
						data.InvokeVoid("SetExpandedWithChildren", treeItem, false);
					}
				}
			}
		}

		private static void CollapseTreeView(EditorWindow focus)
		{
			foreach (object treeView in TreeViewHelper.GetTreeViews(focus))
			{
				if (treeView == null)
				{
					continue;
				}
				var data = treeView.GetValue<object>("data");
				var items = data.InvokeMethod<IList<TreeViewItem>>("GetRows");

				foreach (TreeViewItem item in items)
				{
					if (data.InvokeMethod<bool, TreeViewItem>("IsExpanded", item) && !IsSceneRoot(item))
					{
						data.InvokeVoid("SetExpandedWithChildren", item, false);
					}
				}
			}

			focus.Repaint();
		}

		private static bool IsSceneRoot(TreeViewItem item)
		{
			const string GAMEOBJECT_TREEVIEW_ITEM_TYPE_NAME = "GameObjectTreeViewItem";
			const string ROOT_TREEVIEW_TYPE_NAME = "RootTreeItem";
			string typeName = item.GetType().Name;
			return typeName == ROOT_TREEVIEW_TYPE_NAME || typeName == GAMEOBJECT_TREEVIEW_ITEM_TYPE_NAME && item.GetValue<bool>("isSceneHeader");
		}

		private static IEnumerable<object> GetHierarchyViews()
		{
			var newHierarchyList = hierarchyType.GetValue<IList>("s_SceneHierarchyWindow");
			if (newHierarchyList != null)
			{
				foreach (object item in newHierarchyList)
				{
					var tree = item.GetValue<object>("treeView");
					if (tree != null)
					{
						yield return tree;
					}
				}
			}
		}

		private static void HandleHierarchyItemGUI(int instanceID, Rect rect)
		{
			var GO = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
			if (GO == null)
			{
				return;
			}

			Rect blockRect = rect;
			blockRect.x = -1000;
			blockRect.width = 10000;

			if (selectionParents.Contains(instanceID))
			{
				foreach (Transform transform in GO.transform)
				{
					if (!TreeViewHelper.IsRevealed(transform.gameObject.GetInstanceID()))
					{
						EditorGUI.DrawRect(blockRect, HiddenSelectionColor);
						break;
					}
				}
			}
		}

		private static void HandleSelectionChanged()
		{
			selectionParents.Clear();
			foreach (GameObject selection in Selection.gameObjects)
			{
				foreach (int item in GetParent(selection))
				{
					selectionParents.Add(item);
				}
			}
		}

		private static IEnumerable<int> GetParent(GameObject child)
		{
			Transform transform = child.transform;
			while (transform.parent != null)
			{
				transform = transform.parent;
				yield return transform.gameObject.GetInstanceID();
			}
		}
	}
}
