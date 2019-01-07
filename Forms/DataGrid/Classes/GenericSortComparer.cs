using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using KS.Foundation;


namespace SummerGUI.DataGrid
{    
	public static class ComparerTypeCache
	{
		static readonly Dictionary<Type, IComparer> m_Comparers = new Dictionary<Type, IComparer>();
		public static IComparer GetComparerFromType(Type type)
		{
			if (m_Comparers.ContainsKey(type))
				return m_Comparers[type];
			Type comparerType = typeof(Comparer<>);            
			Type comparerForPropertyType = comparerType.MakeGenericType(type);
			IComparer comp = comparerForPropertyType.InvokeMember("Default",
				BindingFlags.GetProperty |
				BindingFlags.Public |
				BindingFlags.Static,
				null, null, null) as IComparer;
			m_Comparers.Add(type, comp);
			return comp;
		}
	}

	public class GenericSortComparer<T> : Comparer<T>
    {        
        protected DataGridColumn[] m_SortColumns;        
        protected Dictionary<string, string> m_FieldMap;
		protected Dictionary<string, PropertyInfo> m_PropertiesTypeCache;

		public void AddFieldMapping (string key, string value)
		{
			m_FieldMap.Add (key, value);
		}

		public GenericSortComparer(DataGridColumn[] sortColumns)
        {     
            m_SortColumns = sortColumns;
            m_FieldMap = new Dictionary<string, string>(StringComparer.Ordinal);
			m_PropertiesTypeCache = new Dictionary<string, PropertyInfo> ();
        }

		object GetPropertyFromObject(T obj, string prop)
		{
			PropertyInfo info;
			if (m_PropertiesTypeCache.TryGetValue (prop, out info)) {
				return info.GetValue (obj);
			} else {
				info = obj.GetType ().GetProperty (prop, BindingFlags.Public | BindingFlags.Instance);
				if (info != null) {
					m_PropertiesTypeCache.Add (prop, info);
					return info.GetValue (obj);
				}
			}
			return ReflectionUtils.GetPropertyValue (obj, prop);
		}
			        
		public override int Compare (T x, T y)
        {
			if (x.IsDefault () || y.IsDefault ())
				return 0;

            int result = 0;            

			foreach (var col in m_SortColumns) {
				if (col.AllowSort && col.SortDirection != SortDirections.None) {
					string propertyName = MapField (col);
					object val1 = GetPropertyFromObject (x, propertyName);
					object val2 = GetPropertyFromObject (y, propertyName);
	                
					//Type valType = col.ValueType;
					if (col.ValueType == null) {
						if (val1 != null)
							col.ValueType = val1.GetType ();
						else if (val2 != null)
							col.ValueType = val2.GetType ();
						else
							return 0;
						//type = typeof(System.String);
					}                

					int iDirection = 1;
					if (col.SortDirection == SortDirections.Descending)
						iDirection = -1;

					try {
						IComparer comp = ComparerTypeCache.GetComparerFromType (col.ValueType);
						if (comp != null) {
							result = iDirection * comp.Compare (val1, val2);
						}
					} catch (Exception ex) {
						result = 0;
						ex.LogWarning ();
					}

					if (result != 0)
						break;
				}					
			};

            return result;
        }

        public virtual string MapField(DataGridColumn col)
        {
            if (m_FieldMap.ContainsKey(col.Key))
                return m_FieldMap[col.Key];
            else
                return col.Key;
        }
    }


	public class WrappedComparer<T> : IComparer<T>
	{
		readonly IComparer<T> m_UserComparer;

		public WrappedComparer(IComparer<T> userComparer)
		{
			m_UserComparer = userComparer;
		}

		public virtual int Compare(T x, T y)
		{               
			if (m_UserComparer == null || x.IsDefault() || y.IsDefault())
				return 0;

			return m_UserComparer.Compare(x, y);
		}
	}

	public class ComparisonComparer<TComparison> : IComparer<TComparison>
	{
		private readonly Comparison<TComparison> comparison;

		public ComparisonComparer(Func<TComparison, TComparison, int> compare)
		{
			if (compare == null)
			{
				throw new ArgumentNullException("compare");
			}
			comparison = new Comparison<TComparison>(compare);
		}

		public int Compare(TComparison x, TComparison y)
		{
			return comparison(x, y);
		}
	}


    // ********************* Sample ****************************

	/***
    public class GenericSortComparerDemo : GenericSortComparerBase<>
    {
		public GenericSortComparerDemo(DataGridColumn[] sortColumns)
            : base(sortColumns)
        {
            AddFieldMapping("Resource", "Text");
			AddFieldMapping("StartTime", "StartDate");
			AddFieldMapping("EndTime", "EndDate");            
        }        
    }
    ***/
}
