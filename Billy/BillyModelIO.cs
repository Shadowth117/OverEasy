using AquaModelLibrary.Data.BillyHatcher;
using AquaModelLibrary.Data.BillyHatcher.ARCData;
using AquaModelLibrary.Data.BillyHatcher.SetData;
using AquaModelLibrary.Data.Ninja;
using AquaModelLibrary.Data.Ninja.Model;
using AquaModelLibrary.Data.PSO2.Aqua;
using AquaModelLibrary.Data.PSO2.Aqua.CharacterMakingIndexData;
using ArchiveLib;
using Godot;
using OverEasy.TextInfo;
using OverEasy.Util;
using System.Collections.Generic;
using System.IO;
using VrSharp.Gvr;

namespace OverEasy.Billy
{
	public class BillyModelIO
	{

		public static Color orangeHoopColor = new Color(1f, 0.61f, 0.22f, 1f);
		public static Color yellowHoopColor = new Color(1f, .99f, 0f, 1f);
		public static Color greenHoopColor = new Color(.38f, 1f, 0f, 1f);
		public static Color blueHoopColor = new Color(0, .53f, 1f, 1f);
		public static Color tealHoopColor = new Color(0, 1f, .85f, 1f);
		public static Color magentaHoopColor = new Color(1f, 0f, .815f, 1f);
		
		public static void CacheEnemyModelsPC()
		{
			foreach (var set in ObjectVariants.enemyFileMap)
			{
				if(set.Value == null)
				{
					continue;
				}
				//Load textures
				string texturePath = null;
				switch (set.Key)
				{
					case "ar_ene_yellow_boss_green.arc":
						texturePath = OverEasyGlobals.GetAssetPath("ene_yellow_boss.gvm");
						break;
					case "ar_ene_red_boss.arc":
						texturePath = OverEasyGlobals.GetAssetPath("ene_red_boss_dino.gvm");
						break;
					default:
						texturePath = OverEasyGlobals.GetAssetPath(set.Key.Replace(".arc", ".gvm").Replace("ar_", ""));
						break;

				}
				ModelConversion.LoadGVM(set.Key, new PuyoFile(File.ReadAllBytes(texturePath)), out var gvmTextures, out var gvrAlphaTypes);
				if (gvmTextures[0].ResourceName == "am064_e00bstex01.gvr")
				{
					string eyespath = OverEasyGlobals.GetAssetPath("ene_eye.gvm");
					if (File.Exists(eyespath))
					{
						ModelConversion.LoadGVM(set.Key, new PuyoFile(File.ReadAllBytes(eyespath)), out var eyeTextures, out var eyeAlphaTypes);
						gvmTextures[0] = eyeTextures[0];
					}

				}

				//Load models
				var modelPath = OverEasyGlobals.GetAssetPath(set.Key);
				if (File.Exists(modelPath))
				{
					var arc = new ArEnemy(File.ReadAllBytes(modelPath));
					NJSObject nj = null;
					NJTextureList njtl = null;

					Node3D modelNode;
					switch (set.Key)
					{
						case "ar_ene_am02.arc":
						case "ar_ene_blue_boss.arc":
						case "ar_ene_orange_boss.arc":
							nj = arc.models[1];
							njtl = arc.texList[0];
							modelNode = ModelConversion.NinjaToGDModel(set.Key, nj, gvmTextures, gvrAlphaTypes);
							break;
						case "ar_ene_purple_boss.arc":
							nj = arc.models[32];
							njtl = arc.texList[0];
							modelNode = ModelConversion.NinjaToGDModel(set.Key, nj, gvmTextures, gvrAlphaTypes);
							break;
						case "ar_ene_last_ex_boss.arc":
							modelNode = ModelConversion.NinjaToGDModel(set.Key, arc.models[5], gvmTextures, gvrAlphaTypes);
							modelNode = ModelConversion.NinjaToGDModel(set.Key, arc.models[6], gvmTextures, gvrAlphaTypes, null, null, modelNode);
							break;
						default:
							nj = arc.models[0];
							njtl = arc.texList[0];
							modelNode = ModelConversion.NinjaToGDModel(set.Key, nj, gvmTextures, gvrAlphaTypes);
							break;
					}
					ModelConversion.CreateObjectCollision(modelNode);

					string enemyRef = $"enemy_{set.Value}";
					if (!OverEasyGlobals.modelDictionary.ContainsKey(enemyRef))
					{
						OverEasyGlobals.modelDictionary.Add(enemyRef, modelNode);
					}
				}

			}
		}

		public static Node3D LoadBillySpawnModel(int spawnId)
		{
			var name = $"player_{spawnId + 1}";

			Node3D modelNode;
			if (OverEasyGlobals.modelDictionary.ContainsKey(name))
			{
				modelNode = ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary[name]);
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
				OverEasyGlobals.modelDictionary.Add(name, modelNode);
			}

			return modelNode;
		}

		public static Node3D LoadBillySetEnemyModel(SetEnemy ene, Node3D modelNode = null)
		{
			var name = $"enemy_{ene.enemyId.ToString("X")}";
			if (ene.enemyId == 0x101)
			{
				name += $"_{ene.int_38}";
			}

			//If it's not null, we clean up the node.
			if (modelNode != null)
			{
				CleanModelNode(modelNode);
			}
			if (OverEasyGlobals.modelDictionary.ContainsKey(name))
			{
				modelNode = ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary[name], modelNode);
			}
			else if (ene.enemyId == 0)
			{
				if (modelNode == null)
				{
					modelNode = new Node3D();
				}
			}
			else
			{
				Color color = new Color(1, 1, 0, 1);
				modelNode = ModelConversion.CreateDefaultObjectModel(name, color, modelNode);
				((MeshInstance3D)modelNode.GetChild(0)).CreateTrimeshCollision();
				var staticBody = ((StaticBody3D)modelNode.GetChild(0).GetChild(0));
				var child = ((CollisionShape3D)staticBody.GetChild(0));
				child.Disabled = false;
				staticBody.CollisionLayer = 1;
				staticBody.CollisionMask = 1;
				OverEasyGlobals.modelDictionary.Add(name, modelNode);
			}

			return modelNode;
		}

		public static Node3D LoadBillyObjectModel(SetObj obj, bool designObj, Node3D modelNode = null)
		{
			string name = $"object_{obj.objectId}";

			switch(obj.objectId)
			{
				case 4:
					name += $"_{obj.intProperty1}";
					if (obj.intProperty1 != 0 && obj.intProperty1 != 1 && obj.intProperty1 != 2 && !OverEasyGlobals.modelDictionary.ContainsKey(name))
					{
						name = "object_4_5";
					}
					break;
				case 10:
					string objectBank = "Local";
					if(obj.intProperty2 == 1)
					{
						objectBank = "Common";
					}
					name = $"commGeoM2{objectBank}_{obj.intProperty1}";
					break;
				case 11:
					name += $"_{obj.intProperty1}";
					break;
				case 18:
					name = "chickNPC";
					break;
				case 25:
					name = $"egg_{obj.intProperty1}";
					if(!OverEasyGlobals.modelDictionary.ContainsKey(name))
					{
						name = "egg_0";
					}
					break;
				case 30:
					name = "bridge";
					break;
				case 38:
					name += obj.intProperty1 != 0 ? "_red" : "_blue";
					break;
				case 40:
					name = "player_" + (obj.intProperty1 + 1);
					break;
				case 41:
					name += $"_{obj.intProperty1}";
					break;
				case 45:
					int hoopType = obj.intProperty1;
					if(hoopType > 5 || hoopType < 0)
					{
						hoopType = 5;
					}
					name += $"_{hoopType}";
					break;
				case 50:
					name = $"segg_{obj.intProperty1}";
					if (!OverEasyGlobals.modelDictionary.ContainsKey(name))
					{
						name = "segg_0";
					}
					break;
				case 768: //Both of these use the same models
				case 769:
					name = $"object_768_{obj.intProperty1}";
					break;
				case 777:
					name += $"_{obj.intProperty1}";
					break;
				default:
					break;
			}

			//If it's not null, we clean up the node.
			if(modelNode != null)
			{
				CleanModelNode(modelNode);
			}
			if (OverEasyGlobals.modelDictionary.ContainsKey(name) && obj.objectId != 30)
			{
				bool ignoreParentTransform = false;
				Transform3D? rootTransform = null;
				switch(obj.objectId)
				{
					case 777:
						ignoreParentTransform = true;
                        rootTransform = Transform3D.Identity;
                        rootTransform = rootTransform.Value.Translated(new Vector3(0, 5, 0));
                        break;
                    case 768:
                    case 769:
					case 1798:
					case 1802:
                    case 4615:
					case 4616:
						ignoreParentTransform = true;
						break;
				}
				modelNode = ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary[name], modelNode, ignoreParentTransform, rootTransform);
			}
			else if (obj.objectId == 0 || obj.objectId == 30)
			{
				if(modelNode == null)
				{
					modelNode = new Node3D();
				}
			}
			else
			{
				switch (obj.objectId)
				{
					default:
						name = "blueDefaultBox";
						Color color = new Color(0, 0, 1, 1);
						if (designObj)
						{
							color = new Color(0, 1, 0, 1);
							name = "greenDefaultBox";
						}
						modelNode = ModelConversion.CreateDefaultObjectModel(name, color, modelNode);

						//Set up collision
						ModelConversion.CreateObjectCollision(modelNode);
						break;
				}

				if (!OverEasyGlobals.modelDictionary.ContainsKey(name))
				{
					OverEasyGlobals.modelDictionary.Add(name, modelNode);
				}
			}

			//Handle special object data
			switch (obj.objectId)
			{
				case 11:
					modelNode.Scale = obj.intProperty3 == 1 ? new Vector3(2, 2, 2) : new Vector3(1, 1, 1);
					break;
				case 18:
					SetChickInfo(obj, modelNode);
					break;
				case 27:
					modelNode.Scale = new Vector3(1 + obj.fltProperty4, 1 + obj.fltProperty4, 1 + obj.fltProperty4);
					break;
				case 30:
					SetBridgeModel(obj, modelNode);
					break;
                case 33:
                    modelNode.Scale = new Vector3(1 + obj.fltProperty1, 1 + obj.fltProperty1, 1 + obj.fltProperty1);
                    break;
                default:
					modelNode.Scale = new Vector3(1, 1, 1);
					break;
			}

			return modelNode;
		}

		private static void SetBridgeModel(SetObj obj, Node3D modelNode)
		{
			//Bridges are laid out with segments made of either columns or rope, then a plank model in between with columns on either end as well.
			int numSegments = obj.intProperty1 > 0 ? obj.intProperty1 : 0;
			//Create planks and rope + columns
			for (int i = 0; i < numSegments + 2; i++)
			{
				var node = GetBridgePlankSegment(numSegments, i);
				node.SetMeta("parentNode", modelNode);
				modelNode.AddChild(node);
				node.Position += new Vector3(0, 0, 40 * i);

				var columnNode = ((i + numSegments) & 1) > 0 ? ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary["MODEL_TURIBASHI_ROPE"]) : ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary["MODEL_TURIBASHI_HASHIRA01"]);
				columnNode.SetMeta("parentNode", modelNode);
				modelNode.AddChild(columnNode);
				columnNode.Position += new Vector3(0, 0, 40 * i);

				if (i != 0)
				{
					var nodeOpposite = GetBridgePlankSegment(numSegments, -i);
					nodeOpposite.SetMeta("parentNode", modelNode);
					modelNode.AddChild(nodeOpposite);
					nodeOpposite.Position += new Vector3(0, 0, 40 * -i);

					var columnNodeOpposite = ((i + numSegments) & 1) > 0 ? ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary["MODEL_TURIBASHI_ROPE"]) : ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary["MODEL_TURIBASHI_HASHIRA01"]);
					columnNodeOpposite.SetMeta("parentNode", modelNode); 
					modelNode.AddChild(columnNodeOpposite);
					columnNodeOpposite.Position += new Vector3(0, 0, 40 * -i);
				}
			}

			//Create end columns
			var end = ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary["MODEL_TURIBASHI_HASHIRA02"]);
			end.SetMeta("parentNode", modelNode);
			modelNode.AddChild(end);
			end.RotateY(Mathf.Pi);
			end.Position += new Vector3(3.5f, 0, 40 * (numSegments + 2));

			var endOpposite = ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary["MODEL_TURIBASHI_HASHIRA02"]);
			endOpposite.SetMeta("parentNode", modelNode);
			modelNode.AddChild(endOpposite);
			endOpposite.Position += new Vector3(0, 0, 40 * -(numSegments + 2));
			ModelConversion.CreateObjectCollision(modelNode);
		}

		private static Node3D GetBridgePlankSegment(int numSegments, int currentSegment)
		{
			if(currentSegment < 0)
			{
				currentSegment = Mathf.Abs(currentSegment) + 1;
			}
			int segmentSequence = currentSegment + numSegments;
			int segmentId = segmentSequence % 3;
			switch(segmentId)
			{
				case 0:
					return ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary["MODEL_TURIBASHI_ITA03"]);
				case 1:
					return ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary["MODEL_TURIBASHI_ITA01"]);
				case 2:
					return ModelConversion.GDModelClone(OverEasyGlobals.modelDictionary["MODEL_TURIBASHI_ITA02"]);
			}

			throw new System.Exception("Unexpected segment Id");
		}

		private static void SetChickInfo(SetObj obj, Node3D modelNode)
		{
			switch (obj.intProperty3)
			{
				case 0:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 1:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCBowTie }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 2:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCHairBow }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 3:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCEggShellCap }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 4:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCHairBow, ModelConversion.ChickNPCBowTie }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 5:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCEggShellCap, ModelConversion.ChickNPCBowTie }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 6:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCHairBow, ModelConversion.ChickNPCEggShellCap, ModelConversion.ChickNPCBowTie }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 7:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCHairBow, ModelConversion.ChickNPCEggShellCap }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 8:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCBaseballCap, }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 9:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCDress, }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 10:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCBaseballCap, ModelConversion.ChickNPCBowTie }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 11:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCBaseballCap, ModelConversion.ChickNPCDress }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 12:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCHairBow, ModelConversion.ChickNPCDress }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 13:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCEggShellCap, ModelConversion.ChickNPCDress }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
				case 14:
				default:
					ModelConversion.SetEnabledFromMatName(modelNode, new List<string>() { ModelConversion.ChickNPCHairBow, ModelConversion.ChickNPCEggShellCap, ModelConversion.ChickNPCDress }, ModelConversion.AllToggleableChickAccessoryNames);
					break;
			}
			switch (obj.intProperty4)
			{
				case 0:
					ModelConversion.SetDyeableColor(modelNode, ModelConversion.DyeableChickAccessoryNames, new Vector4(1, 0, 0, 1));  //Red
					break;
				case 1:
					ModelConversion.SetDyeableColor(modelNode, ModelConversion.DyeableChickAccessoryNames, new Vector4(0, 0.5f, 0, 1)); //Green
					break;
				case 2:
					ModelConversion.SetDyeableColor(modelNode, ModelConversion.DyeableChickAccessoryNames, new Vector4(0, 0, 1, 1)); //Blue
					break;
				case 3:
					ModelConversion.SetDyeableColor(modelNode, ModelConversion.DyeableChickAccessoryNames, new Vector4(0.5f, 0, 0.5f, 1)); //Purple
					break;
				case 4:
					ModelConversion.SetDyeableColor(modelNode, ModelConversion.DyeableChickAccessoryNames, new Vector4(0, 0.5f, 1, 1)); //Aqua
					break;
				case 5:
				default:
					ModelConversion.SetDyeableColor(modelNode, ModelConversion.DyeableChickAccessoryNames, new Vector4(1, 0.5f, 0, 1)); //Orange
					break;
			}
		}

		public static void CleanModelNode(Node3D modelNode)
		{
			bool reAddCamera = false;
			bool reAddTransform = false;
			if(modelNode == OverEasyGlobals.ViewCamera.targetNode.GetParent())
			{
				reAddCamera = true;
				OverEasyGlobals.ViewCamera.ToggleMode();
			}
			if(modelNode == OverEasyGlobals.TransformGizmo.GetParent())
			{
				reAddTransform = true;
				OverEasyGlobals.TransformGizmo.Reparent(OverEasyGlobals.TransformGizmo.GetTree().Root, true);
			}
			foreach (var child in modelNode.GetChildren())
			{
				//Set visibility so this appears more immediately seamless
				if (child is Node3D child3d)
				{
					child3d.Visible = false;
				}
				child.QueueFree();
			}
			if(reAddTransform)
			{
				OverEasyGlobals.TransformGizmo.Reparent(modelNode, true);
			}
			if (reAddCamera)
			{
				OverEasyGlobals.ViewCamera.ToggleMode();
			}
		}

		public static void CacheTitleObj(ArEnemy titleObj, PuyoFile gvm)
		{
			ModelConversion.LoadGVM("titleObj", gvm, out var gvmTextures, out var gvrAlphaTypes);
			//Small flowers
			CacheModel("object_4610", titleObj.models[1], titleObj.texList[0], gvm, false, true);
			//Big flowers
			CacheModel("object_4609", titleObj.models[3], titleObj.texList[0], gvm, false, true);
			//Tree
			CacheModel("object_4611", titleObj.models[4], titleObj.texList[0], gvm, false, true);
			//Bush
			CacheModel("object_4608", titleObj.models[2], titleObj.texList[0], gvm, false, true);
			//Waterfall
			CacheModel("object_4616", titleObj.models[7], titleObj.texList[0], gvm, false, true);
			//Water
			CacheModel("object_4615", titleObj.models[8], titleObj.texList[0], gvm, false, true);
			//Rainbow
			CacheModel("object_4617", titleObj.models[9], titleObj.texList[0], gvm, false, true);
			//Chick
			CacheModel("object_4612", titleObj.models[0], titleObj.texList[0], gvm, false, true);
			//Crow
			CacheModel("object_4612_1", titleObj.models[5], titleObj.texList[0], gvm, false, true);
		}

		public static void CacheEggContentData(GEEGG gegg, List<Texture2D> gplTextures, List<int> gplAlphaTypes)
		{
			//Transform to translate items above eggs
			var tfm = System.Numerics.Matrix4x4.CreateTranslation(new System.Numerics.Vector3(0, 20, 0));
			var tfmEgg = System.Numerics.Matrix4x4.CreateScale(1.5f, 1.5f, 1.5f) * System.Numerics.Matrix4x4.CreateTranslation(new System.Numerics.Vector3(0, 7.5f, 0));

			ModelConversion.LoadGVM($"egg_shiny", gegg.gvm, out var eggGvmTextures, out var eggGvrAlphaTypes);
			var shinyEggTexSet = ModelConversion.GetTextureSubset(eggGvmTextures, gegg.texnamesList[6], eggGvrAlphaTypes, out var shinyAlphaTypes);
			gplTextures.AddRange(shinyEggTexSet);
			gplAlphaTypes.AddRange(shinyAlphaTypes);

			//Default Egg - Blue Speckled
			CacheEggModel(gegg, gplTextures, gplAlphaTypes, null, null, null, 0, tfm, tfmEgg);

			for(int i = 1; i < 66; i++)
			{
				CacheEggEntity(gegg, gplTextures, gplAlphaTypes, tfm, tfmEgg, ObjectVariants.eggFileNames[i], i);
			}
		}

		private static void CacheEggEntity(GEEGG gegg, List<Texture2D> gplTextures, List<int> gplAlphaTypes, System.Numerics.Matrix4x4 tfm, System.Numerics.Matrix4x4 tfmEgg, string file, int id)
		{
			var path = OverEasyGlobals.GetAssetPath(file);
			if (path != "" && File.Exists(path))
			{
				if(file == "obj_ms_bomb.arc")
				{
					var item = new ObjMsBomb(File.ReadAllBytes(path));
					CacheEggModel(gegg, gplTextures, gplAlphaTypes, item.model, item.texLists[0], item.gvm, id, tfm, tfmEgg);
				} else if (file.StartsWith("ani_model"))
				{
					var item = new AniModel(File.ReadAllBytes(path));
					CacheEggModel(gegg, gplTextures, gplAlphaTypes, item.models[0], item.texList, item.gvm, id, tfm, tfmEgg);
				}
				else
				{
					var item = new ItemLibModel(File.ReadAllBytes(path));
					CacheEggModel(gegg, gplTextures, gplAlphaTypes, item.model, item.texList, item.gvm, id, tfm, tfmEgg);
				}
			}
		}

		public static void CacheEggModel(GEEGG gegg, List<Texture2D> gplTextures, List<int> gplAlphaTypes, NJSObject itemNj, NJTextureList texList, PuyoFile gvm, int eggId, System.Numerics.Matrix4x4 tfm, System.Numerics.Matrix4x4 tfmEgg)
		{
			Node3D itemModel = null;
			Node3D itemModel2 = null;
			if(itemNj != null)
			{
				ModelConversion.LoadGVM($"egg_{eggId}_item", gvm, out var gvmTextures, out var gvrAlphaTypes);
				var textureSubSet = ModelConversion.GetTextureSubset(gvmTextures, texList, gvrAlphaTypes, out var itemAlphaTypes);
				textureSubSet.Add(gplTextures[^2]);
				itemAlphaTypes.Add(gplAlphaTypes[^2]);
				itemModel = ModelConversion.NinjaToGDModel($"egg_{eggId}", itemNj, textureSubSet, itemAlphaTypes, null, null, null, tfm, false, new List<float?> { 0.5f });
				itemModel2 = ModelConversion.NinjaToGDModel($"segg_{eggId}", itemNj, textureSubSet, itemAlphaTypes, null, null, null, tfm, false, new List<float?> { 0.5f });
			}
			itemModel = ModelConversion.NinjaToGDModel($"egg_{eggId}", gegg.models[0], new List<Texture2D>() { gplTextures[eggId], gplTextures[^2] }, new List<int>() { gplAlphaTypes[eggId], gplAlphaTypes[^1] }, null, null, itemModel, tfmEgg);
			ModelConversion.CreateObjectCollision(itemModel);
			itemModel2 = ModelConversion.NinjaToGDModel($"segg_{eggId}", gegg.models[0], new List<Texture2D>() { gplTextures[eggId], gplTextures[^2] }, new List<int>() { gplAlphaTypes[eggId], gplAlphaTypes[^1] }, null, null, itemModel2, tfmEgg, false, new List<float?> { 0.5f });
			ModelConversion.CreateObjectCollision(itemModel2);
			OverEasyGlobals.modelDictionary[$"egg_{eggId}"] = itemModel;
			OverEasyGlobals.modelDictionary[$"segg_{eggId}"] = itemModel2;
		}

		public static void CacheObjectModelsPC(StageDef.StageDefinition def)
		{
			//Clear prior data
			OverEasyGlobals.cachedStageObjCommonNames.Clear();
			OverEasyGlobals.cachedStageObjLocalNames.Clear();

			//Load egg data
			var amemBootPath = OverEasyGlobals.GetAssetPath("amem_boot.nrc");
			var geEggPath = OverEasyGlobals.GetAssetPath("ge_egg.arc");
			if(amemBootPath != "" && geEggPath != "")
			{
				var nrc = new PRD(File.ReadAllBytes(amemBootPath), true);
				LoadGPLTextures(nrc, out var gplTextures, out var gplAlphaTypes);

				var geEgg = new GEEGG(File.ReadAllBytes(geEggPath));
				CacheEggContentData(geEgg, gplTextures, gplAlphaTypes);
			}

			var eggGoldPath = OverEasyGlobals.GetAssetPath("egg_gold.arc");
			if (eggGoldPath != "")
			{
				var eggGoldObj = new EggGold_Suit(File.ReadAllBytes(eggGoldPath));
				CacheModel("object_28", eggGoldObj.models[0], null, eggGoldObj.gvm, false, false);
			}
			var cagePath = OverEasyGlobals.GetAssetPath("geobj_cage.arc");
			if (cagePath != "")
			{
				var cageObj = new GEObj_Object(File.ReadAllBytes(cagePath));
				CacheModel("object_33", cageObj.models["model"], cageObj.texLists["texlist"], cageObj.gvm, false, false);
			}
			var darkGatePath = OverEasyGlobals.GetAssetPath("geobj_darkgate.arc");
			if (darkGatePath != "")
			{
				var darkGateObj = new GEObj_Object(File.ReadAllBytes(darkGatePath));
				CacheModel("object_35", darkGateObj.models["model"], darkGateObj.texLists["texlist"], darkGateObj.gvm, false, false);
			}
			var goalPath = OverEasyGlobals.GetAssetPath("geobj_goal.arc");
			if (goalPath != "")
			{
				var goalObj = new GEObj_Object(File.ReadAllBytes(goalPath));
				CacheModel("object_36", goalObj.models["model"], goalObj.texLists["texlist"], goalObj.gvm, false, false);
			}
			var emblemPath = OverEasyGlobals.GetAssetPath("geobj_emblem.arc");
			if (emblemPath != "")
			{
				var emblemObj = new GEObj_Object(File.ReadAllBytes(emblemPath));
				CacheModel("object_37", emblemObj.models["model"], emblemObj.texLists["texlist"], emblemObj.gvm, false, false);
			}
			var objCoinPath = OverEasyGlobals.GetAssetPath("geobj_ring.arc");
			if (objCoinPath != "")
			{
				var coinObjBlue = new GEObj_Object(File.ReadAllBytes(objCoinPath));
				CacheModel("object_38_blue", coinObjBlue.models["model"], null, coinObjBlue.gvm, false, false);
				var coinObjRed = new GEObj_Object(File.ReadAllBytes(objCoinPath));
				CacheModel("object_38_red", coinObjRed.models["model"], null, coinObjRed.gvm, false, false);
			}
			var objBombPath = OverEasyGlobals.GetAssetPath("obj_ms_bomb.arc");
			if (objBombPath != "")
			{
				var objBomb = new ObjMsBomb(File.ReadAllBytes(objBombPath));
				CacheModel("object_39", objBomb.model, null, objBomb.gvm, false, false);
			}
			var chickenPath = OverEasyGlobals.GetAssetPath("geobj_chicken.arc");
			if (chickenPath != "")
			{
				var chickenObj = new GEObj_Object(File.ReadAllBytes(chickenPath));
				for (int j = 0; j < chickenObj.texLists.Count; j++)
				{
					if (chickenObj.texLists.ContainsKey($"texList_{j}"))
					{
						BillyModelIO.CacheModel($"object_41_{j}", chickenObj.models["model_0"], chickenObj.texLists[$"texList_{j}"], chickenObj.gvm, false, false);
					}
				}
			}
			var mgLeaderPath = OverEasyGlobals.GetAssetPath("geobj_mg_leader.arc");
			if (mgLeaderPath != "")
			{
				var mgLeaderObj = new GEObj_Object(File.ReadAllBytes(mgLeaderPath));
				CacheModel("object_42", mgLeaderObj.models["model_0"], null, mgLeaderObj.gvm, false, false);
			}
			var eggSuitPath = OverEasyGlobals.GetAssetPath("egg_suit.arc");
			if (eggSuitPath != "")
			{
				var eggSuitObj = new EggGold_Suit(File.ReadAllBytes(eggSuitPath));
				CacheModel("object_46", eggSuitObj.models[0], null, eggSuitObj.gvm, false, false);
			}
			//Load common geobj data
			var commonObjectsPath = OverEasyGlobals.GetAssetPath("geobj_common.arc");
			var commonObjectsDefPath = OverEasyGlobals.GetAssetPath("stgobj_common.arc");
			if(commonObjectsPath != "" && commonObjectsDefPath != "")
			{
				var stgobjCommon = new StageObj(File.ReadAllBytes(commonObjectsDefPath));
				for(int i = 0; i < stgobjCommon.objEntries.Count; i++)
				{
					var obj = stgobjCommon.objEntries[i];
					if(obj.model2Id0 != ushort.MaxValue)
					{
						OverEasyGlobals.cachedStageObjCommonNames.Add(i, obj.objName);
					}
				}

				var commGeobj = new GEObj_Stage(File.ReadAllBytes(commonObjectsPath));
				CacheGeobjCommon(commGeobj);
			}

			//Load local world geobj data
			var objDataFile = def.commonData != null ? def.commonData.objectData : "";
			var objDefFile = def.commonData != null ? def.commonData.objectDefinition : "";
			var localObjectsPath = OverEasyGlobals.GetAssetPath(objDataFile);
			var localObjectsDefPath = OverEasyGlobals.GetAssetPath(objDefFile);
			GEObj_Stage localGeobj = null;

			if (localObjectsPath != "" && localObjectsDefPath != "")
			{
				var localStgobj = new StageObj(File.ReadAllBytes(localObjectsDefPath));
				for (int i = 0; i < localStgobj.objEntries.Count; i++)
				{
					var obj = localStgobj.objEntries[i];
					if (obj.model2Id0 != ushort.MaxValue)
					{
						OverEasyGlobals.cachedStageObjLocalNames.Add(i, obj.objName);
					}
				}

				localGeobj = new GEObj_Stage(File.ReadAllBytes(localObjectsPath));
				CacheGeobjLocal(localStgobj, localGeobj);
			}

			var objKatanaPath = OverEasyGlobals.GetAssetPath("ar_obj_blue_katana.arc");
			var objKatanaGvmPath = OverEasyGlobals.GetAssetPath("obj_blue_katana.gvm");
			if (objKatanaPath != "" && objKatanaGvmPath != "")
			{
				ModelConversion.LoadGVM("blueKatanaGvm", new PuyoFile(File.ReadAllBytes(objKatanaGvmPath)), out var katanaTex, out List<int> gvrAlphaTypes);
				List<Texture2D> textures = new() { katanaTex[0] };
				textures.Add(textures[0]);
				gvrAlphaTypes.Add(0);
				var objKatanaArc = new ArEnemy(File.ReadAllBytes(objKatanaPath));
				var katanaBlade = ModelConversion.NinjaToGDModel("object_259_blade", objKatanaArc.models[0], textures, gvrAlphaTypes);
				var katanaHilt = ModelConversion.NinjaToGDModel("object_259", objKatanaArc.models[1], textures, gvrAlphaTypes, null, null, katanaBlade);
				ModelConversion.CreateObjectCollision(katanaHilt);
				OverEasyGlobals.modelDictionary["object_259"] = katanaHilt;
			}

			var objBlueBossPath = OverEasyGlobals.GetAssetPath("ar_obj_blue_boss.arc");
			var objBlueBossGvmPath = OverEasyGlobals.GetAssetPath("obj_blue_boss.gvm");
			if (objBlueBossPath != "" && objBlueBossGvmPath != "")
			{
				var objBlueBoss = new ArEnemy(File.ReadAllBytes(objBlueBossPath));
				var gvm = new PuyoFile(File.ReadAllBytes(objBlueBossGvmPath));
				ModelConversion.LoadGVM("obj_blue_boss.gvm", gvm, out var bossTex, out List<int> bossGvrAlphaTypes);
				var ropeFence = BillyModelIO.CacheModel("object_513", objBlueBoss.models[0], objBlueBoss.texList[0], gvm, false, true);
				var fenceBladeHilt = ModelConversion.NinjaToGDModel("object_512", objBlueBoss.models[1], bossTex, bossGvrAlphaTypes);
				var fenceBlade = ModelConversion.NinjaToGDModel("object_512_blade", objBlueBoss.models[2], bossTex, bossGvrAlphaTypes, null, null, fenceBladeHilt);
				ModelConversion.CreateObjectCollision(fenceBlade);
				OverEasyGlobals.modelDictionary["object_512"] = fenceBlade;
			}

			//Magma
			var objRedMagmaPath = OverEasyGlobals.GetAssetPath("ar_obj_red_magma.arc");
			var objRedMagmaGvmPath = OverEasyGlobals.GetAssetPath("obj_red_magma.gvm");
			if (objRedMagmaPath != "" && objRedMagmaGvmPath != "")
			{
				var objRedMagma = new ArEnemy(File.ReadAllBytes(objRedMagmaPath));
				var magmaGvm = new PuyoFile(File.ReadAllBytes(objRedMagmaGvmPath));
				CacheModel("object_768_0", objRedMagma.models[0], null, magmaGvm, false);
				CacheModel("object_768_1", objRedMagma.models[1], null, magmaGvm, false);
				CacheModel("object_768_2", objRedMagma.models[2], null, magmaGvm, false);
				CacheModel("object_768_3", objRedMagma.models[3], null, magmaGvm, false);
				CacheModel("object_768_4", objRedMagma.models[4], null, magmaGvm, false);
				CacheModel("object_768_5", objRedMagma.models[5], null, magmaGvm, false);
				CacheModel("object_768_6", objRedMagma.models[6], null, magmaGvm, false);
			}

            //Snowman
            var objSnowmanPath = OverEasyGlobals.GetAssetPath("obj_snowman.arc");
            if (objSnowmanPath != "")
            {
                var objSnowmanObj = new ObjSnowman(File.ReadAllBytes(objSnowmanPath));
                CacheModel("object_1025", objSnowmanObj.models[0], objSnowmanObj.texLists[0], objSnowmanObj.gvm, false, true);
                CacheModel("object_1026", objSnowmanObj.models[1], objSnowmanObj.texLists[0], objSnowmanObj.gvm, false, true);
            }

            //Fireworks Ball
            var objFireworksBallPath = OverEasyGlobals.GetAssetPath("obj_ms_fwball.arc");
            if (objFireworksBallPath != "")
            {
                var objFireworksBall = new ObjMsFwBall(File.ReadAllBytes(objFireworksBallPath));
                CacheModel("object_1284", objFireworksBall.model, objFireworksBall.texList, objFireworksBall.gvm, false, true);
            }

            //Fireworks Cannon
            var orangeCannonPath = OverEasyGlobals.GetAssetPath("geobj_orange_cannon.arc");
            if (orangeCannonPath != "")
            {
                var orangeCannon = new GEObj_Object(File.ReadAllBytes(orangeCannonPath));
                CacheModel("object_1287", orangeCannon.models["model_0"], orangeCannon.texLists["texList_0"], orangeCannon.gvm, false, true);
            }

            //Orange Boss Fence Post with Stringed Flags
            var objOrangeBossPath = OverEasyGlobals.GetAssetPath("ar_obj_orange_boss.arc");
            var objOrangeBossGvmPath = OverEasyGlobals.GetAssetPath("obj_orange_boss.gvm");
            if (objOrangeBossPath != "" && objOrangeBossGvmPath != "")
            {
                var objOrangeBoss = new ArEnemy(File.ReadAllBytes(objOrangeBossPath));
                var objOrangeBossGvm = new PuyoFile(File.ReadAllBytes(objOrangeBossGvmPath));
                CacheModel("object_1536", objOrangeBoss.models[0], null, objOrangeBossGvm, false);
            }
        }

		public static void LoadGPLTextures(PRD nrc, out List<Texture2D> gplTextures, out List<int> gplAlphaTypes)
		{
			gplTextures = new List<Texture2D>();
			gplAlphaTypes = new List<int>();
			for (int i = 0; i < nrc.fileNames.Count; i++)
			{
				if (nrc.fileNames[i] == "egg.gpl")
				{
					var gpl = new GPL(nrc.files[i]);
					var gvrs = gpl.GetGVRs();
					var gvrTextureList = new List<GvrTexture>();
					List<string> names = new List<string>();
					for (int t = 0; t < gvrs.Count; t++)
					{
						gvrTextureList.Add(new GvrTexture(gvrs[t]));
						names.Add($"gpl_{t}");
					}
					ModelConversion.LoadGVRTextures("egg.gpl", gvrTextureList, names, out gplTextures, out gplAlphaTypes);
				}
			}
		}

		public static void CacheGeobjLocal(StageObj stgobj, GEObj_Stage stageGeo)
		{
			ModelConversion.LoadGVM("geobjStage", stageGeo.gvm, out var gvmTextures, out var gvrAlphaTypes);

			//Gates
			if(stageGeo.models.ContainsKey("MODEL_DOOR01"))
			{
				CacheModel("object_5", stageGeo.models["MODEL_DOOR01"], stageGeo.texLists["TLS_MODEL_DOOR01"], stageGeo.gvm, true, true);
			}
			if(stageGeo.models.ContainsKey("MODEL_DOOR02"))
			{
				CacheModel("object_1285", stageGeo.models["MODEL_DOOR02"], stageGeo.texLists["TLS_MODEL_DOOR02"], stageGeo.gvm, true, true);
			}

			//Platforms
			if (stageGeo.models.ContainsKey("MODEL_STAND"))
			{
				CacheModel("object_12", stageGeo.models["MODEL_STAND"], stageGeo.texLists["TLS_MODEL_STAND"], stageGeo.gvm, false, true);
			}

			//Chicken Elder
			if (stageGeo.models.ContainsKey("MODEL_CHICKEN_BOSS"))
			{
				CacheModel("object_21", stageGeo.models["MODEL_CHICKEN_BOSS"], stageGeo.texLists["TLS_MODEL_CHICKEN_BOSS"], stageGeo.gvm, false, true);
			}

			//Bridge
			if (stageGeo.models.ContainsKey("MODEL_TURIBASHI_HASHIRA01"))
			{
				CacheModel("MODEL_TURIBASHI_HASHIRA01", stageGeo.models["MODEL_TURIBASHI_HASHIRA01"], stageGeo.texLists["TLS_MODEL_TURIBASHI_HASHIRA01"], stageGeo.gvm, false, true, null, true);
			}
			if (stageGeo.models.ContainsKey("MODEL_TURIBASHI_HASHIRA02"))
			{
				CacheModel("MODEL_TURIBASHI_HASHIRA02", stageGeo.models["MODEL_TURIBASHI_HASHIRA02"], stageGeo.texLists["TLS_MODEL_TURIBASHI_HASHIRA02"], stageGeo.gvm, false, true, null, true);
			}
			if (stageGeo.models.ContainsKey("MODEL_TURIBASHI_ITA01"))
			{
				CacheModel("MODEL_TURIBASHI_ITA01", stageGeo.models["MODEL_TURIBASHI_ITA01"], stageGeo.texLists["TLS_MODEL_TURIBASHI_ITA01"], stageGeo.gvm, false, true, null, true);
			}
			if (stageGeo.models.ContainsKey("MODEL_TURIBASHI_ITA02"))
			{
				CacheModel("MODEL_TURIBASHI_ITA02", stageGeo.models["MODEL_TURIBASHI_ITA02"], stageGeo.texLists["TLS_MODEL_TURIBASHI_ITA02"], stageGeo.gvm, false, true, null, true);
			}
			if (stageGeo.models.ContainsKey("MODEL_TURIBASHI_ITA03"))
			{
				CacheModel("MODEL_TURIBASHI_ITA03", stageGeo.models["MODEL_TURIBASHI_ITA03"], stageGeo.texLists["TLS_MODEL_TURIBASHI_ITA03"], stageGeo.gvm, false, true, null, true);
			}
			if (stageGeo.models.ContainsKey("MODEL_TURIBASHI_ROPE"))
			{
				CacheModel("MODEL_TURIBASHI_ROPE", stageGeo.models["MODEL_TURIBASHI_ROPE"], stageGeo.texLists["TLS_MODEL_TURIBASHI_ROPE"], stageGeo.gvm, false, true, null, true);
			}

            //Ice Fence D
			if(stageGeo.models.ContainsKey("MODEL_ICE_FENCE_D"))
            {
                CacheModel("object_1028", stageGeo.models["MODEL_ICE_FENCE_D"], stageGeo.texLists["TLS_MODEL_ICE_FENCE_D"], stageGeo.gvm, false, true);
            }

            //Ice Wall
			if(stageGeo.models.ContainsKey("MODEL_ICE_WALL"))
            {
                CacheModel("object_1029", stageGeo.models["MODEL_ICE_WALL"], stageGeo.texLists["TLS_MODEL_ICE_WALL"], stageGeo.gvm, false, true);
            }

            //Ice Floor
			if(stageGeo.models.ContainsKey("MODEL_ICE_FLOOR"))
            {
                CacheModel("object_1030", stageGeo.models["MODEL_ICE_FLOOR"], stageGeo.texLists["TLS_MODEL_ICE_FLOOR"], stageGeo.gvm, false, true);
            }

            //Wood Snowflake Gear
			if(stageGeo.models.ContainsKey("MODEL_GEAR_S"))
            {
                CacheModel("object_1031", stageGeo.models["MODEL_GEAR_S"], stageGeo.texLists["TLS_MODEL_GEAR_S"], stageGeo.gvm, false, true);
            }

            //Ice Snowflake Gear
			if(stageGeo.models.ContainsKey("MODEL_ICE_GEAR_S"))
            {
                CacheModel("object_1032", stageGeo.models["MODEL_ICE_GEAR_S"], stageGeo.texLists["TLS_MODEL_ICE_GEAR_S"], stageGeo.gvm, false, true);
            }

            //Ice Large Snowflake Gear
			if(stageGeo.models.ContainsKey("MODEL_ICE_GEAR_L"))
            {
                CacheModel("object_1033", stageGeo.models["MODEL_ICE_GEAR_L"], stageGeo.texLists["TLS_MODEL_ICE_GEAR_L"], stageGeo.gvm, false, true);
            }

            //Propeller
			if(stageGeo.models.ContainsKey("MODEL_PROPELLER"))
            {
                CacheModel("object_1034", stageGeo.models["MODEL_PROPELLER"], stageGeo.texLists["TLS_MODEL_PROPELLER"], stageGeo.gvm, false, true);
            }


            //Puzzle Panels
			if(stageGeo.models.ContainsKey("MODEL_PANEL_FLOOR"))
            {
                CacheModel("object_1280", stageGeo.models["MODEL_PANEL_FLOOR"], stageGeo.texLists["TLS_MODEL_PANEL_FLOOR"], stageGeo.gvm, false, true);
            }

            //Battery
			if(stageGeo.models.ContainsKey("MODEL_BATTERY"))
            {
                CacheModel("object_1281", stageGeo.models["MODEL_BATTERY"], stageGeo.texLists["TLS_MODEL_BATTERY"], stageGeo.gvm, false, true);
            }

            var def = OverEasyGlobals.stgDef.defs[OverEasyGlobals.currentMissionId];
			switch (def.worldName)
			{
				case "green":
					break;
				case "blue":
					//Autofire cannons, not to be confused with the ones that launch the player
					var cannonTfm = System.Numerics.Matrix4x4.CreateTranslation(new System.Numerics.Vector3(0, 23, 0));
					cannonTfm *= System.Numerics.Matrix4x4.CreateScale(0.5f, 0.5f, 0.5f);
					cannonTfm *= System.Numerics.Matrix4x4.CreateRotationY(-Mathf.Pi / 2);
					var cannonBaseTfm = System.Numerics.Matrix4x4.Identity;
					cannonBaseTfm *= System.Numerics.Matrix4x4.CreateScale(0.5f, 0.5f, 0.5f);
					cannonBaseTfm *= System.Numerics.Matrix4x4.CreateRotationY(-Mathf.Pi / 2);

					var cannonSubset = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_3"], gvrAlphaTypes, out var cannonGvrTypes);
					var cannon = ModelConversion.NinjaToGDModel("object_256", stageGeo.models["model_3"], cannonSubset, cannonGvrTypes, null, null, null, cannonTfm);
					cannon = ModelConversion.NinjaToGDModel("object_256", stageGeo.models["model_4"], cannonSubset, cannonGvrTypes, null, null, cannon, cannonBaseTfm);
					ModelConversion.CreateObjectCollision(cannon);
					OverEasyGlobals.modelDictionary["object_256"] = cannon;

					//Anchor
					CacheModel("object_257", stageGeo.models["model_2"], stageGeo.texLists["texList_2"], stageGeo.gvm, false, true, null);

					//Shark
					CacheModel("object_258", stageGeo.models["model_0"], stageGeo.texLists["texList_0"], stageGeo.gvm, false, true, null);

					//Waterfall + Vent
					var waterfallSubset = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_19"], gvrAlphaTypes, out var waterfallGvrTypes);
					var waterfall = ModelConversion.NinjaToGDModel("object_260", stageGeo.models["model_21"], waterfallSubset, waterfallGvrTypes);
					waterfall = ModelConversion.NinjaToGDModel("object_260", stageGeo.models["model_22"], waterfallSubset, waterfallGvrTypes, null, null, waterfall);
					ModelConversion.CreateObjectCollision(waterfall);
					OverEasyGlobals.modelDictionary["object_260"] = waterfall;

					//Fire Arrow
					CacheModel("object_261", stageGeo.models["model_5"], stageGeo.texLists["texList_5"], stageGeo.gvm, false, true);

					//Skull & Crossbones Arrow Spawners
					CacheModel("object_262", stageGeo.models["model_1"], stageGeo.texLists["texList_1"], stageGeo.gvm, false, true);

					//Giant Skull + Hookhand
					var hookSubset = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_19"], gvrAlphaTypes, out var hookGvrTypes);
					var giantSkull = ModelConversion.NinjaToGDModel("object_263", stageGeo.models["model_18"], hookSubset, hookGvrTypes);
					var giantHookHandle = ModelConversion.NinjaToGDModel("object_263", stageGeo.models["model_19"], hookSubset, hookGvrTypes, null, null, giantSkull);
					var giantHook = ModelConversion.NinjaToGDModel("object_263", stageGeo.models["model_20"], hookSubset, hookGvrTypes, null, null, giantSkull);
					ModelConversion.CreateObjectCollision(giantSkull);
					OverEasyGlobals.modelDictionary["object_263"] = giantSkull;

					//Ship
					CacheModel("object_264", stageGeo.models["model_14"], stageGeo.texLists["texList_14"], stageGeo.gvm, false, true);

					//Flag banners
					//CacheModel("object_265", stageGeo.models[""], stageGeo.texLists[""], stageGeo.gvm, false, true);
					//CacheModel("object_267", stageGeo.models[""], stageGeo.texLists[""], stageGeo.gvm, false, true);
					break;
				case "red":
					//Rising Meteor
					CacheModel("object_772", stageGeo.models["model_0"], stageGeo.texLists["texList_0"], stageGeo.gvm, false, true);

					//Dino Mouth Shooter
					CacheModel("object_774", stageGeo.models["model_1"], stageGeo.texLists["texList_1"], stageGeo.gvm, false, true);

					//Bone Dragon Head
					CacheModel("object_775", stageGeo.models["model_8"], stageGeo.texLists["texList_8"], stageGeo.gvm, false, true);

					//Cooled lava rocks
					CacheModel("object_777_0", stageGeo.models["model_2"], stageGeo.texLists["texList_2"], stageGeo.gvm, false, true);
					CacheModel("object_777_1", stageGeo.models["model_3"], stageGeo.texLists["texList_2"], stageGeo.gvm, false, true);
					CacheModel("object_777_2", stageGeo.models["model_4"], stageGeo.texLists["texList_2"], stageGeo.gvm, false, true);

                    //WWE Rope
                    CacheModel("object_778", stageGeo.models["model_19"], stageGeo.texLists["texList_10"], stageGeo.gvm, false, true);
                    break;
				case "purple":
                    //Snowflake Iceshooter
                    CacheModel("object_1035", stageGeo.models["model_4"], stageGeo.texLists["texList_1"], stageGeo.gvm, false, true);
                    break;
				case "orange":
                    //Funhouse mirror
                    CacheModel("object_1282", stageGeo.models["model_9"], stageGeo.texLists["texList_10"], stageGeo.gvm, false, true);

                    //Clockwork Tower
                    var clockWorkSubset = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_4"], gvrAlphaTypes, out var clockWorkGvrTypes);
                    var clockWorkSubset1 = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_5"], gvrAlphaTypes, out var clockWorkGvrTypes1);
                    var clockWorkSubset2 = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_6"], gvrAlphaTypes, out var clockWorkGvrTypes2);
                    var clockWorkSubset3 = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_7"], gvrAlphaTypes, out var clockWorkGvrTypes3);
                    var clockWorkSubset4 = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_8"], gvrAlphaTypes, out var clockWorkGvrTypes4);
                    var clockWork = ModelConversion.NinjaToGDModel("object_1283", stageGeo.models["model_3"], clockWorkSubset, clockWorkGvrTypes);
                    clockWork = ModelConversion.NinjaToGDModel("object_1283", stageGeo.models["model_4"], clockWorkSubset1, clockWorkGvrTypes1, null, null, clockWork);
                    clockWork = ModelConversion.NinjaToGDModel("object_1283", stageGeo.models["model_5"], clockWorkSubset2, clockWorkGvrTypes2, null, null, clockWork);
                    clockWork = ModelConversion.NinjaToGDModel("object_1283", stageGeo.models["model_6"], clockWorkSubset3, clockWorkGvrTypes3, null, null, clockWork);
                    clockWork = ModelConversion.NinjaToGDModel("object_1283", stageGeo.models["model_7"], clockWorkSubset4, clockWorkGvrTypes4, null, null, clockWork);
                    ModelConversion.CreateObjectCollision(clockWork);
                    OverEasyGlobals.modelDictionary["object_1283"] = clockWork;
                    break;
				case "yellow":
                    //Ground Gate
                    CacheModel("object_1792", stageGeo.models["model_0"], stageGeo.texLists["texList_0"], stageGeo.gvm, false, true);

                    //Crowshooter
                    CacheModel("object_1793", stageGeo.models["model_1"], stageGeo.texLists["texList_1"], stageGeo.gvm, false, true);

                    //Elder Statue
                    CacheModel("object_1795", stageGeo.models["model_2"], stageGeo.texLists["texList_2"], stageGeo.gvm, false, true);

                    //Falling Pillar
                    CacheModel("object_1797", stageGeo.models["model_6"], stageGeo.texLists["texList_6"], stageGeo.gvm, false, true);

                    //Falling Sand Room Platforms
                    CacheModel("object_1798", stageGeo.models["model_3"], stageGeo.texLists["texList_3"], stageGeo.gvm, false, true);

                    //Falling Sand Flow
                    CacheModel("object_1799", stageGeo.models["model_4"], stageGeo.texLists["texList_4"], stageGeo.gvm, false, true);

                    //Fog Elder
                    CacheModel("object_1801", stageGeo.models["model_7"], stageGeo.texLists["texList_7"], stageGeo.gvm, false, true);

                    //Falling Sand's Rising Sand Pool
                    var sandSubset = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_8"], gvrAlphaTypes, out var sandGvrTypes);
                    var sandSubset1 = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_9"], gvrAlphaTypes, out var sandGvrTypes1);
                    var sand = ModelConversion.NinjaToGDModel("object_1802", stageGeo.models["model_8"], sandSubset, sandGvrTypes, null, null, null, null, true);
                    sand = ModelConversion.NinjaToGDModel("object_1802", stageGeo.models["model_9"], sandSubset1, sandGvrTypes1, null, null, sand, null, true);
                    ModelConversion.CreateObjectCollision(sand);
                    OverEasyGlobals.modelDictionary["object_1802"] = sand;

                    //Rainbow Gate + Light Gates
                    var rainbowGateTexSet = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_10"], gvrAlphaTypes, out var rainbowGvrTypes);
                    var greenGateTexSet = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_11"], gvrAlphaTypes, out var greenGvrTypes);
                    var blueGateTexSet = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_12"], gvrAlphaTypes, out var blueGvrTypes);
                    var redGateTexSet = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_13"], gvrAlphaTypes, out var redGvrTypes);
                    var purpleGateTexSet = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_14"], gvrAlphaTypes, out var purpleGvrTypes);
                    var orangeGateTexSet = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_15"], gvrAlphaTypes, out var orangeGvrTypes);
                    var yellowGateTexSet = ModelConversion.GetTextureSubset(gvmTextures, stageGeo.texLists["texList_16"], gvrAlphaTypes, out var yellowGvrTypes);
                    var rainbowGate = ModelConversion.NinjaToGDModel("object_1803", stageGeo.models["model_10"], rainbowGateTexSet, rainbowGvrTypes);

                    //Math for other gates. They're each placed at 60 degree increments with a starting offset of 30 degrees with some post adjustments
                    //Based on https://stackoverflow.com/questions/13695317/rotate-a-point-around-another-point, simplified since 0 is x and our center is the origin
                    List<Vector3> gateTranslations = new List<Vector3>();
					for(int i = 0; i < 6; i++)
					{
						double radAngle = (i * 60 + 30) * Mathf.Pi / 180;
						double cosTheta = Mathf.Cos(radAngle);
						double sinTheta = Mathf.Sin(radAngle);
						var distance = 180;
						var x = -sinTheta * distance;
						var z = cosTheta * distance;
						var pos = new Vector3((float)x, 0, (float)z);
						
						//Sonic Team manually adjusted some of these
						switch(i)
						{
							case 0:
                                pos += new Vector3(1.1f, 0, -2f);
                                break;
                            case 1:
								pos += new Vector3(-7f, 0, -2f);
                                break;
                            case 2:
                                pos += new Vector3(-1.2f, 0, 0);
                                break;
                            case 3:
                                pos += new Vector3(-3.2f, 0, 0);
                                break;
                            case 4:
                                pos += new Vector3(3f, 0, -2f);
                                break;
                            case 5:
                                pos += new Vector3(-3.8f, 0, -2f);
                                break;
                        }
                        gateTranslations.Add(pos);
					}

                    rainbowGate = ModelConversion.NinjaToGDModel("object_1803", stageGeo.models["model_11"], greenGateTexSet, greenGvrTypes, null, null, rainbowGate, System.Numerics.Matrix4x4.Identity * System.Numerics.Matrix4x4.CreateTranslation(gateTranslations[0].ToSNVec3()));
                    rainbowGate = ModelConversion.NinjaToGDModel("object_1803", stageGeo.models["model_11"], blueGateTexSet, blueGvrTypes, null, null, rainbowGate, System.Numerics.Matrix4x4.Identity * System.Numerics.Matrix4x4.CreateTranslation(gateTranslations[1].ToSNVec3()));
                    rainbowGate = ModelConversion.NinjaToGDModel("object_1803", stageGeo.models["model_11"], redGateTexSet, redGvrTypes, null, null, rainbowGate, System.Numerics.Matrix4x4.Identity * System.Numerics.Matrix4x4.CreateTranslation(gateTranslations[2].ToSNVec3()));
                    rainbowGate = ModelConversion.NinjaToGDModel("object_1803", stageGeo.models["model_11"], purpleGateTexSet, purpleGvrTypes, null, null, rainbowGate, System.Numerics.Matrix4x4.Identity * System.Numerics.Matrix4x4.CreateTranslation(gateTranslations[3].ToSNVec3()));
                    rainbowGate = ModelConversion.NinjaToGDModel("object_1803", stageGeo.models["model_11"], orangeGateTexSet, orangeGvrTypes, null, null, rainbowGate, System.Numerics.Matrix4x4.Identity * System.Numerics.Matrix4x4.CreateTranslation(gateTranslations[4].ToSNVec3()));
                    rainbowGate = ModelConversion.NinjaToGDModel("object_1803", stageGeo.models["model_11"], yellowGateTexSet, yellowGvrTypes, null, null, rainbowGate, System.Numerics.Matrix4x4.Identity * System.Numerics.Matrix4x4.CreateTranslation(gateTranslations[5].ToSNVec3()));
                    ModelConversion.CreateObjectCollision(rainbowGate);
                    OverEasyGlobals.modelDictionary["object_1803"] = rainbowGate;
                    break;
				case "last":
					break;
				case "blueboss":
					break;
				case "redboss":
					break;
				case "purpleboss":
					break;
				case "orangeboss":
					break;
				case "yellowboss":
					break;
				case "greenboss":
					break;
				case "lastboss":
					break;
				case "lastboss2":
					break;
				case "title":
					break;
			}

			//Scenery
			for (int i = 0; i < stgobj.objEntries.Count; i++)
			{
				var objEntry = stgobj.objEntries[i];

				//Model2s all share the same texlist
				if(objEntry.model2Id0 == ushort.MaxValue)
				{
					continue;
				}
				CacheModel($"commGeoM2Local_{i}", stageGeo.model2s[$"model2_{objEntry.model2Id0}"], stageGeo.texList2s["texList2_0"], stageGeo.gvm, false, true);
			}
		}

		public static void CacheGeobjCommon(GEObj_Stage commonGeo)
		{
			List<int> diffuseAsAlphaList = new List<int> { 15, 16, 27, 28 };
			ModelConversion.LoadGVM("geobjCommon", commonGeo.gvm, out var gvmTextures, out var gvrAlphaTypes, diffuseAsAlphaList);

			//Switches
			List<float?> invisOpacity = new List<float?>() { 0.5f };
			CacheModel($"object_4_0", commonGeo.models[$"model_{0}"], commonGeo.texLists["texList_0"], commonGeo.gvm, false, true, invisOpacity);
			CacheModel($"object_4_1", commonGeo.models[$"model_{0}"], commonGeo.texLists["texList_0"], commonGeo.gvm, false, true, invisOpacity);
			CacheModel($"object_4_2", commonGeo.models[$"model_{0}"], commonGeo.texLists["texList_0"], commonGeo.gvm, false, true, invisOpacity);
			CacheModel($"object_4_3", commonGeo.models[$"model_{0}"], commonGeo.texLists["texList_0"], commonGeo.gvm, false, true);
			CacheModel($"object_4_4", commonGeo.models[$"model_{0}"], commonGeo.texLists["texList_0"], commonGeo.gvm, false, true);
			CacheModel($"object_4_5", commonGeo.models[$"model_{0}"], commonGeo.texLists["texList_0"], commonGeo.gvm, false, true);

			//Gates
			//These won't normally be here, but in theory the game will try to load them here if they aren't in the local world's geobj
			//The collision models for these ARE used in the common, but aren't used in OverEasy currently
			if (commonGeo.models.ContainsKey($"MODEL_DOOR01"))
			{
				CacheModel($"object_5", commonGeo.models[$"MODEL_DOOR01"], commonGeo.texLists["TLS_MODEL_DOOR01"], commonGeo.gvm, false, true);
			}
			if (commonGeo.models.ContainsKey($"MODEL_DOOR02"))
			{
				CacheModel($"object_1285", commonGeo.models[$"MODEL_DOOR02"], commonGeo.texLists["TLS_MODEL_DOOR02"], commonGeo.gvm, false, true);
			}

			//Platforms
			if (commonGeo.models.ContainsKey($"MODEL_STAND"))
			{
				CacheModel($"object_12", commonGeo.models[$"MODEL_STAND"], commonGeo.texLists["TLS_MODEL_STAND"], commonGeo.gvm, false, true);
			}

			//Fire Pillar
			CacheModel($"object_13", commonGeo.models[$"model_1"], commonGeo.texLists["texList_0"], commonGeo.gvm, false, true);

			//Climbing Rung
			CacheModel($"object_17", commonGeo.models[$"model_34"], commonGeo.texLists["texList_0"], commonGeo.gvm, false, true);

			//Chick NPC
			//We'll load the pieces in for this, then enable/disable them dynamically and color it dynamically via shader parameters
			var chickAQN = new AquaNode();
			var chickTexSubset = ModelConversion.GetTextureSubset(gvmTextures, commonGeo.texLists["texList_13"], gvrAlphaTypes, out var chickAlphaTypes);
			var chickNPC = ModelConversion.NinjaToGDModel("chickNPC", commonGeo.models["model_14"], chickTexSubset, chickAlphaTypes, chickAQN);
			var baseballCap = ModelConversion.NinjaToGDModel("chickNPCBaseballCap", commonGeo.models["model_15"], chickTexSubset, chickAlphaTypes, null, null, chickNPC, System.Numerics.Matrix4x4.CreateTranslation(chickAQN.nodeList[14].GetInverseBindPoseMatrixInverted().Translation));
			var hairBow = ModelConversion.NinjaToGDModel("chickNPCHairBow", commonGeo.models["model_16"], chickTexSubset, chickAlphaTypes, null, null, chickNPC, System.Numerics.Matrix4x4.CreateTranslation(chickAQN.nodeList[14].GetInverseBindPoseMatrixInverted().Translation));
			var bowTie = ModelConversion.NinjaToGDModel("chickNPCBowTie", commonGeo.models["model_17"], chickTexSubset, chickAlphaTypes, null, null, chickNPC, System.Numerics.Matrix4x4.CreateTranslation(chickAQN.nodeList[38].GetInverseBindPoseMatrixInverted().Translation));
			var eggshellCap = ModelConversion.NinjaToGDModel("chickNPCEggShellCap", commonGeo.models["model_18"], chickTexSubset, chickAlphaTypes, null, null, chickNPC, System.Numerics.Matrix4x4.CreateTranslation(chickAQN.nodeList[14].GetInverseBindPoseMatrixInverted().Translation));
			var dress = ModelConversion.NinjaToGDModel("chickNPCDress", commonGeo.models["model_19"], chickTexSubset, chickAlphaTypes, null, null, chickNPC, System.Numerics.Matrix4x4.CreateTranslation(chickAQN.nodeList[38].GetInverseBindPoseMatrixInverted().Translation));
			ModelConversion.CreateObjectCollision(chickNPC);
			OverEasyGlobals.modelDictionary["chickNPC"] = chickNPC;

			//Bowling Ball
			if (commonGeo.models.ContainsKey($"model_20"))
			{
				CacheModel($"object_23", commonGeo.models[$"model_20"], new NJTextureList() { texNames = new List<string>() { "h_common0564", "h_common0464" } }, commonGeo.gvm, false, true);
			}

			//Bowling Launcher
			if (commonGeo.models.ContainsKey($"model_21"))
			{
				var bowlingAqn = new AquaNode();
				var bowlingTexSet = ModelConversion.GetTextureSubset(gvmTextures, new NJTextureList() { texNames = new List<string>() { "h_common03256" } }, gvrAlphaTypes, out var bowlingTexTypes);
				var bowlingLauncher = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_21"], bowlingTexSet, bowlingTexTypes, bowlingAqn);
				var bowlingLauncher1 = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_22"], bowlingTexSet, bowlingTexTypes, bowlingAqn, null, bowlingLauncher);
				var pinOpacity = new List<float?>() { 0.15f };
				var bowlingPin0 = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_23"], bowlingTexSet, bowlingTexTypes, bowlingAqn, null, bowlingLauncher, System.Numerics.Matrix4x4.CreateTranslation(0, 0, 200f), false, pinOpacity);
				var bowlingPin1 = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_23"], bowlingTexSet, bowlingTexTypes, bowlingAqn, null, bowlingLauncher, System.Numerics.Matrix4x4.CreateTranslation(0, 0, 280f), false, pinOpacity);
				var bowlingPin2 = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_23"], bowlingTexSet, bowlingTexTypes, bowlingAqn, null, bowlingLauncher, System.Numerics.Matrix4x4.CreateTranslation(23f, 0, 240f), false, pinOpacity);
				var bowlingPin3 = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_23"], bowlingTexSet, bowlingTexTypes, bowlingAqn, null, bowlingLauncher, System.Numerics.Matrix4x4.CreateTranslation(-23f, 0, 240f), false, pinOpacity);
				var bowlingPin4 = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_23"], bowlingTexSet, bowlingTexTypes, bowlingAqn, null, bowlingLauncher, System.Numerics.Matrix4x4.CreateTranslation(-46f, 0, 280f), false, pinOpacity);
				var bowlingPin5 = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_23"], bowlingTexSet, bowlingTexTypes, bowlingAqn, null, bowlingLauncher, System.Numerics.Matrix4x4.CreateTranslation(46f, 0, 280f), false, pinOpacity);
				var bowlingPin6 = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_23"], bowlingTexSet, bowlingTexTypes, bowlingAqn, null, bowlingLauncher, System.Numerics.Matrix4x4.CreateTranslation(-69f, 0, 320f), false, pinOpacity);
				var bowlingPin7 = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_23"], bowlingTexSet, bowlingTexTypes, bowlingAqn, null, bowlingLauncher, System.Numerics.Matrix4x4.CreateTranslation(69f, 0, 320f), false, pinOpacity);
				var bowlingPin8 = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_23"], bowlingTexSet, bowlingTexTypes, bowlingAqn, null, bowlingLauncher, System.Numerics.Matrix4x4.CreateTranslation(-23f, 0, 320f), false, pinOpacity);
				var bowlingPin9 = ModelConversion.NinjaToGDModel($"object_24", commonGeo.models[$"model_23"], bowlingTexSet, bowlingTexTypes, bowlingAqn, null, bowlingLauncher, System.Numerics.Matrix4x4.CreateTranslation(23f, 0, 320f), false, pinOpacity);
				ModelConversion.CreateObjectCollision(bowlingLauncher);
				OverEasyGlobals.modelDictionary["object_24"] = bowlingLauncher;
			}

			//Egg Bounce Switch
			var switchTexSet = ModelConversion.GetTextureSubset(gvmTextures, commonGeo.texLists["texList_11"], gvrAlphaTypes, out var bounceAlphaTypes);
			var eggBounceSwitchModel = ModelConversion.NinjaToGDModel($"object_26_base", commonGeo.models[$"model_32"], switchTexSet, bounceAlphaTypes, null, null, null, System.Numerics.Matrix4x4.Identity * System.Numerics.Matrix4x4.CreateTranslation(0, -8, 0));
			var eggBounceSwitchFinalModel = ModelConversion.NinjaToGDModel($"object_26", commonGeo.models[$"model_33"], switchTexSet, bounceAlphaTypes, null, null, eggBounceSwitchModel);
			ModelConversion.CreateObjectCollision(eggBounceSwitchFinalModel);
			OverEasyGlobals.modelDictionary["object_26"] = eggBounceSwitchFinalModel;

			//Animal Breakable Boulder
			CacheModel($"object_27", commonGeo.models[$"model_36"], commonGeo.texLists["texList_36"], commonGeo.gvm, false, true);

			//Cannon
			CacheModel($"object_29", commonGeo.models[$"model_35"], commonGeo.texLists["texList_34"], commonGeo.gvm, false, true);

			//Hoops
			//Base Hoop
			var hoopMainMesh = (Mesh)GD.Load(@"res://Models/HoopModel.obj");
			var hoopSphereMesh = (Mesh)GD.Load(@"res://Models/HoopSphere.obj");
			var hoopStretchSphere = (PackedScene)GD.Load(@"res://Models/HoopStretchSphere.fbx");

			//Orange
			Node3D orangeHoop = GetBaseHoop(hoopMainMesh, orangeHoopColor);
			GetHoopSphere(orangeHoop, hoopSphereMesh, orangeHoopColor, new Vector3(0, 0, 25));
			GetHoopSphere(orangeHoop, hoopSphereMesh, orangeHoopColor, new Vector3(21.65f, 0, -12.5f)); //Since these are angled at 60 degrees and still 25 units away, we can use the 30 60 90 rule to get this 
			GetHoopSphere(orangeHoop, hoopSphereMesh, orangeHoopColor, new Vector3(-21.65f, 0, -12.5f)); //We could have placed the origin at the center and rotated, but that's no fun
			ModelConversion.CreateObjectCollision(orangeHoop);
			OverEasyGlobals.modelDictionary["object_45_0"] = orangeHoop;

			//Yellow
			Node3D yellowHoop = GetBaseHoop(hoopMainMesh, yellowHoopColor);
			ModelConversion.CreateObjectCollision(yellowHoop);
			OverEasyGlobals.modelDictionary["object_45_1"] = yellowHoop;

			//Green
			Node3D greenHoop = GetBaseHoop(hoopMainMesh, greenHoopColor);
			ModelConversion.CreateObjectCollision(greenHoop);
			OverEasyGlobals.modelDictionary["object_45_2"] = greenHoop;

			//Blue
			Node3D blueHoop = GetBaseHoop(hoopMainMesh, blueHoopColor, true);
			GetHoopSphere(blueHoop, hoopSphereMesh, blueHoopColor, new Vector3(0, 27, 0));
			ModelConversion.CreateObjectCollision(blueHoop);
			OverEasyGlobals.modelDictionary["object_45_3"] = blueHoop;

			//Teal
			Node3D tealHoop = GetBaseHoop(hoopMainMesh, tealHoopColor, true);
			ModelConversion.CreateObjectCollision(tealHoop);
			OverEasyGlobals.modelDictionary["object_45_4"] = tealHoop;

			//Magenta
			Node3D magentaHoop = GetBaseHoop(hoopMainMesh, magentaHoopColor, true);
			ModelConversion.CreateObjectCollision(magentaHoop);
			OverEasyGlobals.modelDictionary["object_45_5"] = magentaHoop;

			//Chick Coin
			CacheModel($"object_47", commonGeo.models[$"model_52"], commonGeo.texLists["texList_45"], commonGeo.gvm, false, true);

			//Breakable X Platform
			CacheModel($"object_49", commonGeo.models[$"model_53"], commonGeo.texLists["texList_47"], commonGeo.gvm, false, true);
			
			//Fruit balls
			var tfm = System.Numerics.Matrix4x4.CreateTranslation(new System.Numerics.Vector3(0, 5, 0));

			var appleBall = ModelConversion.NinjaToGDModel("fruitBall", commonGeo.models["model_24"], ModelConversion.GetTextureSubset(gvmTextures, commonGeo.texLists["texList_23"], gvrAlphaTypes, out var sphereAlphaTypes), sphereAlphaTypes, null, null, null, tfm);
			var bananaBall = ModelConversion.GDModelClone(appleBall);
			var cherryBall = ModelConversion.GDModelClone(appleBall);
			var melonBall = ModelConversion.GDModelClone(appleBall);
			var pineappleBall = ModelConversion.GDModelClone(appleBall);
			var strawberryBall = ModelConversion.GDModelClone(appleBall);
			var watermelonBall = ModelConversion.GDModelClone(appleBall);

			var texList24 = ModelConversion.GetTextureSubset(gvmTextures, commonGeo.texLists["texList_24"], gvrAlphaTypes, out var fruitAlphaTypes);
			var apple = ModelConversion.NinjaToGDModel("apple", commonGeo.models["model_25"], texList24, fruitAlphaTypes, null, null, appleBall, tfm);
			var banana = ModelConversion.NinjaToGDModel("banana", commonGeo.models["model_26"], texList24, fruitAlphaTypes, null, null, bananaBall, tfm);
			var cherry = ModelConversion.NinjaToGDModel("cherry", commonGeo.models["model_27"], texList24, fruitAlphaTypes, null, null, cherryBall, tfm);
			var melon = ModelConversion.NinjaToGDModel("melon", commonGeo.models["model_28"], texList24, fruitAlphaTypes, null, null, melonBall, tfm);
			var pineapple = ModelConversion.NinjaToGDModel("pineapple", commonGeo.models["model_29"], texList24, fruitAlphaTypes, null, null, pineappleBall, tfm);
			var strawberry = ModelConversion.NinjaToGDModel("strawberry", commonGeo.models["model_30"], texList24, fruitAlphaTypes, null, null, strawberryBall, tfm);
			var watermelon = ModelConversion.NinjaToGDModel("watermelon", commonGeo.models["model_31"], texList24, fruitAlphaTypes, null, null, watermelonBall, tfm);

			ModelConversion.CreateObjectCollision(apple);
			ModelConversion.CreateObjectCollision(banana);
			ModelConversion.CreateObjectCollision(cherry);
			ModelConversion.CreateObjectCollision(melon);
			ModelConversion.CreateObjectCollision(pineapple);
			ModelConversion.CreateObjectCollision(strawberry);
			ModelConversion.CreateObjectCollision(watermelon);

			OverEasyGlobals.modelDictionary["object_11_0"] = apple;
			OverEasyGlobals.modelDictionary["object_11_1"] = banana;
			OverEasyGlobals.modelDictionary["object_11_2"] = cherry;
			OverEasyGlobals.modelDictionary["object_11_3"] = melon;
			OverEasyGlobals.modelDictionary["object_11_4"] = pineapple;
			OverEasyGlobals.modelDictionary["object_11_5"] = strawberry;
			OverEasyGlobals.modelDictionary["object_11_6"] = watermelon;

			//Scenery
			for (int i = 0; i < commonGeo.model2s.Count; i++)
			{
				//Model2s all share the same texlist
				CacheModel($"commGeoM2Common_{i}", commonGeo.model2s[$"model2_{i}"], commonGeo.texList2s["texList2_0"], commonGeo.gvm, false, true);
			}
		}

		private static Node3D GetBaseHoop(Mesh hoopMainMesh, Color color, bool rotateX = false)
		{
			Node3D hoop = new();
			MeshInstance3D hoopInst = new();
			hoopInst.SetMeta("skipNight", 1);
			hoopInst.Name = "hoopMesh";
			hoopInst.Mesh = hoopMainMesh;
			hoopInst.MaterialOverride = new StandardMaterial3D() { AlbedoColor = color };
			hoop.AddChild(hoopInst);
			if(rotateX)
			{
				hoopInst.RotateX(Mathf.Pi / 2);
			}
			return hoop;
		}

		private static Node3D GetHoopSphere(Node3D root, Mesh hoopSphereMesh, Color color, Vector3 offset)
		{
			MeshInstance3D orangeHoopInst = new();
			orangeHoopInst.Mesh = hoopSphereMesh;
			orangeHoopInst.MaterialOverride = new StandardMaterial3D() { AlbedoColor = color };
			root.AddChild(orangeHoopInst);
			orangeHoopInst.Translate(offset);

			return root;
		}

		public static void CachePlayerModelsPC()
		{
			var billy = new GEPlayer(File.ReadAllBytes(OverEasyGlobals.GetAssetPath("ge_player1.arc")));
			var rolly = new GEPlayer(File.ReadAllBytes(OverEasyGlobals.GetAssetPath("ge_player2.arc")));
			var chick = new GEPlayer(File.ReadAllBytes(OverEasyGlobals.GetAssetPath("ge_player3.arc")));
			var bantam = new GEPlayer(File.ReadAllBytes(OverEasyGlobals.GetAssetPath("ge_player4.arc")));

			CachePlayerModel("player_1", billy, false);
			CachePlayerModel("player_2", rolly, false);
			CachePlayerModel("player_3", chick, false);
			CachePlayerModel("player_4", bantam, false);
		}
		public static Node3D CachePlayerModel(string name, GEPlayer player, bool forceAdd)
		{
			ModelConversion.LoadGVM(name, player.gvm, out var gvmTextures, out var gvrAlphaTypes);
			AquaNode playerAqn = new AquaNode();
			var modelNode = ModelConversion.NinjaToGDModel(name, player.models[0], gvmTextures, gvrAlphaTypes, playerAqn);
			var combNode = ModelConversion.NinjaToGDModel(name, player.models[1], gvmTextures, gvrAlphaTypes, null, null, modelNode, System.Numerics.Matrix4x4.CreateTranslation(playerAqn.nodeList[55].GetInverseBindPoseMatrixInverted().Translation));
			var faceNode = ModelConversion.NinjaToGDModel(name, player.models[2], gvmTextures, gvrAlphaTypes, null, null, modelNode, System.Numerics.Matrix4x4.CreateTranslation(playerAqn.nodeList[57].GetInverseBindPoseMatrixInverted().Translation));
			var leftHandNode = ModelConversion.NinjaToGDModel(name, player.models[3], gvmTextures, gvrAlphaTypes, null, null, modelNode, System.Numerics.Matrix4x4.CreateRotationY(Mathf.Pi) * System.Numerics.Matrix4x4.CreateTranslation(playerAqn.nodeList[47].GetInverseBindPoseMatrixInverted().Translation));
			var rightHandNode = ModelConversion.NinjaToGDModel(name, player.models[4], gvmTextures, gvrAlphaTypes, null, null, modelNode, System.Numerics.Matrix4x4.CreateTranslation(playerAqn.nodeList[37].GetInverseBindPoseMatrixInverted().Translation));

			ModelConversion.CreateObjectCollision(modelNode);
			if (forceAdd || !OverEasyGlobals.modelDictionary.ContainsKey(name))
			{
				OverEasyGlobals.modelDictionary[name] = modelNode;
			}

			return modelNode;
		}

		public static Node3D CacheModel(string name, NJSObject nj, NJTextureList njtl, PuyoFile gvm, bool forceAdd, bool blockVertColors = false, List<float?> forcedOpacityList = null, bool skipCollision = false)
		{
			ModelConversion.LoadGVM(name, gvm, out var gvmTextures, out var gvrAlphaTypes);
			List<Texture2D> textureSubSet;
			List<int> texAlphaTypes;
			if(njtl != null)
			{
				textureSubSet = ModelConversion.GetTextureSubset(gvmTextures, njtl, gvrAlphaTypes, out texAlphaTypes);
			} else
			{
				textureSubSet = gvmTextures;
				texAlphaTypes = gvrAlphaTypes;
			}

			System.Numerics.Matrix4x4? rootTfm = null;
			System.Numerics.Matrix4x4 posMat = System.Numerics.Matrix4x4.Identity;
			System.Numerics.Matrix4x4 sclMat = System.Numerics.Matrix4x4.Identity;

			switch (name)
			{
				case "object_38_red":
					sclMat = System.Numerics.Matrix4x4.CreateScale(2, 2, 2);
					posMat = System.Numerics.Matrix4x4.CreateTranslation(0, 1.5f, 0);
					rootTfm = System.Numerics.Matrix4x4.Identity * posMat * sclMat;
					break;
				case "object_38_blue":
					posMat = System.Numerics.Matrix4x4.CreateTranslation(0, 1.5f, 0);
					rootTfm = System.Numerics.Matrix4x4.Identity * posMat;
					break;
				case "object_28":
				case "object_46":
					rootTfm = System.Numerics.Matrix4x4.CreateScale(1.5f, 1.5f, 1.5f) * System.Numerics.Matrix4x4.CreateTranslation(new System.Numerics.Vector3(0, 7.5f, 0));
					break;
			}

			var modelNode = ModelConversion.NinjaToGDModel(name, nj, textureSubSet, texAlphaTypes, null, null, null, rootTfm, blockVertColors, forcedOpacityList);
			if(!skipCollision)
			{
				ModelConversion.CreateObjectCollision(modelNode);
			}
			if (forceAdd || !OverEasyGlobals.modelDictionary.ContainsKey(name))
			{
				OverEasyGlobals.modelDictionary[name] = modelNode;
			}

			return modelNode;
		}
	}
}
