using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Terminals.Connections;
using Terminals.Network;

namespace Tests.Network
{
    [TestClass]
    public class ServiceDetectorTests
    {
        private const string IRRELEVANT_IP_ADDRESS = "IrrelevantIp";

        private const int IRRELEVANT_PORT = 1234;

        [TestMethod]
        public void UnknownPlugin_ResolveServiceName_ReturnsRdp()
        {
            var dummyPlugin = new DummyPlugin();
            string resolved = Resolve(0, dummyPlugin);
            const string MESSAGE = "If only one plugin represents required port, than its portName is resolved.";
            Assert.AreEqual(dummyPlugin.PortName, resolved, MESSAGE);
        }

        [TestMethod]
        public void WorkingExtraDetectionPlugin_ResolveServiceName_ReturnsExtraDetection()
        {
            var standardPlugin = CreatePlugin("Standard");
            var extraDetectionPlugin = CreateExtraDetectionPlugin("ExtraDetected", valid: true);
            var resolved = Resolve(IRRELEVANT_PORT, standardPlugin, extraDetectionPlugin);
            Assert.AreEqual("ExtraDetected", resolved, "If extra check is successfull, than that plugins is resolved.");
        }

        [TestMethod]
        public void FailingExtraDetectionPlugin_ResolveServiceName_ReturnsStandard()
        {
            var standardPlugin = CreatePlugin("Standard");
            var extraDetectionPlugin = CreateExtraDetectionPlugin("ExtraDetected", valid: false);
            var resolved = Resolve(IRRELEVANT_PORT, standardPlugin, extraDetectionPlugin);
            Assert.AreEqual("Standard", resolved, "If extra check fails standard plugin is resolved.");
        }

        private static IConnectionPlugin CreatePlugin(string portName)
        {
            var plugin = new Mock<IConnectionPlugin>();
            plugin.SetupGet(p => p.PortName).Returns(portName);
            return plugin.Object;
        }

        private static IConnectionPlugin CreateExtraDetectionPlugin(string portName, bool valid)
        {
            var plugin = new Mock<IConnectionPlugin>();
            plugin.SetupGet(p => p.PortName).Returns(portName);
            plugin.As<IExtraDetection>()
                .Setup(d => d.IsValid(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(valid);
            return plugin.Object;
        }

        private static string Resolve(int port, params IConnectionPlugin[] plugins)
        {
            Mock<IConnectionManager> mockConnectionManager = CreateConnecitonManager(plugins);
            var detector = new ServiceDetector(mockConnectionManager.Object);
            return detector.ResolveServiceName(IRRELEVANT_IP_ADDRESS, port);
        }

        private static Mock<IConnectionManager> CreateConnecitonManager(params IConnectionPlugin[] plugins)
        {
            var mockConnectionManager = new Mock<IConnectionManager>();
            mockConnectionManager.Setup(m => m.GetPluginsByPort(It.IsAny<int>()))
                .Returns(new List<IConnectionPlugin>(plugins));
            return mockConnectionManager;
        }
    }
}
