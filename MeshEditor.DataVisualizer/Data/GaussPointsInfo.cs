using MeshEditor.CoreInterface;
using MeshEditor.Data;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	public class GaussPointsInfo
	{
		public enum ElementTypes
		{
			None = 0,
			Point,
			Linear,
			Triangle,
			Quadrilateral,
			Tetrahedra,
			Hexahedra,
			Prism // Wedge
		}

		public enum NaturalCoordinatesTypes
		{
			None = 0,
			Internal,
			Given
		}

		#region Fields, Constructor, Properties

		private double[,] naturalCoordinates;
		
		private int[] nearestGaussPointsIndices;

		public GaussPointsInfo(ElementTypes elementType)
		{
			this.ElementType = elementType;
		}

		public NaturalCoordinatesTypes NaturalCoordinatesType { get; private set; }

		public ElementTypes ElementType { get; private set; }

		public int GPNumber { get; private set; }

		public string MeshName { get; set; }

		public bool NodesIncluded { get; set; }

		#endregion

		#region Public methods

		public void SetGaussPointsNumber(int number)
		{
			GPNumber = number;
			naturalCoordinates = new double[GPNumber, GetDimension()];
		}

		public void SetNaturalCoordinate(int gaussPointIndex, int dimension, double value)
		{
			naturalCoordinates[gaussPointIndex, dimension] = value;
			NaturalCoordinatesType = NaturalCoordinatesTypes.Given;
		}

		public int GetDimension()
		{
			switch (ElementType)
			{
				case ElementTypes.None:
				case ElementTypes.Point:
					return 0;
				case ElementTypes.Linear:
					return 1;
				case ElementTypes.Triangle:
				case ElementTypes.Quadrilateral:
					return 2;
				case ElementTypes.Tetrahedra:
				case ElementTypes.Hexahedra:
				case ElementTypes.Prism:
					return 3;
				default:
					throw new NotSupportedException();
			}
		}

		public bool IsAllowedGaussPointsNumber(int number)
		{
			if (number < 1 || ElementType == ElementTypes.None || ElementType == ElementTypes.Point)
				return false;
			if (number == 1)
				return true;
			switch (ElementType)
			{
				case ElementTypes.Linear:
					return true;
				case ElementTypes.Triangle:
					return number == 3 || number == 6;
				case ElementTypes.Quadrilateral:
					return number == 4 || number == 9;
				case ElementTypes.Tetrahedra:
					return number == 4 || number == 10;
				case ElementTypes.Hexahedra:
					return number == 8 || number == 27;
				case ElementTypes.Prism:
					return number == 6;
				default:
					throw new NotSupportedException();
			}
		}

		public void SetInternalNaturalCoordinates()
		{
			Debug.Assert(naturalCoordinates.GetLength(0) == GPNumber && naturalCoordinates.GetLength(1) == GetDimension());

			switch (ElementType)
			{
				case ElementTypes.None:
				case ElementTypes.Point:
					throw new NotSupportedException();
				case ElementTypes.Linear:
					{
						double step;
						double start;
						if (NodesIncluded)
						{
							step = 1.0 / (GPNumber - 1);
							start = 0.0;
						}
						else // Nodes Not Included
						{
							step = 1.0 / (GPNumber + 1);
							start = step;
						}
						for (int i = 0; i < GPNumber; i++)
						{
							naturalCoordinates[i, 0] = start + step * i;
						}
					}
					break;
				case ElementTypes.Triangle:
					switch (GPNumber)
					{
						case 1:
							{
								double a = 1.0 / 3.0;
								naturalCoordinates[0, 0] = naturalCoordinates[0, 1] = a;
							}
							break;
						case 3:
							{
								double a = 0.5;
								naturalCoordinates[0, 0] = a; naturalCoordinates[0, 1] = 0.0;
								naturalCoordinates[1, 0] = naturalCoordinates[1, 1] = a;
								naturalCoordinates[2, 0] = 0.0; naturalCoordinates[2, 1] = a;
							}
							break;
						case 6:
							{
								double a = 0.09157621;
								double b = 0.81684757;
								double c = 0.44594849;
								double d = 0.10810301;
								naturalCoordinates[0, 0] = naturalCoordinates[0, 1] = a;
								naturalCoordinates[1, 0] = b; naturalCoordinates[1, 1] = a;
								naturalCoordinates[2, 0] = a; naturalCoordinates[2, 1] = b;
								naturalCoordinates[3, 0] = c; naturalCoordinates[3, 1] = d;
								naturalCoordinates[4, 0] = naturalCoordinates[4, 1] = c;
								naturalCoordinates[5, 0] = d; naturalCoordinates[5, 1] = c;
							}
							break;
						default:
							throw new NotSupportedException();
					}
					break;
				case ElementTypes.Quadrilateral:
					switch (GPNumber)
					{
						case 1:
							naturalCoordinates[0, 0] = naturalCoordinates[0, 1] = 0.0;
							break;
						case 4:
							{
								double a = 0.57735027;
								naturalCoordinates[0, 0] = naturalCoordinates[0, 1] = -a;
								naturalCoordinates[1, 0] = a; naturalCoordinates[1, 1] = -a;
								naturalCoordinates[2, 0] = naturalCoordinates[2, 1] = a;
								naturalCoordinates[3, 0] = -a; naturalCoordinates[3, 1] = a;
							}
							break;
						case 9:
							{
								double a = 0.77459667;
								naturalCoordinates[0, 0] = naturalCoordinates[0, 1] = -a;
								naturalCoordinates[1, 0] = a; naturalCoordinates[1, 1] = -a;
								naturalCoordinates[2, 0] = naturalCoordinates[2, 1] = a;
								naturalCoordinates[3, 0] = -a; naturalCoordinates[3, 1] = a;
								naturalCoordinates[4, 0] = 0.0; naturalCoordinates[4, 1] = -a;
								naturalCoordinates[5, 0] = a; naturalCoordinates[5, 1] = 0.0;
								naturalCoordinates[6, 0] = 0.0; naturalCoordinates[6, 1] = a;
								naturalCoordinates[7, 0] = -a; naturalCoordinates[7, 1] = 0.0;
								naturalCoordinates[8, 0] = naturalCoordinates[8, 1] = 0.0;
							}
							break;
						default:
							throw new NotSupportedException();
					}
					break;
				case ElementTypes.Tetrahedra:
					switch (GPNumber)
					{
						case 4:
							{
								double a = 0.58541020;
								double b = 0.13819660;
								naturalCoordinates[0, 0] = naturalCoordinates[0, 1] = naturalCoordinates[0, 2] = b;
								naturalCoordinates[1, 0] = a; naturalCoordinates[1, 1] = naturalCoordinates[1, 2] = b;
								naturalCoordinates[2, 0] = b; naturalCoordinates[2, 1] = a; naturalCoordinates[2, 2] = b;
								naturalCoordinates[3, 0] = naturalCoordinates[3, 1] = b; naturalCoordinates[3, 2] = a;
							}
							break;
						case 10:
							{
								double a = 0.10810301;
								double b = 0.44594849;
								double c = 0.81684757;
								naturalCoordinates[0, 0] = naturalCoordinates[0, 1] = naturalCoordinates[0, 2] = a;
								naturalCoordinates[1, 0] = c; naturalCoordinates[1, 1] = naturalCoordinates[1, 2] = a;
								naturalCoordinates[2, 0] = a; naturalCoordinates[2, 1] = c; naturalCoordinates[2, 2] = a;
								naturalCoordinates[3, 0] = naturalCoordinates[3, 1] = a; naturalCoordinates[3, 2] = c;
								naturalCoordinates[4, 0] = b; naturalCoordinates[4, 1] = naturalCoordinates[4, 2] = a;
								naturalCoordinates[5, 0] = naturalCoordinates[5, 1] = b; naturalCoordinates[5, 2] = a;
								naturalCoordinates[6, 0] = a; naturalCoordinates[6, 1] = b; naturalCoordinates[6, 2] = a;
								naturalCoordinates[7, 0] = naturalCoordinates[7, 1] = a; naturalCoordinates[7, 2] = b;
								naturalCoordinates[8, 0] = b; naturalCoordinates[8, 1] = a; naturalCoordinates[8, 2] = b;
								naturalCoordinates[9, 0] = a; naturalCoordinates[9, 1] = naturalCoordinates[9, 2] = b;
							}
							break;
						default:
							throw new NotSupportedException();
					}
					break;
				case ElementTypes.Hexahedra:
					switch (GPNumber)
					{
						case 8:
							{
								double a = 0.57735027;
								naturalCoordinates[0, 0] = naturalCoordinates[0, 1] = naturalCoordinates[0, 2] = -a;
								naturalCoordinates[1, 0] = a; naturalCoordinates[1, 1] = naturalCoordinates[0, 2] = -a;
								naturalCoordinates[2, 0] = naturalCoordinates[2, 1] = a; naturalCoordinates[0, 2] = -a;
								naturalCoordinates[3, 0] = -a; naturalCoordinates[3, 1] = a; naturalCoordinates[0, 2] = -a;
								naturalCoordinates[4, 0] = naturalCoordinates[4, 1] = -a; naturalCoordinates[0, 2] = a;
								naturalCoordinates[5, 0] = a; naturalCoordinates[5, 1] = -a; naturalCoordinates[0, 2] = a;
								naturalCoordinates[6, 0] = naturalCoordinates[6, 1] = naturalCoordinates[0, 2] = a;
								naturalCoordinates[7, 0] = -a; naturalCoordinates[7, 1] = naturalCoordinates[0, 2] = a;
							}
							break;
						case 27:
							{
								double a = 0.77459667;

								naturalCoordinates[0, 0] = naturalCoordinates[0, 1] = naturalCoordinates[0, 2] = -a;
								naturalCoordinates[1, 0] = a; naturalCoordinates[1, 1] = naturalCoordinates[0, 2] = -a;
								naturalCoordinates[2, 0] = naturalCoordinates[2, 1] = a; naturalCoordinates[0, 2] = -a;
								naturalCoordinates[3, 0] = -a; naturalCoordinates[3, 1] = a; naturalCoordinates[0, 2] = -a;

								naturalCoordinates[4, 0] = naturalCoordinates[4, 1] = -a; naturalCoordinates[0, 2] = a;
								naturalCoordinates[5, 0] = a; naturalCoordinates[5, 1] = -a; naturalCoordinates[0, 2] = a;
								naturalCoordinates[6, 0] = naturalCoordinates[6, 1] = naturalCoordinates[0, 2] = a;
								naturalCoordinates[7, 0] = -a; naturalCoordinates[7, 1] = naturalCoordinates[0, 2] = a;

								naturalCoordinates[8, 0] = 0.0; naturalCoordinates[8, 1] = naturalCoordinates[8, 2] = -a;
								naturalCoordinates[9, 0] = a; naturalCoordinates[9, 1] = 0.0; naturalCoordinates[9, 2] = -a;
								naturalCoordinates[10, 0] = 0.0; naturalCoordinates[10, 1] = a; naturalCoordinates[10, 2] = -a;
								naturalCoordinates[11, 0] = -a; naturalCoordinates[11, 1] = 0.0; naturalCoordinates[11, 2] = -a;

								naturalCoordinates[12, 0] = naturalCoordinates[12, 1] = -a; naturalCoordinates[12, 2] = 0.0;
								naturalCoordinates[13, 0] = a; naturalCoordinates[13, 1] = -a; naturalCoordinates[13, 2] = 0.0;
								naturalCoordinates[14, 0] = naturalCoordinates[14, 1] = a; naturalCoordinates[14, 2] = 0.0;
								naturalCoordinates[15, 0] = -a; naturalCoordinates[15, 1] = a; naturalCoordinates[15, 2] = 0.0;

								naturalCoordinates[16, 0] = 0.0; naturalCoordinates[16, 1] = -a; naturalCoordinates[16, 2] = a;
								naturalCoordinates[17, 0] = a; naturalCoordinates[17, 1] = 0.0; naturalCoordinates[17, 2] = a;
								naturalCoordinates[18, 0] = 0.0; naturalCoordinates[18, 1] = naturalCoordinates[18, 2] = a;
								naturalCoordinates[19, 0] = -a; naturalCoordinates[19, 1] = 0.0; naturalCoordinates[19, 2] = a;

								naturalCoordinates[20, 0] = naturalCoordinates[20, 1] = 0.0; naturalCoordinates[20, 2] = -a;

								naturalCoordinates[21, 0] = 0.0; naturalCoordinates[21, 1] = -a; naturalCoordinates[21, 2] = 0.0;
								naturalCoordinates[22, 0] = a; naturalCoordinates[22, 1] = naturalCoordinates[22, 2] = 0.0;
								naturalCoordinates[23, 0] = 0.0; naturalCoordinates[23, 1] = a; naturalCoordinates[23, 2] = 0.0;
								naturalCoordinates[24, 0] = -a; naturalCoordinates[24, 1] = naturalCoordinates[24, 2] = 0.0;

								naturalCoordinates[25, 0] = naturalCoordinates[25, 1] = 0.0; naturalCoordinates[25, 2] = a;
								naturalCoordinates[26, 0] = naturalCoordinates[26, 1] = naturalCoordinates[26, 2] = 0.0;
							}
							break;
						default:
							throw new NotSupportedException();
					}
					break;
				case ElementTypes.Prism:
					{
						if (GPNumber != 6)
							throw new NotSupportedException();

						double a = 0.16666666;
						double b = 0.66666666;
						double c = 0.21132486;
						double d = 0.78867513;

						naturalCoordinates[0, 0] = naturalCoordinates[0, 1] = a; naturalCoordinates[0, 2] = b;
						naturalCoordinates[1, 0] = b; naturalCoordinates[1, 1] = a; naturalCoordinates[1, 2] = c;
						naturalCoordinates[2, 0] = a; naturalCoordinates[2, 1] = b; naturalCoordinates[2, 2] = c;
						naturalCoordinates[3, 0] = naturalCoordinates[3, 1] = a; naturalCoordinates[3, 2] = d;
						naturalCoordinates[4, 0] = b; naturalCoordinates[4, 1] = a; naturalCoordinates[4, 2] = d;
						naturalCoordinates[5, 0] = a; naturalCoordinates[5, 1] = b; naturalCoordinates[5, 2] = d;
					}
					break;
				default:
					throw new NotSupportedException();
			}

			NaturalCoordinatesType = NaturalCoordinatesTypes.Internal;
		}

		public Dictionary<Node, double> ExtrapolateElementGaussPointValuesToNodes(Element element, double[] gaussPointValues, GaussPointsExtrapolationStrategy strategy)
		{
			Dictionary<Node, double> values = new Dictionary<Node, double>();
			switch (strategy)
			{
				case GaussPointsExtrapolationStrategy.NearestGaussPoint:
					if (nearestGaussPointsIndices == null)
					{
						nearestGaussPointsIndices = getNearestGaussPointIndicesForElementNodes(); // lazy initialization of index array
					}
					Debug.Assert(nearestGaussPointsIndices.Length <= element.NodeCount);
					int nodeIndex = 0;
					foreach (Node node in element.IterateThroughAllNodes())
					{
						if (nodeIndex >= nearestGaussPointsIndices.Length)
							break;
						int gpIndex = nearestGaussPointsIndices[nodeIndex];
						values[node] = gaussPointValues[gpIndex];
						++nodeIndex;
					}
					break;
				default:
					throw new NotSupportedException();
			}
			return values;
		}

		#endregion

		#region Private methods

		private int[] getNearestGaussPointIndicesForElementNodesWithNaturalCoordinates(double[] elementNodes) // 1D
		{
			Debug.Assert(GetDimension() == 1 && naturalCoordinates.GetLength(1) == 1);
			int[] indices = new int[elementNodes.Length];
			for (int i = 0; i < elementNodes.Length; i++)
			{
				Debug.Assert(elementNodes[i] >= 0.0 && elementNodes[i] <= 1.0);
				double minDistance = double.MaxValue;
				for (int j = 0; j < GPNumber; j++)
				{
					double distance = Math.Abs(naturalCoordinates[j, 0] - elementNodes[i]);
					if (distance < minDistance)
					{
						indices[i] = j;
						minDistance = distance;
					}
				}
			}
			return indices;
		}

		private int[] getNearestGaussPointIndicesForElementNodesWithNaturalCoordinates(Vector2d[] elementNodes) // 2D
		{
			Debug.Assert(GetDimension() == 2 && naturalCoordinates.GetLength(1) == 2);
			int[] indices = new int[elementNodes.Length];
			for (int i = 0; i < elementNodes.Length; i++)
			{
				Debug.Assert(elementNodes[i].X >= -1.0 && elementNodes[i].X <= 1.0 && elementNodes[i].Y >= -1.0 && elementNodes[i].Y <= 1.0);
				double minDistanceSQR = double.MaxValue;
				for (int j = 0; j < GPNumber; j++)
				{
					double diffX = naturalCoordinates[j, 0] - elementNodes[i].X;
					double diffY = naturalCoordinates[j, 1] - elementNodes[i].Y;
					double distanceSQR = diffX * diffX + diffY * diffY;
					if (distanceSQR < minDistanceSQR)
					{
						indices[i] = j;
						minDistanceSQR = distanceSQR;
					}
				}
			}
			return indices;
		}

		private int[] getNearestGaussPointIndicesForElementNodesWithNaturalCoordinates(Vector3d[] elementNodes) // 3D
		{
			Debug.Assert(GetDimension() == 3 && naturalCoordinates.GetLength(1) == 3);
			int[] indices = new int[elementNodes.Length];
			for (int i = 0; i < elementNodes.Length; i++)
			{
				Debug.Assert(elementNodes[i].X >= -1.0 && elementNodes[i].X <= 1.0 && elementNodes[i].Y >= -1.0 && elementNodes[i].Y <= 1.0 && elementNodes[i].Z >= -1.0 && elementNodes[i].Z <= 1.0);
				double minDistance = double.MaxValue;
				for (int j = 0; j < GPNumber; j++)
				{
					double distance = Math.Abs(naturalCoordinates[j, 0] - elementNodes[i].X) + Math.Abs(naturalCoordinates[j, 1] - elementNodes[i].Y) + Math.Abs(naturalCoordinates[j, 2] - elementNodes[i].Z);
					if (distance < minDistance)
					{
						indices[i] = j;
						minDistance = distance;
					}
				}
			}
			return indices;
		}

		private int[] getNearestGaussPointIndicesForElementNodes()
		{
			Debug.Assert(NaturalCoordinatesType != NaturalCoordinatesTypes.None);

			switch (ElementType)
			{
				case ElementTypes.None:
				case ElementTypes.Point:
					throw new NotSupportedException();
				case ElementTypes.Linear:
					return getNearestGaussPointIndicesForElementNodesWithNaturalCoordinates(new double[] { 0.0, 1.0 });
				case ElementTypes.Triangle:
					return getNearestGaussPointIndicesForElementNodesWithNaturalCoordinates(new Vector2d[] { new Vector2d(0.0, 0.0), new Vector2d(1.0, 0.0), new Vector2d(0.0, 1.0) });
				case ElementTypes.Quadrilateral:
					return getNearestGaussPointIndicesForElementNodesWithNaturalCoordinates(new Vector2d[] { new Vector2d(-1.0, -1.0), new Vector2d(1.0, -1.0), new Vector2d(1.0, 1.0), new Vector2d(-1.0, 1.0) });
				case ElementTypes.Tetrahedra:
					return getNearestGaussPointIndicesForElementNodesWithNaturalCoordinates(new Vector3d[] { new Vector3d(0.0, 0.0, 0.0), new Vector3d(1.0, 0.0, 0.0), new Vector3d(0.0, 1.0, 0.0), new Vector3d(0.0, 0.0, 1.0) });
				case ElementTypes.Hexahedra:
					return getNearestGaussPointIndicesForElementNodesWithNaturalCoordinates(new Vector3d[] { new Vector3d(-1.0, -1.0, -1.0), new Vector3d(1.0, -1.0, -1.0), new Vector3d(1.0, 1.0, -1.0), new Vector3d(-1.0, 1.0, -1.0), new Vector3d(-1.0, -1.0, 1.0), new Vector3d(1.0, -1.0, 1.0), new Vector3d(1.0, 1.0, 1.0), new Vector3d(-1.0, 1.0, 1.0) });
				case ElementTypes.Prism:
					return getNearestGaussPointIndicesForElementNodesWithNaturalCoordinates(new Vector3d[] { new Vector3d(0.0, 0.0, 0.0), new Vector3d(1.0, 0.0, 0.0), new Vector3d(0.0, 1.0, 0.0), new Vector3d(0.0, 0.0, 1.0), new Vector3d(1.0, 0.0, 1.0), new Vector3d(0.0, 1.0, 1.0) });
				default:
					throw new NotSupportedException();
			}

			//  Debug.Assert(NaturalCoordinatesType == NaturalCoordinatesTypes.Internal);
			//	switch (ElementType)
			//	{
			//		case ElementTypes.None:
			//		case ElementTypes.Point:
			//			throw new NotSupportedException();
			//		case ElementTypes.Linear:
			//			return new int[] { 0, GPNumber - 1 };
			//		case ElementTypes.Triangle:
			//			switch (GPNumber)
			//			{
			//				case 1:
			//					return new int[] { 0, 0, 0 };
			//				case 3:
			//					//return new int[] { 0, 1, 2 }; /**/
			//					throw new NotImplementedException();
			//				case 6:
			//					return new int[] { 0, 1, 2 };
			//				default:
			//					throw new NotSupportedException();
			//			}
			//		case ElementTypes.Quadrilateral:
			//			switch (GPNumber)
			//			{
			//				case 1:
			//					return new int[] { 0, 0, 0, 0 };
			//				case 4:
			//				case 9:
			//					return new int[] { 0, 1, 2, 3 };
			//				default:
			//					throw new NotSupportedException();
			//			}
			//		case ElementTypes.Tetrahedra:
			//			switch (GPNumber)
			//			{
			//				case 4:
			//				case 10:
			//					return new int[] { 0, 1, 2, 3 };
			//				default:
			//					throw new NotSupportedException();
			//			}
			//		case ElementTypes.Hexahedra:
			//			switch (GPNumber)
			//			{
			//				case 8:
			//				case 27:
			//					return new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };
			//				default:
			//					throw new NotSupportedException();
			//			}
			//		case ElementTypes.Prism:
			//			switch (GPNumber)
			//			{
			//				case 6:
			//					return new int[] { 0, 1, 2, 3, 4, 5 };
			//				default:
			//					throw new NotSupportedException();
			//			}
			//		default:
			//			throw new NotSupportedException();
			//	}
		}

		#endregion

	}
}
