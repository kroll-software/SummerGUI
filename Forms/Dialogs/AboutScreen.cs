using System;
using System.Reflection;
using OpenTK;
using OpenTK.Input;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using KS.Foundation;

namespace SummerGUI
{
	public class AboutScreen : ChildFormWindow
	{
		public AboutScreen (string caption, SummerGUIWindow parent)
			: this ("AboutScreen", caption, 520, 460, parent)
		{
			
		}

		public TableLayoutContainer Table { get; private set; }

		ImagePanel ImageWidget;
		TextWidget TitleWidget;
		TextWidget SubTitleWidget;
		TextWidget VersionWidget;
		TextWidget UrlCaptionWidget;
		UrlTextWidget UrlWidget;
		TextWidget LicenseInfoWidget;
		TextWidget CreditsCaptionWidget;
		ScrollingBox CreditsWidget;
		TextWidget CopyrightWidget;


		public AboutScreen (string name, string caption, int width, int height, SummerGUIWindow parent)
			: base(name, caption, width, height, parent, true)
		{						
			ShowInTaskBar = false;

			InitButtons ();
			InitTable ();
		}			
			
		protected virtual void InitButtons()
		{
			// panel
			ButtonContainer buttonContainer = this.Controls.AddChild (new ButtonContainer("buttoncontainer"));
			buttonContainer.BackColor = Theme.Colors.Base2;

			Button btnOK = buttonContainer.AddChild (new Button ("okbutton", "OK"));
			btnOK.Click += (sender, eOK) => OnOK();
			btnOK.HAlign = Alignment.Far;
			btnOK.MinSize = new System.Drawing.SizeF (96, btnOK.MinSize.Height);
		}

		protected virtual void InitTable()
		{
			Table = Controls.AddChild (new TableLayoutContainer ("table"));
			Table.Padding = new Padding (12, 6);

			ImageWidget = Table.AddChild (new ImagePanel ("image", Docking.Fill, String.Empty), 0, 0, 5);
			ImageWidget.Padding = new Padding (0, 8, 8, 0);
			ImageWidget.VAlign = Alignment.Near;
			ImageWidget.SizeMode = ImageSizeModes.None;

			TitleWidget = Table.AddChild (new TextWidget ("title"), 0, 1, 1, 2);
			TitleWidget.SetFontByTag(CommonFontTags.ExtraLarge);
			TitleWidget.Format = FontFormat.DefaultMultiLine;
			// ToDo: DPI Scaling
			TitleWidget.Margin = new Padding (TitleWidget.Margin.Left, 8, TitleWidget.Margin.Right, TitleWidget.Margin.Bottom);

			SubTitleWidget = Table.AddChild (new TextWidget ("subtitle"), 1, 1, 1, 2);
			SubTitleWidget.Format = FontFormat.DefaultMultiLine;		

			VersionWidget = Table.AddChild (new TextWidget ("version"), 2, 1, 1, 2);
			VersionWidget.Format = FontFormat.DefaultSingleLine;
			//VersionWidget.Margin = new Padding (VersionWidget.Margin.Left, 8, VersionWidget.Margin.Right, 8);
			VersionWidget.Text = GetVersion ();

			LicenseInfoWidget = Table.AddChild (new TextWidget ("licinfo"), 3, 1, 1, 2);
			LicenseInfoWidget.Format = FontFormat.DefaultMultiLine;
			LicenseInfoWidget.Margin = new Padding (LicenseInfoWidget.Margin.Left, 0, LicenseInfoWidget.Margin.Right, 8);
			LicenseInfoWidget.Visible = false;

			CreditsCaptionWidget = Table.AddChild (new TextWidget ("creditscaption"), 4, 0);
			CreditsCaptionWidget.Format = FontFormat.DefaultSingleLine;
			CreditsCaptionWidget.Text = "Credits:";

			UrlCaptionWidget = Table.AddChild (new TextWidget ("moreinfocaption"), 4, 1);
			UrlCaptionWidget.Format = FontFormat.DefaultSingleLine;
			UrlCaptionWidget.Text = "More Info:";
			UrlCaptionWidget.Visible = false;

			UrlWidget = Table.AddChild (new UrlTextWidget ("url", ""), 4, 2);			

			CreditsWidget = Table.AddChild (new ScrollingBox ("credits"), 5, 0, 1, 3);
			CreditsWidget.SetFontByTag(CommonFontTags.Serif);

			CopyrightWidget = Table.AddChild (new TextWidget ("copyright"), 6, 0, 1, 3);
			CopyrightWidget.Format = FontFormat.DefaultMultiLine;

			// finally set some SizeModes
			Table.Columns [0].SizeMode = TableSizeModes.Content;
			Table.Columns [1].SizeMode = TableSizeModes.Content;
			Table.Rows [5].SizeMode = TableSizeModes.Fill;
		}

		protected virtual string GetVersion()
		{
			try {				
				Version v = Assembly.GetExecutingAssembly().GetName().Version;
				string strVersion = String.Format("Version {0}.{1}.{2}", v.Major, v.Minor, v.Build);

				if (v.Revision > 0)
				{
					strVersion += " - Revision " + v.Revision;
				}

				return strVersion;	
			} catch (Exception ex) {
				ex.LogError ();
				return "?.?.?";
			}				
		}

		public string ImagePath
		{
			get{	
				return ImageWidget.FilePath;
			}
			set{
				ImageWidget.FilePath = value;
			}
		}

		public string ProgramTitle
		{
			get{	
				return TitleWidget.Text;
			}
			set{
				TitleWidget.Text = value;
			}
		}

		public string ProgramSubTitle
		{
			get{	
				return SubTitleWidget.Text;
			}
			set{
				SubTitleWidget.Text = value;
			}
		}
			
		public string Version
		{
			get{	
				return VersionWidget.Text;
			}
			set{
				VersionWidget.Text = value;
			}
		}

		public string UrlCaption
		{
			get{	
				return UrlCaptionWidget.Text;
			}
			set{
				UrlCaptionWidget.Text = value;
			}
		}

		public string Url
		{
			get{	
				return UrlWidget.Url;
			}
			set{
				UrlWidget.Url = value;
				UrlWidget.Visible = !String.IsNullOrEmpty (value);
				UrlCaptionWidget.Visible = !String.IsNullOrEmpty (value);
			}
		}

		public string UrlText
		{
			get{	
				return UrlWidget.Text;
			}
			set{
				UrlWidget.Text = value;
				UrlCaptionWidget.Visible = !String.IsNullOrEmpty (value);
			}
		}

		public string Copyright
		{
			get{			
				return CopyrightWidget.Text;	
			}
			set{
				CopyrightWidget.Text = value;
			}
		}

		public string LicenseInfo
		{
			get{		
				return LicenseInfoWidget.Text;
			}
			set{
				LicenseInfoWidget.Text = value;
				LicenseInfoWidget.Visible = !String.IsNullOrEmpty(value);
			}
		}

		public string CreditsCaption
		{
			get{			
				return CreditsCaptionWidget.Text;	
			}
			set{
				CreditsCaptionWidget.Text = value;
			}
		}

		public void AddCreditsParagraph(string text)
		{			
			CreditsWidget.AddParagraph (text);
		}

		public void AddCreditsFromTextFile(string filePath)
		{			
			filePath = filePath.FixedExpandedPath ();
			if (!Strings.FileExists (filePath)) {
				this.LogWarning ("Credits file not found: {0}", filePath);
				return;
			}

			string str3rdPartyLicenses = TextFile.LoadTextFile(filePath);
			if (!String.IsNullOrEmpty(str3rdPartyLicenses))
				CreditsWidget.AddParagraph (str3rdPartyLicenses);
			else
				this.LogWarning ("Failed to load credits file {0}, {1}", filePath, TextFile.TextFileLastErrorDescription + String.Empty);
		}

		public void AddCreditsImage(string filePath, IGUIContext ctx, float opacity = 1f)
		{			
			filePath = filePath.FixedExpandedPath ();
			if (!Strings.FileExists (filePath)) {
				this.LogWarning ("Credits image file not found: {0}", filePath);
				return;
			}

			try {
				CreditsWidget.AddImage(filePath, ctx, opacity);
			} catch (Exception ex) {
				this.LogWarning ("Failed to load credits image file {0}, {1}", filePath, ex.Message);
			}
		}
			
		protected override void OnKeyDown (KeyboardKeyEventArgs e)
		{			
			switch (e.Key) {
			case Keys.Space:
				CreditsWidget.ToggleAnimation ();
				break;
			case Keys.Enter:
			case Keys.Escape:
				Close();
				break;
			default:
				base.OnKeyDown (e);
				break;
			}
		}
	}
}

