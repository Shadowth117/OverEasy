using AquaModelLibrary.Data.BillyHatcher.SetData;
using Godot;
using OverEasy;

namespace OverEasy.Editor
{
    public class BillyAddObjectCommand : IEditorCommand
    {
        private readonly EditingType editingType;
        private readonly int insertIndex;
        private readonly SetObj? newSetObj;
        private readonly SetEnemy? newSetEnemy;
        private readonly TreeItem categoryTreeItem;

        public BillyAddObjectCommand(EditingType type, int index, TreeItem categoryNode, SetObj obj)
        {
            editingType = type;
            insertIndex = index;
            newSetObj = obj;
            categoryTreeItem = categoryNode;
        }

        public BillyAddObjectCommand(EditingType type, int index, TreeItem categoryNode, SetEnemy enemy)
        {
            editingType = type;
            insertIndex = index;
            newSetEnemy = enemy;
            categoryTreeItem = categoryNode;
        }

        public void Execute()
        {
            switch (editingType)
            {
                case EditingType.BillySetObj:
                    OverEasyGlobals.loadedBillySetObjects.setObjs.Insert(insertIndex, newSetObj.Value);
                    break;
                case EditingType.BillySetDesign:
                    OverEasyGlobals.loadedBillySetDesignObjects.setObjs.Insert(insertIndex, newSetObj.Value);
                    break;
                case EditingType.BillySetEnemy:
                    OverEasyGlobals.loadedBillySetEnemies.setEnemies.Insert(insertIndex, newSetEnemy.Value);
                    break;
            }
            OverEasyGlobals.RebuildBillyObjectCategory(categoryTreeItem, editingType);

            var newItem = categoryTreeItem.GetChild(insertIndex);
            if (newItem != null && GodotObject.IsInstanceValid(newItem))
            {
                OverEasyGlobals.setDataTree.SetSelected(newItem, 0);
                OverEasyGlobals.HandleTreeNodeSelected();
            }
        }

        public void Undo()
        {
            OverEasyGlobals.ClearObjectSelection();
            switch (editingType)
            {
                case EditingType.BillySetObj:
                    OverEasyGlobals.loadedBillySetObjects.setObjs.RemoveAt(insertIndex);
                    break;
                case EditingType.BillySetDesign:
                    OverEasyGlobals.loadedBillySetDesignObjects.setObjs.RemoveAt(insertIndex);
                    break;
                case EditingType.BillySetEnemy:
                    OverEasyGlobals.loadedBillySetEnemies.setEnemies.RemoveAt(insertIndex);
                    break;
            }
            OverEasyGlobals.RebuildBillyObjectCategory(categoryTreeItem, editingType);
        }
    }
}
