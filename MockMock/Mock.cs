using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MockMock
{
    public class Mock
    {
        public static T Create<T>()
        {
            return DispatchProxy.Create<T, MockProxy>();            
        }        

        public static ActionExpectation Arrange(Expression<Action> expression)
        {
            return new ActionExpectation(expression);
        }

        public static FunctionOrPropertyExpectation<T> Arrange<T>(Expression<Func<T>> expression)
        {
            return new FunctionOrPropertyExpectation<T>(expression);
        }

        public static void Assert(object mock)
        {
            var assertable = mock as IAssertable;
            if (assertable == null) throw new NotSupportedException("Cannot call Assert on non-assertable object.");
            assertable.Assert();
        }
    }
}