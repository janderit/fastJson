using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests.Regressions.reftype
{
    public struct ReadonlyList<T> : IEnumerable<T>
    {
        public ReadonlyList(List<T> source)
        {
            _basis = source.ToList();
        }

        public ReadonlyList(IEnumerable<T> source)
            : this()
        {
            _basis = source.ToList();
        }

        private List<T> _basis;

        private ReadonlyList(List<T> source, bool dummy)
        {
            _basis = source;
        }

        public static ReadonlyList<T> Empty { get { return default(ReadonlyList<T>); } }

        public List<T> Basis
        {
            get
            {
                return _basis.ToList();
            }
            set
            {
                if (_basis == null) _basis = value.ToList();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _basis != null ? _basis.GetEnumerator()
                : new List<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _basis != null ? _basis.GetEnumerator()
                : new List<T>().GetEnumerator();
        }

        public static implicit operator ReadonlyList<T>(List<T> source)
        {
            return new ReadonlyList<T>(source);
        }

        public static ReadonlyList<T> operator +(ReadonlyList<T> that, T item)
        {
            return that.Concat(new[] { item }).ToReadonlyList();
        }

        public static ReadonlyList<T> operator -(ReadonlyList<T> that, T item)
        {
            return that.Except(new[] { item }).ToReadonlyList();
        }

        public static ReadonlyList<T> operator +(ReadonlyList<T> a, ReadonlyList<T> b)
        {
            return a.Concat(b).ToReadonlyList();
        }

        public static ReadonlyList<T> operator +(ReadonlyList<T> a, IEnumerable<T> b)
        {
            return a.Concat(b).ToReadonlyList();
        }

        public static ReadonlyList<T> operator -(ReadonlyList<T> a, ReadonlyList<T> b)
        {
            return a.Except(b).ToReadonlyList();
        }

        public ReadonlyList<T> Sorted()
        {
            return new ReadonlyList<T>(_basis.OrderBy(_ => _));
        }

        public ReadonlyList<T> Sorted_by<TKey>(Func<T, TKey> selector)
        {
            return new ReadonlyList<T>(_basis.OrderBy(selector));
        }

        /// <summary>
        /// Use at your own risk !!! : Erzeugt eine ReadonlyList aus der angegebenen Liste. Die Liste wird dabei nicht kopiert. Daher darf auf keinen Fall nach diesem Aufruf noch eine weitere Referenz auf die Liste bestehen bleiben. 
        /// </summary>
        public static ReadonlyList<T> Create_ReadonlyList__at_your_own_risk__from_last_active_reference_of(List<T> list)
        {
            return new ReadonlyList<T>(list, false);
        }
        
    }

    public static class IEnumerableExtensions
    {
        public static ReadonlyList<T> ToReadonlyList<T>(this IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            return new ReadonlyList<T>(collection);
        }
    }
}