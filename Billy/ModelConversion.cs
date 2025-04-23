using AquaModelLibrary.Data.BillyHatcher;
using AquaModelLibrary.Data.BillyHatcher.LNDH;
using AquaModelLibrary.Data.Ninja;
using AquaModelLibrary.Data.Ninja.Model;
using AquaModelLibrary.Data.Ninja.Motion;
using AquaModelLibrary.Data.PSO2.Aqua;
using AquaModelLibrary.Data.PSO2.Aqua.AquaNodeData;
using AquaModelLibrary.Data.PSO2.Aqua.AquaObjectData;
using AquaModelLibrary.Helpers.Readers;
using ArchiveLib;
using Godot;
using OverEasy.Util;
using System;
using System.Collections.Generic;
using System.IO;
using VrSharp;
using VrSharp.Gvr;
using Material = Godot.Material;

namespace OverEasy.Billy
{
	public class ModelConversion
	{
		public static void BillyModeNightToggleParent(Node3D parentNode)
		{
			OverEasyGlobals.SetBillyLighting();
			BillyModeNightToggle(parentNode);
		}
		public static void BillyModeNightToggle(Node parentNode)
		{
			if (parentNode is MeshInstance3D)
			{
				if (((MeshInstance3D)parentNode).Mesh is ArrayMesh)
				{
					BillyModeNightToggleMesh((ArrayMesh)((MeshInstance3D)parentNode).Mesh);
				}
			}
			var nodes = parentNode.GetChildren();
			foreach (var node in nodes)
			{
				BillyModeNightToggle(node);
			}
		}

		public static void BillyModeNightToggleMesh(ArrayMesh mesh)
		{
			List<Godot.Collections.Array> meshList = new List<Godot.Collections.Array>();
			List<Material> matList = new List<Material>();

			//Swap valid color data
			for (int s = 0; s < mesh.GetSurfaceCount(); s++)
			{
				var meshArrs = mesh.SurfaceGetArrays(s);
				var format = mesh.SurfaceGetFormat(s);
				List<Mesh.ArrayFormat> formatList = new List<Mesh.ArrayFormat>();
				foreach (var fmt in Enum.GetValues(typeof(Mesh.ArrayFormat)))
				{
					if ((format & (Mesh.ArrayFormat)fmt) > 0)
					{
						formatList.Add((Mesh.ArrayFormat)fmt);
					}
				}
				var mat = mesh.SurfaceGetMaterial(s);
				if (meshArrs == null || (format & Mesh.ArrayFormat.FormatCustom0) == 0)
				{
					matList.Add(mat);
					meshList.Add(meshArrs);
					continue;
				}
				Color[] colors = meshArrs[(int)Mesh.ArrayType.Color].AsColorArray();
				float[] color2s = meshArrs[(int)Mesh.ArrayType.Custom0].AsFloat32Array();
				if (colors.Length == 0 || color2s.Length == 0)
				{
					continue;
				}

				for (int i = 0; i < colors.Length; i++)
				{
					var tempColor = colors[i];
					colors[i] = new Color(color2s[i * 4], color2s[i * 4 + 1], color2s[i * 4 + 2], color2s[i * 4 + 3]);

					color2s[i * 4] = tempColor.R;
					color2s[i * 4 + 1] = tempColor.G;
					color2s[i * 4 + 2] = tempColor.B;
					color2s[i * 4 + 3] = tempColor.A;
				}

				meshArrs[(int)Mesh.ArrayType.Color] = colors;
				meshArrs[(int)Mesh.ArrayType.Custom0] = color2s;
				matList.Add(mat);
				meshList.Add(meshArrs);
			}
			mesh.ClearSurfaces();

			//Reassign model data
			for (int i = 0; i < meshList.Count; i++)
			{
				mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, meshList[i], null, null, (Mesh.ArrayFormat)((int)Mesh.ArrayCustomFormat.RgbaFloat << (int)Mesh.ArrayFormat.FormatCustom0Shift));
				mesh.SurfaceSetMaterial(i, matList[i]);
			}
		}

		public static Node3D CreateDefaultObjectModel(string name, Color color, Node3D root = null)
		{
			if(root == null)
			{
				root = new Node3D();
			}
			MeshInstance3D meshInst = new MeshInstance3D();
			var box = new BoxMesh();
			box.Size = new Vector3(10, 10, 10);
			var mat = new StandardMaterial3D();
			mat.AlbedoColor = color;
			mat.BlendMode = BaseMaterial3D.BlendModeEnum.Mix;
			mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerVertex;
			box.Material = mat;
			meshInst.Mesh = box;
			root.AddChild(meshInst);

			return root;
		}

		public static Node3D GDModelClone(Node3D modelNode, Node3D root = null)
		{
			if(root == null)
			{
				root = new Node3D();
			}
			foreach(var child in modelNode.GetChildren())
			{
				root.AddChild(child.Duplicate());
			}

			return root;
		}

		public static void NinjaAnimsToGDAnims(NJSObject nj, List<NJSMotion> njmList, Skeleton3D skel)
		{
			//We need to know which bones are actually used for animation so first we gather up mappings for that.
			List<int> animBoneMappings = new List<int>();
			int skipCount = 0;
			GetNinjaNoAnimateMapping(animBoneMappings, nj, ref skipCount);

			//
		}

		public static void GetNinjaNoAnimateMapping(List<int> mappings, NJSObject nj, ref int skipCount)
		{
			bool animatable = (nj.flags & ObjectFlags.NoAnimate) > 0;
			if(animatable)
			{
				skipCount++;
				mappings.Add(-1);
			} else
			{
				mappings.Add(mappings.Count + skipCount);
			}

			if (nj.childObject != null)
			{
				GetNinjaNoAnimateMapping(mappings, nj, ref skipCount);
			}

			if (nj.siblingObject != null)
			{
				GetNinjaNoAnimateMapping(mappings, nj, ref skipCount);
			}
		}

		public static NJSObject ReadNJ(byte[] file, NinjaVariant variant, bool hasHeader, int offset)
		{
			using (MemoryStream ms = new MemoryStream(file))
			using (BufferedStreamReaderBE<MemoryStream> sr = new BufferedStreamReaderBE<MemoryStream>(ms))
			{
				return ReadNJ(sr, variant, hasHeader, offset);
			}
		}

		public static NJSObject ReadNJ(BufferedStreamReaderBE<MemoryStream> sr, NinjaVariant variant, bool hasHeader, int offset)
		{
			if(hasHeader)
			{
				sr.ReadBE<long>();
				offset += 8;
			}
			int leCheck = sr.Peek<int>();
			int beCheck = sr.PeekBigEndianInt32();
			sr._BEReadActive = sr.Peek<uint>() > sr.PeekBigEndianUInt32();
			return new NJSObject(sr, variant, sr._BEReadActive, offset);
		}

		public static List<Texture2D> GetTextureSubset(List<Texture2D> textureList, NJTextureList texList, List<int> alphaTypes, out List<int> newAlphaTypes)
		{
			List<Texture2D> newTextures = new List<Texture2D>();
			newAlphaTypes = new List<int>();
			for(int i = 0; i < textureList.Count; i++)
			{
				if (texList.texNames.Contains(Path.GetFileNameWithoutExtension(textureList[i].ResourceName)))
				{
					newTextures.Add(textureList[i]);
					newAlphaTypes.Add(alphaTypes[i]);

				}
			}

			return newTextures;
		}

		/// <summary>
		/// Returns a Node3D containing a mesh instances with the model's arraymesh and a skeleton equivalent to the NJSObject nodes of the provided model.
		/// Note that providing a rootTfm input will NOT transform bones by it!
		/// </summary>
		public static Node3D NinjaToGDModel(string name, NJSObject nj, List<Texture2D> gvrTextures, List<int> gvrAlphaTypes, AquaNode aqn = null, 
			System.Numerics.Matrix4x4? baseTfm = null, Node3D root = null, System.Numerics.Matrix4x4? rootTfm = null, bool blockVertColors = false, float? forcedOpacity = null)
		{
			if(root == null)
			{
				root = new Node3D();
			}
			root.Name = name;
		
			Skeleton3D skeleton = new Skeleton3D();
			skeleton.RotationOrder = EulerOrder.Xyz;
			skeleton.Name = name + "_skel";
			int nodeId = 0;

			VTXL fullVertList = null;
			if (nj.HasWeights())
			{
				fullVertList = new VTXL();
				int nodeCounter = 0;
				NinjaModelConvert.GatherFullVertexListRecursive(nj, fullVertList, ref nodeCounter, System.Numerics.Matrix4x4.Identity, -1);
				fullVertList.ProcessToPSO2Weights();
			}

			if(baseTfm == null)
			{
				baseTfm = System.Numerics.Matrix4x4.Identity;
			}
			if (rootTfm == null)
			{
				rootTfm = System.Numerics.Matrix4x4.Identity;
			}
			IterateNJSObject(nj, fullVertList, ref nodeId, -1, root, skeleton, (System.Numerics.Matrix4x4)baseTfm, gvrTextures, gvrAlphaTypes, (System.Numerics.Matrix4x4)rootTfm, aqn, blockVertColors, forcedOpacity);

			return root;
		}

		private static void IterateNJSObject(NJSObject nj, VTXL fullVertList, ref int nodeId, int parentId, Node3D modelRoot, Skeleton3D skel,
			System.Numerics.Matrix4x4 parentMatrix, List<Texture2D> gvrTextures, List<int> gvrAlphaTypes, System.Numerics.Matrix4x4 rootTfm, AquaNode aqn = null, bool blockVertColors = false, float? forcedOpacity = null)
		{
			int currentNodeId = nodeId;
			string boneName = $"Node_{nodeId}";

			skel.AddBone(boneName);
			int gdIndex = skel.GetBoneCount() - 1;
			int parGdIndex = skel.FindBone($"Node_{parentId}");
			if (parGdIndex != -1) {
				skel.SetBoneParent(gdIndex, parGdIndex);
			}
			//Animation base position for ninja stuff should just be 0ed essentially
			skel.SetBoneRest(gdIndex, new Transform3D());
			skel.SetBonePosePosition(gdIndex, nj.pos.ToGVec3());
			skel.SetBonePoseRotation(gdIndex, Quaternion.FromEuler(nj.rot.ToGVec3()));

			//Calc node matrices for mesh transforms
			//We need this regardless of if there's a mesh or not since child meshes might exist
			System.Numerics.Matrix4x4 mat = System.Numerics.Matrix4x4.Identity;
			mat *= System.Numerics.Matrix4x4.CreateScale(nj.scale);
			var rotation = System.Numerics.Matrix4x4.CreateRotationX(nj.rot.X) *
				System.Numerics.Matrix4x4.CreateRotationY(nj.rot.Y) *
				System.Numerics.Matrix4x4.CreateRotationZ(nj.rot.Z);
			mat *= rotation;
			mat *= System.Numerics.Matrix4x4.CreateTranslation(nj.pos);
			mat = mat * parentMatrix;

			if(aqn != null)
			{
				NODE aqNode = new NODE();
				aqNode.boneShort1 = 0x1C0;
				aqNode.animatedFlag = 1;
				aqNode.parentId = parentId;
				aqNode.nextSibling = -1;
				aqNode.firstChild = -1;
				aqNode.unkNode = -1;
				aqNode.pos = nj.pos;
				aqNode.eulRot = new System.Numerics.Vector3((float)(nj.rot.X * 180 / Math.PI),
					(float)(nj.rot.Y * 180 / Math.PI), (float)(nj.rot.Z * 180 / Math.PI));
				aqNode.scale = nj.scale;
				System.Numerics.Matrix4x4.Invert(mat, out var invMat);
				aqNode.m1 = new System.Numerics.Vector4(invMat.M11, invMat.M12, invMat.M13, invMat.M14);
				aqNode.m2 = new System.Numerics.Vector4(invMat.M21, invMat.M22, invMat.M23, invMat.M24);
				aqNode.m3 = new System.Numerics.Vector4(invMat.M31, invMat.M32, invMat.M33, invMat.M34);
				aqNode.m4 = new System.Numerics.Vector4(invMat.M41, invMat.M42, invMat.M43, invMat.M44);
				aqNode.boneName.SetString(aqn.nodeList.Count.ToString());
				aqn.nodeList.Add(aqNode);
			}

			if (nj.mesh != null)
			{

				VTXL tempVtxl;
				//Get vertex data and face data
				if(fullVertList != null)
				{
					tempVtxl = fullVertList;
				} else
				{
					tempVtxl = new VTXL();
					nj.mesh.GetVertexData(currentNodeId, tempVtxl, mat);
					tempVtxl.ProcessToPSO2Weights();
				}

				//It'd probably be more efficient to pull the face data out directly here, but for now we'll use the Aqua focused method.
				var testAqo = new AquaObject();
				nj.GetFaceData(nodeId, tempVtxl, testAqo);

				//Assign vertex and face data to GD ArrayMesh
				foreach(var tempTri in testAqo.tempTris)
				{
					MeshInstance3D meshInst = new MeshInstance3D();
					ArrayMesh mesh = new ArrayMesh();
					var arrays = new Godot.Collections.Array();
					arrays.Resize((int)Mesh.ArrayType.Max);

					List<Vector3> vertPosList = new List<Vector3>();
					List<Vector3> vertNrmList = new List<Vector3>();
					List<Vector2> vertUvList = new List<Vector2>();
					List<Color> vertClrList = new List<Color>();
					for(int i = 0; i < tempTri.faceVerts.Count; i++)
					{
						var faceVtxl = tempTri.faceVerts[i];
						for(int j = faceVtxl.vertPositions.Count - 1; j >= 0; j--)
						{
							vertPosList.Add(faceVtxl.vertPositions[j].ToGVec3());
						}
						for (int j = faceVtxl.vertNormals.Count - 1; j >= 0; j--)
						{
							vertNrmList.Add(faceVtxl.vertNormals[j].ToGVec3());
						}
						for (int j = faceVtxl.uv1List.Count - 1; j >= 0; j--)
						{
							vertUvList.Add(faceVtxl.uv1List[j].ToGVec2());
						}
						if(!blockVertColors)
                        {
                            for (int j = faceVtxl.vertColors.Count - 1; j >= 0; j--)
                            {
                                var vertColor = faceVtxl.vertColors[j];
                                vertClrList.Add(new Color(Mathf.Pow(vertColor[2] / 255f, 2.2f), Mathf.Pow(vertColor[1] / 255f, 2.2f), Mathf.Pow(vertColor[0] / 255f, 2.2f), vertColor[3] / 255f));
                            }
                        }
					}
					if(vertPosList.Count > 0)
					{
						arrays[(int)Mesh.ArrayType.Vertex] = vertPosList.ToArray();
					}
					if (vertNrmList.Count > 0)
					{
						arrays[(int)Mesh.ArrayType.Normal] = vertNrmList.ToArray();
					}
					if (vertUvList.Count > 0)
					{
						arrays[(int)Mesh.ArrayType.TexUV] = vertUvList.ToArray();
					}
					if (vertClrList.Count > 0)
					{
						arrays[(int)Mesh.ArrayType.Color] = vertClrList.ToArray();
					}

					//Set up material
					StandardMaterial3D gdMaterial = new StandardMaterial3D();
					var matId = tempTri.matIdList.Count > 0 ? tempTri.matIdList[0] : 0;
					gdMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel;
					gdMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
					
					if(testAqo.tempMats.Count > 0)
					{
						var texId = Int32.Parse(testAqo.tempMats[matId].texNames[0]);
						if(texId >= 0)
						{
							if(texId < gvrTextures.Count)
							{
								gdMaterial.AlbedoTexture = gvrTextures[texId];
							}
							if(gvrAlphaTypes.Count > texId)
							{
								switch (gvrAlphaTypes[texId])
								{
									case 0:
										gdMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
										break;
									case 1:
										gdMaterial.Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor;
										break;
									case 2:
										gdMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
										break;
								}
							}
						}
                    }

					//In case we want to force a transparent model
                    if (forcedOpacity != null)
                    {
                        var albedo = gdMaterial.AlbedoColor;
                        albedo.A = forcedOpacity.Value;
                        gdMaterial.AlbedoColor = albedo;
                        gdMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                    }
                    else
                    {
                        gdMaterial.VertexColorUseAsAlbedo = true;
                    }
                    mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null);
					mesh.SurfaceSetMaterial(0, gdMaterial);

					//Final assignment steps
					meshInst.Mesh = mesh;
					System.Numerics.Matrix4x4.Decompose(rootTfm, out var scale, out var rot, out var pos);
					meshInst.Scale = scale.ToGVec3();
					meshInst.Quaternion = rot.ToGQuat();
					meshInst.Position = pos.ToGVec3();
					modelRoot.AddChild(meshInst);
				}

			}

			if(nj.childObject != null)
			{
				nodeId++;
				IterateNJSObject(nj.childObject, fullVertList, ref nodeId, currentNodeId, modelRoot, skel, mat, gvrTextures, gvrAlphaTypes, rootTfm, aqn, blockVertColors, forcedOpacity);
			}

			if(nj.siblingObject != null)
			{
				nodeId++;
				IterateNJSObject(nj.siblingObject, fullVertList, ref nodeId, parentId, modelRoot, skel, parentMatrix, gvrTextures, gvrAlphaTypes, rootTfm, aqn, blockVertColors, forcedOpacity);
			}
		}

		public static Node3D MC2ToGDModel(string name, MC2 mc2)
		{
			Node3D modelRoot = new Node3D();

			MeshInstance3D meshInst = new MeshInstance3D();
			ArrayMesh mesh = new ArrayMesh();
			var arrays = new Godot.Collections.Array();
			arrays.Resize((int)Mesh.ArrayType.Max);

			List<Vector3> vertPosList = new List<Vector3>();
			List<Vector3> vertNrmList = new List<Vector3>();
			List<Vector2> vertUvList = new List<Vector2>();
			List<Color> vertClrList = new List<Color>();
			foreach (var poly in mc2.faceData)
			{
				vertPosList.Add(poly.vert2Value.ToGVec3());
				vertPosList.Add(poly.vert1Value.ToGVec3());
				vertPosList.Add(poly.vert0Value.ToGVec3());
				vertNrmList.Add(poly.faceNormal.ToGVec3());
				vertNrmList.Add(poly.faceNormal.ToGVec3());
				vertNrmList.Add(poly.faceNormal.ToGVec3());
				vertUvList.Add(new Vector2(0, 1));
				vertUvList.Add(new Vector2(0, 1));
				vertUvList.Add(new Vector2(0, 1));
				Color color = new Color(0.45f, 0.45f, 0.45f);

				if((poly.flagSet0 & MC2.FlagSet0.Lava) > 0)
				{
					color = new Color(1, 0.184f, 0);
				} 
				else if((poly.flagSet1 & MC2.FlagSet1.Quicksand) > 0)
				{
					color = new Color(1, 1, 0);
				}
				else if ((poly.flagSet1 & MC2.FlagSet1.Drown) > 0)
				{
					color = new Color(0, 0.415f, 1);
				}
				else if ((poly.flagSet0 & MC2.FlagSet0.Death) > 0)
				{
					color = new Color(0.1f, 0.1f, 0.1f);
				}
				else if ((poly.flagSet1 & MC2.FlagSet1.Snow) > 0)
				{
					color = new Color(1f, 1f, 1f);
				}
				else if ((poly.flagSet0 & MC2.FlagSet0.Slide) > 0)
				{
					color = new Color(1f, 0.5f, 0f);
				}
				else if ((poly.flagSet1 & MC2.FlagSet1.DefaultGround) > 0)
				{
					color = new Color(0.75f, 0.75f, 0.75f);
				}

				vertClrList.Add(color);
				vertClrList.Add(color);
				vertClrList.Add(color);
			}
			if (vertPosList.Count > 0)
			{
				arrays[(int)Mesh.ArrayType.Vertex] = vertPosList.ToArray();
			}
			if (vertNrmList.Count > 0)
			{
				arrays[(int)Mesh.ArrayType.Normal] = vertNrmList.ToArray();
			}
			if (vertUvList.Count > 0)
			{
				arrays[(int)Mesh.ArrayType.TexUV] = vertUvList.ToArray();
			}
			if (vertClrList.Count > 0)
			{
				arrays[(int)Mesh.ArrayType.Color] = vertClrList.ToArray();
			}                    
			
			//Set up material
			StandardMaterial3D gdMaterial = new StandardMaterial3D();
			gdMaterial.VertexColorUseAsAlbedo = true;
			gdMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel;
			gdMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
			gdMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;

			mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null);
			mesh.SurfaceSetMaterial(0, gdMaterial);
			meshInst.Mesh = mesh;
			modelRoot.AddChild(meshInst);

			//Masks and layers are checked at the bit level. Ex. Setting this 3 would have it conflict with 1 (objects) or 2 (transform gizmo)
			CreateObjectCollision(modelRoot, 4, 4);

			return modelRoot;
		}

		public static void CreateObjectCollision(Node3D modelNode, uint layerId = 1, uint maskId = 1)
		{
			List<MeshInstance3D> meshInstances = new List<MeshInstance3D>();
			GetObjectMeshInstances(modelNode, meshInstances);

			foreach (var meshInst in meshInstances)
			{
				meshInst.CreateTrimeshCollision();
				var staticBody = (StaticBody3D)meshInst.GetChild(0);
				var collChild = ((CollisionShape3D)staticBody.GetChild(0));
				collChild.Disabled = false;
				staticBody.CollisionLayer = layerId;
				staticBody.CollisionMask = maskId;
			}
		}

		public static void GetObjectMeshInstances(Node modelNode, List<MeshInstance3D> meshInstances)
		{
			var children = modelNode.GetChildren();
			foreach (var nodeChild in children)
			{
				if (nodeChild is MeshInstance3D meshChild)
				{
					meshInstances.Add(meshChild);
				}
				GetObjectMeshInstances(nodeChild, meshInstances);
			}
		}

		public static int GetGvrAlphaType(GvrDataFormat format)
		{
			switch (format)
			{
				case GvrDataFormat.Intensity4:
				case GvrDataFormat.Intensity8:
				case GvrDataFormat.Rgb565:
				case GvrDataFormat.Index4:
				case GvrDataFormat.Index8:
				case GvrDataFormat.Unknown:
					return 0;
				case GvrDataFormat.Dxt1:
				case GvrDataFormat.Rgb5a3:
					return 1;
				case GvrDataFormat.IntensityA4:
				case GvrDataFormat.IntensityA8:
				case GvrDataFormat.Argb8888:
					return 2;
			}

			return 0;
		}


		public static Node3D LNDToGDModel(string name, LND lnd)
		{
			//Load in textures
			var gvm = lnd.gvm;
			List<Texture2D> gvmTextures;
			List<int> gvrAlphaTypes;
			LoadGVM(name, gvm, out gvmTextures, out gvrAlphaTypes);

			//Load in models
			Node3D root = new Node3D();
			root.Name = name;

			if (lnd.isArcLND)
			{
				//Add static models
				//While these all have nodes with transform data, that data is always 0ed. Bounding data does exist, but isn't useful to us here
				foreach (var modelSet in lnd.arcLndModels)
				{
					string modelName = modelSet.name;
					Node3D node = new Node3D();
					node.Name = modelName;
					AddARCLNDModelData(lnd, modelSet.model, node, gvmTextures, gvrAlphaTypes, false);
					if (node.Name.ToString().ToLower() == "sphere000")
					{
						OverEasyGlobals.daySkybox = node;
					}
					else if (node.Name.ToString().ToLower() == "sphere001")
					{
						OverEasyGlobals.nightSkybox = node;
					}

					root.AddChild(node);
				}

				//Add animated models
				//These could technically contain multiple nodes per model, but they don't. Howver the nodes that are there do transform the model, unlike the static models
				foreach (var modelSet in lnd.arcLndAnimatedMeshDataList)
				{
					string modelName = $"{modelSet.GetHashCode()}_{modelSet.MPLAnimId}";
					Node3D node = new Node3D();
					node.Name = modelName;
					AddARCLNDModelData(lnd, modelSet.model, node, gvmTextures, gvrAlphaTypes, true);
					var bnd = modelSet.model.arcBoundingList[0];
					var pos = bnd.Position;
					//We want the radians, so we leave it like this
					var rot = bnd.GetRotation();
					var scl = bnd.Scale;
					node.Scale = new Vector3(scl.X, scl.Y, scl.Z);
					node.Rotation = new Vector3(rot.X, rot.Y, rot.Z);
					node.Position = new Vector3(pos.X, pos.Y, pos.Z);

					//Convert MPL animation to a Godot animation

					//Add animation and AnimationPlayer, set animation

					root.AddChild(node);
				}
			}
			else
			{
				var lndh = LNDConvert.LNDHToAqua(lnd, false)[0];
				for(int i = 0; i < lndh.aqp.tempTris.Count; i++)
				{
					var tempTris = lndh.aqp.tempTris[i];

					MeshInstance3D meshInst = new MeshInstance3D();
					ArrayMesh mesh = new ArrayMesh();
					var arrays = new Godot.Collections.Array();
					arrays.Resize((int)Mesh.ArrayType.Max);

					List<Vector3> vertPosList = new List<Vector3>();
					List<Vector3> vertNrmList = new List<Vector3>();
					List<Vector2> vertUvList = new List<Vector2>();
					List<Color> vertClrList = new List<Color>();
					foreach (var polyVerts in tempTris.faceVerts)
					{
						vertPosList.Add(polyVerts.vertPositions[2].ToGVec3());
						vertPosList.Add(polyVerts.vertPositions[1].ToGVec3());
						vertPosList.Add(polyVerts.vertPositions[0].ToGVec3());
						if(polyVerts.vertNormals.Count > 0)
						{
							vertNrmList.Add(polyVerts.vertNormals[2].ToGVec3());
							vertNrmList.Add(polyVerts.vertNormals[1].ToGVec3());
							vertNrmList.Add(polyVerts.vertNormals[0].ToGVec3());
						}
						if(polyVerts.uv1List.Count > 0)
						{
							vertUvList.Add(polyVerts.uv1List[2].ToGVec2());
							vertUvList.Add(polyVerts.uv1List[1].ToGVec2());
							vertUvList.Add(polyVerts.uv1List[0].ToGVec2());
						}
						if(polyVerts.vertColors.Count > 0)
						{
							for(int v = 2; v >= 0; v--)
							{
								var color = polyVerts.vertColors[v];
								vertClrList.Add(Color.Color8(color[2], color[1], color[0], color[3]));
							}
						}
					}
					if (vertPosList.Count > 0)
					{
						arrays[(int)Mesh.ArrayType.Vertex] = vertPosList.ToArray();
					}
					if (vertNrmList.Count > 0)
					{
						arrays[(int)Mesh.ArrayType.Normal] = vertNrmList.ToArray();
					}
					if (vertUvList.Count > 0)
					{
						arrays[(int)Mesh.ArrayType.TexUV] = vertUvList.ToArray();
					}
					if (vertClrList.Count > 0)
					{
						arrays[(int)Mesh.ArrayType.Color] = vertClrList.ToArray();
					}

					//Set up material
					var tempMat = lndh.aqp.tempMats[tempTris.matIdList[0]];
					StandardMaterial3D gdMaterial = new StandardMaterial3D();
					gdMaterial.VertexColorUseAsAlbedo = true;
					gdMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel;
					gdMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
					gdMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
					if(tempMat.texNames?.Count > 0 && gvm?.Entries?.Count > 0)
					{
						gdMaterial.AlbedoTexture = gvmTextures[lnd.njtexList.texNames.IndexOf(Path.GetFileNameWithoutExtension(tempMat.texNames[0]))];
					}

					mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null);
					mesh.SurfaceSetMaterial(0, gdMaterial);
					meshInst.Mesh = mesh;
					root.AddChild(meshInst);
				}
			}

			return root;
		}

		public static void LoadGVM(string name, PuyoFile gvm, out List<Texture2D> gvmTextures, out List<int> gvrAlphaTypes, List<int> diffuseAsAlphalist = null)
        {
            gvmTextures = new List<Texture2D>();
            gvrAlphaTypes = new List<int>();
            if (gvm == null)
            {
                return;
            }
            List<string> texNames = new List<string>();
			List<GvrTexture> gvrTextures = new List<GvrTexture>();
			for(int i = 0; i < gvm.Entries.Count; i++)
			{
				texNames.Add(gvm.Entries[i].Name);
				gvrTextures.Add(new GvrTexture(gvm.Entries[i].Data));
			}
            LoadGVRTextures(name, gvrTextures, texNames, out gvmTextures, out gvrAlphaTypes, diffuseAsAlphalist);
        }

		public static void LoadGVRTextures(string name, List<GvrTexture> gvrTextures, List<string> gvrNames, out List<Texture2D> gvmTextures, out List<int> gvrAlphaTypes, List<int> diffuseAsAlphalist = null)
        {
            if (diffuseAsAlphalist == null)
            {
                diffuseAsAlphalist = new List<int>();
            }
            gvmTextures = new List<Texture2D>();
            gvrAlphaTypes = new List<int>();
            for (int i = 0; i < gvrTextures.Count; i++)
            {
				bool texHasDiffuseAlpha = diffuseAsAlphalist.Contains(i);
                var tex = gvrTextures[i];
                var texArray = tex.ToArray();
                for (int t = 0; t < texArray.Length - 4; t += 4)
                {
                    var temp = texArray[t + 2];
                    texArray[t + 2] = texArray[t];
                    texArray[t] = temp;

					switch(texHasDiffuseAlpha)
					{
						case true:
                            texArray[t + 3] = (byte)Math.Min((texArray[t] + texArray[t + 1] + texArray[t + 2]) / 3, 255);
                            break;
						case false:
							break;
					}
                }
                if (diffuseAsAlphalist.Contains(i))
                {
                    gvrAlphaTypes.Add(2);
                }
                else
                {
					if(tex is GvrTexture gvrTex)
                    {
                        gvrAlphaTypes.Add(GetGvrAlphaType(gvrTex.DataFormat));
                    } else
					{
						throw new NotImplementedException();
					}
                }

                var img = Godot.Image.CreateFromData(tex.TextureWidth, tex.TextureHeight, false, Image.Format.Rgba8, texArray);
                img.GenerateMipmaps();
                var imgTex = ImageTexture.CreateFromImage(img);
                imgTex.ResourceName = gvrNames[i];
                gvmTextures.Add(imgTex);
            }
            if (!OverEasyGlobals.orderedTextureArchivePools.ContainsKey(name))
            {
                OverEasyGlobals.orderedTextureArchivePools.Add(name, gvmTextures);
            }
        }

		private static void AddARCLNDModelData(LND lnd, ARCLNDModel mdl, Node3D modelParent, List<Texture2D> gvrTextures, List<int> gvrAlphaTypes, bool forceShaded)
		{
			for (int i = 0; i < mdl.arcMeshDataList.Count; i++)
			{
				for (int m = 0; m < mdl.arcMeshDataList[i].Count; m++)
				{
					MeshInstance3D meshInst = new MeshInstance3D();
					ArrayMesh meshData = new ArrayMesh();
					var meshInfo = mdl.arcMeshDataList[i][m];
					var faceData = mdl.arcFaceDataList[meshInfo.faceDataId];
					var mat = mdl.arcMatEntryList[meshInfo.matEntryId];
					var texId = mat.entry.TextureId;

					var arrays = new Godot.Collections.Array();
					arrays.Resize((int)Mesh.ArrayType.Max);
					ProcessLNDMesh(mdl, faceData, arrays);
					meshData.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null, (Mesh.ArrayFormat)((int)Mesh.ArrayCustomFormat.RgbaFloat << (int)Mesh.ArrayFormat.FormatCustom0Shift));

					//Set up and assign material to surface
					StandardMaterial3D gdMaterial = new StandardMaterial3D();
					gdMaterial.VertexColorUseAsAlbedo = true;

					gdMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel;
					if (!((mat.entry.RenderFlags & (ARCLNDRenderFlags.RFUnknown0x2)) == 0) && forceShaded == false)
					{
						gdMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
					}
					gdMaterial.CullMode = BaseMaterial3D.CullModeEnum.Front;

					if ((mat.entry.RenderFlags & (ARCLNDRenderFlags.TwoSided)) > 0)
					{
						gdMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
					}
					if(texId != -1)
					{
						gdMaterial.AlbedoTexture = gvrTextures[texId];
						switch (gvrAlphaTypes[texId])
						{
							case 0:
								gdMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
								break;
							case 1:
								gdMaterial.Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor;
								break;
							case 2:
								gdMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
								break;
						}
					}

					meshInst.Mesh = meshData;
					meshData.SurfaceSetMaterial(0, gdMaterial);
					modelParent.AddChild(meshInst);
				}
			}
		}

		private class LNDVertContainer
		{
			public List<Vector3> posList = new List<Vector3>();
			public List<Vector3> nrmList = new List<Vector3>();
			public List<Color> colors = new List<Color>();
			public List<float> color2s = new List<float>();
			public List<Vector2> uvList = new List<Vector2>();
		}

		private static void ProcessLNDMesh(ARCLNDModel model, ARCLNDFaceDataHead faceData, Godot.Collections.Array arrays)
		{
			LNDVertContainer billyData = new LNDVertContainer();
			if (faceData.triIndicesList0.Count > 0)
			{
				AddFromARCPolyData(model, faceData.triIndicesList0, faceData.triIndicesListStarts0, faceData.flags, 1, billyData);
			}
			if (faceData.triIndicesList1.Count > 0)
			{
				AddFromARCPolyData(model, faceData.triIndicesList1, faceData.triIndicesListStarts1, faceData.flags, 0, billyData);
			}

			if (billyData.posList.Count > 0)
			{
				arrays[(int)Mesh.ArrayType.Vertex] = billyData.posList.ToArray();
			}
			if (billyData.nrmList.Count > 0)
			{
				arrays[(int)Mesh.ArrayType.Normal] = billyData.nrmList.ToArray();
			}
			if (billyData.colors.Count > 0)
			{
				arrays[(int)Mesh.ArrayType.Color] = billyData.colors.ToArray();
				if (billyData.color2s.Count > 0)
				{
					arrays[(int)Mesh.ArrayType.Custom0] = billyData.color2s.ToArray();
				}
			}
			if (billyData.uvList.Count > 0)
			{
				arrays[(int)Mesh.ArrayType.TexUV] = billyData.uvList.ToArray();
			}
			billyData.posList.Clear();
			billyData.nrmList.Clear();
			billyData.colors.Clear();
			billyData.color2s.Clear();
			billyData.uvList.Clear();
		}

		private static void AddFromARCPolyData(ARCLNDModel mdl, List<List<List<int>>> triIndicesList, List<List<List<int>>> triIndicesListStarts,
			ArcLndVertType flags, int listFlip, LNDVertContainer billyData)
		{
			for (int s = 0; s < triIndicesList.Count; s++)
			{
				var strip = triIndicesList[s];
				var stripStart = triIndicesListStarts[s];
				if (stripStart[0][0] == 0x98)
				{
					for (int i = 0; i < strip.Count - 2; i++)
					{
						if (((i + listFlip) & 1) > 0)
						{
							AddARCVert(mdl, strip[i + 2], flags, billyData);
							AddARCVert(mdl, strip[i + 1], flags, billyData);
							AddARCVert(mdl, strip[i], flags, billyData);
						}
						else
						{
							AddARCVert(mdl, strip[i], flags, billyData);
							AddARCVert(mdl, strip[i + 1], flags, billyData);
							AddARCVert(mdl, strip[i + 2], flags, billyData);
						}
					}
				}
				else if (stripStart[0][0] == 0x90)
				{
					for (int i = 0; i < strip.Count - 2; i += 3)
					{
						AddARCVert(mdl, strip[i], flags, billyData);
						AddARCVert(mdl, strip[i + 1], flags, billyData);
						AddARCVert(mdl, strip[i + 2], flags, billyData);
					}
				}
			}
		}

		private static void AddARCVert(ARCLNDModel mdl, List<int> faceIds, ArcLndVertType flags, LNDVertContainer billyData)
		{
			int i = 0;
			if ((flags & ArcLndVertType.Position) > 0)
			{
				var pos = mdl.arcVertDataSetList[0].PositionData[faceIds[i]];
				billyData.posList.Add(new Vector3(pos.X, pos.Y, pos.Z));
				i++;
			}
			if ((flags & ArcLndVertType.Normal) > 0)
			{
				var nrm = System.Numerics.Vector3.Normalize(mdl.arcVertDataSetList[0].NormalData[faceIds[i]]);
				billyData.nrmList.Add(new Vector3(-nrm.X, -nrm.Y, -nrm.Z));
				i++;
			}
			else
			{
				var nrm = System.Numerics.Vector3.Normalize(mdl.arcVertDataSetList[0].faceNormalDict[faceIds[0]]);
				billyData.nrmList.Add(new Vector3(-nrm.X, -nrm.Y, -nrm.Z));
			}
			if ((flags & ArcLndVertType.VertColor) > 0)
			{
				var billyColor = mdl.arcVertDataSetList[0].VertColorData[faceIds[i]];
				billyData.colors.Add(new Color(Mathf.Pow(billyColor[0] / 255f, 2.2f), Mathf.Pow(billyColor[1] / 255f, 2.2f), Mathf.Pow(billyColor[2] / 255f, 2.2f), billyColor[3] / 255f));

				if (mdl.arcAltVertColorList.Count > 0 && mdl.arcAltVertColorList[0]?.VertColorData.Count > 0)
				{
					billyColor = mdl.arcAltVertColorList[0]?.VertColorData[faceIds[i]];
					billyData.color2s.Add(Mathf.Pow(billyColor[0] / 255f, 2.2f));
					billyData.color2s.Add(Mathf.Pow(billyColor[1] / 255f, 2.2f));
					billyData.color2s.Add(Mathf.Pow(billyColor[2] / 255f, 2.2f));
					billyData.color2s.Add(Mathf.Pow(billyColor[3] / 255f, 2.2f));
				}
				i++;
			}
			if ((flags & ArcLndVertType.UV1) > 0)
			{
				var billyUv = mdl.arcVertDataSetList[0].UV1Data[faceIds[i]];
				billyData.uvList.Add(new Vector2((float)(billyUv[0] / 256.0), (float)(billyUv[1] / 256.0)));
				i++;
			}
		}
	}
}
