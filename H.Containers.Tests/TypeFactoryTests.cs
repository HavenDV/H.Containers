using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace H.Containers.Tests
{
    [TestClass]
    public class TypeFactoryTests
    {
        public abstract class AbstractClass
        {
            public abstract int Test1(string test);
            public abstract void Test2();
        }

        [TestMethod]
        public void AbstractTest()
        {
            var instance = TypeFactory.CreateInstance<AbstractClass>();

            var result = instance.Test1("hello");
            Console.WriteLine($"Result: {result}");
            instance.Test2();
        }

        public interface IInterface
        {
            int Test1(string test);
            void Test2();
        }

        [TestMethod]
        public void InterfaceTest()
        {
            var instance = TypeFactory.CreateInstance<IInterface>();

            var result = instance.Test1("hello");
            Console.WriteLine($"Result: {result}");
            instance.Test2();
        }
    }
}
