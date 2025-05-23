using Godot;
using OverEasy.Billy;
using OverEasy.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace OverEasy
{
	partial class OverEasyGlobals
	{
		/// <summary>
		/// X threshold in pixels for determining if the mouse has 'moved' for selection purposes. If not, the user may continue to select objects underneath the current one from the current camera.
		/// </summary>
		public const int MouseNotMovedThresholdX = 5;
		/// <summary>
		/// Y threshold in pixels for determining if the mouse has 'moved' for selection purposes. If not, the user may continue to select objects underneath the current one from the current camera.
		/// </summary>
		public const int MouseNotMovedThresholdY = 5;

		//Schema templates
		public static VBoxContainer ColorSchemaTemplate = null;
		public static VBoxContainer FloatSchemaTemplate = null;
		public static VBoxContainer IntSchemaTemplate = null;
		public static VBoxContainer LabelSchemaTemplate = null;
		public static VBoxContainer StringSchemaTemplate = null;
		public static VBoxContainer Vector2SchemaTemplate = null;
		public static VBoxContainer Vector3SchemaTemplate = null;
		public static VBoxContainer Vector4SchemaTemplate = null;

		public static DisplayButton DisplayBtn = null;
		public static SettingsButton SettingsBtn = null;

		public static CollisionShape2D MenuBarCollision = null;
		public static CollisionShape2D setDataTreeCollision = null;
		public static CollisionShape2D setDataTreeButtonCollision = null;
		public static CollisionShape2D objectPanelCollision = null;
		public static CollisionShape2D objectPanelButtonCollision = null;

		public static ViewerCamera ViewCamera = null;
		public static Gizmo TransformGizmo = null;
		/// <summary>
		/// False for local, true for world
		/// </summary>
		public static bool TransformGizmoWorld = false;
		public static bool WarpCameraToNewSelection = true;

		public static Dictionary<string, Texture2D> globalTexturePool = new Dictionary<string, Texture2D>();
		public static Dictionary<string, List<Texture2D>> orderedTextureArchivePools = new Dictionary<string, List<Texture2D>>();

		public static string gameFolderLocation = null;
		public static string backupFolderLocation = null;
		public static string modFolderLocation = null;
		public static GameType gameType = GameType.None;
		public static List<string> mapKeyOrderList = new List<string>();
		public static Dictionary<string, string> mapNames = new Dictionary<string, string>();
		public static bool allowedToUpdate = false;

		public static bool mouseInGuiArea = false;
		public static SceneTree OESceneTree = null;
		public static Viewport OEMainViewPort = null;
		public static Tree setDataTree = null;
		public static Tree dummyTree = null;
		public static Node3D modelRoot = null;
		public static List<Node3D> terrainModels = new List<Node3D>();
		public static List<Node3D> terrainCollision = new List<Node3D>();

		public static bool setDataTreeItemActivatedSet = false;
		public static ScrollContainer objectScrollContainer = null;
		public static Button setDataTreeButton = null;
		public static Button objectScrollContainerButton = null;
		public static Dictionary<string, Container> activeObjectEditorObjects = new Dictionary<string, Container>();
		public static Dictionary<string, Node3D> modelDictionary = new Dictionary<string, Node3D>();

		public static EditingType currentEditorType = EditingType.None;

		public static int currentObjectId = -1;
		public static int currentArchiveFileId = -1;
		public static string currentArhiveFilename = null;
		public static int currentMissionId = -1;
		public static string editorRootDirectory = null;
		public static TreeItem currentObjectTreeItem = null;

		public static Godot.Collections.Array<Rid> PreviousMouseSelectionPointRidCache = new Godot.Collections.Array<Rid>();
		public static Vector2 PreviousMouseSelectionPoint = new Vector2(-MouseNotMovedThresholdX - 1, -MouseNotMovedThresholdY - 1);

		public static Color mainFillColorInactive = new Color(0.7F, 0.7F, 0.7F, 0.6F); 
		public static Color mainFillColorActive = new Color(0.7F, 0.7F, 0.7F, 1.2F); 

		public static bool CanAccess3d
		{
			get
			{
				return !mouseInGuiArea;
			}
		}

		public enum EditingType
		{
			None = 0,
			BillySetObj = 1,
			BillySetDesign = 2,
			BillySpawnPoint = 3,
			BillySetEnemy = 4,
		}

		public static void SetCameraSettings()
		{
			ViewerCamera.SCROLL_SPEED = 10;
			ViewerCamera.ZOOM_SPEED = 5;
			ViewerCamera.SPIN_SPEED = 10;
			ViewerCamera.DEFAULT_DISTANCE = 20;
			ViewerCamera.ROTATE_SPEED_X = 40;
			ViewerCamera.ROTATE_SPEED_Y = 40;
			ViewerCamera.TOUCH_ZOOM_SPEED = 40;
			ViewerCamera.SHIFT_MULTIPLIER = 2.5;
			ViewerCamera.CTRL_MULTIPLIER = 0.4;
			ViewerCamera.FREECAM_ACCELERATION = 30;
			ViewerCamera.FREECAM_DECELERATION = -10;
			ViewerCamera.FREECAM_VELOCITY_MULTIPLIER = 4;
		}

		/// <summary>
		/// Dialog for loading a compatible game or project
		/// </summary>
		private static FileDialog getMainGameFileDialog = new FileDialog()
		{
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Filters = new string[] { "amem_boot.nrc, *.oeproj ; Game Identifier Files", "*.oeproj ; OverEasy Project File", "amem_boot.nrc ; Billy Hatcher Boot File" },
			UseNativeDialog = true,
			Access = FileDialog.AccessEnum.Filesystem
		};

		/// <summary>
		/// Dialog for loading a compatible game
		/// </summary>
		private static FileDialog getOnlyGameFileDialog = new FileDialog()
		{
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Filters = new string[] { "amem_boot.nrc ; Game Identifier Files", "amem_boot.nrc ; Billy Hatcher Boot File" },
			UseNativeDialog = true,
			Access = FileDialog.AccessEnum.Filesystem
		};

		public static ConfirmationDialog confirmCopyModFilesDialog = new ConfirmationDialog()
		{
			PopupWindow = true,
			InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen,
			DialogText = "Are you sure you want to overwrite current game files with mod project files?",
		};

		public static ConfirmationDialog confirmRestoreBackupFilesDialog = new ConfirmationDialog()
		{
			PopupWindow = true,
			InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen,
			DialogText = "Are you sure you want to overwrite game files from backup files?",
		};

		/// <summary>
		/// Actions that need to happen right on editor startup
		/// </summary>
		public static void StartUp()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			var basePath = ProjectSettings.GlobalizePath("res://");
			if (basePath == null || basePath == "")
			{
				basePath = Assembly.GetExecutingAssembly().CodeBase.Substring(8);
			}
			editorRootDirectory = Path.GetDirectoryName(basePath);
			getMainGameFileDialog.FileSelected += ResetLoadedData;
			getMainGameFileDialog.FileSelected += LoadInitialData;

			confirmCopyModFilesDialog.Confirmed += CopyModFiles;
			confirmRestoreBackupFilesDialog.Confirmed += RestoreBackupFiles;
		}

		public static void AttachDialogs()
		{
			OEMainViewPort.AddChild(confirmCopyModFilesDialog);
			OEMainViewPort.AddChild(confirmRestoreBackupFilesDialog);
		}

		public static void ClearPreviousMouseSelectionCache()
		{
			PreviousMouseSelectionPoint = new Vector2(-MouseNotMovedThresholdX - 1, -MouseNotMovedThresholdY - 1);
			PreviousMouseSelectionPointRidCache.Clear();
		}

		public static void SetGizmoWorldStatus(bool worldMode)
		{
			if(worldMode)
			{
				TransformGizmo.GlobalRotation = new Vector3();
			} else
			{
				TransformGizmo.Rotation = new Vector3();
			}
		}

		public static void CopyModFiles()
		{
			var modFiles = Directory.GetFiles(modFolderLocation);
			foreach (var file in modFiles)
			{
				if (Path.GetExtension(file) != ".oeproj")
				{
					File.Copy(file, file.Replace(modFolderLocation, gameFolderLocation), true);
				}
			}
		}

		public static void RestoreBackupFiles()
		{
			var backupFiles = Directory.GetFiles(backupFolderLocation);
			foreach (var file in backupFiles)
			{
				File.Copy(file, file.Replace(backupFolderLocation, gameFolderLocation), true);
			}
		}

		/// <summary>
		/// Actions for when the window size changes
		/// </summary>
		public static void OnWindowSizeChanged()
		{
			var viewPortSize = OEMainViewPort.GetVisibleRect().Size;
			setDataTree.Size = new Vector2(300, viewPortSize.Y - 31);
			setDataTreeButton.Size = new Vector2(setDataTreeButton.Size.X, viewPortSize.Y - 31);
			dummyTree.Size = new Vector2(objectScrollContainer.Size.X, viewPortSize.Y - 31);
			objectScrollContainer.Size = new Vector2(objectScrollContainer.Size.X, viewPortSize.Y - 31);
			objectScrollContainerButton.Size = new Vector2(objectScrollContainerButton.Size.X, viewPortSize.Y - 31);

			setDataTreeCollision.GlobalPosition = new Vector2(setDataTreeCollision.GlobalPosition.X, 31 + (viewPortSize.Y - 31) / 2);
			var setDataTreeShape = (RectangleShape2D)setDataTreeCollision.Shape;
			if (setDataTree.Visible == false)
			{
				setDataTreeShape.Size = new Vector2(setDataTreeShape.Size.X, 0);
			}
			else
			{
				setDataTreeShape.Size = new Vector2(setDataTreeShape.Size.X, (viewPortSize.Y - 31));
			}
			setDataTreeCollision.Shape = setDataTreeShape;

			objectPanelCollision.GlobalPosition = new Vector2(objectPanelCollision.GlobalPosition.X, 31 + (viewPortSize.Y - 31) / 2);
			var objPanelShape = (RectangleShape2D)objectPanelCollision.Shape;
			if (objectScrollContainer.Visible == false)
			{
				objPanelShape.Size = new Vector2(objPanelShape.Size.X, 0);
			}
			else
			{
				objPanelShape.Size = new Vector2(objPanelShape.Size.X, (viewPortSize.Y - 31));
			}
			objectPanelCollision.Shape = objPanelShape;

			setDataTreeButtonCollision.GlobalPosition = new Vector2(setDataTreeButtonCollision.GlobalPosition.X, 31 + (viewPortSize.Y - 31) / 2);
			var setDataTreeButtonShape = (RectangleShape2D)setDataTreeButtonCollision.Shape;
			setDataTreeButtonShape.Size = new Vector2(setDataTreeButtonShape.Size.X, (viewPortSize.Y - 31));
			setDataTreeButtonCollision.Shape = setDataTreeButtonShape;

			objectPanelButtonCollision.GlobalPosition = new Vector2(objectPanelButtonCollision.GlobalPosition.X, 31 + (viewPortSize.Y - 31) / 2);
			var objPanelButtonShape = (RectangleShape2D)objectPanelButtonCollision.Shape;
			objPanelButtonShape.Size = new Vector2(objPanelButtonShape.Size.X, (viewPortSize.Y - 31));
			objectPanelButtonCollision.Shape = objPanelButtonShape;
		}

		/// <summary>
		/// Method for handling the result of clicking the Show/Hide button for the level data tree
		/// </summary>
		public static void OnSetDataTreeButtonReleased()
		{
			var currentVisibility = !setDataTree.Visible;
			setDataTree.Visible = currentVisibility;
			setDataTree.SetProcessInput(currentVisibility);

			var viewPortSize = OEMainViewPort.GetVisibleRect().Size;
			var setDataTreeShape = (RectangleShape2D)setDataTreeCollision.Shape;
			if (setDataTree.Visible == false)
			{
				setDataTreeShape.Size = new Vector2(setDataTreeShape.Size.X, 0);
			}
			else
			{
				setDataTreeShape.Size = new Vector2(setDataTreeShape.Size.X, (viewPortSize.Y - 31));
			}
			setDataTreeCollision.Shape = setDataTreeShape;
		}

		/// <summary>
		/// Method for handling the result of clicking the Show/Hide button for the object data panel
		/// </summary>
		public static void OnObjectScrollContainerButtonReleased()
		{
			var currentVisibility = !objectScrollContainer.Visible;
			if (activeObjectEditorObjects.Count == 0)
			{
				return;
			}
			objectScrollContainer.Visible = currentVisibility;
			objectScrollContainer.SetProcessInput(currentVisibility);
			dummyTree.Visible = currentVisibility;

			foreach (var objSet in activeObjectEditorObjects)
			{
				objSet.Value.Visible = currentVisibility;
			}

			ToggleObjectScrollContainerCollision();
		}

		public static void ToggleObjectScrollContainerCollision()
		{
			var viewPortSize = OEMainViewPort.GetVisibleRect().Size;
			var objPanelShape = (RectangleShape2D)objectPanelCollision.Shape;
			if (objectScrollContainer.Visible == false)
			{
				objPanelShape.Size = new Vector2(objPanelShape.Size.X, 0);
			}
			else
			{
				objPanelShape.Size = new Vector2(objPanelShape.Size.X, (viewPortSize.Y - 31));
			}
			objectPanelCollision.Shape = objPanelShape;
		}

		/// <summary>
		/// Method for handling what happens when we select an option under the File menu
		/// </summary>
		public static void OnFileButtonMenuSelection(long id)
		{
			switch (id)
			{
				case 0:
					if (setDataTree != null)
					{
						getMainGameFileDialog.Show();
					}
					break;
				case 1:
					SaveData();
					break;
				case 2:
					confirmCopyModFilesDialog.Show();
					break;
				case 3:
					confirmRestoreBackupFilesDialog.Show();
					break;
			}
		}

		/// <summary>
		/// Method for handling what happens when we select an option under the Edit menu
		/// </summary>
		public static void OnEditButtonMenuSelection(long id)
		{
			switch (id)
			{
				case 0:
					CopyObjectData();
					break;
				case 1:
					PasteTransformData();
					break;
				case 2:
					PasteNonTransformData();
					break;
				case 3:
					PasteFullObjectData();
					break;
				case 4:
					DropObjToNearestSolid();
                    break;
			}
		}

		public static void OnDisplayButtonMenuSelection(int id)
		{
			DisplayBtn.GetPopup().SetItemChecked(id, !DisplayBtn.GetPopup().IsItemChecked(id));
			var showTerrain = DisplayBtn.GetPopup().IsItemChecked(0);
			var showCollision = DisplayBtn.GetPopup().IsItemChecked(1);

			foreach(var trn in terrainModels)
			{
				trn.Visible = showTerrain;
			}
			foreach (var trn in terrainCollision)
			{
				trn.Visible = showCollision;
			}
        }

        public static void OnSettingsButtonMenuSelection(int id)
        {
			var previousIsDay = isDay;
			var previousTransformGizmoWorld = TransformGizmoWorld;

            SettingsBtn.GetPopup().SetItemChecked(id, !SettingsBtn.GetPopup().IsItemChecked(id));
            isDay = SettingsBtn.GetPopup().IsItemChecked(0);
            TransformGizmoWorld = SettingsBtn.GetPopup().IsItemChecked(1);
            WarpCameraToNewSelection = SettingsBtn.GetPopup().IsItemChecked(2);

			//Handle day/night settings
			if(previousIsDay != isDay)
            {
                if (daySkybox != null)
                {
                    daySkybox.Visible = isDay;
                }
                if (nightSkybox != null)
                {
                    nightSkybox.Visible = !isDay;
                }
                ModelConversion.BillyModeNightToggleParent(modelRoot);
            }

			//World transform toggle
			if(previousTransformGizmoWorld != TransformGizmoWorld)
            {
                SetGizmoWorldStatus(TransformGizmoWorld);
            }
        }

        public static void CopyObjectData()
		{
			switch(gameType)
			{
				case GameType.BillyPC:
				case GameType.BillyGC:
					BillyCopyObjectData();
					break;
				default:
					throw new NotImplementedException();
			}
		}

		public static void PasteTransformData()
		{
			switch (gameType)
			{
				case GameType.BillyPC:
				case GameType.BillyGC:
					BillyPasteTransformData();
					break;
			}
		}

		public static void PasteNonTransformData()
		{
			switch (gameType)
			{
				case GameType.BillyPC:
				case GameType.BillyGC:
					BillyPasteNonTransformData();
					break;
			}
		}

		public static void PasteFullObjectData()
		{
			switch (gameType)
			{
				case GameType.BillyPC:
				case GameType.BillyGC:
					BillyPasteFullObjectData();
					break;
			}
		}

		/// <summary>
		/// Attempts to drop an object from its current position in space to the next lowest solid, if one exists.
		/// This way, we can place objects flush with the ground rather than having to guesstimate constantly.
		/// </summary>
		public static void DropObjToNearestSolid()
		{
            switch (gameType)
            {
                case GameType.BillyPC:
                case GameType.BillyGC:
                    BillyDropObjToNearestSolid();
                    break;
            }
        }

		private static void ClearModelAndTextureData()
        {
            terrainModels.Clear();
            terrainCollision.Clear();
            foreach (var child in modelRoot.GetChildren())
			{
				modelRoot.RemoveChild(child);
				child.QueueFree();
			}
			modelDictionary.Clear();
			foreach (var set in globalTexturePool)
			{
				set.Value.Dispose();
			}
			globalTexturePool.Clear();
			foreach (var set in orderedTextureArchivePools)
			{
				foreach(var tex in set.Value)
				{
					tex.Dispose();
				}
				set.Value.Clear();
			}
			orderedTextureArchivePools.Clear();
		}

		/// <summary>
		/// Resets editor data when we try to load a new project to avoid shenanigans.
		/// </summary>
		public static void ResetLoadedData(string path)
		{
			ViewCamera.SetToFreecam();
			ResetTransformGizmo();
			terrainModels.Clear();
			terrainCollision.Clear();
			if(modelRoot.GetWorld3D().Environment != null)
			{
				modelRoot.GetWorld3D().Environment.Dispose();
				modelRoot.GetWorld3D().Environment = null;
			}
			modelRoot.GetWorld3D().Environment = new Godot.Environment();
			//Project level environment color is NOT reset, but we may want to just set that with the game
			modFolderLocation = null;
			allowedToUpdate = false;

			currentObjectId = -1;
			currentArchiveFileId = -1;
			currentArhiveFilename = null;
			currentMissionId = -1;
			currentObjectTreeItem = null;
			currentEditorType = EditingType.None;

			switch (gameType)
			{
				case GameType.BillyPC:
				case GameType.BillyGC:
					ResetBillyLoadedData();
					break;
			}

			var objDataContainer = (VBoxContainer)objectScrollContainer.GetChild(0);
			foreach (var obj in activeObjectEditorObjects)
			{
				//Assume the object is in there. If it's not, we have some problems.
				objDataContainer.RemoveChild(obj.Value);
			}
			activeObjectEditorObjects.Clear();

			if (setDataTree != null)
			{
				setDataTree.Clear();
			}

			ClearModelAndTextureData();
		}

		public static void ResetTransformGizmo()
		{
			TransformGizmo.SetCurrentTransformType(Gizmo.TransformType.None);
			TransformGizmo.Reparent(TransformGizmo.GetTree().Root, false);
			SetGizmoWorldStatus(TransformGizmoWorld);
        }

		/// <summary>
		/// Method for handling what happens when we load a game
		/// </summary>
		public static void LoadInitialData(string path)
		{
			//Handle if we loaded a mod project
			if (path.Contains(".oeproj"))
			{
				modFolderLocation = Path.GetDirectoryName(path);
				var modSettings = File.ReadAllLines(path);
				if (modSettings.Length > 0)
				{
					path = modSettings[0];

					if (!File.Exists(path))
					{
						path = null;
					}
				}
				else
				{
					path = null;
				}
			}

			//If we loaded a project and it didn't have a valid path, try to have the user choose a game folder
			if (path == null)
			{
				getOnlyGameFileDialog.Show();
				return;
			}

			//Decide what game type this is
			switch (Path.GetFileName(path).ToLower())
			{
				case "amem_boot.nrc":
					if (File.Exists(Path.Combine(Path.GetDirectoryName(path), "billyhatcher.exe")))
					{
						gameType = GameType.BillyPC;
					}
					else
					{
						gameType = GameType.BillyGC;
					}
					break;
				default:
					gameType = GameType.None;
					OS.Alert("Please select a proper file!");
					return;
			}

			setDataTree.Clear();
			gameFolderLocation = Path.GetDirectoryName(path);

			//Set up backup file folder
			backupFolderLocation = Path.Combine(gameFolderLocation, "BackupFiles");
			Directory.CreateDirectory(backupFolderLocation);

			//Set up mod project folder, if not already set
			if (modFolderLocation == null)
			{
				modFolderLocation = Path.Combine(gameFolderLocation, "ModProject");
				Directory.CreateDirectory(modFolderLocation);
				File.WriteAllText(Path.Combine(modFolderLocation, "mod.oeproj"), path);
			}

			//Do initial loading for game specific things
			switch (gameType)
			{
				case GameType.BillyPC:
				case GameType.BillyGC:
					LoadInitialDataBilly();
					break;
			}

			//Don't readd this method if it's already there
			if (setDataTreeItemActivatedSet == false)
			{
				setDataTree.ItemActivated += HandleTreeNodeActivate;
				setDataTree.CellSelected += HandleTreeNodeSelected;
				setDataTreeItemActivatedSet = true;
			}
		}

		/// <summary>
		/// Update the internal data we're editing so it's ready to save back to the disk
		/// </summary>
		public static void UpdateData(bool updateFromForm = true)
		{
			if (allowedToUpdate)
			{
				switch (currentEditorType)
				{
					case EditingType.BillySetObj:
						UpdateBillySetObjects(loadedBillySetObjects, currentObjectId, false);
						LoadBillySetObjectTemplateInfo(loadedBillySetObjects);
						break;
					case EditingType.BillySetDesign:
						UpdateBillySetObjects(loadedBillySetDesignObjects, currentObjectId, true);
						LoadBillySetObjectTemplateInfo(loadedBillySetDesignObjects);
						break;
					case EditingType.BillySpawnPoint:
						UpdateBillySpawnPoint(currentObjectId);
						break;
					case EditingType.BillySetEnemy:
						UpdateBillySetEnemies(currentObjectId);
						LoadBillySetEnemyTemplateInfo();
						break;
				}
				SetGizmoWorldStatus(TransformGizmoWorld);
			}
		}

		/// <summary>
		/// Save our current data back to the disk
		/// </summary>
		public static void SaveData()
		{
			switch (gameType)
			{
				case GameType.BillyGC:
					SaveDataBillyGC();
					break;
				case GameType.BillyPC:
					SaveDataBillyPC();
					break;
			}
		}

		public static void LoadSetObject()
		{
			//Force visible if it's not
			if (objectScrollContainer.Visible == false)
			{
				OnObjectScrollContainerButtonReleased();
			}
			dummyTree.Visible = true;
			switch (currentEditorType)
			{
				case EditingType.BillySetObj:
					LoadBillySetObject(loadedBillySetObjects);
					break;
				case EditingType.BillySetDesign:
					LoadBillySetObject(loadedBillySetDesignObjects);
					break;
				case EditingType.BillySpawnPoint:
					LoadBillySpawn();
					break;
				case EditingType.BillySetEnemy:
					LoadBillyEnemy();
					break;
			}
		}

		public static void HandleTreeNodeActivate()
		{
			var activeNode = setDataTree.GetSelected();
			switch (activeNode.GetMetadata(0).AsInt32())
			{
				//Mission Node
				case 1:
					ResetTransformGizmo();
					ViewCamera.SetToFreecam();
					foreach (TreeItem item in setDataTree.GetRoot().GetChildren())
					{
						if (item == activeNode)
						{
							continue;
						}
						if (item.Collapsed != true)
						{
							item.Collapsed = true;
						}
						foreach (var childChild in item.GetChildren())
						{
							childChild.Free();
						}
					}

					//Don't add duplicate instances
					if (activeNode.GetChildCount() > 0)
					{
						break;
					}

					//Load Data for current area
					currentMissionId = activeNode.GetMetadata(1).AsInt32();
					LazyLoadAreaData();

					//Populate Set Objects
					switch (gameType)
					{
						case GameType.BillyPC:
						case GameType.BillyGC:
							PopulateSetObjectsBilly(activeNode);
							break;
					}
					activeNode.Collapsed = false;
					break;
				//mission Node Data Type - SetObject, Camera, etc.
				case 2:
					break;
				//Object Node
				case 3:
					break;
			}
		}

		public static void HandleTreeNodeSelected()
		{
			var activeNode = setDataTree.GetSelected();
			var parentNode = activeNode.GetParent();

			switch (activeNode.GetMetadata(0).AsInt32())
			{
				//Mission Node
				case 1:
				//mission Node Data Type - SetObject, Camera, etc.
				case 2:
					break;
				//Object Node
				case 3:
					currentObjectId = activeNode.GetMetadata(1).AsInt32();
					currentEditorType = (EditingType)(activeNode.GetMetadata(2).AsInt32());
					currentObjectTreeItem = activeNode;

					//Position camera on object
					var activeNode3d = (Node3D)activeNode.GetMetadata(3);
					HandleEditorObjectsOnselection(activeNode3d);

					//Load object GUI controls
					LoadSetObject();
					setDataTree.ScrollToItem(activeNode);
					break;
			}
		}

		private static void HandleEditorObjectsOnselection(Node3D activeNode3d)
        {
            TransformGizmo.Reparent(activeNode3d, false);
            TransformGizmo.SetCurrentTransformType(OverEasy.Editor.Gizmo.TransformType.Translation);
            SetGizmoWorldStatus(TransformGizmoWorld);
            if (WarpCameraToNewSelection)
            {
                ViewCamera.orbitFocusNode = activeNode3d;
                ViewCamera.TrySetOrbitCam();
                ViewCamera.oneTimeProcessTransform = true;
                ViewCamera.ToggleMode();
            }
		}

		/// <summary>
		/// Method for lazy loading area data. Should be called upon expansion of a node.
		/// </summary>
		public static void LazyLoadAreaData()
		{
			switch (gameType)
			{
				case GameType.BillyPC:
					LazyLoadBillyPC();
					break;
				case GameType.BillyGC:
					LazyLoadBillyGC();
					break;
			}
		}

		public static void LoadMapNames(string mapNamesReference, bool clearNames = true)
		{
			if (clearNames)
			{
				mapKeyOrderList.Clear();
			}
			var path = Path.Combine(editorRootDirectory, mapNamesReference);
			var mapNames = File.ReadAllLines(path);
			foreach (var pair in mapNames)
			{
				var parts = pair.Split(' ', 2);
				OverEasyGlobals.mapNames[parts[0]] = parts[1];
				mapKeyOrderList.Add(parts[0]);
			}
		}

		public static void LoadSetObjTemplates(string setObjDefinitionsReference)
		{
			var definitions = Directory.GetFiles(Path.Combine(editorRootDirectory, setObjDefinitionsReference));
			foreach (var defPath in definitions)
			{
				var fileName = Path.GetFileNameWithoutExtension(defPath);
				if (Int32.TryParse(fileName, out int id))
				{
					cachedBillySetObjDefinitions.Add(id, JsonSerializer.Deserialize<TextInfo.SetObjDefinition>(File.ReadAllText(defPath)));
				}
			}
		}

		public static void LoadSetEnemyTemplates(string setEnemyDefinitionsReference)
		{
			var definitions = Directory.GetFiles(Path.Combine(editorRootDirectory, setEnemyDefinitionsReference));
			foreach (var defPath in definitions)
			{
				var fileName = Path.GetFileNameWithoutExtension(defPath);
				if (Int32.TryParse(fileName, out int id))
				{
					cachedBillySetEnemyDefinitions.Add(id, JsonSerializer.Deserialize<TextInfo.SetEnemyDefinition>(File.ReadAllText(defPath)));
				}
			}
		}

		/// <summary>
		/// For files where we want to prioritize the modified path vs the game path, but load the game path as a fallback
		/// </summary>
		public static string GetAssetPath(string fileName)
		{
			if (fileName == "" || fileName == null)
			{
				return "";
			}
			var modPath = Path.Combine(modFolderLocation, fileName);
			if (File.Exists(modPath))
			{
				return modPath;
			}
			else
			{
				return Path.Combine(gameFolderLocation, fileName);
			}
		}

		public static void TransformFromGizmo(Vector3? pos, Quaternion? rot, Vector3? scale)
		{
			switch(gameType)
			{
				case GameType.BillyPC:
				case GameType.BillyGC:
					TransformFromGizmoBilly(pos, rot, scale);
					break;
			}
			SetGizmoWorldStatus(TransformGizmoWorld);
		}
	}
}
