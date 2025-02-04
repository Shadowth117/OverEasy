using AquaModelLibrary.Data.AM2.BorderBreakPS4;
using AquaModelLibrary.Data.BillyHatcher;
using AquaModelLibrary.Data.BillyHatcher.LNDH;
using AquaModelLibrary.Data.Ninja;
using AquaModelLibrary.Data.Ninja.Model;
using AquaModelLibrary.Data.Ninja.Motion;
using AquaModelLibrary.Data.PSO2.Aqua;
using AquaModelLibrary.Data.PSO2.Aqua.AquaObjectData;
using AquaModelLibrary.Helpers.Readers;
using ArchiveLib;
using Godot;
using OverEasy.Util;
using System;
using System.Collections.Generic;
using System.IO;
using VrSharp.Gvr;
using Material = Godot.Material;

namespace OverEasy.Billy
{
	public class ModelConversion
	{
		public static void BillyModeNightToggle(Node parentNode)
		{
			if (parentNode is MeshInstance3D)
			{
				BillyModeNightToggleMesh((ArrayMesh)((MeshInstance3D)parentNode).Mesh);
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

		public static Node3D CreateDefaultObjectModel(string name, Color color)
		{
			Node3D root = new Node3D();
			MeshInstance3D meshInst = new MeshInstance3D();
			var box = new BoxMesh();
			box.Size = new Vector3(20, 20, 20);
			var mat = new StandardMaterial3D();
			mat.AlbedoColor = color;
			mat.BlendMode = BaseMaterial3D.BlendModeEnum.Mix;
			mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerVertex;
			box.Material = mat;
			meshInst.Mesh = box;
			root.AddChild(meshInst);

			return root;
		}

		public static Node3D GDModelClone(Node3D modelNode)
		{
			Node3D root = new Node3D();
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

		/// <summary>
		/// Returns a Node3D containing a mesh instances with the model's arraymesh and a skeleton equivalent to the NJSObject nodes of the provided model.
		/// </summary>
		public static Node3D NinjaToGDModel(string name, NJSObject nj, NJTextureList texList, List<Texture2D> gvrTextures, List<int> gvrAlphaTypes)
		{
			Node3D root = new Node3D();
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

			IterateNJSObject(nj, fullVertList, ref nodeId, -1, root, skeleton, System.Numerics.Matrix4x4.Identity, texList, gvrTextures, gvrAlphaTypes);

			return root;
		}

		private static void IterateNJSObject(NJSObject nj, VTXL fullVertList, ref int nodeId, int parentId, Node3D modelRoot, Skeleton3D skel,
			System.Numerics.Matrix4x4 parentMatrix, NJTextureList texList, List<Texture2D> gvrTextures, List<int> gvrAlphaTypes)
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
						foreach(var vertPos in faceVtxl.vertPositions)
						{
							vertPosList.Add(vertPos.ToGVec3());
						}
						foreach (var vertNrm in faceVtxl.vertNormals)
						{
							vertNrmList.Add(vertNrm.ToGVec3());
						}
						foreach (var vertUv in faceVtxl.uv1List)
						{
							vertUvList.Add(vertUv.ToGVec2());
						}
						foreach (var vertColor in faceVtxl.vertColors)
						{
							vertClrList.Add(new Color(vertColor[2] / 255f, vertColor[1] / 255f, vertColor[0] / 255f, vertColor[3] / 255f));
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
					gdMaterial.VertexColorUseAsAlbedo = true;
					gdMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel;
					gdMaterial.CullMode = BaseMaterial3D.CullModeEnum.Front;
					var matId = tempTri.matIdList.Count > 0 ? tempTri.matIdList[0] : 0;
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
					mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null);
					mesh.SurfaceSetMaterial(0, gdMaterial);
					//Final assignment steps
					meshInst.Mesh = mesh;
					modelRoot.AddChild(meshInst);
				}

			}

			if(nj.childObject != null)
			{
				nodeId++;
				IterateNJSObject(nj.childObject, fullVertList, ref nodeId, currentNodeId, modelRoot, skel, mat, texList, gvrTextures, gvrAlphaTypes);
			}

			if(nj.siblingObject != null)
			{
				nodeId++;
				IterateNJSObject(nj.siblingObject, fullVertList, ref nodeId, parentId, modelRoot, skel, parentMatrix, texList, gvrTextures, gvrAlphaTypes);
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
					AddARCLNDModelData(lnd, modelSet.model, node, gvmTextures, gvrAlphaTypes);
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
					AddARCLNDModelData(lnd, modelSet.model, node, gvmTextures, gvrAlphaTypes);
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

			}

			return root;
		}

		public static void LoadGVM(string name, PuyoFile gvm, out List<Texture2D> gvmTextures, out List<int> gvrAlphaTypes)
		{
			gvmTextures = new List<Texture2D>();
			gvrAlphaTypes = new List<int>();
			for (int i = 0; i < gvm.Entries.Count; i++)
			{
				var entry = (GVMEntry)gvm.Entries[i];
				var tex = new GvrTexture(entry.Data);
				var texArray = tex.ToArray();
				for (int t = 0; t < texArray.Length - 4; t += 4)
				{
					var temp = texArray[t + 2];
					texArray[t + 2] = texArray[t];
					texArray[t] = temp;
				}
				gvrAlphaTypes.Add(GetGvrAlphaType(tex.DataFormat));

				var img = Godot.Image.CreateFromData(tex.TextureWidth, tex.TextureHeight, false, Image.Format.Rgba8, texArray);
				img.GenerateMipmaps();
				var imgTex = ImageTexture.CreateFromImage(img);
				imgTex.ResourceName = entry.Name; //We don't need the GBIX (global index) here, probably.
				gvmTextures.Add(imgTex);
			}
			if(!OverEasyGlobals.orderedTextureArchivePools.ContainsKey(name))
			{
				OverEasyGlobals.orderedTextureArchivePools.Add(name, gvmTextures);
			}
		}

		private static void AddARCLNDModelData(LND lnd, ARCLNDModel mdl, Node3D modelParent, List<Texture2D> gvrTextures, List<int> gvrAlphaTypes)
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

					/*
					if ((mat.entry.RenderFlags & (ARCLNDRenderFlags.EnableLighting)) == 0)
					{
						gdMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
					}*/
					gdMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
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
				AddFromARCPolyData(model, faceData.triIndicesList0, faceData.triIndicesListStarts0, faceData.flags, 0, billyData);
			}
			if (faceData.triIndicesList1.Count > 0)
			{
				AddFromARCPolyData(model, faceData.triIndicesList1, faceData.triIndicesListStarts1, faceData.flags, 1, billyData);
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
						int x, y, z;
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
				billyData.colors.Add(new Color(billyColor[0] / 255f, billyColor[1] / 255f, billyColor[2] / 255f, billyColor[3] / 255f));

				if (mdl.arcAltVertColorList.Count > 0 && mdl.arcAltVertColorList[0]?.VertColorData.Count > 0)
				{
					billyColor = mdl.arcAltVertColorList[0]?.VertColorData[faceIds[i]];
					billyData.color2s.Add(billyColor[0] / 255f);
					billyData.color2s.Add(billyColor[1] / 255f);
					billyData.color2s.Add(billyColor[2] / 255f);
					billyData.color2s.Add(billyColor[3] / 255f);
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
