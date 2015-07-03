using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;
using MeshEditor.IO;
using MeshEditor.CoreInterface;

namespace MeshEditor.IO
{
	/// <summary>
	/// rozhrani poskytujici udalost informujici o postupu nejake akce
	/// </summary>
	public interface IProgressNotifier
	{
		event MeshIOEventHandler Step;
	}

	/// <summary>
	/// rozhrani, ktere bude poskytovat objekt majici za ukol vytvorit sit.
	/// pro vytvoreni je pozadovano predat objekt pro parsovani vstupniho souboru
	/// </summary>
	public interface IMeshCreator : IProgressNotifier
	{
		Mesh CreateMesh(IMeshFileParser meshFileParser, YesNoQuestion cancelled);
	}

	/// <summary>
	/// rozhrani urcene pro objekt majici za ukol ulozit sit do souboru
	/// </summary>
	public interface IMeshSaver : IProgressNotifier
	{
		/// <summary>
		/// Saves mesh to file.
		/// </summary>
		/// <param name="mesh">Mesh object to save</param>
		/// <param name="filename">Destination filename</param>
		/// <param name="saveWithoutCuttedElements">Indicates whether to save deleted element or save the mesh in uncutted form</param>
		/// <param name="cancelled">Cancellation handler</param>
		void SaveMesh(Mesh mesh, string filename, bool saveWithoutCuttedElements, YesNoQuestion cancelled); // used for saving mesh to file

		/// <summary>
		/// Used for converting between various file formats.
		/// </summary>
		/// <param name="fileParser">Generic mesh file loader</param>
		/// <param name="destination">Destination filename</param>
		/// <param name="cancelled">Cancellation handler</param>
		void SaveMesh(IMeshFileParser fileParser, string destination, YesNoQuestion cancelled);
	}

	/// <summary>
	/// Object with this interface can load and parse mesh-file.
	/// Enables iterating through nodes and elements in file,
	/// encapsulates inner structure and logic in file loading.
	/// </summary>
	public interface IMeshFileParser : IDisposable
	{
		/// <summary>
		/// Gets path and name of mesh source-file
		/// </summary>
		string Filename { get; }

		/// <summary>
		/// Gets number of nodes in file
		/// </summary>
		int NodeCount { get; }

		/// <summary>
		/// Enumerates through all nodes in file
		/// </summary>
		IEnumerable<Node> ReadNodes();

		/// <summary>
		/// Gets number of elements in file
		/// </summary>
		int ElementCount { get; }

		/// <summary>
		/// Enumerates through all elements in file
		/// </summary>
		IEnumerable<ElementDraft> ReadElements();

		/// <summary>
		/// Gets current position in file
		/// </summary>
		int CurrentLineNumber { get; }
	}

	/// <summary>
	/// interface for parsing default file format
	/// </summary>
	public interface IDefaultFileFormatParser : IMeshFileParser
	{
		/// <summary>
		/// Gets number of faces in file
		/// </summary>
		int FaceCount { get; }

		/// <summary>
		/// Enumerates through all faces with some property in file
		/// </summary>
		IEnumerable<FaceDraft> ReadFaces();

		/// <summary>
		/// Gets number of edges in file
		/// </summary>
		int EdgeCount { get; }

		/// <summary>
		/// Enumerates through all edges with some property in file
		/// </summary>
		IEnumerable<EdgeDraft> ReadEdges();

		/// <summary>
		/// Gets current line in text form if possible
		/// </summary>
		string CurrentLine { get; }

		/// <summary>
		/// Fires when some line is skipped - e.g. comment line
		/// </summary>
		event EventHandler LineWasSkipped;

		/// <summary>
		/// Reads stream to end and fires LineWasSkipped event after each line.
		/// </summary>
		void ReadToEnd();

		/// <summary>
		/// Reads next line in stream and returns this line
		/// </summary>
		/// <returns></returns>
		string ReadNextLine();

	}
}
