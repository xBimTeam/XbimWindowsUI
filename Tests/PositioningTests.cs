using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Presentation;

namespace Tests
{
    [TestClass]
    public class PositioningTests
    {
        [TestMethod]
        [Ignore("Reminder for Claudio")]
        public void XbimModelPositioningReview()
        {
            throw new Exception(
                "Xbim.Presentation\\XbimModelPositioning.cs TODOs need to be cleaned up.");
        }

        // todo: Must review scaling of models.
        [TestMethod]
        [DeploymentItem(@"FederationPositioningTests\", @"Scale\")]
        [Ignore]
        public void ScaledPositioningBoxes()
        {
            // this test is currently failing because some core functions do not work on old geometry models
            // it has to be decided if the function needs to be implemented for v3.1 models as well.
            // 
            var m = new List<IfcStore>();

            var m0 = IfcStore.Open(@"Scale\P1_cm.xBIM");
            m.Add(m0);
            
            var m1 = IfcStore.Open(@"Scale\P2_cm.xBIM");
            m.Add(m1);

            var m2 = IfcStore.Open(@"Scale\P2_mm.xBIM");
            m.Add(m2);

            var m3 = IfcStore.Open(@"Scale\GeomV1\P2_mm.xBIM");
            m.Add(m3);

            // var p = new List<XbimModelPositioning>();
            var r = new List<XbimRect3D>();
            foreach (var xbimModel in m)
            {
                var tmp = new XbimModelPositioning(xbimModel);
                r.Add(tmp.SelectedRegionInMeters);
            }

            HaveSameSize(r[1], r[2]);
            HaveSameSize(r[1], r[3]);
            // HaveSameSize(r[0], r[2]);
            // HaveSameSize(r[0], r[3]);
            
            HaveSameLocation(r[1], r[2]);
            HaveSameLocation(r[1], r[3]);
            // NeedToBeSame(r[1], r[0]);
            // NeedToBeSame(r[0], r[3]);

            foreach (var xbimModel in m)
            {
                xbimModel.Close();
            }            
        }

        private static void HaveSameSize(XbimRect3D r1, XbimRect3D r2)
        {
            const double delta = 0.00001;
            Assert.AreEqual(r1.SizeX, r2.SizeX, delta, "Size X out of error margin.");
            Assert.AreEqual(r1.SizeY, r2.SizeY, delta, "Size Y out of error margin.");
            Assert.AreEqual(r1.SizeZ, r2.SizeZ, delta, "Size Z out of error margin.");
        }

        private static void HaveSameLocation(XbimRect3D r1, XbimRect3D r2)
        {
            const double delta = 0.00001;
            Assert.AreEqual(r1.Location.X, r2.Location.X, delta, "Position X out of error margin.");
            Assert.AreEqual(r1.Location.Y, r2.Location.Y, delta, "Position Y out of error margin.");
            Assert.AreEqual(r1.Location.Z, r2.Location.Z, delta, "Position Z out of error margin.");
        }
    }
}
