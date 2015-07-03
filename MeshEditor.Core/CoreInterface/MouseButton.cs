using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// Specifies constants that define which mouse button was pressed.
	/// </summary>
	[Flags]
	public enum MouseButton
	{
		// Summary:
		//     No mouse button was pressed.
		None = 0,
		//
		// Summary:
		//     The left mouse button was pressed.
		Left = 1048576,
		//
		// Summary:
		//     The right mouse button was pressed.
		Right = 2097152,
		//
		// Summary:
		//     The middle mouse button was pressed.
		Middle = 4194304,
		//
		// Summary:
		//     The first XButton was pressed.
		XButton1 = 8388608,
		//
		// Summary:
		//     The second XButton was pressed.
		XButton2 = 16777216,
	}
}
