using System;
using System.Linq.Expressions;

namespace Xbim.Presentation.Extensions.Utility
{
    internal static class PropertyHelper
    {
        public static string GetPropertyName<TObject, TProperty>(this TObject sender, Expression<Func<TObject, TProperty>> expression)
        {
            var body = expression.Body as MemberExpression;
            
            return body != null ? body.Member.Name : null;
        }
    }
}
