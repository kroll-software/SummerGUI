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
            // Dein bestehendes IsDefault-Verhalten beibehalten (z.B. behandelt null als Default)
            if (x.IsDefault() || y.IsDefault())
                return 0;

            int result = 0;

            foreach (var col in m_SortColumns)
            {
                if (col == null) continue;
                if (!col.AllowSort || col.SortDirection == SortDirections.None) continue;

                string propertyName = MapField(col);
                object val1 = GetPropertyFromObject(x, propertyName);
                object val2 = GetPropertyFromObject(y, propertyName);

                // Versuche, deklarierte PropertyType aus dem Cache zu verwenden (robuster als Laufzeit-Type)
                if (col.ValueType == null)
                {
                    if (m_PropertiesTypeCache.TryGetValue(propertyName, out var info) && info != null)
                    {
                        col.ValueType = info.PropertyType;
                    }
                    else if (val1 != null)
                    {
                        col.ValueType = val1.GetType();
                    }
                    else if (val2 != null)
                    {
                        col.ValueType = val2.GetType();
                    }
                    else
                    {
                        // Beide Werte null: dieses Sortierungs-Kriterium überspringen,
                        // statt sofort 0 zurückzugeben — andere Sortier-Spalten könnten relevant sein.
                        continue;
                    }
                }

                int iDirection = col.SortDirection == SortDirections.Descending ? -1 : 1;

                try
                {
                    // Null-Behandlung: falls beide null -> weiter zur nächsten Spalte.
                    if (val1 == null && val2 == null)
                    {
                        result = 0;
                        // continue to next column
                    }
                    else if (val1 == null)
                    {
                        result = -1 * iDirection; // null < not-null
                    }
                    else if (val2 == null)
                    {
                        result = 1 * iDirection; // not-null > null
                    }
                    else
                    {
                        // Versuche, einen passenden IComparer zu benutzen
                        IComparer comp = ComparerTypeCache.GetComparerFromType(col.ValueType);
                        if (comp != null)
                        {
                            // IComparer.Compare erwartet object/object - gut hier
                            result = iDirection * comp.Compare(val1, val2);
                        }
                        else
                        {
                            // Fallback: wenn val1 IComparable implementiert, nutze CompareTo
                            if (val1 is IComparable ic1)
                            {
                                result = iDirection * ic1.CompareTo(val2);
                            }
                            else if (val2 is IComparable ic2)
                            {
                                // inverser Vergleich (nicht ideal, aber besser als 0)
                                result = -iDirection * ic2.CompareTo(val1);
                            }
                            else
                            {
                                // Letzter Rückfall: String-Vergleich der ToString-Repr.
                                result = iDirection * StringComparer.Ordinal.Compare(val1.ToString(), val2.ToString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Bei Fehlern das Kriterium ignorieren (aber loggen)
                    result = 0;
                    ex.LogWarning();
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
                throw new ArgumentNullException(nameof(compare));
            }
            comparison = new Comparison<TComparison>(compare);
        }

        public int Compare(TComparison x, TComparison y)
        {
            return comparison(x, y);
        }
    }
}
