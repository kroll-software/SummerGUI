using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using KS.Foundation;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL; // Für IGLContext

namespace SummerGUI
{
	public interface IGUIContext
	{
		NativeWindow GlWindow { get; }
		int TitleBarHeight { get; }
		//GL GL { get; }

		Devices Device { get; set; }
		int DPI { get; }
		float ScaleFactor { get; }

		int Width { get; }
		int Height { get; }
		int OriginalWidth { get; }
		int OriginalHeight { get; }
		Color BackColor  { get; }
		void Invalidate();
		void Invalidate (int frames);
		void SetCursor (Cursors cursor);
		void SetCustomCursor (string name);
		Rectangle Bounds { get; }		
		AnimationService Animator { get; }

		public int DeltaTicks { get; }

		ClipBoundStackClass ClipBoundStack { get; }
		MenuManager MenuManager { get; }
		IRootController Controller { get; }

		GUIRenderBatcher Batcher { get; }
	}

	/***
	public interface IApplication
	{		
		string GetSetting(string key);
		void SaveSetting(string key, string value);
	}
	***/
		
	public interface IApplicationWindow
	{
		/***
		void ShowInfo(string message);
		void ShowWarning(string message);
		void ShowError(string message);
		DialogResults ShowQuestion(string message);
		***/
	}

	public interface IChildFormHost
	{
		//IGUIContext GUIContext { get; }
		//SummerGUIWindow ParentWindow { get; }
		//RootContainer Controls { get; }
		void OnOK();
		void OnCancel();
		bool AllowMinimize { get; }
		bool AllowMaximize { get; }
		bool IsModal { get; }
		//Container ButtonBar { get; }
		//ToolBar Tools { get; }
		//NotificationPanel Notifications { get; }
		DialogResults Result { get; }
		void ShowDialog(SummerGUIWindow parent);
	}

	public interface IChildForm
	{
		IChildFormHost Host { get; set; }
		bool Validate ();
		void OnOK ();
	}

	public interface IStatusPresenter
	{	
		void ShowStatus ();
		void ShowStatus (string message, bool waitCursor, bool useStack = true);
		void ClearStatus();
	}		

	public interface IGuiMenuWidget
	{	
		
	}

	// Implemented by Widget and called internally
	public interface IGuiMenuInterface
	{
		void UpdateMenus ();
	}

	[GuiMenuInterface]
	public interface ISupportsClipboard : IGuiMenuInterface
	{
		bool CanCopy { get; }
		bool CanPaste { get; }
		bool CanCut { get; }
		bool CanDelete { get; }

		void Copy ();
		void Paste ();
		void Cut ();
		void Delete ();
	}

	[GuiMenuInterface]
	public interface ISupportsUndoRedo : IGuiMenuInterface
	{
		bool CanUndo { get; }
		bool CanRedo { get; }

		void Undo ();
		void Redo ();
	}

	[GuiMenuInterface]
	public interface ISupportsSelection : IGuiMenuInterface
	{
		bool CanSelectAll { get; }
		bool CanInvertSelection { get; }

		void SelectAll ();
		void InvertSelection();
	}

	[GuiMenuInterface]
	public interface ISupportsFind : IGuiMenuInterface
	{
		bool CanFind { get; }
		bool CanFindNext { get; }
		bool CanFindPrevious { get; }

		void Find ();
		void FindNext();
		void FindPrevious();
	}

	[GuiMenuInterface]
	public interface ISupportsNavigation : IGuiMenuInterface
	{
		bool CanStepBack { get; }
		bool CanStepForward { get; }

		void StepBack ();
		void StepForward();
	}

	[GuiMenuInterface]
	public interface ISupportsMove : IGuiMenuInterface
	{
		bool CanMoveFirst { get; }
		bool CanMovePrevious { get; }
		bool CanMoveNext { get; }
		bool CanMoveLast { get; }

		void MoveFirst ();
		void MovePrevious ();
		void MoveNext ();
		void MoveLast ();
	}

	[GuiMenuInterface]
	public interface ISupportsShift : IGuiMenuInterface
	{
		bool CanShiftUp { get; }
		bool CanShiftDown { get; }
		bool CanShiftLeft { get; }
		bool CanShiftRight { get; }

		void ShiftUp ();
		void ShiftDown ();
		void ShiftLeft ();
		void ShiftRight ();
	}

	[GuiMenuInterface]
	public interface ISupportsPersistency : IGuiMenuInterface
	{
		bool CanNew { get; }
		bool CanOpen { get; }
		bool CanClose { get; }
		bool CanSave { get; }
		bool CanSaveAs { get; }

        /***
		void New ();
		void Open ();
		void Close ();
		void Save ();
		void SaveAs ();
		***/       
	}

	[GuiMenuInterface]
	public interface ISupportsCrud : IGuiMenuInterface
	{
		bool CanCreate { get; }
		bool CanRetreive { get; }
		bool CanUpdate { get; }
		bool CanDelete { get; }

		void Create ();
		void Retreive ();
		void Update ();
		void Delete ();
	}

	[GuiMenuInterface]
	public interface ISupportsCancellation : IGuiMenuInterface
	{
		bool CanCancel { get; }
		void Cancel ();
	}

	[GuiMenuInterface]
	public interface ISupportsZoom : IGuiMenuInterface
	{
		bool CanZoomIn { get; }
		bool CanZoomOut { get; }
		bool CanZoomToFit { get; }
		bool CanZoomOriginal { get; }

		void ZoomIn ();
		void ZoomOut ();
		void ZoomToFit ();
		void ZoomOriginal ();
	}

	[GuiMenuInterface]
	public interface ISupportsSorting : IGuiMenuInterface
	{
		bool CanSortAscending { get; }
		bool CanSortDescending { get; }
		bool CanResetSort { get; }

		void SortAscending ();
		void SortDescending ();
		void ResetSort ();
	}

	[GuiMenuInterface]
	public interface ISupportsCollapseExpand : IGuiMenuInterface
	{
		bool CanCollapse { get; }
		bool CanExpand { get; }

		void Collapse ();
		void Expand ();
	}

	[GuiMenuInterface]
	public interface ISupportsSplitting : IGuiMenuInterface
	{
		bool CanSplitHorizontal { get; }
		bool CanSplitVertical { get; }
		bool CanEndSplit { get; }

		void SplitHorizontal ();
		void SplitVertical ();
		void EndSplit ();
	}

	[GuiMenuInterface]
	public interface ISupportsNonPrintingCharactersDisplay : IGuiMenuInterface
	{			
		bool WhiteSpaceVisible { get; set; }
		bool LineBreaksVisible { get; set; }
		bool EndOfTextVisible { get; set; }
	}
}

