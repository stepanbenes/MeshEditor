using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Common.Extensions;
using MeshEditor.LayerManager.Data;

namespace MeshEditor.LayerManager.Import
{
	partial class GiDDataFormatParser
	{
		private class GaussPointsInfo
		{
			public enum NaturalCoordinatesTypes
			{
				None = 0,
				Internal,
				Given
			}

			#region Fields, Constructor, Properties

			private double[,] naturalCoordinates;
			private int gaussPointIndex;

			public GaussPointsInfo(string locationName, string elementType, string meshName)
			{
				LocationName = locationName;
				ElementType = elementType;
				MeshName = meshName;
			}

			public string LocationName { get; }
			public string ElementType { get; }
			public string MeshName { get; }

			public NaturalCoordinatesTypes NaturalCoordinatesType { get; set; }
			public int NumberOfGaussPoints { get; set; }
			public bool NodesIncluded { get; set; }

			public double this[int gpIndex, int dimensionIndex] => naturalCoordinates[gpIndex, dimensionIndex];

			#endregion

			#region Public methods

			public void SetInternalNaturalCoordinates()
			{
				Debug.Assert(NaturalCoordinatesType == NaturalCoordinatesTypes.Internal);
				Debug.Assert(naturalCoordinates == null);

				naturalCoordinates = new double[NumberOfGaussPoints, getDimension()];

				switch (ElementType?.ToLower())
				{
					case "linear":
						{
							double step;
							double start;
							if (NodesIncluded)
							{
								step = 1.0 / (NumberOfGaussPoints - 1);
								start = 0.0;
							}
							else // Nodes Not Included
							{
								step = 1.0 / (NumberOfGaussPoints + 1);
								start = step;
							}
							for (int i = 0; i < NumberOfGaussPoints; i++)
							{
								naturalCoordinates[i, 0] = start + step * i;
							}
						}
						break;
					case "triangle":
						switch (NumberOfGaussPoints)
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
					case "quadrilateral":
						switch (NumberOfGaussPoints)
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
					case "tetrahedra":
						switch (NumberOfGaussPoints)
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
					case "hexahedra":
						switch (NumberOfGaussPoints)
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
					case "prism":
						{
							if (NumberOfGaussPoints != 6)
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
					case "none":
					case "point":
					default:
						throw new NotSupportedException($"This element type is not supported ({ElementType}).");
				}
			}

			public void AddNaturalCoordinates(double[] gpNaturalCoordinates)
			{
				Debug.Assert(NaturalCoordinatesType == NaturalCoordinatesTypes.Given);
				Debug.Assert(NumberOfGaussPoints > 0);
				Debug.Assert(gpNaturalCoordinates?.Length > 0);

				if (naturalCoordinates == null)
				{
					naturalCoordinates = new double[NumberOfGaussPoints, getDimension()];
					gaussPointIndex = 0;
				}

				for (int i = 0; i < gpNaturalCoordinates.Length; i++)
				{
					naturalCoordinates[gaussPointIndex, i] = gpNaturalCoordinates[i];
				}
				gaussPointIndex++;
			}

			public int GetIndexOfNearestGaussPoint(CellType cellType, int cellPointIndex)
			{
				double x, y = 0.0, z = 0.0; // natural coordinates
				switch (cellType)
				{
					case CellType.LineLinear:
					case CellType.LineQuadratic:
						switch (cellPointIndex)
						{
							case 0:
								x = 0.0;
								break;
							case 1:
								x = 1.0;
								break;
							case 2:
								x = 0.5;
								break;
							default:
								throw new NotSupportedException();
						}
						break;
					case CellType.TriangleLinear:
					case CellType.TriangleQuadratic:
						switch (cellPointIndex)
						{
							case 0:
								x = 0.0;
								y = 0.0;
								break;
							case 1:
								x = 1.0;
								y = 0.0;
								break;
							case 2:
								x = 0.0;
								y = 1.0;
								break;
							case 3:
								x = 0.5;
								y = 0.0;
								break;
							case 4:
								x = 0.5;
								y = 0.5;
								break;
							case 5:
								x = 0.0;
								y = 0.5;
								break;
							default:
								throw new NotSupportedException();
						}
						break;
					case CellType.QuadLinear:
					case CellType.QuadQuadratic:
						switch (cellPointIndex)
						{
							case 0:
								x = -1.0;
								y = -1.0;
								break;
							case 1:
								x = 1.0;
								y = -1.0;
								break;
							case 2:
								x = 1.0;
								y = 1.0;
								break;
							case 3:
								x = -1.0;
								y = 1.0;
								break;
							case 4:
								x = 0.0;
								y = -1.0;
								break;
							case 5:
								x = 1.0;
								y = 0.0;
								break;
							case 6:
								x = 0.0;
								y = 1.0;
								break;
							case 7:
								x = -1.0;
								y = 0.0;
								break;
							//case 8:
							//	x = 0.0;
							//	y = 0.0;
							//	break;
							default:
								throw new NotSupportedException();
						}
						break;
					case CellType.TetraLinear:
					case CellType.TetraQuadratic:
						switch (cellPointIndex)
						{
							case 0:
								x = 0.0;
								y = 0.0;
								z = 0.0;
								break;
							case 1:
								x = 1.0;
								y = 0.0;
								z = 0.0;
								break;
							case 2:
								x = 0.0;
								y = 1.0;
								z = 0.0;
								break;
							case 3:
								x = 0.0;
								y = 0.0;
								z = 1.0;
								break;
							case 4:
								x = 0.5;
								y = 0.0;
								z = 0.0;
								break;
							case 5:
								x = 0.5;
								y = 0.5;
								z = 0.0;
								break;
							case 6:
								x = 0.0;
								y = 0.5;
								z = 0.0;
								break;
							case 7:
								x = 0.0;
								y = 0.0;
								z = 0.5;
								break;
							case 8:
								x = 0.5;
								y = 0.0;
								z = 0.5;
								break;
							case 9:
								x = 0.0;
								y = 0.5;
								z = 0.5;
								break;
							default:
								throw new NotSupportedException();
						}
						break;
					case CellType.WedgeLinear:
					case CellType.WedgeQuadratic:
						switch (cellPointIndex)
						{
							case 0:
								x = 0.0;
								y = 0.0;
								z = 0.0;
								break;
							case 1:
								x = 1.0;
								y = 0.0;
								z = 0.0;
								break;
							case 2:
								x = 0.0;
								y = 1.0;
								z = 0.0;
								break;
							case 3:
								x = 0.0;
								y = 0.0;
								z = 1.0;
								break;
							case 4:
								x = 1.0;
								y = 0.0;
								z = 1.0;
								break;
							case 5:
								x = 0.0;
								y = 1.0;
								z = 1.0;
								break;
							case 6:
								x = 0.5;
								y = 0.0;
								z = 0.0;
								break;
							case 7:
								x = 0.5;
								y = 0.5;
								z = 0.0;
								break;
							case 8:
								x = 0.0;
								y = 0.5;
								z = 0.0;
								break;
							case 9:
								x = 0.0;
								y = 0.0;
								z = 0.5;
								break;
							case 10:
								x = 1.0;
								y = 0.0;
								z = 0.5;
								break;
							case 11:
								x = 0.0;
								y = 1.0;
								z = 0.5;
								break;
							case 12:
								x = 0.5;
								y = 0.0;
								z = 1.0;
								break;
							case 13:
								x = 0.5;
								y = 0.5;
								z = 1.0;
								break;
							case 14:
								x = 0.0;
								y = 0.5;
								z = 1.0;
								break;
							default:
								throw new NotSupportedException();
						}
						break;
					case CellType.HexaLinear:
					case CellType.HexaQuadratic:
						switch (cellPointIndex)
						{
							case 0:
								x = -1.0;
								y = -1.0;
								z = -1.0;
								break;
							case 1:
								x = 1.0;
								y = -1.0;
								z = -1.0;
								break;
							case 2:
								x = 1.0;
								y = 1.0;
								z = -1.0;
								break;
							case 3:
								x = -1.0;
								y = 1.0;
								z = -1.0;
								break;
							case 4:
								x = -1.0;
								y = -1.0;
								z = 1.0;
								break;
							case 5:
								x = 1.0;
								y = -1.0;
								z = 1.0;
								break;
							case 6:
								x = 1.0;
								y = 1.0;
								z = 1.0;
								break;
							case 7:
								x = -1.0;
								y = 1.0;
								z = 1.0;
								break;

							case 8:
								x = 0.0;
								y = -1.0;
								z = -1.0;
								break;
							case 9:
								x = 1.0;
								y = 0.0;
								z = -1.0;
								break;
							case 10:
								x = 0.0;
								y = 1.0;
								z = -1.0;
								break;
							case 11:
								x = -1.0;
								y = 0.0;
								z = -1.0;
								break;
							case 12:
								x = -1.0;
								y = -1.0;
								z = 0.0;
								break;
							case 13:
								x = 1.0;
								y = -1.0;
								z = 0.0;
								break;
							case 14:
								x = 1.0;
								y = 1.0;
								z = 0.0;
								break;
							case 15:
								x = -1.0;
								y = 1.0;
								z = 0.0;
								break;
							case 16:
								x = 0.0;
								y = -1.0;
								z = 1.0;
								break;
							case 17:
								x = 1.0;
								y = 0.0;
								z = 1.0;
								break;
							case 18:
								x = 0.0;
								y = 1.0;
								z = 1.0;
								break;
							case 19:
								x = -1.0;
								y = 0.0;
								z = 1.0;
								break;
							default:
								throw new NotSupportedException();
						}
						break;
					case CellType.Undefined:
					case CellType.Point:
					default:
						throw new NotSupportedException();
				}
				return getIndexOfNearestGaussPoint(x, y, z);
			}

			#endregion

			#region Private methods

			private int getDimension()
			{
				switch (ElementType?.ToLower())
				{
					case "linear":
						return 1;
					case "triangle":
					case "quadrilateral":
						return 2;
					case "tetrahedra":
					case "hexahedra":
					case "prism": // wedge
						return 3;
					case "none":
					case "point":
					default:
						throw new NotSupportedException($"This element type is not supported ({ElementType}).");
				}
			}

			private int getIndexOfNearestGaussPoint(double x, double y, double z)
			{
				Debug.Assert(naturalCoordinates?.Length > 0);
				int index = -1;
				double smallestDistanceSquared = double.MaxValue;
				int dimension = naturalCoordinates.GetLength(1);
				for (int i = 0; i < NumberOfGaussPoints; i++)
				{
					double xDiff = naturalCoordinates[i, 0] - x;
					double yDiff = dimension > 1 ? naturalCoordinates[i, 1] - y : 0.0;
					double zDiff = dimension > 2 ? naturalCoordinates[i, 2] - z : 0.0;
					double distanceSquared = xDiff * xDiff + yDiff * yDiff + zDiff * zDiff;
					if (distanceSquared < smallestDistanceSquared)
					{
						smallestDistanceSquared = distanceSquared;
						index = i;
					}
				}
				return index;
			}

			#endregion

		}

		private static void convertValues(IReadOnlyList<double> values, IReadOnlyList<int> ids, int numberOfComponents, GeometryDescription geometry, DataLocationType targetLocation, FileDataLocation fileLocation, GaussPointsInfo gaussPoints, GaussPointsExtrapolationStrategy gaussPointsExtrapolationStrategy, double[] result)
		{
			// Place values to appropriate position according to PointIdIndexMap resp. CellIdIndexMap (depending on nodes or gauss-points data location)
			// Do extrapolation if Location is Gauss-points

			Debug.Assert((fileLocation == FileDataLocation.GaussPoints) == (gaussPoints != null));
			Debug.Assert(values.Count == ids.Count * numberOfComponents * (gaussPoints?.NumberOfGaussPoints ?? 1));

			switch (targetLocation)
			{
				case DataLocationType.Points:
					switch (fileLocation)
					{
						case FileDataLocation.Nodes:
							{
								var mapping = (ImportGeometryEntityMapping)geometry.Mapping;
								for (int idIndex = 0; idIndex < ids.Count; idIndex++)
								{
									int pointId = ids[idIndex];
									int pointIndex;
									if (mapping.TryGetNewPointId(pointId, out pointIndex))
									{
										for (int componentIndex = 0; componentIndex < numberOfComponents; componentIndex++)
										{
											result[pointIndex * numberOfComponents + componentIndex] = values[idIndex * numberOfComponents + componentIndex];
										}
									}
								}
							}
							break;
						case FileDataLocation.GaussPoints:
							
							double[] cellPointResult = createEmptyValueArray(geometry, DataLocationType.CellPoints, numberOfComponents);
							// recursive call to calculate CellPoints values
							convertValues(values, ids, numberOfComponents, geometry, DataLocationType.CellPoints, fileLocation, gaussPoints, gaussPointsExtrapolationStrategy, cellPointResult);

							var map = new List<KeyValuePair<double, double[]>>[geometry.NumberOfPoints];

							int startOffset = 0;
							for (int cellIndex = 0; cellIndex < geometry.NumberOfCells; cellIndex++)
							{
								double cellVolume = computeCellVolume(cellIndex, geometry);
								int endOffset = geometry.CellOffsets[cellIndex];
								for (int offset = startOffset; offset < endOffset; offset++)
								{
									int pointIndex = geometry.CellConnectivity[offset];
									if (map[pointIndex] == null)
										map[pointIndex] = new List<KeyValuePair<double, double[]>>();
									map[pointIndex].Add(new KeyValuePair<double, double[]>(cellVolume, cellPointResult.CreateSlice(offset * numberOfComponents, numberOfComponents)));
								}
								startOffset = endOffset;
							}

							for (int pointIndex = 0; pointIndex < geometry.NumberOfPoints; pointIndex++)
							{
								var list = map[pointIndex];
								if (list != null)
								{
									for (int componentIndex = 0; componentIndex < numberOfComponents; componentIndex++)
									{
										double volumeSum = 0.0;
										double componentVolumeSum = 0.0;
										foreach (var pair in list)
										{
											double cellVolume = pair.Key;
											volumeSum += cellVolume;
											componentVolumeSum += cellVolume * pair.Value[componentIndex];
										}
										result[pointIndex * numberOfComponents + componentIndex] = componentVolumeSum / volumeSum;
									}
								}
							}
							break;
						default:
							throw new NotSupportedException();
					}
					break;
				case DataLocationType.CellPoints:
					switch (fileLocation)
					{
						case FileDataLocation.GaussPoints:

							if (gaussPointsExtrapolationStrategy == GaussPointsExtrapolationStrategy.Nearest)
							{
								var mapping = (ImportGeometryEntityMapping)geometry.Mapping;
								for (int idIndex = 0; idIndex < ids.Count; idIndex++)
								{
									int cellId = ids[idIndex];
									int cellIndex;
									if (mapping.TryGetNewCellId(cellId, out cellIndex))
									{
										int previousCellOffset = (cellIndex > 0) ? geometry.CellOffsets[cellIndex - 1] : 0;
										int cellOffset = geometry.CellOffsets[cellIndex];
										for (int offset = previousCellOffset; offset < cellOffset; offset++)
										{
											int nearestGaussPointIndex = gaussPoints.GetIndexOfNearestGaussPoint(geometry.CellTypes[cellIndex], offset - previousCellOffset);
											for (int componentIndex = 0; componentIndex < numberOfComponents; componentIndex++)
											{
												result[offset * numberOfComponents + componentIndex] = values[idIndex * gaussPoints.NumberOfGaussPoints * numberOfComponents + nearestGaussPointIndex * numberOfComponents + componentIndex];
											}
										}
									}
								}
							}
							else
							{
								throw new NotSupportedException();
							}

							break;
						case FileDataLocation.Nodes:
						default:
							throw new NotSupportedException();
					}
					break;
				case DataLocationType.Cells:
					switch (fileLocation)
					{
						case FileDataLocation.Nodes: // TODO: do arithmetic mean of values in all nodes of a cell
							throw new NotImplementedException();
						case FileDataLocation.GaussPoints: // do arithmetic mean of all values in gauss points if a cell
							{
								int numberOfGaussPoints = gaussPoints.NumberOfGaussPoints;
								var mapping = (ImportGeometryEntityMapping)geometry.Mapping;
								for (int idIndex = 0; idIndex < ids.Count; idIndex++)
								{
									int cellId = ids[idIndex];
									int cellIndex;
									if (mapping.TryGetNewCellId(cellId, out cellIndex))
									{
										for (int componentIndex = 0; componentIndex < numberOfComponents; componentIndex++)
										{
											double aggregate = 0.0;
											for (int gpIndex = 0; gpIndex < numberOfGaussPoints; gpIndex++)
											{
												aggregate += values[idIndex * numberOfGaussPoints * numberOfComponents + gpIndex * numberOfComponents + componentIndex];
											}
											result[cellIndex * numberOfComponents + componentIndex] = aggregate / numberOfGaussPoints;
										}
									}
								}
							}
							break;
						default:
							throw new NotSupportedException();
					}
					break;
				default:
					throw new NotSupportedException();
			}
		}

		private static double computeCellVolume(int cellIndex, GeometryDescription geometry)
		{
			throw new NotImplementedException();
		}

		private static double[] createEmptyValueArray(GeometryDescription geometry, DataLocationType targetLocation, int numberOfComponents)
		{
			int length;
			switch (targetLocation)
			{
				case DataLocationType.Points:
					length = geometry.NumberOfPoints * numberOfComponents;
					break;
				case DataLocationType.CellPoints:
					length = geometry.CellConnectivity.Length * numberOfComponents;
					break;
				case DataLocationType.Cells:
					length = geometry.NumberOfCells * numberOfComponents;
					break;
				default:
					throw new NotSupportedException();
			}

			const double EMPTY_VALUE = double.NaN;
			double[] array = new double[length];
			array.Fill(EMPTY_VALUE);
			//for (int i = 0; i < length; i++)
			//{
			//	array[i] = emptyValue;
			//}
			return array;
		}
	}
}
