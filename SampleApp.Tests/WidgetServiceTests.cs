using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using MockMock;

namespace SampleApp.Tests
{
    [TestClass]
    public class WidgetServiceTests
    {
        [TestMethod]
        public void MinimumQuantity_test()
        {
            // arrange
            var widgetService = Mock.Create<IWidgetService>();
            Mock.Arrange(() => widgetService.MinimumQuantity).Returns(7).MustBeCalled();

            // act
            var minQty = widgetService.MinimumQuantity;

            // assert
            Mock.Assert(widgetService); // this is equivalent to the line above
        }

        [TestMethod]
        public void GetWidgetDescription_tests()
        {
            // arrange
            var widgetService = Mock.Create<IWidgetService>();
            Mock.Arrange(() => widgetService.GetWidgetDescription(Arg.AnyString, true)).Returns("description").MustBeCalled();
            //Mock.Arrange(() => widgetService.ValidateWidgets(Arg.IsAny<DateTime>())).OccursNever();

            // act
            var desc = widgetService.GetWidgetDescription("name", true);
            //widgetService.ValidateWidgets(DateTime.Now); // this line will cause a test failure (ValidateWidget should not be called)

            // assert
            Mock.Assert(widgetService);
        }

        [TestMethod]
        public void GetWidgetDescription_tests2()
        {
            // arrange
            var widgetService = Mock.Create<IWidgetService>();
            //Mock.Arrange(() => widgetService.GetWidgetDescription("name", false)).Returns("description").MustBeCalled();
            Mock.Arrange(() => widgetService.ValidateWidgets(Arg.IsAny<DateTime>())).Throws(new Exception("ValidateWidgets exception"));

            // act
            var desc = widgetService.GetWidgetDescription("test", false); // will cause test failure - does not match expected params
            widgetService.ValidateWidgets(DateTime.Now); // will cause test failure - throws

            // assert
            Mock.Assert(widgetService);
        }
    }
}
