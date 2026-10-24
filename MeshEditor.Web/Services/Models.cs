namespace MeshEditor.Web.Services;

public sealed record UserInfo(string Id, string Name);

public sealed record SolutionSummary(int Id, string ProjectName, int LayerCount, string[] ResultFiles, string Location);

public sealed record SolutionDetail(int Id, string ProjectName, string Location, LayerNode[] Layers);

public sealed record LayerNode(Guid Id, string Name, string? FilterType, LayerNode[] Children);

public sealed record LayerDetail(Guid Id, string Name, Guid? ParentId, string? FilterType, string? Filter, MeshInfo[] Meshes, FieldInfo[] Fields, long Stored);

public sealed record MeshInfo(int Index, decimal[] TimeSteps);

public sealed record FieldInfo(string Name, string[] Components, decimal[] TimeSteps);

public sealed record CreatedItem(int Id, string ProjectName);

public sealed record PickedFile(string Name, string RelativePath, long Size);

/// <summary>
/// What the 3D viewer reports after showing a layer.
/// </summary>
public sealed record ViewSummary(int Points, int Cells, int Triangles, double? Min, double? Max, double Scale, float[]? Bounds, string? Error);

public sealed class RemoteStorageException(string message, bool cancelled) : Exception(message)
{
	public bool Cancelled { get; } = cancelled;
}
