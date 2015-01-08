using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Presentation
{
    // todo: cb: Jochem: is this needed?
    class StringPropertyValue : IPropertyValue
    {

        public bool BooleanVal
        {
            get { throw new NotImplementedException(); }
        }

        public string EnumVal
        {
            get { throw new NotImplementedException(); }
        }

        public object EntityVal
        {
            get { throw new NotImplementedException(); }
        }

        public long HexadecimalVal
        {
            get { throw new NotImplementedException(); }
        }

        public long IntegerVal
        {
            get { throw new NotImplementedException(); }
        }

        public double NumberVal
        {
            get { throw new NotImplementedException(); }
        }

        public double RealVal
        {
            get { throw new NotImplementedException(); }
        }

        public string StringVal
        {
            get;
            set;
        }

        public XbimExtensions.IfcParserType Type
        {
            get { throw new NotImplementedException(); }
        }
    }
}
