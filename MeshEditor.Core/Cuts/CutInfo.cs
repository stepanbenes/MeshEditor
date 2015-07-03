using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;

namespace MeshEditor.Cuts
{
	/// <summary>
	/// trida obsahujici vsechny informace potrebne pro provedeni rezu
	/// </summary>
	public class CutInfo
	{

		#region Enums

		/// <summary>
		/// zpusob nakladani s vice reznymi plochami (prunik ci sjednoceni poloprostoru jimi urcenych?)
		/// </summary>
		public enum CutMeshByPlanesType
		{
			Intersection = 0, // prunik
			Union = 1 // sjednoceni
		}

		/// <summary>
		/// typ provedene akce (rez nebo selekce entit?)
		/// </summary>
		public enum ActionType
		{
			Cut = 0,
			SelectElements = 1,
			SelectNodes = 2,
			SelectFaces = 3,
			SelectEdges = 4,
			SelectBeams = 5,
			ShowHideElements = 6
		}

		/// <summary>
		/// jak striktni bude rozhodnuti o uriznuti entity (vsechny uzly v oblasti rezu, nebo staci jeden?)
		/// </summary>
		public enum ItemHitDecision
		{
			AllNodes = 0, // vsechny uzly musi byt v rezaci oblasti, aby se objekt povazoval za zasah
			SomeNodes = 1 // staci aby byl jeden uzel objektu v oblasti
		}

		#endregion

		private CutTest cutTestMethod;
		private CutMeshByPlanesType options;
		private ActionType action;
		private ItemHitDecision hitDecision;
		private Property[] elementPropertiesToShow;
		private ElementType[] elementTypesToShow;
		private DataValueRange valueLimit;

		public Property[] ElementPropertiesToShow
		{
			get { return elementPropertiesToShow; }
			set { elementPropertiesToShow = value; }
		}
		
		public ElementType[] ElementTypesToShow
		{
			get { return elementTypesToShow; }
			set { elementTypesToShow = value; }
		}

		public DataValueRange ValueLimit
		{
			get { return valueLimit; }
			set { valueLimit = value; }
		}

		public ItemHitDecision HitDecision
		{
			get { return hitDecision; }
			set { hitDecision = value; }
		}

		public ActionType Action
		{
			get { return action; }
			set { action = value; }
		}

		public CutMeshByPlanesType Options
		{
			get { return options; }
			set { options = value; }
		}

		public CutTest CutTestMethod
		{
			get { return cutTestMethod; }
			set { cutTestMethod = value; }
		}

		public CutInfo()
		{
			this.hitDecision = ItemHitDecision.SomeNodes;
			this.action = ActionType.Cut;
			this.options = CutMeshByPlanesType.Intersection;
			this.cutTestMethod = null;
			this.elementPropertiesToShow = null;
			this.elementTypesToShow = null;
			this.valueLimit = null;
		}
	}
}
