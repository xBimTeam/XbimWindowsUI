using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO;
using System.IO;

namespace XbimXplorer
{
    class Sample
    {
       
        bool HasPropertyLikeExternalTrue(IfcWall wall)
{
    //get relations property
    IEnumerable<IfcRelDefinesByProperties> rels = wall.IsDefinedByProperties;
    foreach (var rel in rels)
    {
	 //get property set
        IfcPropertySet pSet = rel.RelatingPropertyDefinition as IfcPropertySet;
        if (pSet == null) continue;
        foreach (IfcProperty prop in pSet.HasProperties)
        {
	     //get properties
            IfcPropertySingleValue singleVal = prop as IfcPropertySingleValue;
            if (singleVal == null) continue;
            if (singleVal.Name == "Wall Function")
            {
	 //check value of the property
                IfcValue val = singleVal.NominalValue;
                if (val.UnderlyingSystemType == typeof(int))
                {
                    if ((int)val.Value == 1) return true;
                }
                else if (val.UnderlyingSystemType == typeof(int?))
                {
                    if ((int?)val.Value == 1) return true;
                }
            }
        }
    }
    return false;
}

        private void DoorInRoom(IModel model)
        {
            int doorCount = 0;
            IEnumerable<IfcRelContainedInSpatialStructure> relations = model.Instances.OfType<IfcRelContainedInSpatialStructure>();
            IfcRelContainedInSpatialStructure relation = relations.FirstOrDefault();
            if (relation != null)
            {
                IEnumerable<IfcDoor> doors = relation.RelatedElements.OfType<IfcDoor>();
                doorCount = doors.Count();
                
            }
        }

        StringWriter Output;

        //This will perform selection of the objects. 
        //Selected objects with the geometry will be highlighted
        public IEnumerable<IfcProduct> Select(IModel model)
        {
            Output.WriteLine("Hello selected products");
            return model.Instances.OfType<IfcWall>();
        }

        //This will hide all objects except for the returned ones
        public IEnumerable<IfcProduct> ShowOnly(IModel model)
        {
            Output.WriteLine("Hello visible products!");
            return model.Instances.Where<IfcProduct>(p => p.Name != null && ((string)p.Name).ToLower().Contains("wall"));
        }

        //This will execute arbitrary code with no return value
        public void Execute(IModel model)
        {
            IEnumerable<IfcSpace> spaces = model.Instances.OfType<IfcSpace>();
            foreach (IfcSpace space in spaces)
            {
                Output.WriteLine(space.Name + " - " + space.LongName);
            }
        }

        private void CountDoor(IModel model)
        {
List<IfcDoor> doors = new List<IfcDoor>();
IEnumerable<IfcRelContainedInSpatialStructure> relations = model.
    Instances.Where<IfcRelContainedInSpatialStructure>(rel => rel.RelatingStructure.Name == "311"
    );
List<IfcWall> walls = new List<IfcWall>();
foreach (IfcRelContainedInSpatialStructure relation in relations)
{
    if (relation != null)
    {
        IEnumerable<IfcWall> w = relation.RelatedElements.OfType<IfcWall>();
        walls.AddRange(w);
    }
}

foreach (IfcWall wall in walls)
{
    IEnumerable<IfcRelVoidsElement> relsVoid = model.Instances.
    Where<IfcRelVoidsElement>(rel => rel.RelatingBuildingElement == wall);
    foreach (var relation in relsVoid)
    {
        IfcOpeningElement opening = relation.RelatedOpeningElement as IfcOpeningElement;
        if (opening != null)
        {
            IEnumerable<IfcRelFillsElement> relsFill = model.Instances.
                Where<IfcRelFillsElement>(r => 
                    r.RelatingOpeningElement == opening && 
                    r.RelatedBuildingElement is IfcDoor
                    );
            foreach (IfcRelFillsElement relFill in relsFill)
            {
                doors.Add(relFill.RelatedBuildingElement as IfcDoor);
            }
        }
    }
}
        }
        
    }
}
