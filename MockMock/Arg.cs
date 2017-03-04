using System;

namespace MockMock
{
    public class Arg
    {
        public static string AnyString
        {
            get
            {
                return IsAny<string>();
            }
        }

        public static string NullOrEmpty
        {
            get
            {
                MethodExpectationBase.CurrentParameterMatcher = new ParameterMatcher<string>((val) => String.IsNullOrEmpty(val));
                return default(string);
            }
        }

        public static int AnyInt
        {
            get
            {
                return IsAny<int>();
            }
        }

        public static object AnyObject
        {
            get
            {
                return IsAny<object>();
            }
        }

        public static T IsAny<T>()
        {
            return Matches<T>((val) => true);
        }

        public static T Matches<T>(Func<T, bool> matcher)
        {
            MethodExpectationBase.CurrentParameterMatcher = new ParameterMatcher<T>(matcher);
            return default(T);
        }
    }

    internal class ParameterMatcher
    {
        public static ParameterMatcher Exact<T>(T match)
        {
            return new ParameterMatcher<T>((val) => object.Equals(val, match));
        }

        public virtual bool IsMatch(object val)
        {
            return false;
        }
    }

    internal class ParameterMatcher<T> : ParameterMatcher
    {
        private readonly Func<T, bool> _matchFunc;

        public ParameterMatcher(Func<T, bool> matchFunc)
        {
            _matchFunc = matchFunc;
        }

        public bool IsMatch(T val)
        {
            return _matchFunc(val);
        }

        public override bool IsMatch(object val)
        {
            return IsMatch((T)val);
        }
    }
}
