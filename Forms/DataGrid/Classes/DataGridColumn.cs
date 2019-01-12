using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI.DataGrid
{
	public enum EditTypes
	{
		Text,
		Date,
		Time,
		DropDownList,
		DropDownCombo,
		CheckBox,
		NumericUpDown,
		Duration,
		MultilineText
	}

	public enum SortDirections
	{
		None,
		Ascending,
		Descending
	}

	public enum ColumnScopes
	{
		BuiltIn,
		Custom,
		UserDefined
	}
		
	public class ValueListItem
	{                				
		public string Text { get; set; }
		public object Value { get; set; }

		public ValueListItem()
		{
		}

		public ValueListItem(string text)
		{			
			Text = text;
			Value = text;
		}

		public ValueListItem(string text, object value)
		{
			Text = text;
			Value = value;
		}
	}
		
	public class DataGridColumnCollection : BinarySortedList<DataGridColumn>
	{        				
		public DataGridColumnCollection()
			: base()
		{
		}

		public DataGridColumnCollection(object context)
			: base()
		{            
		}

		public override int GetHashCode()
		{
			// UNCHECKED STILL THROWS ERRORS WITH LINQ !!!
			unchecked
			{
				int i = base.GetHashCode();
				foreach (DataGridColumn col in this)
					i += i.CombineHash(col.Version);

				return i;
			}            
		}
			
		internal void ResetSortedColumns()
		{			
			NaturalMergeSort ();
		}

		public override void OnInsert (DataGridColumn elem)
		{
			if (elem != null)
				elem.ColumnCollection = this;
		}			

		//[XmlIgnore]    
		public IEnumerable<DataGridColumn> SortedVisibleColumns
		{
			get
			{
				return this.Where(col => col.Visible);
			}
		}

		//[XmlIgnore]
		public DataGridColumn TreeColumn
		{
			get
			{
				DataGridColumn col = this.FirstOrDefault (c => c.IsTreeColumn);
				if (col == null)
					col = this.FirstOrDefault (c => c.Key == "Text" || c.Key == "Resource"); // to preserve backward compatibility
				return col; 
			}            
		}
			
		public DataGridColumn Add(string key, string caption)
		{
			DataGridColumn item = new DataGridColumn(null, key, caption);
			item.Position = this.Count;
			this.AddLast(item);
			return item;
		}

		public DataGridColumn Add(string key, string caption, float width, bool isTreeColumn = false)
		{
			DataGridColumn item = new DataGridColumn(null, key, caption, width);
			item.Position = this.Count;
			item.IsTreeColumn = isTreeColumn;
			this.AddLast(item);
			return item;
		}

		public bool ContainsKey(string key)
		{
			return this.Any (c => c.Key == key);
		}

		public void SetPositionsByIndex()
		{
			int i = 0;
			this.ForEach(col => col.Position = i++);
		}
			
		// ********* Detect Changes **********

		protected int m_UnchangedHash = 0;
		public virtual void SetDefault()
		{
			m_UnchangedHash = this.GetHashCode();
		}

		public virtual bool IsDefault()
		{
			return this.GetHashCode() == m_UnchangedHash;
		}

		protected override void OnDeserialized()
		{
			base.OnDeserialized ();
			SetDefault();
		}
	}
		
	public class DataGridColumn : FoundationItemText, IComparable<DataGridColumn>
	{
		public DataGridColumn()
			: base()
		{
		}

		public DataGridColumn(object context)
			: base(context)
		{            
		}

		public DataGridColumn(object context, string key)
			: base(context)
		{
			m_Key = key;
		}

		public DataGridColumn(object context, string key, string caption)
			: base (context)
		{
			m_Key = key;
			m_Text = caption;
		}

		public DataGridColumn(object context, string key, string caption, float width)
			: base (context)
		{
			m_Key = key;
			m_Text = caption;
			m_Width = width;
		}

		public int CompareTo(DataGridColumn other)
		{
			if (other == null)
				return 0;
			return this.Position.CompareTo (other.Position);
		}

		protected SortDirections m_SortDirection = SortDirections.None;
		[DefaultValue(SortDirections.None)]
		public SortDirections SortDirection
		{
			get
			{
				return m_SortDirection;
			}
			set
			{
				m_SortDirection = value;
			}
		}

		protected bool m_AllowSort = true;
		[DefaultValue(false)]
		public bool AllowSort
		{
			get
			{
				return m_AllowSort;
			}
			set
			{
				if (m_AllowSort != value)
				{
					m_AllowSort = value;
					if (!m_AllowSort)
						m_SortDirection = SortDirections.None;
				}
			}
		}

		//[XmlIgnore]        
		public Type ValueType { get; set; }

		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		//[XmlElement("ValueType")]
		[DefaultValue("")]        
		public string ValueTypeXml 
		{
			get
			{
				if (ValueType == null)
					return "";
				else
					return ValueType.ToString();
			}
			set
			{
				try
				{
					if (String.IsNullOrEmpty(value))
						ValueType = null;
					else
						ValueType = Type.GetType(value);
				}
				catch (Exception)
				{
					ValueType = null;
				}                
			}
		}        
			
		public RectangleF ColumnHeaderBounds = RectangleF.Empty;        

		// this method is automatically called during serialization
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);                       

			info.AddValue("Position", m_Position);            
			info.AddValue("Visible", m_Visible);
			info.AddValue("Width", m_Width);
			info.AddValue("MinWidth", m_MinWidth);

			info.AddValue("IsTreeColumn", m_IsTreeColumn);
			info.AddValue("Scope", this.m_ColumnScope);

			info.AddValue("DisplayFormatString", m_DisplayFormatString);
			info.AddValue("EditFormatString", m_EditFormatString);
			info.AddValue("TextAlignment", m_TextAlignment, typeof(StringAlignment));
			info.AddValue("LineAlignment", m_LineAlignment, typeof(StringAlignment));

			info.AddValue("EditType", m_EditType, typeof(EditTypes));
			info.AddValue("CanEdit", m_CanEdit);
			info.AddValue("AllowResize", m_AllowResize);
			info.AddValue("AllowSort", m_AllowSort);
			info.AddValue("SortDirection", m_SortDirection);            

			info.AddValue("AutoMinWidth", m_AutoMinWidth);
			info.AddValue("ValueList", m_ValueList, typeof(List<ValueListItem>));

			info.AddValue("AllowNull", m_AllowNull);

			info.AddValue("ValueType", ValueType, typeof(Type));

			//if (context.State == StreamingContextStates.Other)
			//{
			if (m_DefaultValue != null && (m_DefaultValue as ISerializable != null || (Attribute.IsDefined(m_DefaultValue.GetType(), typeof(SerializableAttribute)))))
			{
				info.AddValue("DefaultValue", m_DefaultValue);
			}
			else
			{
				info.AddValue("DefaultValue", null);
			}
			//}
		}

		// this constructor is automatically called during deserialization
		public DataGridColumn(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{            
			foreach (SerializationEntry entry in info)
			{
				switch (entry.Name)
				{                    
				case "Position":
					m_Position = (int)entry.Value;
					break;                    

				case "Visible":
					m_Visible = (bool)entry.Value;
					break;

				case "Width":
					m_Width = (float)entry.Value;
					break;

				case "MinWidth":
					m_MinWidth = (int)entry.Value;
					break;

				case "IsTreeColumn":
					m_IsTreeColumn = (bool)entry.Value;
					break;

				case "Scope":
					m_ColumnScope = (ColumnScopes)entry.Value;
					break;

				case "DisplayFormatString":
					m_DisplayFormatString = (string)entry.Value;
					break;

				case "EditFormatString":
					m_EditFormatString = (string)entry.Value;
					break;

				case "TextAlignment":
					m_TextAlignment = (Alignment)entry.Value;
					break;

				case "LineAlignment":
					m_LineAlignment = (Alignment)entry.Value;
					break;

				case "EditType":
					m_EditType = (EditTypes)entry.Value;
					break;

				case "CanEdit":
					m_CanEdit = (bool)entry.Value;
					break;

				case "AllowResize":
					m_AllowResize = (bool)entry.Value;
					break;

				case "AllowSort":
					m_AllowSort = (bool)entry.Value;
					break;

				case "SortDirection":
					m_SortDirection = (SortDirections)entry.Value;
					break;

				case "AutoMinWidth":
					m_AutoMinWidth = (bool)entry.Value;
					break;

				case "ValueList":
					m_ValueList = (List<ValueListItem>)entry.Value;
					break;								

				case "AllowNull":
					m_AllowNull = (bool)entry.Value;
					break;

				case "DefaultValue":					
					try
					{
						m_DefaultValue = entry.Value;
					}
					catch (SerializationException)
					{
						m_DefaultValue = null;
					}
					catch (Exception)
					{
						m_DefaultValue = null;
					}
					break;

				case "ValueType":
					ValueType = entry.Value as Type;
					break;                    
				}
			}                                
		}

		internal DataGridColumnCollection ColumnCollection = null;
		internal int Version = 0;
		private void IncreaseVersion()
		{
			unchecked
			{
				Version++;
			}
		}

		protected int m_Position = 0;
		public int Position
		{
			get
			{
				return m_Position;
			}
			set
			{
				if (m_Position != value)
				{
					m_Position = value;

					if (ColumnCollection != null)
						ColumnCollection.ResetSortedColumns();
					IncreaseVersion();
				}
			}
		}

		//[XmlIgnore]        
		public string Caption
		{
			get
			{
				return m_Text;
			}
			set
			{
				if (m_Text != value)
				{
					m_Text = value;
					IncreaseVersion();
				}
			}
		}

		protected bool m_Visible = true;
		[DefaultValue(true)]
		public bool Visible
		{
			get
			{
				return m_Visible;
			}
			set
			{
				if (m_Visible != value)
				{
					m_Visible = value;
					IncreaseVersion();
				}
			}
		}

		protected float m_Width = 100;
		[DefaultValue(100)]
		public float Width
		{
			get
			{
				return m_Width;
			}
			set
			{
				if (m_Width != value)
				{
					m_Width = value;
					IncreaseVersion();
				}
			}
		}

		public void SetWidth(float width, float boundsWidth, DataGridColumnCollection columns)
		{
			if (m_Width > 0 && m_Width <= 1) {
				//m_Width = Math.Min(0.9f, (float)width / (float)boundsWidth);

				float totalWidth = 0f;
				foreach (var col in columns) {			
					totalWidth += col.AbsoluteWidth (boundsWidth);
				}

				float ratio = totalWidth / (float)boundsWidth;
				float total2 = 0;
				if (Math.Abs(ratio - 1f) > float.Epsilon) {
					foreach (var col in columns) {
						if (col != this) {
							col.Width /= ratio;
							float absw = col.AbsoluteWidth (boundsWidth);
							if (absw < col.MinWidth) {
								col.Width *= col.MinWidth / absw;
							}
						}
						total2 += col.AbsoluteWidth (boundsWidth);
					}
				}

				if (total2 > boundsWidth) {
					m_Width *= boundsWidth / total2;
				}

			} else
				m_Width = width;
		}

		public int AbsoluteWidth(float boundsWidth)
		{
			if (m_Width < 0)
				return 0;
			if (m_Width <= 1)
				return (int)(m_Width * boundsWidth);
			return (int)m_Width;
		}

		protected int m_MinWidth = 32;
		[DefaultValue(32)]
		public int MinWidth
		{
			get
			{
				return m_MinWidth;
			}
			set
			{
				if (m_MinWidth != value)
				{
					m_MinWidth = value;
					IncreaseVersion ();
				}
			}
		}

		protected string m_DisplayFormatString = "";
		public string DisplayFormatString
		{
			get
			{
				return m_DisplayFormatString;
			}
			set
			{
				if (m_DisplayFormatString != value)
				{
					m_DisplayFormatString = value;
					IncreaseVersion ();
				}
			}
		}

		protected string m_EditFormatString = "";
		public string EditFormatString
		{
			get
			{
				return m_EditFormatString;
			}
			set
			{
				if (m_EditFormatString != value)
				{
					m_EditFormatString = value;
					IncreaseVersion ();
				}
			}
		}

		protected Alignment m_TextAlignment = Alignment.Near;
		[DefaultValue(Alignment.Near)]
		public Alignment TextAlignment
		{
			get
			{
				return m_TextAlignment;
			}
			set
			{
				if (m_TextAlignment != value)
				{
					m_TextAlignment = value;
					IncreaseVersion ();
				}
			}
		}

		protected Alignment m_LineAlignment = Alignment.Center;
		[DefaultValue(Alignment.Center)]
		public Alignment LineAlignment
		{
			get
			{
				return m_LineAlignment;
			}
			set
			{
				if (m_LineAlignment != value)
				{
					m_LineAlignment = value;
					IncreaseVersion ();
				}
			}
		}

		protected EditTypes m_EditType = EditTypes.Text;
		[DefaultValue(EditTypes.Text)]
		public EditTypes EditType
		{
			get
			{
				return m_EditType;
			}
			set
			{
				if (m_EditType != value)
				{
					m_EditType = value;

					// Set some default Display and Edit-Formats
					switch (m_EditType)
					{
					case EditTypes.Date:
						if (String.IsNullOrEmpty(m_DisplayFormatString))
							m_DisplayFormatString = "d";
						break;

					case EditTypes.Time:
						if (String.IsNullOrEmpty(m_DisplayFormatString))
							m_DisplayFormatString = "t";

						if (String.IsNullOrEmpty(m_EditFormatString))                            
							m_EditFormatString = "HH:mm";
						break;
					}

					IncreaseVersion();
				}
			}
		}


		protected bool m_AllowNull = true;
		[DefaultValue(true)]
		public bool AllowNull
		{
			get
			{
				return m_AllowNull;
			}
			set
			{
				if (m_AllowNull != value)
				{
					m_AllowNull = value;
					IncreaseVersion();
				}
			}
		}

		protected object m_DefaultValue = null;
		[DefaultValue(null)]
		public object DefaultValue
		{
			get
			{
				return m_DefaultValue;
			}
			set
			{
				if (m_DefaultValue != value)
				{
					if (value != null && !(value as ISerializable != null || (Attribute.IsDefined(value.GetType(), typeof(SerializableAttribute)))))
						throw new Exception("DefaultValue-Property must be set to a serializable type or attribute");

					m_DefaultValue = value;
					IncreaseVersion();
				}
			}
		}

		protected bool m_CanEdit = true;
		[DefaultValue(true)]
		public bool CanEdit
		{
			get
			{
				return m_CanEdit;
			}
			set
			{
				m_CanEdit = value;
				IncreaseVersion();
			}
		}

		protected bool m_AllowResize = true;
		[DefaultValue(true)]
		public bool AllowResize
		{
			get
			{
				return m_AllowResize;
			}
			set
			{
				m_AllowResize = value;
				IncreaseVersion();
			}
		}

		protected bool m_AutoMinWidth = false;
		[DefaultValue(false)]
		public bool AutoMinWidth
		{
			get
			{
				return m_AutoMinWidth;
			}
			set
			{
				m_AutoMinWidth = value;
				IncreaseVersion ();
			}
		}        
			
		internal int DesiredWidth = 0;
		protected int GetValueListHashCode()
		{
			return (17).CombineHash(m_ValueList);
		}

		protected ColumnScopes m_ColumnScope = ColumnScopes.BuiltIn;
		public ColumnScopes ColumnScope
		{
			get
			{
				return m_ColumnScope;
			}
			set
			{
				m_ColumnScope = value;
				IncreaseVersion();
			}
		}

		protected bool m_IsTreeColumn = false;
		public bool IsTreeColumn
		{
			get
			{
				return m_IsTreeColumn;
			}
			set
			{
				m_IsTreeColumn = value;
				IncreaseVersion();
			}
		}

		protected List<ValueListItem> m_ValueList = new List<ValueListItem>();
		public List<ValueListItem> ValueList
		{
			get
			{
				return m_ValueList;
			}
			set
			{
				if (m_ValueList != value)
				{
					m_ValueList = value;
					IncreaseVersion();
				}
			}
		}

		public void AddValueListItem(string Text, object Value)
		{
			m_ValueList.Add(new ValueListItem(Text, Value));
		}			

		// TOOK ME 8 HOURS TO FIND OUT THAT THIS CRAPPY DATABINDING DOES NOT WORK
		// WHEN ToString() WAS OVERRIDDEN.        
		// NEVER EVER RETURN TOSTRING() FOR THE VALUEMEMBER WITH DATABINDING
		//public override string ToString()
		//{
		//    return Key.ToString();
		//}

		public override object CopyTo(FoundationItem retval, object targetContext)
		{
			DataGridColumn col = base.CopyTo(retval, targetContext) as DataGridColumn;

			col.m_Position = m_Position;
			//col.m_Key = m_Key;
			//col.m_Caption = m_Caption;
			col.m_Visible = m_Visible;
			col.m_Width = m_Width;
			col.m_MinWidth = m_MinWidth;

			col.m_IsTreeColumn = m_IsTreeColumn;
			col.m_ColumnScope = m_ColumnScope;

			col.m_DisplayFormatString = m_DisplayFormatString;
			col.m_EditFormatString = m_EditFormatString;
			col.m_TextAlignment = m_TextAlignment;
			col.m_LineAlignment = m_LineAlignment;

			col.m_EditType = m_EditType;
			col.m_CanEdit = m_CanEdit;
			col.m_AllowResize = m_AllowResize;

			col.m_AutoMinWidth = m_AutoMinWidth;
			col.m_ValueList = m_ValueList;

			col.m_AllowNull = m_AllowNull;
			col.ValueType = ValueType;
			col.m_DefaultValue = m_DefaultValue;

			return col;
		}        

		// Track position for the Editing-Control
		internal float Left = 0;

		protected override void CleanupManagedResources()
		{
			ColumnCollection = null;
			base.CleanupManagedResources();
		}
	}

	public static class DataColumnExtensions
	{
		public static string ToShortString(this SortDirections sort)
		{
			switch (sort) {
			case SortDirections.Ascending:
				return "asc";
			case SortDirections.Descending:
				return "desc";
			default:
				return "none";
			}
		}

		public static SortDirections ToSortDirection(this string sort)
		{			
			if (sort.StartsWith ("asc", StringComparison.InvariantCultureIgnoreCase))
				return SortDirections.Ascending;
			else if (sort.StartsWith ("des", StringComparison.InvariantCultureIgnoreCase))
				return SortDirections.Descending;
			else
				return SortDirections.None;			
		}
	}
}

