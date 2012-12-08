using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1 {
    [TestClass]
    public class UnitTest1 {
        [TestMethod]
        public void TestMethod1() {
            Console.WriteLine("hi");
        }
        [TestMethod]
        public void TestMethod2() {
            Console.WriteLine("hi2");
            throw new Exception("?");
        }
    }
}
