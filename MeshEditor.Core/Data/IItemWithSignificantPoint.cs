using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace MeshEditor.Data
{
	/// <summary>
	/// toto rozhrani implementuji objekty u nichz je zadouci byti reprezentovan jednim bodem
	/// (napriklad pro urceni souradnic, do jake casti site ma byt prvek vlozen)
	/// </summary>
	public interface IItemWithSignificantPoint
	{
		Vector3 GetSignificantPoint();
	}
}
