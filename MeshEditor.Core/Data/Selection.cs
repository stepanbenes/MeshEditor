using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;

namespace MeshEditor.Data
{
	/// <summary>
	/// toto rozhrani musi implementovat kazdy objekt, ktery ma byt vlozen do mnoziny vyberu.
	/// vynuti se tim mimojine, aby objekt obsahoval vlastnost, kterou lze precist ci nastavit
	/// </summary>
	public interface ISelectable
	{
		/// <summary>
		/// hodnota vlastnosti
		/// </summary>
		Property Property { get; set; }
		
		// -----------------------------
		
		/// <summary>
		/// priznak urcujici, zda dany objekt muze obsahovat vice vlastnosti nebo jen jednu (tyka se uzlu)
		/// </summary>
		bool ContainsMultipleProperties { get; }
		
		/// <summary>
		/// smaze naposledy prirazenou vlastnost.
		/// urceno pro objekty ktere obsahuji vice vlastnoti (uzly)
		/// </summary>
		void RemoveLastProperty();
	}

	/// <summary>
	/// selekcni mod (determinovan poctem stisknuti mysi)
	/// </summary>
	public enum SelectMode
	{
		None = 0, // cisla jsou dulezita - predstavuji zaroven pocet kliku mysi
		Single = 1,
		NearSurface = 2,
		ExtendedSurface = 3,
		Object = 4
	}

	/// <summary>
	/// typ selekcni operace - jak se maji k sobe zachovat predchozi oznacena mnozina entit a nove oznacena mnozina entit
	/// (ovlivne stisknutim shift nebo ctrl)
	/// </summary>
	public enum SelectOperationType
	{
		New,
		Union,				// OR
		Except,				// minus
		Intersection,		// AND
		SymetricDifference	// XOR
	}

	/// <summary>
	/// typ entity, ktera ma byt vybrana
	/// </summary>
	public enum ItemTypeToSelect
	{
		Node,
		Edge, // edge on face or 2D element
		Face, // face of 3D element
		Element, // 2D or 3D element
		Beam // 1D element
	}
}
