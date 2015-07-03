using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;
using Wintellect.PowerCollections;
using MeshEditor.Graphics;
using MeshEditor.Utilities;

namespace MeshEditor.UndoRedo
{
	/// <summary>
	/// abstraktni trida pro memento uchovavajici stav objektu site (objekt typu Mesh)
	/// </summary>
	public abstract class MeshMemento : IMemento<Mesh>
	{

		public static Camera TempCameraBox;
		
		// -------------------------------------------------

		protected Camera viewstate;

		public Camera Viewstate
		{
			get { return viewstate; }
		}

		public MeshMemento(Camera viewstate)
		{
			this.viewstate = viewstate.Clone();
		}

		#region IMemento<Mesh> Members

		public abstract IMemento<Mesh> Restore(Mesh target);

		#endregion

	}
	
	/// <summary>
	/// memento uchovavajici stav objektu pred operaci selekce entit
	/// </summary>
	public class SelectionMemento : MeshMemento
	{
		private Set<ISelectable> selectedItems;

		public int SelectedItemsCount
		{
			get { return selectedItems.Count; }
		}

		public SelectionMemento(Set<ISelectable> selectedItems, Camera viewstate)
			: base(viewstate)
		{
			this.selectedItems = selectedItems;
		}

		public override IMemento<Mesh> Restore(Mesh target)
		{
			IMemento<Mesh> inverse = new SelectionMemento(target.SelectedItems, TempCameraBox);
			TempCameraBox = viewstate;
			target.SelectedItems = selectedItems;
			target.UpdateColors();
			return inverse;
		}
	}

	/// <summary>
	/// memento slouzici k uchovani stavu pred inverzi normalovych vektoru ploch prvku
	/// </summary>
	public class InvertNormalsMemento : MeshMemento
	{
		public InvertNormalsMemento(Camera viewstate)
			: base(viewstate)
		{ }

		public override IMemento<Mesh> Restore(Mesh target)
		{
			IMemento<Mesh> inverse = new InvertNormalsMemento(TempCameraBox);
			TempCameraBox = viewstate;
			target.InvertAllNormals();
			return inverse;
		}
	}

	/// <summary>
	/// memento uchovavajici stav objektu pred operaci nastaveni vlastnosti vybranym entitam
	/// </summary>
	public class SetPropertyMemento : MeshMemento
	{
		private Property propertyToSet;
		private Property[] propertiesOfSelected;

		public SetPropertyMemento(Property property, Set<ISelectable> selectedItems, Camera viewstate)
			: base(viewstate)
		{
			this.propertyToSet = property;
			propertiesOfSelected = new Property[selectedItems.Count];
			int index = 0;
			foreach (ISelectable item in selectedItems)
				propertiesOfSelected[index++] = item.Property;
		}

		public override IMemento<Mesh> Restore(Mesh target)
		{
			IMemento<Mesh> inverse = new RestorePropertyMemento(propertyToSet, TempCameraBox);
			TempCameraBox = viewstate;
			int index = 0;
			foreach (ISelectable item in target.SelectedItems)
			{
				if (item.ContainsMultipleProperties) // pokud polozka obsahuje vice property,
					item.RemoveLastProperty(); // tak se tyto nabaluji pri nastavovani na konec pole, staci je tedy odmazat z konce
				else
					item.Property = propertiesOfSelected[index];
				index++;
			}
			return inverse;
		}
	}

	/// <summary>
	/// memento uchovavajici stav pred operaci navraceni puvodnich hodnot vlastnosti vybranych entit
	/// </summary>
	public class RestorePropertyMemento : MeshMemento
	{
		private Property property;

		public RestorePropertyMemento(Property property, Camera viewstate)
			: base(viewstate)
		{
			this.property = property;
		}

		public override IMemento<Mesh> Restore(Mesh target)
		{
			IMemento<Mesh> inverse = new SetPropertyMemento(property, target.SelectedItems, TempCameraBox);
			TempCameraBox = viewstate;
			// nastavit property vybranych polozek
			foreach (ISelectable item in target.SelectedItems)
				item.Property = property;
			return inverse;
		}
	}

	/// <summary>
	/// memento pro uchovani stavu pred pridanim vlastnosti vybranym uzlum
	/// </summary>
	public class AddPropertyToSelectedNodesMemento : MeshMemento
	{
		private Property propertyToSet;
		
		public AddPropertyToSelectedNodesMemento(Property property, Camera viewstate)
			: base(viewstate)
		{
			this.propertyToSet = property;
		}

		public override IMemento<Mesh> Restore(Mesh target)
		{
			IMemento<Mesh> inverse = new RemovePropertyFromSelectedNodesMemento(propertyToSet, TempCameraBox);
			TempCameraBox = viewstate;
			foreach (ISelectable item in target.SelectedItems)
			{
				Node node = item as Node;
				if (node != null)
					item.RemoveLastProperty();
			}
			return inverse;
		}
	}

	/// <summary>
	/// memento slouzici k uchovani objektu pred odebranim vlastnosti vybranych uzlu
	/// </summary>
	public class RemovePropertyFromSelectedNodesMemento : MeshMemento
	{
		private Property property;

		public RemovePropertyFromSelectedNodesMemento(Property property, Camera viewstate)
			: base(viewstate)
		{
			this.property = property;
		}

		public override IMemento<Mesh> Restore(Mesh target)
		{
			IMemento<Mesh> inverse = new AddPropertyToSelectedNodesMemento(property, TempCameraBox);
			TempCameraBox = viewstate;
			// nastavit property vybranych polozek
			foreach (ISelectable item in target.SelectedItems)
			{
				if (item is Node)
					item.Property = property;
			}
			return inverse;
		}
	}

	/// <summary>
	/// memento pro uchovani stavu pred skrytim ci obnovenim mnoziny prvku
	/// </summary>
	public class HideRestoreElementsMemento : MeshMemento
	{
		private bool selectFacesOnCut, currentSelectState;
		private Element[] hiddenElements;

		public HideRestoreElementsMemento(Set<Element> hiddenElements, bool selectFacesOnCut, bool currentSelectState, Camera viewstate)
			: base(viewstate)
		{
			this.selectFacesOnCut = selectFacesOnCut;
			this.currentSelectState = currentSelectState;
			this.hiddenElements = hiddenElements.ToArray();
		}

		public override IMemento<Mesh> Restore(Mesh target)
		{
			IMemento<Mesh> inverse = new HideRestoreElementsMemento(target.HiddenElements, selectFacesOnCut, !currentSelectState, TempCameraBox);
			TempCameraBox = viewstate;

			Set<Element> wereHiddenSet = new Set<Element>(hiddenElements);
			Set<Element> toHide = new Set<Element>();
			Set<Element> toRestore = new Set<Element>();

			foreach (Element e in target.Elements)
			{
				bool wasHidden = wereHiddenSet.Contains(e);
				bool isHidden = target.HiddenElements.Contains(e);
				if (wasHidden && !isHidden)
					toHide.Add(e);
				else if (!wasHidden && isHidden)
					toRestore.Add(e);
			}

			// --------------------------------------------------------
			Cuts.Cutter.HideRestoreElements(target, toHide, toRestore, selectFacesOnCut && !currentSelectState);
			// --------------------------------------------------------
			return inverse;
		}
	}

}
