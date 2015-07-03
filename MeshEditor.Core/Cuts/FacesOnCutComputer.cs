using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Construction;
using MeshEditor.Data;
using Wintellect.PowerCollections;

namespace MeshEditor.Cuts
{
	/// <summary>
	/// trida, ktera zjisti, ktera vypocte a uchova mnozinu ploch, ktere jsou na povrchu rezu
	/// </summary>
	public class FacesOnCutComputer
	{
		private Dictionary<TriangleMark, Triangle> triangleHits;
		private Dictionary<QuadMark, Quadrilateral> quadHits;

		public FacesOnCutComputer()
		{
			triangleHits = new Dictionary<TriangleMark, Triangle>();
			quadHits = new Dictionary<QuadMark, Quadrilateral>();
		}

		public void Init(IEnumerable<Element2D> allFaces, Set<Element> processedElements)
		{
			foreach (Element2D face in allFaces)
			{
				TriangleFaceOfElement3D t = face as TriangleFaceOfElement3D;
				if (t != null && processedElements.Contains(t.ParentElement))
					triangleHits[new TriangleMark(t.Node1.ID, t.Node2.ID, t.Node3.ID)] = t;
				else
				{
					QuadFaceOfElement3D q = face as QuadFaceOfElement3D;
					if (q != null && processedElements.Contains(q.ParentElement))
						quadHits[new QuadMark(q.Node1.ID, q.Node2.ID, q.Node3.ID, q.Node4.ID)] = q;
				}
			}
		}

		public Set<ISelectable> GetFacesOnCut(IEnumerable<Element2D> allFaces, Dictionary<TriangleMark, Triangle> triangleFaces, Dictionary<QuadMark, Quadrilateral> quadFaces)
		{
			Set<ISelectable> facesOnCut = new Set<ISelectable>();
			Set<Element2D> oldFacesSet = new Set<Element2D>(allFaces);
			foreach (KeyValuePair<TriangleMark, Triangle> pair in triangleFaces)
			{
				if (!oldFacesSet.Contains(pair.Value) && !triangleHits.ContainsKey(pair.Key))
					facesOnCut.Add(pair.Value);
			}
			foreach (KeyValuePair<QuadMark, Quadrilateral> pair in quadFaces)
			{
				if (!oldFacesSet.Contains(pair.Value) && !quadHits.ContainsKey(pair.Key))
					facesOnCut.Add(pair.Value);
			}
			return facesOnCut;
		}
	}
}
