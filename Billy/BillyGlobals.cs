using AquaModelLibrary.Data.BillyHatcher;
using AquaModelLibrary.Data.BillyHatcher.ARCData;
using AquaModelLibrary.Data.BillyHatcher.SetData;
using AquaModelLibrary.Data.Ninja;
using AquaModelLibrary.Data.Ninja.Model;
using AquaModelLibrary.Helpers.Readers;
using ArchiveLib;
using Godot;
using OverEasy.Billy;
using OverEasy.TextInfo;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VrSharp.Gvr;
using static AquaModelLibrary.Data.BillyHatcher.EggLevel;

namespace OverEasy
{
	partial class OverEasyGlobals
	{
		public static StageDef stgDef = null;
		public static bool stgDefModified = false;
		public static SetEnemyList loadedBillySetEnemies = null;
		public static SetObjList loadedBillySetObjects = null;
		public static SetObjList loadedBillySetDesignObjects = null;
		public static Dictionary<int, SetObjDefinition> cachedBillySetObjDefinitions = new Dictionary<int, SetObjDefinition>();
		public static Dictionary<int, SetEnemyDefinition> cachedBillySetEnemyDefinitions = new Dictionary<int, SetEnemyDefinition>();
		public static bool isDay = true;
		public static Node3D daySkybox = null;
		public static Node3D nightSkybox = null;
		public static DayNightToggle dayNightToggle = null;
		
		public static void SetCameraSettingsBilly()
		{
			ViewerCamera.SCROLL_SPEED = 1000;
			ViewerCamera.ZOOM_SPEED = 500;
			ViewerCamera.SPIN_SPEED = 10;
			ViewerCamera.DEFAULT_DISTANCE = 2000;
			ViewerCamera.ROTATE_SPEED_X = 40;
			ViewerCamera.ROTATE_SPEED_Y = 40;
			ViewerCamera.TOUCH_ZOOM_SPEED = 4000;
			ViewerCamera.SHIFT_MULTIPLIER = 2.5;
			ViewerCamera.CTRL_MULTIPLIER = 0.4;
			ViewerCamera.FREECAM_ACCELERATION = 3000;
			ViewerCamera.FREECAM_DECELERATION = -10;
			ViewerCamera.FREECAM_VELOCITY_MULTIPLIER = 400;
		}

		private static void LoadInitialDataBilly()
		{
			LoadMapNames("TextInfo\\BillyMapNames.txt");
			LoadSetObjTemplates("TextInfo\\BillyObjDefinitions\\");
			LoadSetEnemyTemplates("TextInfo\\BillyEnemyDefinitions\\");
			if (!ReadBillyGEStageDef(modFolderLocation))
			{
				ReadBillyGEStageDef(gameFolderLocation);
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
					if (mapNames.ContainsKey(mission))
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
			foreach (var mission in defsDictKeys)
			{
				var node = setDataTree.CreateItem(root);
				node.SetText(0, $"{mission}");

				//Store node type as int, store actual stageDef id for later use
				node.SetMetadata(0, 1);
				node.SetMetadata(1, stgDef.defs.IndexOf(stgDef.defsDict[mission]));
			}
		}

		private static void LazyLoadBillyPC()
		{
			var def = stgDef.defs[currentMissionId];
			daySkybox = null;
			nightSkybox = null;
			dayNightToggle.ButtonPressed = true;
			isDay = true;
			SetCameraSettingsBilly();
			ClearModelAndTextureData();

			//Load Player Models
			CachePlayerModelsPC();

			//Load Enemy Models
			CacheEnemyModelsPC();

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

			//Load Set Enemies
			string setEnemyFilePath = GetAssetPath(def.setEnemyFilename);
			if (File.Exists(setEnemyFilePath))
			{
				loadedBillySetEnemies = new SetEnemyList(File.ReadAllBytes(setEnemyFilePath));
			}

			string lndPath = GetAssetPath(def.lndFilename);
			if (File.Exists(lndPath))
			{
				modelRoot.AddChild(Billy.ModelConversion.LNDToGDModel(def.lndFilename, new LND(File.ReadAllBytes(lndPath))));
			}

			if (daySkybox != null)
			{
				daySkybox.Visible = isDay;
			}
			if (nightSkybox != null)
			{
				nightSkybox.Visible = !isDay;
			}
		}

		private static void LazyLoadBillyGC()
		{
			var def = stgDef.defs[currentMissionId];
			daySkybox = null;
			nightSkybox = null;
			dayNightToggle.ButtonPressed = true;
			isDay = true;
			SetCameraSettingsBilly();
			ClearModelAndTextureData();
			var prdMissionname = GetBillyMissionName(def.missionName);
			currentArhiveFilename = $"k_{prdMissionname}.prd";
			currentPRD = new PRD(File.ReadAllBytes(GetAssetPath(currentArhiveFilename)));

			//Load common object models, enemies, items. May want to do this in the initial load step
			var battleCommonPRD = new PRD(File.ReadAllBytes(GetAssetPath("k_battle_common.prd")));
			for (int i = 0; i < battleCommonPRD.files.Count; i++)
			{
				switch(battleCommonPRD.fileNames[i])
				{
					case "ge_player1l.arc":
						var billy = new GEPlayer(battleCommonPRD.files[i]);
						CacheModel("player_1", billy.models[0], billy.texnames, billy.gvm, false);
						break;
					case "ge_player2l.arc":
						var rolly = new GEPlayer(battleCommonPRD.files[i]);
						CacheModel("player_2", rolly.models[0], rolly.texnames, rolly.gvm, false);
						break;
					case "ge_player3l.arc":
						var chick = new GEPlayer(battleCommonPRD.files[i]);
						CacheModel("player_3", chick.models[0], chick.texnames, chick.gvm, false);
						break;
					case "ge_player4l.arc":
						var bantam = new GEPlayer(battleCommonPRD.files[i]);
						CacheModel("player_4", bantam.models[0], bantam.texnames, bantam.gvm, false);
						break;
				}
			}

			Dictionary<string, ArEnemy> enemyArchiveDict = new Dictionary<string, ArEnemy>();
			Dictionary<string, PuyoFile> enemyGVMDict = new Dictionary<string, PuyoFile>();
			for (int i = 0; i < currentPRD.files.Count; i++)
			{
				//Hold Enemy GVM
				if (currentPRD.fileNames[i].ToLower().StartsWith("ar_"))
				{
					if (Path.GetExtension(currentPRD.fileNames[i].ToLower()) == ".gvm")
					{
						enemyGVMDict.Add(Path.GetFileNameWithoutExtension(currentPRD.fileNames[i].ToLower()), new PuyoFile(currentPRD.files[i]));
					} else if (Path.GetExtension(currentPRD.fileNames[i].ToLower()) == ".arc")
					{
						enemyArchiveDict.Add(Path.GetFileNameWithoutExtension(currentPRD.fileNames[i].ToLower().Replace("ar_", "")), new ArEnemy(currentPRD.files[i]));
					}
				} else
				//Load High Quality Player Model
				if(currentPRD.fileNames[i] == "ge_player1.arc")
				{
					var billy = new GEPlayer(currentPRD.files[i]);
					CacheModel("player_1", billy.models[0], billy.texnames, billy.gvm, true);
				}
				else if (currentPRD.fileNames[i] == "ge_player2.arc")
				{
					var rolly = new GEPlayer(currentPRD.files[i]);
					CacheModel("player_2", rolly.models[0], rolly.texnames, rolly.gvm, true);
				}
				else if (currentPRD.fileNames[i] == "ge_player3.arc")
				{
					var chick = new GEPlayer(currentPRD.files[i]);
					CacheModel("player_3", chick.models[0], chick.texnames, chick.gvm, true);
				}
				else if (currentPRD.fileNames[i] == "ge_player4.arc")
				{
					var bantam = new GEPlayer(currentPRD.files[i]);
					CacheModel("player_4", bantam.models[0], bantam.texnames, bantam.gvm, true);
				}
				else

				//Load Set Camera

				//Load Set Design
				if (currentPRD.fileNames[i] == def.setDesignFilename)
				{
					loadedBillySetDesignObjects = new SetObjList(currentPRD.files[i]);
				} else

				//Load Set Enemeies
				if (currentPRD.fileNames[i] == def.setEnemyFilename)
				{
					loadedBillySetEnemies = new SetEnemyList(currentPRD.files[i]);
				} else

				//Load Set Objects
				if (currentPRD.fileNames[i] == def.setObjFilename)
				{
					loadedBillySetObjects = new SetObjList(currentPRD.files[i]);
				} else

				//Load Stage Model
				if (currentPRD.fileNames[i] == def.lndFilename)
				{
					modelRoot.AddChild(Billy.ModelConversion.LNDToGDModel(def.lndFilename, new LND(currentPRD.files[i])));
				}

				//Load Stage Object models (We need to link these somehow...)

				//Load Stage Collision Model

				//Load Stage Path

				//Load Stage Event Camera

				//When figured out, load stage event file
			}

			//Load enemies
			foreach(var pair in enemyArchiveDict)
			{
				CacheModel($"enemy_{ObjectVariants.enemyFileMap[$"ar_{pair.Key}.arc"]}", pair.Value.models[0], pair.Value.texList[0], enemyGVMDict[pair.Key], false);
			}

			//Set skybox setting
			if (daySkybox != null)
			{
				daySkybox.Visible = isDay;
			}
			if (nightSkybox != null)
			{
				nightSkybox.Visible = !isDay;
			}
		}

		public static bool ReadBillyGEStageDef(string location)
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

		public static void LoadBillyEnemy()
		{
			allowedToUpdate = false;
			foreach (var objSet in activeObjectEditorObjects)
			{
				objSet.Value.Free();
			}
			activeObjectEditorObjects.Clear();

			//Load in enemy data
			LoadBillySetEnemyGui();
			LoadBillySetEnemyTemplateInfo();
			ToggleObjectScrollContainerCollision();
			allowedToUpdate = true;
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
			LoadBillySetObjectGui(setObjList);
			LoadBillySetObjectTemplateInfo(setObjList);
			ToggleObjectScrollContainerCollision();
			allowedToUpdate = true;
		}
		
		public static void LoadBillySpawn()
		{
			allowedToUpdate = false;
			foreach (var objSet in activeObjectEditorObjects)
			{
				objSet.Value.Free();
			}
			activeObjectEditorObjects.Clear();

			//Load in object data
			LoadBillySpawnPoint();
			LoadBillySpawnPointInfo();
			ToggleObjectScrollContainerCollision();
			allowedToUpdate = true;
		}

		public static void LoadBillySpawnPointInfo()
		{
			LoadVec3SchemaTemplateInfo("PlayerPosition", "Player Position", "Position", "The position of the player at spawn", "", "", "");
			LoadSchemaTemplateInfo("PlayerRotation", "Player Rotation", "Rotation", "The rotation of the player at spawn");
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
				var templatePath = Path.Combine(editorRootDirectory, "TextInfo\\BillyObjDefinitions", $"{setObj.objectId}.json");
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
				objNameText.Text = GetObjectName(false, setObj);
				currentObjectTreeItem.SetText(0, GetObjectName(true, setObj));
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

		public static void LoadBillySetEnemyTemplateInfo()
		{
			var setObj = loadedBillySetEnemies.setEnemies[currentObjectId];
			//Check if there's a template for this object type, else we use the defaults
			var template = new SetEnemyDefinition();
			var key = setObj.enemyId;

			//Check if we've loaded this template already
			if (cachedBillySetEnemyDefinitions.ContainsKey(key))
			{
				template = cachedBillySetEnemyDefinitions[key];

			}
			else
			{
				var templatePath = Path.Combine(editorRootDirectory, "TextInfo\\BillyEnemyDefinitions", $"{setObj.enemyId}.json");
				if (File.Exists(templatePath))
				{
					template = JsonSerializer.Deserialize<SetEnemyDefinition>(File.ReadAllText(templatePath));
					cachedBillySetEnemyDefinitions.Add(key, template);
				}
			}

			if (activeObjectEditorObjects.ContainsKey("ObjectId"))
			{
				var objName = activeObjectEditorObjects["ObjectId"];
				var objNameText = (RichTextLabel)objName.GetChild(0);
				objNameText.Text = GetEnemyName(false, setObj);
				currentObjectTreeItem.SetText(0, GetEnemyName(true, setObj));
			}

			LoadVec3SchemaTemplateInfo("ObjectPosition", template.Position, "Position", template.PositionHint, template.PositionX, template.PositionY, template.PositionZ);
			LoadVec3SchemaTemplateInfo("ObjectRotation", template.Rotation, "Rotation", template.RotationHint, template.RotationX, template.RotationY, template.RotationZ);
			LoadSchemaTemplateInfo("Int_1C", template.Int_1C, "Int_1C", template.Int_1CHint);
			LoadSchemaTemplateInfo("Int_20", template.Int_20, "Int_20", template.Int_20Hint);
			LoadSchemaTemplateInfo("Int_24", template.Int_24, "Int_24", template.Int_24Hint);
			LoadSchemaTemplateInfo("Int_28", template.Int_28, "Int_28", template.Int_28Hint);
			LoadSchemaTemplateInfo("Int_2C", template.Int_2C, "Int_2C", template.Int_2CHint);
			LoadSchemaTemplateInfo("Int_30", template.Int_30, "Int_30", template.Int_30Hint);
			LoadSchemaTemplateInfo("Int_34", template.Int_34, "Int_34", template.Int_34Hint);
			LoadSchemaTemplateInfo("Int_38", template.Int_38, "Int_38", template.Int_38Hint);
			LoadSchemaTemplateInfo("Int_3C", template.Int_3C, "Int_3C", template.Int_3CHint);
			LoadSchemaTemplateInfo("Flt_40", template.Flt_40, "Flt_40", template.Flt_40Hint);
			LoadSchemaTemplateInfo("Flt_44", template.Flt_44, "Flt_44", template.Flt_44Hint);
			LoadSchemaTemplateInfo("Flt_48", template.Flt_48, "Flt_48", template.Flt_48Hint);
			LoadSchemaTemplateInfo("Flt_4C", template.Flt_4C, "Flt_4C", template.Flt_4CHint);
		}

		public static void LoadBillySpawnPoint()
		{
			var def = stgDef.defs[currentMissionId];
			StageDef.PlayerStart start;
			switch(currentObjectId)
			{
				case 0:
					start = def.player1Start;
					break;
				case 1:
					start = def.player2Start;
					break;
				case 2:
					start = def.player3Start;
					break;
				case 3:
					start = def.player4Start;
					break;
				default:
					throw new System.Exception("Bad player spawn");
			}
			CreateVector3Schema("PlayerPosition", new Vector3(start.playerPosition.X, start.playerPosition.Y, start.playerPosition.Z));
			CreateFloatSchema("PlayerRotation", start.rotation);
		}

		public static void LoadBillySetEnemyGui()
		{
			var setEne = loadedBillySetEnemies.setEnemies[currentObjectId];
			CreateIntSchema("ObjectId", setEne.enemyId);
			CreateVector3Schema("ObjectPosition", new Vector3(setEne.Position.X, setEne.Position.Y, setEne.Position.Z));
			CreateVector3Schema("ObjectRotation", new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * setEne.BAMSRotation.X),
				(float)(NinjaConstants.FromBAMSValueToDegrees * setEne.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * setEne.BAMSRotation.Z)));
			CreateIntSchema("Int_1C", setEne.int_1C);
			CreateIntSchema("Int_20", setEne.int_20);
			CreateIntSchema("Int_24", setEne.int_24);
			CreateIntSchema("Int_28", setEne.int_28);
			CreateIntSchema("Int_2C", setEne.int_2C);
			CreateIntSchema("Int_30", setEne.int_30);
			CreateIntSchema("Int_34", setEne.int_34);
			CreateIntSchema("Int_38", setEne.int_38);
			CreateIntSchema("Int_3C", setEne.int_3C);
			CreateFloatSchema("Flt_40", setEne.flt_40);
			CreateFloatSchema("Flt_44", setEne.flt_44);
			CreateFloatSchema("Flt_48", setEne.flt_48);
			CreateFloatSchema("Flt_4C", setEne.flt_4C);
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

		public static void UpdateBillySpawnPoint(int spawnId)
		{
			//Gather some initial values
			var objPosition = GetVec3SchemaValues("PlayerPosition");
			var objRotation = GetSpinBoxValue("PlayerRotation");

			//Update 3d representation
			var parentNode = (Node3D)TransformGizmo.GetParent();
			parentNode.GlobalPosition = new Vector3(objPosition.X, objPosition.Y, objPosition.Z);
			parentNode.RotationDegrees = new Vector3(0, (float)objRotation, 0);

			//Gather current object values
			var spawn = new StageDef.PlayerStart();
			spawn.playerPosition = objPosition;
			spawn.rotation = (float)objRotation;

			var objRaw = stgDef.defs[currentMissionId];
			switch (spawnId)
			{
				case 0:
					objRaw.player1Start = spawn;
					break;
				case 1:
					objRaw.player2Start = spawn;
					break;
				case 2:
					objRaw.player3Start = spawn;
					break;
				case 3:
					objRaw.player4Start = spawn;
					break;
			}

			stgDefModified = true;
		}

		public static void UpdateBillySetObjects(SetObjList setObjList, int objectId)
		{
			//Gather some initial values
			var objPosition = GetVec3SchemaValues("ObjectPosition");
			var objRotation = GetVec3SchemaValues("ObjectRotation");

			//Update 3d representation
			var parentNode = (Node3D)TransformGizmo.GetParent();
			parentNode.GlobalPosition = new Vector3(objPosition.X, objPosition.Y, objPosition.Z);
			parentNode.RotationDegrees = new Vector3(objRotation.X, objRotation.Y, objRotation.Z);

			//Gather current object values
			var objRaw = setObjList.setObjs[objectId];
			foreach (var objSet in activeObjectEditorObjects)
			{
				switch (objSet.Key)
				{
					case "ObjectId":
						objRaw.objectId = (int)GetSpinBoxValue("ObjectId");
						break;
					case "ObjectPosition":
						objRaw.Position = objPosition;
						break;
					case "ObjectRotation":
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
			setObjList.setObjs[objectId] = objRaw;
		}

		public static void UpdateBillySetEnemies(int objectId)
		{
			//Gather some initial values
			var objPosition = GetVec3SchemaValues("ObjectPosition");
			var objRotation = GetVec3SchemaValues("ObjectRotation");

			//Update 3d representation
			var parentNode = (Node3D)TransformGizmo.GetParent();
			parentNode.GlobalPosition = new Vector3(objPosition.X, objPosition.Y, objPosition.Z);
			parentNode.RotationDegrees = new Vector3(objRotation.X, objRotation.Y, objRotation.Z);

			//Gather current object values
			var objRaw = loadedBillySetEnemies.setEnemies[objectId];
			foreach (var objSet in activeObjectEditorObjects)
			{
				switch (objSet.Key)
				{
					case "ObjectId":
						objRaw.enemyId = (int)GetSpinBoxValue("ObjectId");
						break;
					case "ObjectPosition":
						objRaw.Position = objPosition;
						break;
					case "ObjectRotation":
						objRaw.BAMSRotation = new AquaModelLibrary.Data.DataTypes.Vector3Int.Vec3Int((int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.X),
							(int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.Y), (int)(NinjaConstants.ToBAMSValueFromDegrees * objRotation.Z));
						break;
					case "Int_1C":
						objRaw.int_1C = (int)GetSpinBoxValue("Int_1C");
						break;
					case "Int_20":
						objRaw.int_20 = (int)GetSpinBoxValue("Int_20");
						break;
					case "Int_24":
						objRaw.int_24 = (int)GetSpinBoxValue("Int_24");
						break;
					case "Int_28":
						objRaw.int_28 = (int)GetSpinBoxValue("Int_28");
						break;
					case "Int_2C":
						objRaw.int_2C = (int)GetSpinBoxValue("Int_2C");
						break;
					case "Int_30":
						objRaw.int_30 = (int)GetSpinBoxValue("Int_30");
						break;
					case "Int_34":
						objRaw.int_34 = (int)GetSpinBoxValue("Int_34");
						break;
					case "Int_38":
						objRaw.int_38 = (int)GetSpinBoxValue("Int_38");
						break;
					case "Int_3C":
						objRaw.int_3C = (int)GetSpinBoxValue("Int_3C");
						break;
					case "Flt_40":
						objRaw.flt_40 = (int)GetSpinBoxValue("Flt_40");
						break;
					case "Flt_44":
						objRaw.flt_44 = (int)GetSpinBoxValue("Flt_44");
						break;
					case "Flt_48":
						objRaw.flt_48 = (int)GetSpinBoxValue("Flt_48");
						break;
					case "Flt_4C":
						objRaw.flt_4C = (int)GetSpinBoxValue("Flt_4C");
						break;
				}
			}

			//Insert over original file object values
			loadedBillySetEnemies.setEnemies[objectId] = objRaw;
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

		private static void SaveDataBillyPC()
		{
			string backupFileName;
			string setName = stgDef.defs[currentMissionId].setObjFilename;
			string setDesignName = stgDef.defs[currentMissionId].setDesignFilename;
			//Make sure we have backups if they're not there already
			backupFileName = Path.Combine(backupFolderLocation, setName);
			if (!File.Exists(backupFileName))
			{
				File.Copy(Path.Combine(gameFolderLocation, setName), backupFileName);
			}
			backupFileName = Path.Combine(backupFolderLocation, setDesignName);
			if (!File.Exists(backupFileName))
			{
				File.Copy(Path.Combine(gameFolderLocation, setDesignName), backupFileName);
			}

			File.WriteAllBytes(Path.Combine(modFolderLocation, setName), loadedBillySetObjects.GetBytes());
			File.WriteAllBytes(Path.Combine(modFolderLocation, setDesignName), loadedBillySetDesignObjects.GetBytes());

			//StageDef
			if(stgDefModified)
			{
				backupFileName = Path.Combine(backupFolderLocation, "ge_stagedef.bin");
				if (!File.Exists(backupFileName))
				{
					File.Copy(Path.Combine(gameFolderLocation, "ge_stagedef.bin"), backupFileName);
				}
				File.WriteAllBytes(Path.Combine(modFolderLocation, "ge_stagedef.bin"), stgDef.GetBytes());
				stgDefModified = false;
			}
		}

		private static void SaveDataBillyGC()
		{
			string backupFileName;
			string setName = stgDef.defs[currentMissionId].setObjFilename;
			string setDesignName = stgDef.defs[currentMissionId].setDesignFilename;
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

			//StageDef
			string stgDefLocation = null;
			if(File.Exists(Path.Combine(modFolderLocation, "k_boot.prd")))
			{
				stgDefLocation = Path.Combine(modFolderLocation, "k_boot.prd");

			} else if(File.Exists(Path.Combine(gameFolderLocation, "k_boot.prd")))
			{
				stgDefLocation = Path.Combine(gameFolderLocation, "k_boot.prd");
			}

			PRD bootPrd = null;
			if(stgDefModified == true && stgDefLocation != null)
			{
				bootPrd = new PRD(stgDefLocation);
				for (int i = 0; i < bootPrd.files.Count; i++)
				{
					var fName = bootPrd.fileNames[i];
					if (fName == "ge_stagedef.bin")
					{
						bootPrd.files[i] = stgDef.GetBytes();
					}
				}

				//Make sure we have a backup if it's not there already
				backupFileName = Path.Combine(backupFolderLocation, "k_boot.prd");
				if (!File.Exists(backupFileName))
				{
					File.Copy(Path.Combine(gameFolderLocation, "k_boot.prd"), backupFileName);
				}
				File.WriteAllBytes(Path.Combine(modFolderLocation, "k_boot.prd"), bootPrd.GetBytes());
			}

			//Make sure we have backups if they're not there already
			backupFileName = Path.Combine(backupFolderLocation, currentArhiveFilename);
			if (!File.Exists(backupFileName))
			{
				File.Copy(Path.Combine(gameFolderLocation, currentArhiveFilename), backupFileName);
			}
			File.WriteAllBytes(Path.Combine(modFolderLocation, currentArhiveFilename), currentPRD.GetBytes());
		}

		private static void PopulateSetObjectsBilly(TreeItem activeNode)
		{
			TreeItem temp;

			//We will ALWAYS have 4 spawnpoints. 
			temp = activeNode.CreateChild();
			temp.SetText(0, "Player Spawnpoints");
			temp.SetMetadata(0, 2);
			for(int i = 0; i < 4; i++)
			{
				StageDef.PlayerStart start;
				switch(i)
				{
					case 0:
						start = stgDef.defs[currentMissionId].player1Start;
						break;
					case 1:
						start = stgDef.defs[currentMissionId].player2Start;
						break;
					case 2:
						start = stgDef.defs[currentMissionId].player3Start;
						break;
					case 3:
						start = stgDef.defs[currentMissionId].player4Start;
						break;
					default:
						throw new System.Exception("Bad player spawn point");
				}

				var objNode = temp.CreateChild();
				objNode.SetText(0, $"Player Spawn {i + 1}");

				//Node type
				objNode.SetMetadata(0, 3);
				//Node's original object id
				objNode.SetMetadata(1, i);
				//Node's object category
				objNode.SetMetadata(2, 3);
				//Attach a model instance
				var modelNode = LoadBillySpawnModel(i);
				modelNode.SetMeta("treeItem", objNode);
				objNode.SetMetadata(3, modelNode);
				modelNode.RotationDegrees = new Vector3(0, start.rotation, 0);
				modelNode.Position = new Vector3(start.playerPosition.X, start.playerPosition.Y, start.playerPosition.Z);
				modelRoot.AddChild(modelNode);
			}
			temp.Collapsed = true;

			if (loadedBillySetObjects != null && loadedBillySetObjects?.setObjs?.Count != 0)
			{
				temp = activeNode.CreateChild();
				temp.SetText(0, "Set Objects");
				temp.SetMetadata(0, 2);
				for (int i = 0; i < loadedBillySetObjects.setObjs.Count; i++)
				{
					var obj = loadedBillySetObjects.setObjs[i];
					var objNode = temp.CreateChild();
					objNode.SetText(0, GetObjectName(true, obj));

					//Node type
					objNode.SetMetadata(0, 3);
					//Node's original object id
					objNode.SetMetadata(1, i);
					//Node's object category
					objNode.SetMetadata(2, 1);
					//Attach a model instance
					var modelNode = LoadBillyObjectModel(obj, false);
					modelNode.SetMeta("treeItem", objNode);
					objNode.SetMetadata(3, modelNode);

					modelNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.X), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Z));
					modelNode.Position = new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z);
					modelRoot.AddChild(modelNode);
				}
				temp.Collapsed = true;
			}

			if (loadedBillySetDesignObjects?.setObjs?.Count != 0)
			{
				temp = activeNode.CreateChild();
				temp.SetText(0, "Set Design Objects");
				temp.SetMetadata(0, 2);
				for (int i = 0; i < loadedBillySetDesignObjects.setObjs.Count; i++)
				{
					var obj = loadedBillySetDesignObjects.setObjs[i];
					var objNode = temp.CreateChild();
					objNode.SetText(0, GetObjectName(true, obj));

					//Node type
					objNode.SetMetadata(0, 3);
					//Node's original object id
					objNode.SetMetadata(1, i);
					//Node's object category
					objNode.SetMetadata(2, 2);
					//Attach a model instance
					var modelNode = LoadBillyObjectModel(obj, true);
					modelNode.SetMeta("treeItem", objNode);
					objNode.SetMetadata(3, modelNode);

					modelNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.X), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Z));
					modelNode.Position = new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z);
					modelRoot.AddChild(modelNode);
				}
				temp.Collapsed = true;
			}

			if (loadedBillySetEnemies?.setEnemies?.Count != 0)
			{
				temp = activeNode.CreateChild();
				temp.SetText(0, "Set Enemy Objects");
				temp.SetMetadata(0, 2);
				for (int i = 0; i < loadedBillySetEnemies.setEnemies.Count; i++)
				{
					var obj = loadedBillySetEnemies.setEnemies[i];
					var objNode = temp.CreateChild();
					objNode.SetText(0, GetEnemyName(true, obj));

					//Node type
					objNode.SetMetadata(0, 3);
					//Node's original object id
					objNode.SetMetadata(1, i);
					//Node's object category
					objNode.SetMetadata(2, 4);
					//Attach a model instance
					var modelNode = LoadBillySetEnemyModel(obj);
					modelNode.SetMeta("treeItem", objNode);
					objNode.SetMetadata(3, modelNode);

					modelNode.RotationDegrees = new Vector3((float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.X), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Y), (float)(NinjaConstants.FromBAMSValueToDegrees * obj.BAMSRotation.Z));
					modelNode.Position = new Vector3(obj.Position.X, obj.Position.Y, obj.Position.Z);
					modelRoot.AddChild(modelNode);
				}
				temp.Collapsed = true;
			}
		}

		private static void CachePlayerModelsPC()
		{
			var billy = new GEPlayer(File.ReadAllBytes(GetAssetPath("ge_player1.arc")));
			var rolly = new GEPlayer(File.ReadAllBytes(GetAssetPath("ge_player2.arc")));
			var chick = new GEPlayer(File.ReadAllBytes(GetAssetPath("ge_player3.arc")));
			var bantam = new GEPlayer(File.ReadAllBytes(GetAssetPath("ge_player4.arc")));

			CacheModel("player_1", billy.models[0], billy.texnames, billy.gvm, false);
			CacheModel("player_2", rolly.models[0], rolly.texnames, rolly.gvm, false);
			CacheModel("player_3", chick.models[0], chick.texnames, chick.gvm, false);
			CacheModel("player_4", bantam.models[0], bantam.texnames, bantam.gvm, false);
		}

		private static Node3D CacheModel(string name, NJSObject nj, NJTextureList njtl, PuyoFile gvm, bool forceAdd)
		{
			ModelConversion.LoadGVM(name, gvm, out var gvmTextures, out var gvrAlphaTypes);
			var modelNode = ModelConversion.NinjaToGDModel(name, nj, njtl, gvmTextures, gvrAlphaTypes);
			CreateObjectCollision(modelNode);
			if (forceAdd || !modelDictionary.ContainsKey(name))
			{
				modelDictionary.Add(name, modelNode);
			}

			return modelNode;
		}

		private static void CacheEnemyModelsPC()
		{
			foreach (var set in ObjectVariants.enemyFileMap)
			{
				//Load textures
				string texturePath = null;
				switch(set.Key)
				{
					case "ar_ene_yellow_boss_green.arc":
						texturePath = GetAssetPath("ene_yellow_boss.gvm");
						break;
					case "ar_ene_red_boss.arc":
						texturePath = GetAssetPath("ene_red_boss_dino.gvm");
						break;
					default:
						texturePath = GetAssetPath(set.Key.Replace(".arc", ".gvm").Replace("ar_", ""));
						break;

				}
				ModelConversion.LoadGVM(set.Key, new PuyoFile(File.ReadAllBytes(texturePath)), out var gvmTextures, out var gvrAlphaTypes);
				if (gvmTextures[0].ResourceName == "am064_e00bstex01.gvr")
				{
					string eyespath = GetAssetPath("ene_eye.gvm");
					if (File.Exists(eyespath))
					{
						ModelConversion.LoadGVM(set.Key, new PuyoFile(File.ReadAllBytes(eyespath)), out var eyeTextures, out var eyeAlphaTypes);
						gvmTextures[0] = eyeTextures[0];
					}

				}

				//Load models
				var modelPath = GetAssetPath(set.Key);
				if (File.Exists(modelPath))
				{
					var arc = new ArEnemy(File.ReadAllBytes(modelPath));
					NJSObject nj = null;
					NJTextureList njtl = null;

					switch (set.Key)
					{
						case "ar_ene_am02.arc":
							nj = arc.models[1];
							njtl = arc.texList[0];
							break;
						default:
							nj = arc.models[0];
							njtl = arc.texList[0];
							break;
					}
					GD.Print(modelPath);
					var modelNode = ModelConversion.NinjaToGDModel(set.Key, nj, njtl, gvmTextures, gvrAlphaTypes);
					CreateObjectCollision(modelNode);

					string enemyRef = $"enemy_{set.Value}";
					if (!modelDictionary.ContainsKey(enemyRef))
					{
						modelDictionary.Add(enemyRef, modelNode);
					}
				}

			}
		}

		private static Node3D LoadBillySpawnModel(int spawnId)
		{
			var name = $"player_{spawnId + 1}";

			Node3D modelNode;
			if (modelDictionary.ContainsKey(name))
			{
				modelNode = ModelConversion.GDModelClone(modelDictionary[name]);
			}
			else
			{
				Color color = new Color(1, 0, 0, 1);
				modelNode = ModelConversion.CreateDefaultObjectModel(name, color);
				((MeshInstance3D)modelNode.GetChild(0)).CreateTrimeshCollision();
				var staticBody = ((StaticBody3D)modelNode.GetChild(0).GetChild(0));
				var child = ((CollisionShape3D)staticBody.GetChild(0));
				child.Disabled = false;
				staticBody.CollisionLayer = 1;
				staticBody.CollisionMask = 1;
				modelDictionary.Add(name, modelNode);
			}

			return modelNode;
		}

		private static Node3D LoadBillySetEnemyModel(SetEnemy ene)
		{
			var name = $"enemy_{ene.enemyId.ToString("X")}";
			if(ene.enemyId == 0x101)
			{
				name += $"_{ene.int_38}";
			}

			Node3D modelNode;
			if (modelDictionary.ContainsKey(name))
			{
				modelNode = ModelConversion.GDModelClone(modelDictionary[name]);
			}
			else
			{
				Color color = new Color(1, 1, 0, 1);
				modelNode = ModelConversion.CreateDefaultObjectModel(name, color);
				((MeshInstance3D)modelNode.GetChild(0)).CreateTrimeshCollision();
				var staticBody = ((StaticBody3D)modelNode.GetChild(0).GetChild(0));
				var child = ((CollisionShape3D)staticBody.GetChild(0));
				child.Disabled = false;
				staticBody.CollisionLayer = 1;
				staticBody.CollisionMask = 1;
				modelDictionary.Add(name, modelNode);
			}

			return modelNode;
		}

		private static Node3D LoadBillyObjectModel(SetObj obj, bool designObj)
		{
			string name = obj.objectId.ToString();

			Node3D modelNode;
			if (modelDictionary.ContainsKey(name))
			{
				modelNode = ModelConversion.GDModelClone(modelDictionary[name]);
			}
			else
			{
				switch(obj.objectId)
				{
					default:
						NJTextureList texList;
						using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(@"C:\Program Files (x86)\SEGA\Billy Hatcher_\ge_animal_c1.glk_out\y_anic_iwhale_base.gjt")))
						using (BufferedStreamReaderBE<MemoryStream> sr = new BufferedStreamReaderBE<MemoryStream>(ms))
						{
							sr.Seek(8, SeekOrigin.Begin);
							sr._BEReadActive = true;
							texList = new NJTextureList(sr, 8);
						}

						var gvm = new PuyoFile(File.ReadAllBytes(@"C:\Program Files (x86)\SEGA\Billy Hatcher_\ge_animal_c1.glk_out\y_anic_iwhale_base.gvm"));
						ModelConversion.LoadGVM(name, gvm, out var gvmTextures, out var gvrAlphaTypes);
						modelNode = ModelConversion.NinjaToGDModel(name,
							ModelConversion.ReadNJ(File.ReadAllBytes(@"C:\Program Files (x86)\SEGA\Billy Hatcher_\ge_animal_c1.glk_out\y_anic_iwhale_base.gj"), AquaModelLibrary.Data.Ninja.Model.NinjaVariant.Ginja, true, 0),
							texList, gvmTextures, gvrAlphaTypes);


						name = "blueDefaultBox";
						Color color = new Color(0, 0, 1, 1);
						if (designObj)
						{
							color = new Color(0, 1, 1, 1);
							name = "greenDefaultBox";
						}
						modelNode = ModelConversion.CreateDefaultObjectModel(name, color);

						//Set up collision
						CreateObjectCollision(modelNode);
						break;
				}

				if(!modelDictionary.ContainsKey(name))
				{
					modelDictionary.Add(name, modelNode);
				}
			}

			return modelNode;
		}

		private static void CreateObjectCollision(Node3D modelNode)
		{
			var children = modelNode.GetChildren();
			foreach (var nodeChild in children)
			{
				if (nodeChild is MeshInstance3D meshChild)
				{
					meshChild.CreateTrimeshCollision();
					var staticBody = (StaticBody3D)meshChild.GetChild(0);
					var collChild = ((CollisionShape3D)staticBody.GetChild(0));
					collChild.Disabled = false;
					staticBody.CollisionLayer = 1;
					staticBody.CollisionMask = 1;
				}
			}
		}

		public static string GetObjectName(bool isNode, SetObj obj)
		{
			string nodeAddition = "";
			if (isNode)
			{
				nodeAddition = "Type ";
			}

			if (obj.objectId == 0xB)
			{
				string fruit;
				switch(obj.intProperty1)
				{
					case 0:
					case 1:
					case 2:
					case 3:
					case 4:
					case 5:
					case 6:
						fruit = ObjectVariants.fruits[obj.intProperty1];
						break;
					default:
						fruit = ObjectVariants.fruits[6];
						break;
				}
				return cachedBillySetObjDefinitions[obj.objectId].ObjectName + $" {fruit}";
			}
			else if (cachedBillySetObjDefinitions.ContainsKey(obj.objectId))
			{
				return cachedBillySetObjDefinitions[obj.objectId].ObjectName;
			}
			else
			{
				return $"Object {nodeAddition}{obj.objectId} 0x{obj.objectId:X}";
			}
		}

		public static string GetEnemyName(bool isNode, SetEnemy obj)
		{
			string nodeAddition = "";
			if (isNode)
			{
				nodeAddition = "Type ";
			}

			if(obj.enemyId == 0x101)
			{
				switch (obj.int_38)
				{
					case 0:
					case 1:
					case 2:
					case 3:
					case 4:
						return $"Zako - {ObjectVariants.zakoCrows[obj.int_38]}";
					default:
						return $"Zako - Out of range";
				}
			}

			if (cachedBillySetEnemyDefinitions.ContainsKey(obj.enemyId))
			{
				return cachedBillySetEnemyDefinitions[obj.enemyId].EnemyName;
			}
			else
			{
				return $"Enemy {nodeAddition}0x{obj.enemyId:X}";
			}
		}

		public static void TransformFromGizmoBilly(Vector3? pos, Quaternion? rot, Vector3? scale)
		{
			switch(currentEditorType)
			{
				case EditingType.BillySetObj:
				case EditingType.BillySetDesign:
				case EditingType.BillySetEnemy:
					if(pos != null)
					{
						SetVec3SchemaValues("ObjectPosition", (Vector3)pos);
					}
					if(rot != null)
					{
						var eulRot = ((Quaternion)rot).GetEuler() * 180 / Mathf.Pi;
						SetVec3SchemaValues("ObjectRotation", eulRot);
					}
					if(scale != null)
					{
						//SetVec3SchemaValues("ObjectScale", (Vector3)scale);
					}
					break;
				case EditingType.BillySpawnPoint:
					if (pos != null)
					{
						SetVec3SchemaValues("PlayerPosition", (Vector3)pos);
					}
					if (rot != null)
					{
						var eulRot = ((Quaternion)rot).GetEuler() * 180 / Mathf.Pi;
						SetSpinBoxValue("PlayerRotation", eulRot.Z);
					}
					break;
			}
		}
	}
}
