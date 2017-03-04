using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MockMock
{      
    public abstract class Expectation : IExpectation
    {
        protected IMock _mock;
        protected string _name;
        private bool _mustBeCalled;
        private int? _mustOccur;
        private int _callCount;
        private Exception _exceptionToThrow;
        private List<ParameterMatcher> _parameterMatchers = new List<ParameterMatcher>();

        protected Expectation()
        {
        }

        public Expectation Throws(Exception ex)
        {
            _exceptionToThrow = ex;
            return this;
        }

        public Expectation Throws<TEx>() where TEx : Exception
        {
            return Throws((TEx)Activator.CreateInstance(typeof(TEx)));
        }

        void IAssertable.Assert()
        {
            if (_mustBeCalled && _callCount == 0)
            {
                throw new Exception($"{_name} was not called.");
            }

            if (_mustOccur.HasValue && _mustOccur.Value == 0 && _callCount > 0)
            {
                throw new Exception($"{_name} should not have been called.");
            }

            if (_mustOccur.HasValue && _mustOccur.Value != _callCount)
            {
                throw new Exception($"{_name} should have been called {_mustOccur.Value} times but was actually called {_callCount} times.");
            }
        }

        public void MustBeCalled()
        {
            _mustBeCalled = true;
        }

        public void OccursNever()
        {
            _mustOccur = 0;
        }

        public void OccursOnce()
        {
            _mustOccur = 1;
        }

        public void Occurs(int count)
        {
            _mustOccur = count;
        }

        bool IExpectationChecker.ShouldThrow(out Exception ex)
        {
            ex = _exceptionToThrow;
            return ex != null;
        }

        void IExpectationChecker.Called()
        {
            _callCount++;
        }

        object IExpectationChecker.GetReturnValue(object[] args)
        {
            return GetReturnValue(args);
        }

        protected virtual object GetReturnValue(object[] args)
        {
            return null;
        }

        bool IExpectationChecker.Matches(MethodBase method, object obj, Type returnType)
        {
            return Matches(method, obj, returnType);
        }

        bool IExpectationChecker.Matches(MethodInfo method, object[] args)
        {
            return Matches(method, args);
        }

        protected abstract bool Matches(MethodBase method, object obj, Type returnType);

        protected abstract bool Matches(MethodInfo method, object[] args);
    }

    public abstract class MethodExpectationBase : Expectation
    {
        internal static ParameterMatcher CurrentParameterMatcher;

        private readonly MethodInfo _method;
        private readonly List<ParameterMatcher> _parameterMatchers = new List<ParameterMatcher>();

        protected MethodExpectationBase(LambdaExpression expression)
        {
            var mce = expression.Body as MethodCallExpression;
            if (mce.Arguments != null)
            {
                foreach (var argument in mce.Arguments)
                {
                    var ple = Expression.Lambda(argument);
                    var pce = ple.Compile();
                    CurrentParameterMatcher = null;
                    var argVal = pce.DynamicInvoke();
                    //invoking the lamda expression sets any Arg params as CurrentParameterMatcher              
                    if (CurrentParameterMatcher == null)
                    {
                        CurrentParameterMatcher = ParameterMatcher.Exact(argVal);
                    }
                    _parameterMatchers.Add(CurrentParameterMatcher);
                    CurrentParameterMatcher = null;
                }
            }

            var le = Expression.Lambda(mce.Object);
            var ce = le.Compile();
            _method = mce.Method;
            _name = _method.Name;
            _mock = ce.DynamicInvoke() as IMock;                                                
            if (_mock == null) throw new Exception("Invalid mock type. Mock types must implement IMock.");
            _mock.SetExpectation(this);
        }

        protected override bool Matches(MethodBase method, object parameters, Type returnType)
        {
            if (method.Name != _method.Name) return false;
            if (returnType == null)
            {
                if (_method.ReturnType.Name != "Void") return false;
            }
            else
            {
                if (returnType != _method.ReturnType) return false;
            }
            var arguments = new PropertyInfo[0];
            if (parameters != null) arguments = parameters.GetType().GetProperties();
            if (arguments.Length != _parameterMatchers.Count) return false;
            for (int i = 0; i < arguments.Length; i++)
            {
                var argumentValue = arguments[i].GetValue(parameters);
                if (!_parameterMatchers[i].IsMatch(argumentValue)) return false;
            }
            return true;
        }

        protected override bool Matches(MethodInfo method, object[] args)
        {
            if (method.Name != _method.Name) return false;
            if (method.ReturnType == null)
            {
                if (_method.ReturnType.Name != "Void") return false;
            }
            else
            {
                if (method.ReturnType != _method.ReturnType) return false;
            }            
            if (args == null && !_parameterMatchers.Any()) return true;
            if (args.Length != _parameterMatchers.Count) return false;
            for (int i = 0; i < args.Length; i++)
            {
                var argumentValue = args[i];
                if (!_parameterMatchers[i].IsMatch(argumentValue))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class PropertyExpectation : Expectation, IFuncExpectation
    {
        private readonly MemberInfo _member;
        private Func<object[], object> _funcReturnValue;

        public PropertyExpectation(LambdaExpression expression)
        {
            var me = expression.Body as MemberExpression;
            var le = Expression.Lambda(me.Expression);
            var ce = le.Compile();
            _member = me.Member;
            _name = _member.Name;
            _mock = ce.DynamicInvoke() as IMock;
            _mock.SetExpectation(this);
        }

        protected override bool Matches(MethodBase method, object parameters, Type returnType)
        {
            var name = method.Name.Replace("get_", "");
            return name == _name;
        }

        protected override bool Matches(MethodInfo method, object[] args)
        {
            var name = method.Name.Replace("get_", "");
            return name == _name;
        }

        public Expectation Returns<T1>(T1 result)
        {
            _funcReturnValue = (args) => result;
            return this;
        }

        public Expectation ReturnsMany<T1>(params T1[] results)
        {
            var list = new Queue<T1>(results);
            _funcReturnValue = (args) =>
            {
                return list.Dequeue();
            };
            return this;
        }

        public Expectation Returns(object result)
        {
            _funcReturnValue = (args) => result;
            return this;
        }

        public Expectation Returns<T1>(Func<T1> funcReturn)
        {
            _funcReturnValue = (args) => funcReturn();
            return this;
        }

        public Expectation ReturnsValueFromArgs(Func<object[], object> valueFromInputArgs)
        {
            _funcReturnValue = (args) => valueFromInputArgs(args);
            return this;
        }

        protected override object GetReturnValue(object[] args)
        {
            return _funcReturnValue(args);
        }
    }

    public class ActionExpectation : MethodExpectationBase, IActionExpectation
    {
        public ActionExpectation(Expression<Action> expression)
            : base(expression)
        {
        }
    }

    public class FunctionExpectation<T> : MethodExpectationBase, IFuncExpectation
    {
        private Func<object[], object> _funcReturnValue;

        public FunctionExpectation(Expression<Func<T>> expression)
            : base(expression)
        {
        }

        public Expectation Returns<T1>(T1 result)
        {
            _funcReturnValue = (args) => result;
            return this;
        }

        public Expectation ReturnsMany<T1>(params T1[] results)
        {
            var list = new Queue<T1>(results);
            _funcReturnValue = (args) =>
            {
                return list.Dequeue();
            };
            return this;
        }

        public Expectation Returns(object result)
        {
            _funcReturnValue = (args) => result;
            return this;
        }

        public Expectation Returns<T1>(Func<T1> funcReturn)
        {
            _funcReturnValue = (args) => funcReturn();
            return this;
        }

        public Expectation ReturnsValueFromArgs(Func<object[], object> valueFromInputArgs)
        {
            _funcReturnValue = (args) => valueFromInputArgs(args);
            return this;
        }

        protected override object GetReturnValue(object[] args)
        {
            return _funcReturnValue(args);
        }
    }

    public class FunctionOrPropertyExpectation<T> : IExpectation, IFuncExpectation
    {
        private IFuncExpectation _expectation;

        public FunctionOrPropertyExpectation(Expression<Func<T>> expression)
        {
            var mce = expression.Body as MethodCallExpression;
            if (mce != null) _expectation = new FunctionExpectation<T>(expression);
            else _expectation = new PropertyExpectation(expression);
        }

        public Expectation Returns<T1>(T1 result)
        {
            return _expectation.Returns<T1>(result);
        }

        public Expectation ReturnsMany<T1>(params T1[] results)
        {
            return _expectation.ReturnsMany(results);
        }

        public Expectation Returns(object result)
        {
            return _expectation.Returns(result);
        }

        public Expectation Returns<T1>(Func<T1> funcReturn)
        {
            return _expectation.Returns<T1>(funcReturn);
        }

        public Expectation ReturnsValueFromArgs(Func<object[], object> valueFromInputArgs)
        {
            return _expectation.ReturnsValueFromArgs(valueFromInputArgs);
        }

        object IExpectationChecker.GetReturnValue(object[] args)
        {
            return _expectation.GetReturnValue(args);
        }

        bool IExpectationChecker.Matches(MethodBase method, object obj, Type returnType)
        {
            return _expectation.Matches(method, obj, returnType);
        }

        bool IExpectationChecker.Matches(MethodInfo method, object[] args)
        {
            return _expectation.Matches(method, args);
        }

        public Expectation Throws(Exception ex)
        {
            return _expectation.Throws(ex);
        }

        public Expectation Throws<TEx>() where TEx : Exception
        {
            return _expectation.Throws<TEx>();
        }

        bool IExpectationChecker.ShouldThrow(out Exception ex)
        {
            return _expectation.ShouldThrow(out ex);
        }

        void IExpectationChecker.Called()
        {
            _expectation.Called();
        }

        void IAssertable.Assert()
        {
            _expectation.Assert();
        }

        public void MustBeCalled()
        {
            _expectation.MustBeCalled();
        }

        public void OccursNever()
        {
            _expectation.OccursNever();
        }

        public void OccursOnce()
        {
            _expectation.OccursOnce();
        }

        public void Occurs(int count)
        {
            _expectation.Occurs(count);
        }
    }


    public interface IAssertable
    {
        void Assert();        
    }

    public interface IMock : IAssertable
    {
        void SetExpectation(IExpectation expectation);
    }

    public interface IExpectationChecker: IAssertable
    {
        bool ShouldThrow(out Exception ex);
        bool Matches(MethodBase method, object obj, Type returnType);
        bool Matches(MethodInfo method, object[] args);
        void Called();
        object GetReturnValue(object[] args);
    }

    public interface IExpectation : IExpectationChecker
    {
        void MustBeCalled();
        void OccursNever();
        void OccursOnce();
        void Occurs(int count);
        Expectation Throws(Exception ex);
        Expectation Throws<TEx>() where TEx : Exception;
    }

    public interface IActionExpectation : IExpectation
    {
    }

    public interface IFuncExpectation : IExpectation
    {
        Expectation Returns(object returnValue);
        Expectation Returns<T1>(T1 result);
        Expectation Returns<T1>(Func<T1> result);
        Expectation ReturnsMany<T1>(params T1[] returnValues);
        Expectation ReturnsValueFromArgs(Func<object[], object> valueFromInputArgs);
    }
}