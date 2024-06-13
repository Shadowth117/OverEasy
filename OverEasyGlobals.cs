using AquaModelLibrary.Data.BillyHatcher;
using AquaModelLibrary.Data.BillyHatcher.SetData;
using AquaModelLibrary.Data.Ninja;
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace OverEasy
{
	public class OverEasyGlobals
	{
		//Schema templates
		public static VBoxContainer ColorSchemaTemplate = null;
		public static VBoxContainer FloatSchemaTemplate = null;
		public static VBoxContainer IntSchemaTemplate = null;
		public static VBoxContainer LabelSchemaTemplate = null;
		public static VBoxContainer StringSchemaTemplate = null;
		public static VBoxContainer Vector2SchemaTemplate = null;
		public static VBoxContainer Vector3SchemaTemplate = null;
		public static VBoxContainer Vector4SchemaTemplate = null;

		public static CollisionShape2D MenuBarCollision = null;
		public static CollisionShape2D setDataTreeCollision = null;
		public static CollisionShape2D setDataTreeButtonCollision = null;
		public static CollisionShape2D objectPanelCollision = null;
		public static CollisionShape2D objectPanelButtonCollision = null;

		public static string gameFolderLocation = null;
		public static string backupFolderLocation = null;
		public static string modFolderLocation = null;
		public static GameType gameType = GameType.None;
		public static List<string> mapKeyOrderList = new List<string>();
		public static Dictionary<string, string> mapNames = new Dictionary<string, string>();

		public static bool mouseInGuiArea = false;
		public static SceneTree OESceneTree = null;
		public static Viewport OEMainViewPort = null;
		public static Tree setDataTree = null;

		public static bool setDataTreeItemActivatedSet = false;
		public static ScrollContainer objectScrollContainer = null;
		public static Button setDataTreeButton = null;
		public static Button objectScrollContainerButton = null;

		public static StageDef stgDef = null;
		public static SetObjList loadedBillySetObjects = null;
		public static SetObjList loadedBillySetDesignObjects = null;
		public static Dictionary<int, SetObjDefinition> cachedBillySetObjDefinitions = new Dictionary<int, SetObjDefinition>();
		public static EditingType currentEditorType = EditingType.None;
		public static bool allowedToUpdate = false;

		public static int currentObjectId = -1;
		public static int currentArchiveFileId = -1;
		public static PRD currentPRD = null;
		public static string currentArhiveFilename = null;
		public static int currentStageDefId = -1;
		public static string editorRootDirectory = null;
		public static TreeItem currentObjectTreeItem = null;

        public static bool CanMove3dCamera 
		{ 
			get 
			{
				return !mouseInGuiArea;
			} 
		}

        public static Dictionary<string, Container> activeObjectEditorObjects = new Dictionary<string, Container>();

		public enum EditingType
		{
			None = 0,
			BillySetObj = 1,
			BillySetDesign = 2,
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
			editorRootDirectory = Path.GetDirectoryName(ProjectSettings.GlobalizePath("res://"));
			getMainGameFileDialog.FileSelected += ResetLoadedData;
			getMainGameFileDialog.FileSelected += LoadData;
			getOnlyGameFileDialog.FileSelected += LoadData;

			confirmCopyModFilesDialog.Confirmed += CopyModFiles;
			confirmRestoreBackupFilesDialog.Confirmed += RestoreBackupFiles;
		}

		public static void AttachDialogs()
		{
			OEMainViewPort.AddChild(confirmCopyModFilesDialog);
			OEMainViewPort.AddChild(confirmRestoreBackupFilesDialog);
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
		/// Resets editor data when we try to load a new project to avoid shenanigans.
		/// </summary>
		public static void ResetLoadedData(string path)
		{
			modFolderLocation = null;
			allowedToUpdate = false;

			currentObjectId = -1;
			currentArchiveFileId = -1;
			currentPRD = null;
			currentArhiveFilename = null;
			currentStageDefId = -1;
			currentObjectTreeItem = null;
			stgDef = null;
			loadedBillySetObjects = null;
			cachedBillySetObjDefinitions.Clear();
			currentEditorType = EditingType.None;
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
		}

		/// <summary>
		/// Actions for when the window size changes
		/// </summary>
		public static void OnWindowSizeChanged()
		{
			var viewPortSize = OEMainViewPort.GetVisibleRect().Size;
			setDataTree.Size = new Vector2(300, viewPortSize.Y - 31);
			setDataTreeButton.Size = new Vector2(setDataTreeButton.Size.X, viewPortSize.Y - 31);
			objectScrollContainer.Size = new Vector2(objectScrollContainer.Size.X, viewPortSize.Y - 31);
			objectScrollContainerButton.Size = new Vector2(objectScrollContainerButton.Size.X, viewPortSize.Y - 31);

			setDataTreeCollision.GlobalPosition = new Vector2(setDataTreeCollision.GlobalPosition.X, 31 + (viewPortSize.Y - 31) / 2);
			var setDataTreeShape = (RectangleShape2D)setDataTreeCollision.Shape;
			if(setDataTree.Visible == false)
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
			if(objectScrollContainer.Visible == false)
			{
				objPanelShape.Size = new Vector2(objPanelShape.Size.X, 0);
			} else
            {
                objPanelShape.Size = new Vector2(objPanelShape.Size.X, (viewPortSize.Y - 31));
            }
			objectPanelCollision.Shape = objPanelShape;

            setDataTreeButtonCollision.GlobalPosition = new Vector2(setDataTreeButtonCollision.GlobalPosition.X, 31 + (viewPortSize.Y - 31) / 2);
            var setDataTreeButtonShape = (RectangleShape2D)setDataTreeButtonCollision.Shape;
            setDataTreeButtonShape.Size = new Vector2(setDataTreeButtonShape.Size.X, (viewPortSize.Y - 31));
            setDataTreeButtonCollision.Shape = setDataTreeButtonShape;

            objectPanelButtonCollision.GlobalPosition = new Vector2(objectPanelButtonCollision.GlobalPosition.X, 31 +(viewPortSize.Y - 31) / 2);
            var objPanelButtonShape = (RectangleShape2D)objectPanelButtonCollision.Shape;
            objPanelButtonShape.Size = new Vector2(objPanelButtonShape.Size.X, (viewPortSize.Y - 31));
            objectPanelButtonCollision.Shape = objPanelButtonShape;
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
			objectScrollContainer.Visible = currentVisibility;
			objectScrollContainer.SetProcessInput(currentVisibility);

			foreach (var objSet in activeObjectEditorObjects)
			{
				objSet.Value.Visible = currentVisibility;
			}

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
		/// Method for handling what happens when we load a game
		/// </summary>
		public static void LoadData(string path)
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
					LoadMapNames("TextInfo/BillyMapNames.txt");
					LoadSetObjTemplates("TextInfo/BillyObjDefinitions/");
					if (!ReadGEStageDef(modFolderLocation))
					{
						ReadGEStageDef(gameFolderLocation);
					}

					var tempList = stgDef.defs.ToList();

					var root = setDataTree.CreateItem();
					setDataTree.HideRoot = true;
					var defsDictKeys = stgDef.defsDict.Keys.ToList();
					foreach (var mission in mapKeyOrderList)
					{
						if (defsDictKeys.Contains(mission))
						{
							var node = setDataTree.CreateItem(root);
							string description = "";
							if(mapNames.ContainsKey(mission))
							{
								description = $"{mapNames[mission]}";
							}
							node.SetText(0, $"{mission} - {description}");

							//Store node type as int, store actual stageDef id for later use
							node.SetMetadata(0, 1);
							node.SetMetadata(1, stgDef.defs.IndexOf(stgDef.defsDict[mission]));

							defsDictKeys.Remove(mission);
						}
					}

					//Catch unlisted missions
					foreach(var mission in defsDictKeys)
					{
						var node = setDataTree.CreateItem(root);
						node.SetText(0, $"{mission}");

						//Store node type as int, store actual stageDef id for later use
						node.SetMetadata(0, 1);
						node.SetMetadata(1, stgDef.defs.IndexOf(stgDef.defsDict[mission]));
					}
					break;
			}
			if (setDataTreeItemActivatedSet == false)
			{
				setDataTree.ItemActivated += HandleSetNodeExpand;
				setDataTreeItemActivatedSet = true;
			}
		}

		/// <summary>
		/// Update the internal data we're editing so it's ready to save back to the disk
		/// </summary>
		public static void UpdateData()
		{
			if (allowedToUpdate)
			{
				switch (currentEditorType)
				{
					case EditingType.BillySetObj:
						UpdateBillySetObjects(loadedBillySetObjects);
						LoadBillySetObjectTemplateInfo(loadedBillySetObjects);
						break;
					case EditingType.BillySetDesign:
						UpdateBillySetObjects(loadedBillySetDesignObjects);
						LoadBillySetObjectTemplateInfo(loadedBillySetDesignObjects);
						break;
				}
			}
		}

		/// <summary>
		/// Save our current data back to the disk
		/// </summary>
		public static void SaveData()
		{
			//When saving, because things are in archives, we try to save everything together.
			var setName = stgDef.defs[currentStageDefId].setObjFilename;
			var setDesignName = stgDef.defs[currentStageDefId].setObjFilename;
			switch (gameType)
			{
				case GameType.BillyGC:
					int setFileId = -1;
					int setDesignId = -1;
					for (int i = 0; i < currentPRD.fileNames.Count; i++)
					{
						if (currentPRD.fileNames[i] == setName)
						{
							setFileId = i;
						}
						else if (currentPRD.fileNames[i] == setDesignName)
						{
							setDesignId = i;
						}
					}
					if (setFileId != -1)
					{
						currentPRD.files[setFileId] = loadedBillySetObjects.GetBytes();
					}
					if (setDesignId != -1)
					{
						currentPRD.files[setDesignId] = loadedBillySetDesignObjects.GetBytes();
					}
					File.WriteAllBytes(Path.Combine(modFolderLocation, currentArhiveFilename), currentPRD.GetBytes());
					break;
				case GameType.BillyPC:
					File.WriteAllBytes(Path.Combine(modFolderLocation, setName), loadedBillySetObjects.GetBytes());
					File.WriteAllBytes(Path.Combine(modFolderLocation, setDesignName), loadedBillySetDesignObjects.GetBytes());
					break;
			}
		}

		/// <summary>
		/// Method for lazy loading area data. Should be called upon expansion of a node.
		/// </summary>
		public static void LazyLoadAreaData()
		{
			var def = stgDef.defs[currentStageDefId];
			switch (gameType)
			{
				case GameType.BillyPC:

					//Load Set Design
					string setDesignFilePath = GetAssetPath(def.setDesignFilename);
					if (File.Exists(setDesignFilePath))
					{
						loadedBillySetDesignObjects = new SetObjList(File.ReadAllBytes(setDesignFilePath));
					}

					//Load Set Objects
					string setObjFilePath = GetAssetPath(def.setObjFilename);
					if (File.Exists(setObjFilePath))
					{
						loadedBillySetObjects = new SetObjList(File.ReadAllBytes(setObjFilePath));
					}

					break;
				case GameType.BillyGC:
					var prdMissionname = GetBillyMissionName(def.missionName);
					currentPRD = new PRD(File.ReadAllBytes(GetAssetPath($"k_{prdMissionname}.prd")));

					for (int i = 0; i < currentPRD.files.Count; i++)
					{
						//Load Set Camera

						//Load Set Design
						if (currentPRD.fileNames[i] == def.setDesignFilename)
						{
							loadedBillySetDesignObjects = new SetObjList(currentPRD.files[i]);
						}

						//Load Set Enemeies

						//Load Set Objects
						if (currentPRD.fileNames[i] == def.setObjFilename)
						{
							loadedBillySetObjects = new SetObjList(currentPRD.files[i]);
						}

						//Load Stage Model

						//Load Stage Object models (We need to link these somehow...)

						//Load Stage Collision Model

						//Load Stage Path

						//Load Stage Event Camera

						//When figured out, load stage event file

						//Load stage bsp?
					}


					//Load common object models, enemies, items. May want to do this in the initial load step
					break;
			}
		}

		/// <summary>
		/// For some reason this one mission doesn't correlate to the actual mission name.
		/// Billy mission names are technically hardcoded, but in most cases we can infer them from the stagedef mission name anyways.
		/// </summary>
		private static string GetBillyMissionName(string missionName)
		{
			if (missionName == "last")
			{
				missionName = "last1";
			}

			return missionName;
		}

		/// <summary>
		/// For files where we want to prioritize the modified path vs the game path, but load the game path as a fallback
		/// </summary>
		public static string GetAssetPath(string fileName)
		{
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

		public static void UpdateBillySetObjects(SetObjList setObjList)
		{
			//Gather current object values
			var objRaw = setObjList.setObjs[currentObjectId];
			foreach (var objSet in activeObjectEditorObjects)
			{
				switch (objSet.Key)
				{
					case "ObjectId":
						objRaw.objectId = (int)GetSpinBoxValue("ObjectId");
						break;
					case "ObjectPosition":
						objRaw.Position = GetVec3SchemaValues("ObjectPosition");
						break;
					case "ObjectRotation":
						var objRotation = GetVec3SchemaValues("ObjectRotation");
						objRaw.BAMSRotation = new AquaModelLibrary.Data.DataTypes.Vector3Int.Vec3Int((int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.X),
							(int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.Y), (int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.Z));
						break;
					case "IntProperty1":
						objRaw.intProperty1 = (int)GetSpinBoxValue("IntProperty1");
						break;
					case "IntProperty2":
						objRaw.intProperty2 = (int)GetSpinBoxValue("IntProperty2");
						break;
					case "IntProperty3":
						objRaw.intProperty3 = (int)GetSpinBoxValue("IntProperty3");
						break;
					case "IntProperty4":
						objRaw.intProperty4 = (int)GetSpinBoxValue("IntProperty4");
						break;
					case "FloatProperty1":
						objRaw.fltProperty1 = (float)GetSpinBoxValue("FloatProperty1");
						break;
					case "FloatProperty2":
						objRaw.fltProperty2 = (float)GetSpinBoxValue("FloatProperty2");
						break;
					case "FloatProperty3":
						objRaw.fltProperty3 = (float)GetSpinBoxValue("FloatProperty3");
						break;
					case "FloatProperty4":
						objRaw.fltProperty4 = (float)GetSpinBoxValue("FloatProperty4");
						break;
					case "ByteProperty1":
						objRaw.btProperty1 = (byte)GetSpinBoxValue("ByteProperty1");
						break;
					case "ByteProperty2":
						objRaw.btProperty2 = (byte)GetSpinBoxValue("ByteProperty2");
						break;
					case "ByteProperty3":
						objRaw.btProperty3 = (byte)GetSpinBoxValue("ByteProperty3");
						break;
					case "ByteProperty4":
						objRaw.btProperty4 = (byte)GetSpinBoxValue("ByteProperty4");
						break;
				}
			}

			//Insert over original file object values
			setObjList.setObjs[currentObjectId] = objRaw;
		}

		public static void LoadSetObject()
		{
			//Force visible if it's not
			if(objectScrollContainer.Visible == false)
			{
				OnObjectScrollContainerButtonReleased();
			}
            switch (currentEditorType)
			{
				case EditingType.BillySetObj:
					LoadBillySetObject(loadedBillySetObjects);
					break;
				case EditingType.BillySetDesign:
					LoadBillySetObject(loadedBillySetDesignObjects);
					break;
			}
		}

		public static void LoadBillySetObject(SetObjList setObjList)
		{
			allowedToUpdate = false;
			foreach (var objSet in activeObjectEditorObjects)
			{
				objSet.Value.Free();
			}
			activeObjectEditorObjects.Clear();

			//Load in object data
			var setObj = setObjList.setObjs[currentObjectId];
			LoadBillySetObjectGui(setObjList);
			LoadBillySetObjectTemplateInfo(setObjList);
			allowedToUpdate = true;
		}

		public static void LoadBillySetObjectTemplateInfo(SetObjList setObjList)
		{
			var setObj = setObjList.setObjs[currentObjectId];
			//Check if there's a template for this object type, else we use the defaults
			var template = new SetObjDefinition();
			var key = setObj.objectId;

			//Check if we've loaded this template already
			if (cachedBillySetObjDefinitions.ContainsKey(key))
			{
				template = cachedBillySetObjDefinitions[key];

			}
			else
			{
				var templatePath = Path.Combine(editorRootDirectory, @"BillyHatcher\\SetData\\SetObjDefinitions", $"{setObj.objectId}.json");
				if (File.Exists(templatePath))
				{
					template = JsonSerializer.Deserialize<SetObjDefinition>(File.ReadAllText(templatePath));
					cachedBillySetObjDefinitions.Add(key, template);
				}
			}

			if (activeObjectEditorObjects.ContainsKey("ObjectId"))
			{
				var objName = activeObjectEditorObjects["ObjectId"];
				var objNameText = (RichTextLabel)objName.GetChild(0);
				objNameText.Text = template?.ObjectName != "" && template.ObjectName != null ? template.ObjectName : $"Object {setObj.objectId}";
				currentObjectTreeItem.SetText(0, template?.ObjectName != "" && template.ObjectName != null ? template.ObjectName : $"Object Type {setObj.objectId}");
			}

			LoadVec3SchemaTemplateInfo("ObjectPosition", template.Position, "Position", template.PositionHint, template.PositionX, template.PositionY, template.PositionZ);
			LoadVec3SchemaTemplateInfo("ObjectRotation", template.Rotation, "Rotation", template.RotationHint, template.RotationX, template.RotationY, template.RotationZ);
			LoadSchemaTemplateInfo("IntProperty1", template.IntProperty1, "Int Property 1", template.IntProperty1Hint);
			LoadSchemaTemplateInfo("IntProperty2", template.IntProperty2, "Int Property 2", template.IntProperty2Hint);
			LoadSchemaTemplateInfo("IntProperty3", template.IntProperty3, "Int Property 3", template.IntProperty3Hint);
			LoadSchemaTemplateInfo("IntProperty4", template.IntProperty4, "Int Property 4", template.IntProperty4Hint);
			LoadSchemaTemplateInfo("FloatProperty1", template.FloatProperty1, "Float Property 1", template.FloatProperty1Hint);
			LoadSchemaTemplateInfo("FloatProperty2", template.FloatProperty2, "Float Property 2", template.FloatProperty2Hint);
			LoadSchemaTemplateInfo("FloatProperty3", template.FloatProperty3, "Float Property 3", template.FloatProperty3Hint);
			LoadSchemaTemplateInfo("FloatProperty4", template.FloatProperty4, "Float Property 4", template.FloatProperty4Hint);
			LoadSchemaTemplateInfo("ByteProperty1", template.ByteProperty1, "Byte Property 1", template.ByteProperty1Hint);
			LoadSchemaTemplateInfo("ByteProperty2", template.ByteProperty2, "Byte Property 2", template.ByteProperty2Hint);
			LoadSchemaTemplateInfo("ByteProperty3", template.ByteProperty3, "Byte Property 3", template.ByteProperty3Hint);
			LoadSchemaTemplateInfo("ByteProperty4", template.ByteProperty4, "Byte Property 4", template.ByteProperty4Hint);
		}

		public static void LoadBillySetObjectGui(SetObjList setObjList)
		{
			var setObj = setObjList.setObjs[currentObjectId];

			CreateIntSchema("ObjectId", setObj.objectId);
			CreateVector3Schema("ObjectPosition", new Vector3(setObj.Position.X, setObj.Position.Y, setObj.Position.Z));
			CreateVector3Schema("ObjectRotation", new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * setObj.BAMSRotation.X),
				(float)(NinjaConstants.FromBAMSValueToDegrees * setObj.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * setObj.BAMSRotation.Z)));
			CreateIntSchema("IntProperty1", setObj.intProperty1);
			CreateIntSchema("IntProperty2", setObj.intProperty2);
			CreateIntSchema("IntProperty3", setObj.intProperty3);
			CreateIntSchema("IntProperty4", setObj.intProperty4);
			CreateFloatSchema("FloatProperty1", setObj.fltProperty1);
			CreateFloatSchema("FloatProperty2", setObj.fltProperty2);
			CreateFloatSchema("FloatProperty3", setObj.fltProperty3);
			CreateFloatSchema("FloatProperty4", setObj.fltProperty4);
			CreateByteSchema("ByteProperty1", setObj.btProperty1);
			CreateByteSchema("ByteProperty2", setObj.btProperty2);
			CreateByteSchema("ByteProperty3", setObj.btProperty3);
			CreateByteSchema("ByteProperty4", setObj.btProperty4);

		}

		public static void CreateVector2Schema(string key, Vector2 value)
		{
			var newSchema = (VBoxContainer)Vector3SchemaTemplate.Duplicate();

			var paramX = (SpinBox)newSchema.GetChild(1).GetChild(0).GetChild(1);
			paramX.Value = value.X;
			var paramY = (SpinBox)newSchema.GetChild(1).GetChild(1).GetChild(1);
			paramY.Value = value.Y;
			newSchema.Visible = true;

			objectScrollContainer.GetChild(0).AddChild(newSchema);
			activeObjectEditorObjects[key] = newSchema;
		}

		public static void CreateVector3Schema(string key, Vector3 value)
		{
			var newSchema = (VBoxContainer)Vector3SchemaTemplate.Duplicate();

			var paramX = (SpinBox)newSchema.GetChild(1).GetChild(0).GetChild(1);
			paramX.Value = value.X;
			var paramY = (SpinBox)newSchema.GetChild(1).GetChild(1).GetChild(1);
			paramY.Value = value.Y;
			var paramZ = (SpinBox)newSchema.GetChild(1).GetChild(2).GetChild(1);
			paramZ.Value = value.Z;
			newSchema.Visible = true;

			objectScrollContainer.GetChild(0).AddChild(newSchema);
			activeObjectEditorObjects[key] = newSchema;
		}

		public static void CreateVector4Schema(string key, Vector4 value)
		{
			var newSchema = (VBoxContainer)Vector3SchemaTemplate.Duplicate();

			var paramX = (SpinBox)newSchema.GetChild(1).GetChild(0).GetChild(1);
			paramX.Value = value.X;
			var paramY = (SpinBox)newSchema.GetChild(1).GetChild(1).GetChild(1);
			paramY.Value = value.Y;
			var paramZ = (SpinBox)newSchema.GetChild(1).GetChild(2).GetChild(1);
			paramZ.Value = value.Z;
			var paramW = (SpinBox)newSchema.GetChild(1).GetChild(3).GetChild(1);
			paramW.Value = value.W;
			newSchema.Visible = true;

			objectScrollContainer.GetChild(0).AddChild(newSchema);
			activeObjectEditorObjects[key] = newSchema;
		}

		public static void CreateFloatSchema(string key, double value)
		{
			CreateFloatingPointSchema(key, value);
		}

		public static void CreateDoubleSchema(string key, double value)
		{
			CreateFloatingPointSchema(key, value);
		}

		public static void CreateFloatingPointSchema(string key, double value)
		{
			var newSchema = (VBoxContainer)FloatSchemaTemplate.Duplicate();

			var paramValue = (SpinBox)newSchema.GetChild(1);
			paramValue.Value = value;
			newSchema.Visible = true;

			objectScrollContainer.GetChild(0).AddChild(newSchema);
			activeObjectEditorObjects[key] = newSchema;
		}

		public static void CreateByteSchema(string key, long value)
		{
			CreateIntegerSchema(key, value, byte.MinValue, byte.MaxValue);
		}

		public static void CreateShortSchema(string key, long value)
		{
			CreateIntegerSchema(key, value, short.MinValue, short.MaxValue);
		}

		public static void CreateUShortSchema(string key, long value)
		{
			CreateIntegerSchema(key, value, ushort.MinValue, ushort.MaxValue);
		}

		public static void CreateIntSchema(string key, long value)
		{
			CreateIntegerSchema(key, value, int.MinValue, int.MaxValue);
		}

		public static void CreateUIntSchema(string key, long value)
		{
			CreateIntegerSchema(key, value, uint.MinValue, uint.MaxValue);
		}

		public static void CreateLongSchema(string key, long value)
		{
			CreateIntegerSchema(key, value, long.MinValue, long.MaxValue);
		}

		public static void CreateIntegerSchema(string key, long value, long min, long max)
		{
			var newSchema = (VBoxContainer)IntSchemaTemplate.Duplicate();
			newSchema.SetMeta("Min", min);
			newSchema.SetMeta("Max", max);

			var paramValue = (SpinBox)newSchema.GetChild(1);
			paramValue.Value = value;
			newSchema.Visible = true;

			objectScrollContainer.GetChild(0).AddChild(newSchema);
			activeObjectEditorObjects[key] = newSchema;
		}

		public static double GetSpinBoxValue(string key)
		{
			var vecObj = activeObjectEditorObjects[key];
			var spinBox = (SpinBox)vecObj.GetChild(1);

			return spinBox.Value;
		}

		public static System.Numerics.Vector2 GetVec2SchemaValues(string key)
		{
			var vecObj = activeObjectEditorObjects[key];
			var boxX = (SpinBox)vecObj.GetChild(1).GetChild(0).GetChild(1);
			var boxY = (SpinBox)vecObj.GetChild(1).GetChild(1).GetChild(1);

			return new System.Numerics.Vector2((float)boxX.Value, (float)boxY.Value);
		}

		public static System.Numerics.Vector3 GetVec3SchemaValues(string key)
		{
			var vecObj = activeObjectEditorObjects[key];
			var boxX = (SpinBox)vecObj.GetChild(1).GetChild(0).GetChild(1);
			var boxY = (SpinBox)vecObj.GetChild(1).GetChild(1).GetChild(1);
			var boxZ = (SpinBox)vecObj.GetChild(1).GetChild(2).GetChild(1);

			return new System.Numerics.Vector3((float)boxX.Value, (float)boxY.Value, (float)boxZ.Value);
		}

		public static System.Numerics.Vector4 GetVec4SchemaValues(string key)
		{
			var vecObj = activeObjectEditorObjects[key];
			var boxX = (SpinBox)vecObj.GetChild(1).GetChild(0).GetChild(1);
			var boxY = (SpinBox)vecObj.GetChild(1).GetChild(1).GetChild(1);
			var boxZ = (SpinBox)vecObj.GetChild(1).GetChild(2).GetChild(1);
			var boxW = (SpinBox)vecObj.GetChild(1).GetChild(3).GetChild(1);

			return new System.Numerics.Vector4((float)boxX.Value, (float)boxY.Value, (float)boxZ.Value, (float)boxW.Value);
		}

		public static void LoadVec2SchemaTemplateInfo(string key, string objName, string objNameDefault, string objHint, string xName, string yName)
		{
			LoadVecSchemaTemplateInfo(key, objName, objNameDefault, objHint, xName, yName, "", "");
		}

		public static void LoadVec3SchemaTemplateInfo(string key, string objName, string objNameDefault, string objHint, string xName, string yName, string zName)
		{
			LoadVecSchemaTemplateInfo(key, objName, objNameDefault, objHint, xName, yName, zName, "");
		}

		public static void LoadVec4SchemaTemplateInfo(string key, string objName, string objNameDefault, string objHint, string xName, string yName, string zName, string wName)
		{
			LoadVecSchemaTemplateInfo(key, objName, objNameDefault, objHint, xName, yName, zName, wName);
		}

		public static void LoadSchemaTemplateInfo(string key, string name, string nameDefault, string hint)
		{
			if (!activeObjectEditorObjects.ContainsKey(key))
			{
				return;
			}
			var schObj = activeObjectEditorObjects[key];
			schObj.TooltipText = hint;

			var objText = (RichTextLabel)schObj.GetChild(0);
			var objTextText = name != "" && name != null ? name : nameDefault;
			objText.Text = objTextText;
		}

		private static void LoadVecSchemaTemplateInfo(string key, string objName, string objNameDefault, string objHint, string xName, string yName, string zName, string wName)
		{
			if (!activeObjectEditorObjects.ContainsKey(key))
			{
				return;
			}
			var vecObj = activeObjectEditorObjects[key];
			vecObj.TooltipText = objHint;

			var objText = (RichTextLabel)vecObj.GetChild(0);
			var objTextText = objName != "" && objName != null ? objName : objNameDefault;
			var vecSize = vecObj.GetChild(1).GetChildCount();

			switch (vecSize)
			{
				case 0:
					return;
				case 1:
					return;
				case 2:
					break;
				case 3:
					break;
				case 4:
					break;
				default:
					vecSize = 4;
					break;
			}

			objText.Text = $"[img]res://addons/datatable_godot/icons/Vector{vecSize}.svg[/img] {objTextText}";

			if (vecSize >= 1)
			{
				var vecObjXText = (RichTextLabel)vecObj.GetChild(1).GetChild(0).GetChild(0);
				var vecObjXName = xName != "" && xName != null ? xName : "X";
				vecObjXText.Text = $"[color=fab293]{vecObjXName}: [/color]";
			}

			if (vecSize >= 2)
			{
				var vecObjYText = (RichTextLabel)vecObj.GetChild(1).GetChild(1).GetChild(0);
				var vecObjYName = yName != "" && yName != null ? yName : "Y";
				vecObjYText.Text = $"[color=93fab2]{vecObjYName}: [/color]";
			}

			if (vecSize >= 3)
			{
				var vecObjZText = (RichTextLabel)vecObj.GetChild(1).GetChild(2).GetChild(0);
				var vecObjZName = zName != "" && zName != null ? zName : "Z";
				vecObjZText.Text = $"[color=b293fa]{vecObjZName}: [/color]";
			}

			if (vecSize >= 4)
			{
				var vecObjWText = (RichTextLabel)vecObj.GetChild(1).GetChild(3).GetChild(0);
				var vecObjWName = wName != "" && wName != null ? wName : "W";
				vecObjWText.Text = $"[color=b200b2]{vecObjWName}: [/color]";
			}
		}

		public static void HandleSetNodeExpand()
		{
			var activeNode = setDataTree.GetSelected();
			switch (activeNode.GetMetadata(0).AsInt32())
			{
				//Mission Node
				case 1:
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
					currentStageDefId = activeNode.GetMetadata(1).AsInt32();
					LazyLoadAreaData();

					GD.Print(activeNode.GetText(0) + $" {activeNode.GetIndex()}");

					//Populate Set Objects
					var temp = activeNode.CreateChild();
					temp.SetText(0, "Set Objects");
					temp.SetMetadata(0, 2);
					for (int i = 0; i < loadedBillySetObjects.setObjs.Count; i++)
					{
						var obj = loadedBillySetObjects.setObjs[i];
						var objNode = temp.CreateChild();
						if (cachedBillySetObjDefinitions.ContainsKey(obj.objectId))
						{
							objNode.SetText(0, cachedBillySetObjDefinitions[obj.objectId].ObjectName);
						}
						else
						{
							objNode.SetText(0, $"Object Type {obj.objectId}");
						}

						//Node type
						objNode.SetMetadata(0, 3);
						//Node's original object id
						objNode.SetMetadata(1, i);
						//Node's object category
						objNode.SetMetadata(2, 1);
					}
					temp.Collapsed = true;

					temp = activeNode.CreateChild();
					temp.SetText(0, "Set Design Objects");
					temp.SetMetadata(0, 2);
					for (int i = 0; i < loadedBillySetDesignObjects.setObjs.Count; i++)
					{
						var obj = loadedBillySetDesignObjects.setObjs[i];
						var objNode = temp.CreateChild();
						if (cachedBillySetObjDefinitions.ContainsKey(obj.objectId))
						{
							objNode.SetText(0, cachedBillySetObjDefinitions[obj.objectId].ObjectName);
						}
						else
						{
							objNode.SetText(0, $"Object Type {obj.objectId}");
						}
						//Node type
						objNode.SetMetadata(0, 3);
						//Node's original object id
						objNode.SetMetadata(1, i);
						//Node's object category
						objNode.SetMetadata(2, 2);
					}
					temp.Collapsed = true;
					activeNode.Collapsed = false;
					break;
				//mission Node Data Type - SetObject, Camera, etc.
				case 2:
					break;
				//Object Node
				case 3:
					currentObjectId = activeNode.GetMetadata(1).AsInt32();
					currentEditorType = (EditingType)(activeNode.GetMetadata(2).AsInt32());
					currentObjectTreeItem = activeNode;
					LoadSetObject();
					break;
			}
		}

		public static bool ReadGEStageDef(string location)
		{
			if (gameType is GameType.BillyGC)
			{
				if (File.Exists(Path.Combine(location, "k_boot.prd")))
				{
					var bootPrd = new PRD(Path.Combine(location, "k_boot.prd"));
					for (int i = 0; i < bootPrd.files.Count; i++)
					{
						var fName = bootPrd.fileNames[i];
						if (fName == "ge_stagedef.bin")
						{
							stgDef = new StageDef(bootPrd.files[i]);
							return true;
						}
					}
				}
			}
			else
			{
				if (File.Exists(Path.Combine(location, "ge_stagedef.bin")))
				{
					stgDef = new StageDef(Path.Combine(location, "ge_stagedef.bin"));
					return true;
				}
			}

			return false;
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
			var definitions = Directory.GetFiles(setObjDefinitionsReference);
			foreach (var defPath in definitions)
			{
				var fileName = Path.GetFileNameWithoutExtension(defPath);
				if (Int32.TryParse(fileName, out int id))
				{
					cachedBillySetObjDefinitions.Add(id, JsonSerializer.Deserialize<SetObjDefinition>(File.ReadAllText(defPath)));
				}
			}
		}
	}
}
