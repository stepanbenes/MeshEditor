using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using MeshEditor.Cuts;
using MeshEditor.CoreInterface;

namespace MeshEditor.WinUI
{
	/// <summary>
	/// V tomto dialogovem okne uzivatel vybere oblast, 
	/// ktera ma byt ze site vyriznuta. Rez lze specifikovat pomoci reznych rovin (muze jich byt i vice)
	/// nebo pomoci algebraickeho vyrazu (obecny tvar rezu). 
	/// Po provedeni rezu dojde k vygenerovani noveho povrchu site neobsahujiciho uriznute prvky
	/// a uzivatel muze pracovat s entitami v oblasti rezu.
	/// </summary>
	public partial class CutEditorForm : Form
	{
		private const string INITIAL_EXPRESSION = @"x >= 0.0 and y >= 0.0";
		private static string savedExpression;
		private static CutTest savedTestMethod;

		//static CutEditorForm()
		//{
		//    savedExpression = INITIAL_EXPRESSION;
		//    savedTestMethod = CutTestProvider.ProvideTestFunction(savedExpression);
		//}
		
		// =====================================================================

		public event EventHandler ParentFormFocusNeeded;

		private LongOpNotifier longOpNotifier;
		private SceneFacade sceneFacade;

		public CutEditorForm(SceneFacade sceneFacade, LongOpNotifier longOpNotifier)
		{
			InitializeComponent();

			this.longOpNotifier = longOpNotifier;
			this.sceneFacade = sceneFacade;
			textBoxExpression.Text = INITIAL_EXPRESSION;

			comboBoxAction.SelectedIndex = 0;
			tabControl.SelectedTab = tabPageCuttingPlanes;
			textBoxExpression.CausesValidation = true;
			textBoxExpression.Validating += new CancelEventHandler(textBoxExpression_Validating);
			
			sceneFacade.CutPlaneDefinitionPointsChanged += sceneFacade_CutPlaneDefinitionPointsChanged;
			this.FormClosed += new FormClosedEventHandler(CutEditorForm_FormClosed);
			//this.Shown += delegate { buttonCreateNewCutPlane.Focus(); };

			updateCutPlaneList(); // pokud mesh obsahuje nejake rezne plochy, tak je nacist

			comboBoxCutType.DataSource = System.Enum.GetValues(typeof(CutInfo.CutMeshByPlanesType));
			comboBoxCutType.SelectedItem = CutInfo.CutMeshByPlanesType.Intersection;

		}

		private void CutEditorForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			sceneFacade.CutPlaneDefinitionPointsChanged -= sceneFacade_CutPlaneDefinitionPointsChanged;
			SceneFacade.EditorMode = EditorMode.Orbit;
			
			sceneFacade.CutPlanes.Clear(); // hide cut planes
			
			sceneFacade.PerformAction(AvailableAction.ClearPlaneDefinitionPoints); // tady se mimojine provede Refresh
		}

		private void textBoxExpression_Validating(object sender, CancelEventArgs e)
		{
			if (!compileExpression(textBoxExpression.Text)) // pokud se nepodari kompilace (jsou tam chyby), tak zrusit validaci
				e.Cancel = true;
		}

		private bool compileExpression(string expression)
		{
			bool ook = false;
			string errorText = string.Empty;
			try
			{
				savedTestMethod = CutTestProvider.ProvideTestFunction(expression);
				savedExpression = expression;
				ook = true;
			}
			catch (RuntimeCompilationException)
			{
				errorText = "Please insert valid expression." + Environment.NewLine + "There were some errors while processing expression.";
			}
			catch (Exception)
			{
				errorText = "Please insert valid expression.";
			}
			if (!ook) // error
			{
				Color temp = textBoxExpression.BackColor;
				textBoxExpression.BackColor = Color.Red;
				MessageBox.Show(errorText, "Inserted expression is invalid", MessageBoxButtons.OK, MessageBoxIcon.Error);
				textBoxExpression.BackColor = temp;
			}
			return ook;
		}

		private void precomputeMinimalElementRadius()
		{
			Cursor temp = this.Cursor;
			this.Cursor = Cursors.WaitCursor;
			// predvypocitat minimal element radius
			object minimalElementRadius = sceneFacade.GetValue(AvailableValue.MinimalElementRadius);
			this.Cursor = temp;
		}

		private void buttonDoIt_Click(object sender, EventArgs e)
		{
			CutInfo cutInfo = new CutInfo();
			switch (comboBoxAction.SelectedIndex) // vyber akci
			{
				case 0: // Cut
					cutInfo.Action = CutInfo.ActionType.Cut;
					break;
				case 1: // Select elements
					cutInfo.Action = CutInfo.ActionType.SelectElements;
					break;
				case 2: // Select nodes
					cutInfo.Action = CutInfo.ActionType.SelectNodes;
					break;
				case 3: // Select faces
					cutInfo.Action = CutInfo.ActionType.SelectFaces;
					break;
				case 4: // Select edges
					cutInfo.Action = CutInfo.ActionType.SelectEdges;
					break;
				case 5: // Select beams
					cutInfo.Action = CutInfo.ActionType.SelectBeams;
					break;
				default:
					throw new NotSupportedException();
			}
			// =================================================
			if (checkBoxFullEntityMatch.Checked)
				cutInfo.HitDecision = CutInfo.ItemHitDecision.AllNodes;
			else
				cutInfo.HitDecision = CutInfo.ItemHitDecision.SomeNodes;
			// =================================================
			if (tabControl.SelectedTab == tabPageExpression) // rez podle expression
			{
				if (savedExpression == null)
				{
					if (!compileExpression(textBoxExpression.Text))
						return;
				}
				cutInfo.CutTestMethod = savedTestMethod;
			}
			else if(tabControl.SelectedTab == tabPageCuttingPlanes) // rez podle rezacich ploch
			{
				cutInfo.CutTestMethod = null;
				cutInfo.Options = (CutInfo.CutMeshByPlanesType)comboBoxCutType.SelectedValue;
			}
			else
				throw new NotSupportedException();
			// ---------------------------------------------------
			doCut(cutInfo);
		}

		private void doCut(CutInfo cutInfo)
		{
			Cursor savedCursor = this.Cursor;
			this.Cursor = Cursors.WaitCursor;
			using (longOpNotifier.Begin("Creating cut through mesh"))
			{
				// --------------------------------
				this.sceneFacade.PerformAction(AvailableAction.CutMesh, cutInfo);
				// --------------------------------
			}
			this.Cursor = savedCursor;
		}

		private void buttonClose_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void buttonCreateCutPlane_Click(object sender, EventArgs e)
		{
			precomputeMinimalElementRadius();
			// -----------------------------------------------------------------------
			if (sceneFacade.CutPlaneDefinitionPointsCount < 2) // jdi vybirat body
			{
				// vymaz stavajici body
				sceneFacade.PerformAction(AvailableAction.ClearPlaneDefinitionPoints);
				// napis hlasku
				listBoxCutPlanes.Items.Add("Select 2 or 3 plane definition points on mesh.");
				listBoxCutPlanes.SelectedItems.Clear();
				buttonInsertNextPoint_Click(null, null); // nastav vybiraci mod
			}
			else if (sceneFacade.CutPlaneDefinitionPointsCount == 2)
			{
				createNewCutPlane();
			}
		}

		private void buttonInsertNextPoint_Click(object sender, EventArgs e)
		{
			SceneFacade.EditorMode = EditorMode.PickCuttingPlanePoint;
			if (ParentFormFocusNeeded != null)
				ParentFormFocusNeeded(this, EventArgs.Empty);
			listBoxCutPlanes.SelectedIndices.Clear();
			listBoxCutPlanes_SelectedIndexChanged(null, null); // odoznac plochy
		}

		private void sceneFacade_CutPlaneDefinitionPointsChanged(object sender, EventArgs e)
		{
			if (tabControl.SelectedTab != tabPageCuttingPlanes) // pokud jsem na jiny zalozce, tak hned vratit
				tabControl.SelectedTab = tabPageCuttingPlanes;

			switch (sceneFacade.CutPlaneDefinitionPointsCount)
			{
				case 0:
					buttonInsertNextPoint.Visible = false;
					break;
				case 1:
					buttonInsertNextPoint.Visible = true;
					listBoxCutPlanes.SelectedIndices.Clear();
					listBoxCutPlanes_SelectedIndexChanged(null, null); // odoznac plochy
					break;
				case 2:
					buttonInsertNextPoint.Visible = true;
					break;
				case 3:
					createNewCutPlane();
					break;
			}			
		}
		
		private void buttonDeleteSelectedPlanes_Click(object sender, EventArgs e)
		{
			Predicate<CutPlane> allSelected = delegate(CutPlane plane)
			{
				return plane.IsSelected;
			};
			sceneFacade.CutPlanes.RemoveAll(allSelected);
			sceneFacade.PerformAction(AvailableAction.Refresh);
			updateCutPlaneList();
		}
		
		private void createNewCutPlane()
		{
			buttonInsertNextPoint.Visible = false;
			sceneFacade.PerformAction(AvailableAction.CreateCutPlane);
			updateCutPlaneList();
			listBoxCutPlanes.SelectedIndex = listBoxCutPlanes.Items.Count - 1;
		}

		private void updateCutPlaneList()
		{
			listBoxCutPlanes.Items.Clear();
			for (int i = 0; i < sceneFacade.CutPlanes.Count; i++)
			{
				listBoxCutPlanes.Items.Add(sceneFacade.CutPlanes[i].ToString());
				//if (sceneFacade.CutPlanes[i].IsSelected)
				//    listBoxCutPlanes.SelectedIndices.Add(i);
				sceneFacade.CutPlanes[i].IsSelected = false;
			}
			listBoxCutPlanes.SelectedIndices.Clear();
			buttonInvertSelectedPlanes.Visible = false;
			buttonDeleteSelectedPlanes.Visible = false;

			if (sceneFacade.CutPlanes.Count > 1)
				comboBoxCutType.Visible = labelCutType.Visible = true;
			else
				comboBoxCutType.Visible = labelCutType.Visible = false;
		}

		private void listBoxCutPlanes_SelectedIndexChanged(object sender, EventArgs e)
		{
			for (int i = 0; i < sceneFacade.CutPlanes.Count; i++)
				sceneFacade.CutPlanes[i].IsSelected = (listBoxCutPlanes.SelectedIndices.Contains(i));
			sceneFacade.PerformAction(AvailableAction.Refresh);
			if (listBoxCutPlanes.SelectedIndices.Count > 0)
			{
				buttonInvertSelectedPlanes.Visible = true;
				buttonDeleteSelectedPlanes.Visible = true;
			}
			else
			{
				buttonInvertSelectedPlanes.Visible = false;
				buttonDeleteSelectedPlanes.Visible = false;
			}
		}

		private void buttonInvertSelectedPlanes_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < sceneFacade.CutPlanes.Count; i++)
			{
				if (sceneFacade.CutPlanes[i].IsSelected)
				{
					sceneFacade.CutPlanes[i].Invert();
					listBoxCutPlanes.Items[i] = sceneFacade.CutPlanes[i].ToString();
				}
			}
			sceneFacade.PerformAction(AvailableAction.Refresh);
		}

		private void comboBoxAction_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (comboBoxAction.SelectedIndex) // vyber akci
			{
				case 0: // Cut
					buttonDoIt.Text = "Cut";
					break;
				default: // Select
					buttonDoIt.Text = "Select";
					break;
			}
		}

		private void buttonRestoreMesh_Click(object sender, EventArgs e)
		{
			buttonRestoreMesh.Enabled = false;
			buttonRestoreMesh.Refresh();
			Cursor savedCursor = this.Cursor;
			this.Cursor = Cursors.WaitCursor;
			using (longOpNotifier.Begin("Restoring mesh"))
			{
				// --------------------------------
				sceneFacade.PerformAction(AvailableAction.RestoreMesh);
				// --------------------------------
			}
			this.Cursor = savedCursor;
			buttonRestoreMesh.Enabled = true;
		}

		private void pictureBoxHelp_Click(object sender, EventArgs e)
		{
			string text = @"Available variables:
	x y z
Available relational operators:
	== != > < >= <=
Available operations or functions:
	+ - * / and or
	pow(base,exp) log(base,arg) min(a,b) max(a,b) 
	sin() cos() tan() abs() sqrt() 
	asin() acos() atan() sinh() cosh() tanh()";

			MessageBox.Show(text, "Expression input help", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

	}
}
