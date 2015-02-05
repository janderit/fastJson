using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace UnitTests.Regressions.reftype
{
    public interface OptionalBox
    {
        Optional<object> BOX();
    }

    public struct Optional<T> : OptionalBox
    {
        private bool Equals(Optional<T> other)
        {
            return HasValue.Equals(other.HasValue) && (!HasValue || EqualityComparer<T>.Default.Equals(Value, other.Value));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = typeof(T).GetHashCode();
                result = (result * 397) ^ HasValue.GetHashCode();
                if (HasValue) result = (result * 397) ^ EqualityComparer<T>.Default.GetHashCode(Value);
                return result;
            }
        }

        public Optional<object> BOX()
        {
            return !HasValue ? new Optional<object>() : Value;
        }

        public static bool operator ==(Optional<T> left, Optional<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Optional<T> left, Optional<T> right)
        {
            return !Equals(left, right);
        }

        /*internal Optional()
        {
            HasValue = false;
            Value = default(T);
        }*/

        internal Optional(T t)
            : this()
        {
            if ((object)t == null) return;
            HasValue = true;
            Value = t;
        }

        public static Optional<T> KeineAngabe
        {
            get { return new Optional<T>(); }
        }

        public readonly bool HasValue;
        public readonly T Value;

        public static implicit operator Optional<T>(T t)
        {
            return new Optional<T>(t);
        }

        public override string ToString()
        {
            return HasValue ? Value.ToString() : "";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Optional<T> && Equals((Optional<T>)obj);
        }
    }

    public static class OptionExtension
    {
        public static TResult SelectOrDefault<TOption, TResult>(this Optional<TOption> t, Func<TOption, TResult> map, TResult default_value) where TOption : class
        {
            return t.HasValue ? map(t.Value) : default_value;
        }

        public static T ValueOr<T>(this Optional<T> t, T default_value) where T : class
        {
            return t.HasValue ? t.Value : default_value;
        }
    }
}