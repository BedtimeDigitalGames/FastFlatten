using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace BedtimeCore.Editor
{
	internal static class FastFlatten
	{
		private static readonly FieldInfo _assetTreeField;
		private static readonly FieldInfo _folderTreeField;

		static FastFlatten()
		{
			var projectBrowserType = typeof(ProjectBrowser);			
			_assetTreeField = projectBrowserType.GetField("m_AssetTree", BindingFlags.Instance | BindingFlags.NonPublic);
			_folderTreeField = projectBrowserType.GetField("m_FolderTree", BindingFlags.Instance | BindingFlags.NonPublic);
		}
		
		[MenuItem("BedtimeCore/Selection/Collapse hierarchy and keep selection %g")]
		private static void CollapseHierarchyKeepSelection() => CollapseHierarchy(EditorWindow.focusedWindow);

		private static void CollapseHierarchy(EditorWindow window)
		{
			if (window == null)
			{
				return;
			}
			
			foreach (TreeViewController controller in GetTreeViewControllers(window))
			{
				if (controller == null)
				{
					continue;
				}
				
				var data = controller.data;

				var selection = data.FindItem(Selection.activeInstanceID);
				
				int collapsedCount = 0;
				foreach (TreeViewItem item in data.GetRows())
				{
					if (data.IsExpanded(item) && !IsRoot(item))
					{
						data.SetExpandedWithChildren(item, false);
						collapsedCount++;
					}
				}
				
				if(selection != null)
				{
					if (collapsedCount > 1) // If there are other expanded items in the hierarchy, keep the selection revealed and collapse everything else
					{
						data.RevealItem(selection.id);
					}
					else // If we collapse on the same object again and nothing else is expanded, collapse everything and deselect
					{
						Selection.activeObject = null;
					}
				}
			}

			window.Repaint();
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

		private static IEnumerable<TreeViewController> GetTreeViewControllers(EditorWindow window)
		{
			if (window is SceneHierarchyWindow sceneWindow)
			{
				yield return sceneWindow.sceneHierarchy.treeView;
			}

			if(window is ProjectBrowser projectWindow)
			{
				if (TryGetProjectBrowserTreeViewController(projectWindow, ProjectBrowserTreeViewType.AssetTree, out TreeViewController output))
				{
					yield return output;
				}
				if (TryGetProjectBrowserTreeViewController(projectWindow, ProjectBrowserTreeViewType.FolderTree, out output))
				{
					yield return output;
				}
			}
		}

		private static bool TryGetProjectBrowserTreeViewController(ProjectBrowser window, ProjectBrowserTreeViewType treeViewType, out TreeViewController treeViewController)
		{
			treeViewController = null;
			switch (treeViewType)
			{
				case ProjectBrowserTreeViewType.AssetTree: 
					treeViewController = _assetTreeField.GetValue(window) as TreeViewController;
					break;
				case ProjectBrowserTreeViewType.FolderTree: 
					treeViewController = _folderTreeField.GetValue(window) as TreeViewController;
					break;
			}
			return treeViewController != null;
		}

		private enum ProjectBrowserTreeViewType
		{
			AssetTree,
			FolderTree,
		}
	}
}
