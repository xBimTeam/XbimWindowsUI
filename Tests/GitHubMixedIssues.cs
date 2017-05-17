using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Ifc2x3.IO;
using Xbim.Presentation;

namespace Tests
{
    [TestClass]
    public class GitHubMixedIssues
    {
        [TestMethod]
        public void Issue52()
        {
            // https://github.com/xBimTeam/XbimWindowsUI/issues/52
            //
            var c = new XbimColour("testTransparency", 1f, 1f, 1f, 0.5f );
            var m = new WpfMaterial(c);
            Assert.IsTrue(m.IsTransparent);
        }
    }
}
