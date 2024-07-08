using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace BedtimeCore.Editor
{
	public static class TreeViewCollapser
	{
		private static readonly FieldInfo _assetTreeField;
		private static readonly FieldInfo _folderTreeField;

		static TreeViewCollapser()
		{
			var projectBrowserType = typeof(ProjectBrowser);			
			_assetTreeField = projectBrowserType.GetField("m_AssetTree", BindingFlags.Instance | BindingFlags.NonPublic);
			_folderTreeField = projectBrowserType.GetField("m_FolderTree", BindingFlags.Instance | BindingFlags.NonPublic);
		}
		
		[MenuItem("BedtimeCore/Selection/Collapse Focused Hierarchy %g")]
		private static void CollapseHierarchy() => CollapseHierarchy(EditorWindow.focusedWindow);

		private static void CollapseHierarchy(EditorWindow window)
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

		private static void CollapseTreeView(EditorWindow focus)
		{
			foreach (var treeView in GetTreeViews(focus))
			{
				if (treeView == null)
				{
					continue;
				}
				var data = treeView.data;
				var items = data.GetRows();

				foreach (TreeViewItem item in items)
				{
					if (data.IsExpanded(item) && !IsRoot(item))
					{
						data.SetExpandedWithChildren(item, false);
					}
				}
			}

			focus.Repaint();
		}

		private static bool IsRoot(TreeViewItem item)
		{
			if (item is GameObjectTreeViewItem goTreeItem)
			{
				return goTreeItem.isSceneHeader;
			}
			if (item is AssetsTreeViewDataSource.RootTreeItem)
			{
				return true;
			}
			return false;
		}
		
		private enum ProjectViewTreeView
		{
			AssetTree,
			FolderTree,
		}
		
		private static IEnumerable<TreeViewController> GetTreeViews(EditorWindow window)
		{
			if (window is SceneHierarchyWindow sceneWindow)
			{
				yield return sceneWindow.sceneHierarchy.treeView;
			}

			TreeViewController output;
			if(window is ProjectBrowser projectWindow)
			{
				if (TryGetProjectWindowTreeView(projectWindow, ProjectViewTreeView.AssetTree, out output))
				{
					yield return output;
				}
				if (TryGetProjectWindowTreeView(projectWindow, ProjectViewTreeView.FolderTree, out output))
				{
					yield return output;
				}
			}
		}

		private static bool TryGetProjectWindowTreeView(ProjectBrowser window, ProjectViewTreeView view, out TreeViewController treeViewController)
		{
			treeViewController = null;
			switch (view)
			{
				case ProjectViewTreeView.AssetTree: 
					treeViewController = _assetTreeField.GetValue(window) as TreeViewController;
					break;
				case ProjectViewTreeView.FolderTree: 
					treeViewController = _folderTreeField.GetValue(window) as TreeViewController;
					break;
			}
			return treeViewController != null;
		}
	}
}
