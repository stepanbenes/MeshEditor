using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;
using MeshEditor.Construction;
using Wintellect.PowerCollections;
using OpenTK;
using System.Diagnostics;
using MeshEditor.Utilities;

namespace MeshEditor.Cuts
{
	/// <summary>
	/// staticka trida obsahujici ruzne funkce pro provedeni rezu site
	/// </summary>
	public static class Cutter
	{
		/// <summary>
		/// delegat zastupujici funkci, ktera vezme prvek a vrati pravdivostni hodnotu, zda ho uriznout ci nikoli
		/// </summary>
		private delegate bool ElementTest(Element e);

		#region Public members

		public static void CutMeshByExpression(Mesh mesh, CutInfo cutInfo)
		{
			if (mesh == null || cutInfo.CutTestMethod == null)
				return;

			doCut(mesh, cutInfo.CutTestMethod, cutInfo.HitDecision,  true);
		}

		public static void CutMeshByPlanes(Mesh mesh, List<CutPlane> cutPlanes, CutInfo cutInfo)
		{
			if (mesh == null || cutPlanes == null || cutPlanes.Count == 0)
				return;

			CutTest test = createCutTestForCutPlanes(cutPlanes, cutInfo);
			doCut(mesh, test, cutInfo.HitDecision, false);
		}

		public static void SelectItemsByExpression(Mesh mesh, CutInfo cutInfo, bool transformCoordinates)
		{
			if (mesh == null || cutInfo.CutTestMethod == null)
				return;

			Set<Element> elementHits;
			Set<Node> nodeHits;

			if (cutInfo.HitDecision == CutInfo.ItemHitDecision.AllNodes)
				getHitsAllNodesInArea(mesh, cutInfo.CutTestMethod, transformCoordinates, out elementHits, (cutInfo.Action != CutInfo.ActionType.SelectElements), out nodeHits);
			else if (cutInfo.HitDecision == CutInfo.ItemHitDecision.SomeNodes)
				getHitsSomeNodesInArea(mesh, cutInfo.CutTestMethod, transformCoordinates, out elementHits, (cutInfo.Action != CutInfo.ActionType.SelectElements), out nodeHits);
			else
				throw new NotSupportedException(cutInfo.HitDecision.ToString() + " option is not supported");

			mesh.SelectedItems = findSelectedItemsInMesh(mesh, cutInfo, elementHits, nodeHits);
		}

		public static void SelectItemsByCutPlanes(Mesh mesh, List<CutPlane> cutPlanes, CutInfo cutInfo)
		{
			//cutInfo.CutTestMethod = createCutTestForCutPlanes(cutPlanes, cutInfo);
			//SelectItemsByExpression(mesh, cutInfo, false);
			Set<ISelectable> newSelection = new Set<ISelectable>();
			mesh.SelectedItems = newSelection;
			bool allNodesFlag = (cutInfo.HitDecision == CutInfo.ItemHitDecision.AllNodes);

			if (cutInfo.Action == CutInfo.ActionType.SelectElements) // elements
			{
				cutInfo.CutTestMethod = createCutTestForCutPlanes(cutPlanes, cutInfo);
				SelectItemsByExpression(mesh, cutInfo, false);
				return;
			}
			// ------------------------------------------------------------------------

			cutInfo.HitDecision = CutInfo.ItemHitDecision.SomeNodes;
			Set<Node> nodeHitsForSomeNodesOption = getNodeHits(getAllNodes(mesh), createCutTestForCutPlanes(cutPlanes, cutInfo));
			cutInfo.HitDecision = CutInfo.ItemHitDecision.AllNodes;
			Set<Node> nodeHitsForAllNodesOption = getNodeHits(getAllNodes(mesh), createCutTestForCutPlanes(cutPlanes, cutInfo));
			// vratit puvodni volbu
			cutInfo.HitDecision = allNodesFlag ? CutInfo.ItemHitDecision.AllNodes : CutInfo.ItemHitDecision.SomeNodes;

			// ------------------------------------------------------------------------
			if (cutInfo.Action == CutInfo.ActionType.SelectNodes) // nodes
			{
				foreach (Node n in (allNodesFlag) ? nodeHitsForSomeNodesOption : nodeHitsForAllNodesOption) // oznacit
					newSelection.Add(n);
				return;
			}

			Set<Node> borderNodes = nodeHitsForAllNodesOption.Difference(nodeHitsForSomeNodesOption);
			//Set<Node> nodeHits = (allNodesFlag) ? nodeHitsForAllNodesOption : nodeHitsForSomeNodesOption;
			if (cutInfo.Action == CutInfo.ActionType.SelectFaces) // faces
			{
				if (allNodesFlag)
				{
					foreach (Element2D face in mesh.Faces)
						if (MeshConstructor.allNodesInSet(face.IterateThroughAllNodes(), nodeHitsForAllNodesOption) && !MeshConstructor.allNodesInSet(face.IterateThroughAllNodes(), borderNodes))
							newSelection.Add(face);
				}
				else
				{
					foreach (Element2D face in mesh.Faces)
						if (MeshConstructor.someNodesInSet(face.IterateThroughAllNodes(), nodeHitsForSomeNodesOption) || MeshConstructor.allNodesInSet(face.IterateThroughAllNodes(), borderNodes))
							newSelection.Add(face);
				}
			}
			else if (cutInfo.Action == CutInfo.ActionType.SelectEdges) // edges
			{
				if (allNodesFlag)
				{
					foreach (WingedEdge edge in mesh.Edges)
						if (nodeHitsForAllNodesOption.Contains(edge.BeginNode) && nodeHitsForAllNodesOption.Contains(edge.EndNode) && !(borderNodes.Contains(edge.BeginNode) && borderNodes.Contains(edge.EndNode)))
							newSelection.Add(edge);
				}
				else
				{
					foreach (WingedEdge edge in mesh.Edges)
						if (nodeHitsForSomeNodesOption.Contains(edge.BeginNode) || nodeHitsForSomeNodesOption.Contains(edge.EndNode) || (borderNodes.Contains(edge.BeginNode) && borderNodes.Contains(edge.EndNode)))
							newSelection.Add(edge);
				}
			}
			else if (cutInfo.Action == CutInfo.ActionType.SelectBeams) // beams
			{
				if (allNodesFlag)
				{
					foreach (Beam beam in mesh.Beams)
						if (nodeHitsForAllNodesOption.Contains(beam.BeginNode) && nodeHitsForAllNodesOption.Contains(beam.EndNode) && !(borderNodes.Contains(beam.BeginNode) && borderNodes.Contains(beam.EndNode)))
							newSelection.Add(beam);
				}
				else
				{
					foreach (Beam beam in mesh.Beams)
						if (nodeHitsForSomeNodesOption.Contains(beam.BeginNode) || nodeHitsForSomeNodesOption.Contains(beam.EndNode) || (borderNodes.Contains(beam.BeginNode) && borderNodes.Contains(beam.EndNode)))
							newSelection.Add(beam);
				}
			}
		}

		public static void HideSelectedElements(Mesh mesh)
		{
			if (mesh == null)
				return;
			
			Set<Element> toHide = new Set<Element>();

			foreach (ISelectable item in mesh.SelectedItems)
			{
				Element3D e3D = item as Element3D;
				if (e3D != null)
				{
					toHide.Add(e3D);
					continue;
				}
				Element2D e2D = item as Element2D;
				if (e2D != null && !(e2D is IFaceOfElement3D))
				{
					toHide.Add(e2D);
					continue;
				}
				Beam b = item as Beam;
				if (b != null)
				{
					toHide.Add(b);
				}
			}

			hideElements(mesh, toHide);
		}

		public static void HideElements(Mesh mesh, Set<Element> elementsToHide)
		{
			if (mesh == null)
				return;
			hideElements(mesh, elementsToHide);
		}

		//public static void SetVisibilityByProperty(Mesh mesh, Property value)
		//{
		//    if(mesh == null)
		//        return;

		//    ElementTest test = delegate(Element e)
		//    {
		//        return e.Property.Equals(value);
		//    };

		//    setElementVisibility(mesh, test);
		//}

		//public static void SetVisibilityByType(Mesh mesh, Type type)
		//{
		//    if (mesh == null)
		//        return;

		//    ElementTest test = delegate(Element e)
		//    {
		//        Type t = e.GetType();
		//        return t.Equals(type) || t.IsSubclassOf(type);
		//    };
			
		//    setElementVisibility(mesh, test);
		//}

		public static void SetVisibility(Mesh mesh, CutInfo cutInfo)
		{
			if (mesh == null)
				return;

			//Property[] propertyValues = cutInfo.ElementPropertiesToShow;
			//Type[] elementTypes = convertToTypeArray(cutInfo.ElementTypesToShow);
			Set<Property> propertyValues = (cutInfo.ElementPropertiesToShow == null) ? null : new Set<Property>(cutInfo.ElementPropertiesToShow);
			Set<ElementType> elementTypes = (cutInfo.ElementTypesToShow == null) ? null : new Set<ElementType>(cutInfo.ElementTypesToShow);

			ElementTest test;

			if (propertyValues == null || propertyValues.Count == 0 || elementTypes == null || elementTypes.Count == 0)
			{
				test = delegate
				{
					return false;
				};
			}
			else
			{
				test = delegate(Element e)
				{
					return propertyValues.Contains(e.Property) && elementTypes.Contains(e.ElementType);
				};
			}
			// ============================================================
			setElementVisibility(mesh, test);
		}

		public static void RestoreElements(Mesh mesh, Set<Element> elementsToRestore)
		{
			if (mesh == null)
				return;

			mesh.SelectedItems = new Set<ISelectable>(); // odoznacit polozky
			mesh.HiddenElements.RemoveMany(elementsToRestore); // smazat obnovene prvky ze seznamu uriznutych prvku
			
			MeshConstructor ctor = new MeshConstructor();
			ctor.CutMesh(mesh, new Set<Element>(), elementsToRestore, new Set<Node>(), false);
		}

		public static void HideRestoreElements(Mesh mesh, Set<Element> toHide, Set<Element> toRestore, bool selectFaces)
		{
			if (mesh == null)
				return;

			Set<Node> allNodesOfHiddenElements = new Set<Node>();
			foreach (Element e in toHide)
			{
				allNodesOfHiddenElements.AddMany(e.IterateThroughAllNodes());
				mesh.HiddenElements.Add(e);
			}
			mesh.HiddenElements.RemoveMany(toRestore);

			// odoznacit polozky
			mesh.SelectedItems = new Set<ISelectable>();
			MeshConstructor ctor = new MeshConstructor();
			Set<ISelectable> facesOnCut = ctor.CutMesh(mesh, toHide, toRestore, allNodesOfHiddenElements, selectFaces);

			if (selectFaces && facesOnCut != null)
			{
				mesh.SelectedItems = facesOnCut;
				mesh.UpdateColors();
			}
		}

		#endregion

		#region Private members
		
		private static CutTest createCutTestForCutPlanes(List<CutPlane> cutPlanes, CutInfo cutInfo)
		{
			CutTest test;

			if (cutPlanes.Count == 1)
			{
				Vector3 n = cutPlanes[0].NormalVector;
				float d = cutPlanes[0].GetDParameter(cutInfo);

				test = delegate(float x, float y, float z)
				{
					return n.X * x + n.Y * y + n.Z * z + d < 0f;
				};
			}
			else
			{
				Vector4[] par = new Vector4[cutPlanes.Count];
				for (int i = 0; i < cutPlanes.Count; i++)
				{
					Vector3 n = cutPlanes[i].NormalVector;
					float d = cutPlanes[i].GetDParameter(cutInfo);
					par[i] = new Vector4(n, d);
				}
				// ---------------------------------------
				if (cutInfo.Options == CutInfo.CutMeshByPlanesType.Union)
				{
					test = delegate(float x, float y, float z)
					{
						for (int i = 0; i < par.Length; i++)
							if (par[i].X * x + par[i].Y * y + par[i].Z * z + par[i].W < 0f)
								return true;
						return false;
					};
				}
				else // cutOption == CutInfo.CutMeshByPlanesType.Intersection
				{
					test = delegate(float x, float y, float z)
					{
						for (int i = 0; i < par.Length; i++)
							if (!(par[i].X * x + par[i].Y * y + par[i].Z * z + par[i].W < 0f))
								return false;
						return true;
					};
				}
			}
			return test;
		}

		private static Set<ISelectable> findSelectedItemsInMesh(Mesh mesh, CutInfo cutInfo, Set<Element> elementHits, Set<Node> nodeHits)
		{
			bool allNodes = (cutInfo.HitDecision == CutInfo.ItemHitDecision.AllNodes);
			Set<ISelectable> result = new Set<ISelectable>();
			switch (cutInfo.Action)
			{
				case CutInfo.ActionType.SelectElements:
					foreach (Element e in elementHits)
						result.Add(e);
					break;
				case CutInfo.ActionType.SelectNodes:
					foreach (Node n in nodeHits)
						result.Add(n);
					break;
				case CutInfo.ActionType.SelectFaces:
					if (allNodes)
					{
						foreach (Element2D face in mesh.Faces)
						{
							bool hit = true;
							foreach (Node n in face.IterateThroughAllNodes())
							{
								if (!nodeHits.Contains(n))
								{
									hit = false;
									break;
								}
							}
							if (hit)
								result.Add(face);
						}
					}
					else
					{
						foreach (Element2D face in mesh.Faces)
						{
							foreach (Node n in face.IterateThroughAllNodes())
							{
								if (nodeHits.Contains(n))
								{
									result.Add(face);
									break;
								}
							}
						}
					}
					break;
				case CutInfo.ActionType.SelectEdges:
					if (allNodes)
					{
						foreach (WingedEdge edge in mesh.Edges)
						{
							if (nodeHits.Contains(edge.BeginNode) && nodeHits.Contains(edge.EndNode))
								result.Add(edge);
						}
					}
					else
					{
						foreach (WingedEdge edge in mesh.Edges)
						{
							if (nodeHits.Contains(edge.BeginNode) || nodeHits.Contains(edge.EndNode))
								result.Add(edge);
						}
					}
					break;
				case CutInfo.ActionType.SelectBeams:
					if (allNodes)
					{
						foreach (Beam b in mesh.Beams)
						{
							if (nodeHits.Contains(b.BeginNode) && nodeHits.Contains(b.EndNode))
								result.Add(b);
						}
					}
					else
					{
						foreach (Beam b in mesh.Beams)
						{
							if (nodeHits.Contains(b.BeginNode) || nodeHits.Contains(b.EndNode))
								result.Add(b);
						}
					}
					break;
				default:
					throw new NotSupportedException();
			}
			return result;
		}

		private static void setElementVisibility(Mesh mesh, ElementTest elementVisibilityTest)
		{
			mesh.SelectedItems = new Set<ISelectable>(); // odoznacit polozky

			Set<Element> toCut = new Set<Element>();
			Set<Element> toRestore = new Set<Element>();
			Set<Node> allNodesOfCuttedElements = new Set<Node>();

			foreach (Element e in mesh.Elements)
			{
				bool cutted = mesh.HiddenElements.Contains(e);
				if (!elementVisibilityTest(e))
				{
					if (!cutted)
					{
						toCut.Add(e);
						mesh.HiddenElements.Add(e);
						allNodesOfCuttedElements.AddMany(e.IterateThroughAllNodes());
					}
				}
				else if (cutted)
				{
					toRestore.Add(e);
				}
			}

			MeshConstructor ctor = new MeshConstructor();
			ctor.CutMesh(mesh, toCut, toRestore, allNodesOfCuttedElements, false);
			mesh.HiddenElements.RemoveMany(toRestore); // uz nejsou zadne uriznute
		}

		private static void hideElements(Mesh mesh, Set<Element> toHide)
		{
			Set<Node> allNodesOfCuttedElements = new Set<Node>();
			foreach(Element e in toHide)
			{
				allNodesOfCuttedElements.AddMany(e.IterateThroughAllNodes());
				mesh.HiddenElements.Add(e);
			}

			if (toHide.Count == 0)
				return;

			// odoznacit polozky
			mesh.SelectedItems = new Set<ISelectable>();
			
			MeshConstructor ctor = new MeshConstructor();
			ctor.CutMesh(mesh, toHide, new Set<Element>(), allNodesOfCuttedElements, false);

		}

		private static void doCut(Mesh mesh, CutTest isToCut, CutInfo.ItemHitDecision hitDecision, bool transformCoordinates)
		{
			Set<Element> elementHits;
			Set<Node> nodeHits;
			//getHits(mesh, isToCut, transformCoordinates, hitDecision, out elementsHits, false, out nodeHits);

			if (hitDecision == CutInfo.ItemHitDecision.AllNodes)
				getHitsAllNodesInArea(mesh, isToCut, transformCoordinates, out elementHits, false, out nodeHits);
			else if (hitDecision == CutInfo.ItemHitDecision.SomeNodes)
				getHitsSomeNodesInArea(mesh, isToCut, transformCoordinates, out elementHits, false, out nodeHits);
			else
				throw new NotSupportedException(hitDecision.ToString() + " option is not supported");

			Set<Node> allNodesOfCuttedElements = new Set<Node>();
			foreach (Element e in elementHits)
				allNodesOfCuttedElements.AddMany(e.IterateThroughAllNodes());

			mesh.HiddenElements.AddMany(elementHits); // pridat do seznamu vymazanych

			mesh.SelectedItems = new Set<ISelectable>(); // odoznacit polozky

			MeshConstructor ctor = new MeshConstructor();
			Set<ISelectable> facesOnCut = ctor.CutMesh(mesh, elementHits, new Set<Element>(), allNodesOfCuttedElements, Scene.SelectFacesOnCut);
			if (Scene.SelectFacesOnCut && facesOnCut != null)
				mesh.SelectedItems = facesOnCut;
		}

		private static IEnumerable<Node> getAllNodes(Mesh mesh)
		{
			Set<Node> allNodes = new Set<Node>();
			foreach (Element e in mesh.Elements)
			{
				if (mesh.HiddenElements.Contains(e))
					continue;
				Element2D face = e as Element2D;
				if (face != null)
				{
					foreach (Node n in face.IterateThroughAllNodesIncludingEdgeMiddleNodes())
						allNodes.Add(n);
				}
				else
				{
					foreach (Node n in e.IterateThroughAllNodes())
						allNodes.Add(n);
				}
			}
			return allNodes;
		}

		private static Set<Node> getNodeHits(IEnumerable<Node> nodes, CutTest isToCut)
		{
			Set<Node>  nodeHits = new Set<Node>();

			foreach (Node n in nodes)
			{
				Vector3 nodePosition = n.Position;
				if (isToCut(nodePosition.X, nodePosition.Y, nodePosition.Z))
					nodeHits.Add(n);
			}
				
			return nodeHits;
		}

		private static void getHitsSomeNodesInArea(Mesh mesh, CutTest isToCut, bool transformCoordinates, out Set<Element> elementsHits, bool computeNodeHits, out Set<Node> nodeHits)
		{
			elementsHits = new Set<Element>();
			nodeHits = new Set<Node>();

			float invertedResizeFactor = 1f / mesh.ResizeFactor;
			Vector3 positionOffset = mesh.PositionOffset;

			foreach (Element e in mesh.Elements)
			{
				if (mesh.HiddenElements.Contains(e))
					continue;

				bool hit = false;
				foreach (Node n in e.IterateThroughAllNodes())
				{
					Vector3 nodePosition = n.Position;

					if (transformCoordinates)
						nodePosition = nodePosition * invertedResizeFactor + positionOffset;

					if (isToCut(nodePosition.X, nodePosition.Y, nodePosition.Z))
					{
						hit = true;
						if (!computeNodeHits)
							break;
						else
							nodeHits.Add(n);
					}
				}
				// -------------------------------
				if (hit)
					elementsHits.Add(e);
			}
		}

		private static void getHitsAllNodesInArea(Mesh mesh, CutTest isToCut, bool transformCoordinates, out Set<Element> elementsHits, bool computeNodeHits, out Set<Node> nodeHits)
		{
			elementsHits = new Set<Element>();
			nodeHits = new Set<Node>();

			float invertedResizeFactor = 1f / mesh.ResizeFactor;
			Vector3 positionOffset = mesh.PositionOffset;

			foreach (Element e in mesh.Elements)
			{
				if (mesh.HiddenElements.Contains(e))
					continue;

				bool hit = true;
				foreach (Node n in e.IterateThroughAllNodes())
				{
					Vector3 nodePosition = n.Position;

					if (transformCoordinates)
						nodePosition = nodePosition * invertedResizeFactor + positionOffset;

					if (isToCut(nodePosition.X, nodePosition.Y, nodePosition.Z))
					{
						if (computeNodeHits)
							nodeHits.Add(n);
					}
					else
					{
						hit = false;
					}
				}
				// -----------------
				if (hit)
					elementsHits.Add(e);
			}
		}

		#endregion
		
	}
}
