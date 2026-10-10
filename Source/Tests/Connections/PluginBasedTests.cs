using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Terminals.Configuration;
using Tests.FilePersisted;

namespace Tests.Connections
{
    /// <summary>
    /// Some tests need Deployed plugins in tests directory.
    /// Because of dynamic loading of the plugins, it is necessary to compare by Type name, because types doenst equals.
    /// This is necessary, because for testing we cant inject the plugins into Connection manager.
    /// See Serializer and its connectionmanager field usage, see also setter of Favorite Protocol property.
    /// </summary>
    [DeploymentItem(PUTTY_PLUGIN, PUTTY_TARGET)]
    [DeploymentItem(RDP_PLUGIN, RDP_TARGET)]
    [TestClass]
    public class PluginBasedTests
    {
        private const string PUTTY_PLUGIN = "Terminals.Plugins.Putty.dll";
        private const string PUTTY_TARGET = @"Plugins\Putty";
        private const string RDP_PLUGIN = "Terminals.Plugins.Rdp.dll";
        private const string RDP_TARGET = @"Plugins\Rdp";

        public TestContext TestContext { get; set; }

        protected string[] CreateAllAvailablePlugins()
        {
            string deploymentDirectory = this.TestContext.DeploymentDirectory;
            return new string[]
            {
                Path.Combine(deploymentDirectory, PUTTY_TARGET, PUTTY_PLUGIN),
                Path.Combine(deploymentDirectory, RDP_TARGET, RDP_PLUGIN),
            };
        }
    }
}
