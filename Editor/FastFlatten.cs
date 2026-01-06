using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
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
			
			foreach (var controller in GetTreeViewControllers(window))
			{
				if (controller == null)
				{
					continue;
				}
				
				var data = controller.data;

				var selection = data.FindItem(Selection.activeInstanceID);
				
				int collapsedCount = 0;
				foreach (var item in data.GetRows())
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

#if UNITY_6000_3_OR_NEWER
        		private static bool IsRoot(TreeViewItem<EntityId> item)
#elif UNITY_6000_2_OR_NEWER
		private static bool IsRoot(TreeViewItem<int> item)
#else
        private static bool IsRoot(TreeViewItem item)
#endif
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
        
#if UNITY_6000_3_OR_NEWER
        private static IEnumerable<TreeViewController<EntityId>> GetTreeViewControllers(EditorWindow window)
#elif UNITY_6000_2_OR_NEWER
		private static IEnumerable<TreeViewController<int>> GetTreeViewControllers(EditorWindow window)
#else
		private static IEnumerable<TreeViewController> GetTreeViewControllers(EditorWindow window)
#endif
		{
			if (window is SceneHierarchyWindow sceneWindow)
			{
				yield return sceneWindow.sceneHierarchy.treeView;
			}

			if(window is ProjectBrowser projectWindow)
			{
				if (TryGetProjectBrowserTreeViewController(projectWindow, ProjectBrowserTreeViewType.AssetTree, out var output))
				{
					yield return output;
				}
				if (TryGetProjectBrowserTreeViewController(projectWindow, ProjectBrowserTreeViewType.FolderTree, out output))
				{
					yield return output;
				}
			}
		}

#if UNITY_6000_3_OR_NEWER
		private static bool TryGetProjectBrowserTreeViewController(ProjectBrowser window, ProjectBrowserTreeViewType treeViewType, out TreeViewController<EntityId> treeViewController)
		{
			treeViewController = null;
			switch (treeViewType)
			{
				case ProjectBrowserTreeViewType.AssetTree: 
					treeViewController = _assetTreeField.GetValue(window) as TreeViewController<EntityId>;
					break;
				case ProjectBrowserTreeViewType.FolderTree: 
					treeViewController = _folderTreeField.GetValue(window) as TreeViewController<EntityId>;
					break;
			}
			return treeViewController != null;
		}
#elif UNITY_6000_2_OR_NEWER
		private static bool TryGetProjectBrowserTreeViewController(ProjectBrowser window, ProjectBrowserTreeViewType treeViewType, out TreeViewController<int> treeViewController)
		{
			treeViewController = null;
			switch (treeViewType)
			{
				case ProjectBrowserTreeViewType.AssetTree: 
					treeViewController = _assetTreeField.GetValue(window) as TreeViewController<int>;
					break;
				case ProjectBrowserTreeViewType.FolderTree: 
					treeViewController = _folderTreeField.GetValue(window) as TreeViewController<int>;
					break;
			}
			return treeViewController != null;
		}
#else
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
#endif

		private enum ProjectBrowserTreeViewType
		{
			AssetTree,
			FolderTree,
		}
	}
}
