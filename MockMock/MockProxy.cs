using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MockMock
{
    public class MockProxy : DispatchProxy, IMock
    {
        private readonly List<IExpectationChecker> _expectations = new List<IExpectationChecker>();

        public void Assert()
        {
            if(_expectations.Count == 0)
            {
                throw new InvalidOperationException("Assert() called on mock with no expectations set.");
            }
            foreach (var expectation in _expectations)
            {
                expectation.Assert();
            }
        }

        public void SetExpectation(IExpectation expectation)
        {
            _expectations.Add(expectation);
        }

        private object Record(MethodInfo method, object[] args)
        {
            object returnedValue = null;
            foreach (var item in _expectations.Reverse<IExpectationChecker>())
            {
                var expectation = item;
                if (expectation.Matches(method, args))
                {
                    expectation.Called();
                    Exception exception;
                    if (expectation.ShouldThrow(out exception)) throw exception;
                    var funcExpectation = expectation as IFuncExpectation;
                    if (funcExpectation != null)
                    {
                        returnedValue = funcExpectation.GetReturnValue(args);
                    }                    
                }
            }

            if (returnedValue == null && typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                System.Diagnostics.Debug.WriteLine($"No expectation provided for {method.Name}. If this method returns a Task that is awaited, it will cause an exception.");
                throw new NotImplementedException($"No expectation provided for {method.Name}. If this method returns a Task that is awaited, it will cause an exception.");
            }
            if (method.ReturnType == typeof(void))
                return null;
            return returnedValue ?? (method.ReturnType.GetTypeInfo().IsValueType
                ? Activator.CreateInstance(method.ReturnType)
                : null);
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            return Record(targetMethod, args);
        }
    }
}
