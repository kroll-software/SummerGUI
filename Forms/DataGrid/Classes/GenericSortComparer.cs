using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using KS.Foundation;
using System.Collections.Concurrent;

namespace SummerGUI.DataGrid
{
    public static class ComparerTypeCache
    {
        // Thread-safe cache
        static readonly ConcurrentDictionary<Type, IComparer> m_Comparers = new ConcurrentDictionary<Type, IComparer>();

        public static IComparer GetComparerFromType(Type type)
        {
            if (type == null) return null;

            return m_Comparers.GetOrAdd(type, t =>
            {
                try
                {
                    // hole Comparer<T>.Default über Reflection auf eine klarere Weise
                    var genericComparer = typeof(Comparer<>).MakeGenericType(t);
                    var prop = genericComparer.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
                    var compObj = prop?.GetValue(null);
                    var comp = compObj as IComparer;
                    return comp;
                }
                catch
                {
                    // im Fehlerfall null zurückgeben — Aufrufer muss mit Fallback umgehen
                    return null;
                }
            });
        }
    }

    public class GenericSortComparer<T> : Comparer<T>
    {
        protected DataGridColumn[] m_SortColumns;
        // Thread-safe dictionaries: Compare-Aufrufe können parallel laufen (Sort-Algorithmen)
        protected ConcurrentDictionary<string, string> m_FieldMap;
        protected ConcurrentDictionary<string, PropertyInfo> m_PropertiesTypeCache;

        readonly string m_DefaultSortKey;
        readonly SortDirections m_DefaultSortDirection;

        public void AddFieldMapping(string key, string value)
        {
            // überschreiben statt Exception bei doppeltem Key
            m_FieldMap[key] = value;
        }

        public GenericSortComparer(DataGridColumn[] sortColumns)
        {
            m_SortColumns = sortColumns ?? Array.Empty<DataGridColumn>();
            m_FieldMap = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            m_PropertiesTypeCache = new ConcurrentDictionary<string, PropertyInfo>();
        }

        public GenericSortComparer(DataGridColumn[] sortColumns, string defaultSortColumnKey, SortDirections defaultSortDirection = SortDirections.Ascending)
        {
            m_SortColumns = sortColumns ?? Array.Empty<DataGridColumn>();
            m_FieldMap = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            m_PropertiesTypeCache = new ConcurrentDictionary<string, PropertyInfo>();

            m_DefaultSortKey = string.IsNullOrWhiteSpace(defaultSortColumnKey) ? null : defaultSortColumnKey;
            m_DefaultSortDirection = defaultSortDirection;
        }

        protected string MapFieldByKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            if (m_FieldMap.TryGetValue(key, out var mapped)) return mapped;
            return key;
        }

        object GetPropertyFromObject(T obj, string prop)
        {
            if (obj == null || String.IsNullOrEmpty(prop)) return null;

            PropertyInfo info;
            if (m_PropertiesTypeCache.TryGetValue(prop, out info))
            {
                return info.GetValue(obj);
            }
            else
            {
                // suche Property (öffentliche Instanz-Eigenschaft)
                info = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
                if (info != null)
                {
                    m_PropertiesTypeCache.TryAdd(prop, info);
                    return info.GetValue(obj);
                }
            }

            // Fallback (z. B. verschachtelte Pfade) — benutze bestehende Hilfsroutine
            return ReflectionUtils.GetPropertyValue(obj, prop);
        }

        public override int Compare(T x, T y)
        {
            if (x.IsDefault() || y.IsDefault())
                return 0;

            // determine active criteria: either columns with a direction, or fallback to default key
            var activeCols = (m_SortColumns ?? Array.Empty<DataGridColumn>())
                            .Where(c => c != null && c.AllowSort && c.SortDirection != SortDirections.None)
                            .ToArray();

            // If no active columns, but we have a default key, build a single pseudo-criterion
            IEnumerable<(string propertyName, SortDirections direction, DataGridColumn column)> criteria;
            if (activeCols.Length > 0)
            {
                criteria = activeCols.Select(c => (MapField(c), c.SortDirection, c));
            }
            else if (!string.IsNullOrEmpty(m_DefaultSortKey))
            {
                var propName = MapFieldByKey(m_DefaultSortKey);
                criteria = new[] { (propName, m_DefaultSortDirection, (DataGridColumn)null) };
            }
            else
            {
                // no sorting at all
                return 0;
            }

            int result = 0;

            foreach (var crit in criteria)
            {
                var propertyName = crit.propertyName;
                object val1 = GetPropertyFromObject(x, propertyName);
                object val2 = GetPropertyFromObject(y, propertyName);

                // resolve valueType: first prefer column.ValueType (if available), else PropertyInfo cache, else runtime types
                Type valueType = null;
                if (crit.column != null && crit.column.ValueType != null)
                    valueType = crit.column.ValueType;
                else if (m_PropertiesTypeCache.TryGetValue(propertyName, out var pinfo) && pinfo != null)
                    valueType = pinfo.PropertyType;
                else if (val1 != null)
                    valueType = val1.GetType();
                else if (val2 != null)
                    valueType = val2.GetType();

                if (valueType == null)
                {
                    // both null -> skip this criterion
                    continue;
                }

                int iDirection = crit.direction == SortDirections.Descending ? -1 : 1;

                try
                {
                    if (val1 == null && val2 == null)
                    {
                        result = 0;
                    }
                    else if (val1 == null)
                    {
                        result = -1 * iDirection;
                    }
                    else if (val2 == null)
                    {
                        result = 1 * iDirection;
                    }
                    else
                    {
                        IComparer comp = ComparerTypeCache.GetComparerFromType(valueType);
                        if (comp != null)
                        {
                            result = iDirection * comp.Compare(val1, val2);
                        }
                        else if (val1 is IComparable ic1)
                        {
                            result = iDirection * ic1.CompareTo(val2);
                        }
                        else if (val2 is IComparable ic2)
                        {
                            result = -iDirection * ic2.CompareTo(val1);
                        }
                        else
                        {
                            result = iDirection * StringComparer.Ordinal.Compare(val1.ToString(), val2.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ignore this criterion, log and continue to next
                    ex.LogWarning();
                    result = 0;
                }

                if (result != 0)
                    break;
            }

            return result;
        }

        public virtual string MapField(DataGridColumn col)
        {
            if (col == null) return null;
            if (m_FieldMap.TryGetValue(col.Key, out var mapped))
                return mapped;
            return col.Key;
        }
    }
}
