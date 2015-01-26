// #define SYSDEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Validation.mvdXML;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

namespace Validation.ValidationExtensions
{
    public static class IPersistIfcEntityExtension
    {
        public static bool PassesConceptRules(this IPersistIfcEntity Entity, MvdConcept cpt)
        {
            // if it's not applicable then it's a pass
            if (!cpt.AppliesTo(Entity))
                return true;
            
            // Debug.WriteLine("Requirements.Count: " + Requirements.Count());

            // looking for pattern existence
            //
            foreach (var DataPattern in cpt.ConceptTemplate.Rules)
            {
                bool thisTestPassed = Entity.PassesRule(DataPattern, PassMode: PassMode.AtLeastOne);
                if (!thisTestPassed)
                {
#if SYSDEBUG
                    Debug.WriteLine("##Fails at pattern existence level.");
#endif
                    return false;
                }
            }

            var Requirements = cpt.TemplateRuleProperties.Select(x => x.ParValues());
            if (!Requirements.Any()) // if no other requirements return true
                return true;

            // start looking for a pattern that will be ok for all the requirements
            foreach (var DataPattern in cpt.ConceptTemplate.Rules)
            {
                // test all the requirements
                bool thisPatternPasses = false;
                foreach (var Requirement in Requirements)
                {
                    // at the moment all tests are run in AND
                    bool allComponentPass = true;
                    foreach (var ReqComponent in Requirement)
                    {
#if SYSDEBUG
                        System.Diagnostics.Debug.WriteLine("##Testing: " + ReqComponent.ToString() + " for " + Entity.EntityLabel + "... ");
                        Debug.Indent();
                        bool thisTestPassed = Entity.PassesRule(DataPattern, ReqComponent);
                        Debug.Unindent();
                        Debug.WriteLine((thisTestPassed) ? "passed." : "failed.");
#else
                        System.Diagnostics.Debug.Write("##Testing: " + ReqComponent.ToString() + " for " + Entity.EntityLabel + "... ");
                        bool thisTestPassed = Entity.PassesRule(DataPattern, ReqComponent);
                        System.Diagnostics.Debug.WriteLine( (thisTestPassed) ? "passed." : "failed.");
#endif

                        if (!thisTestPassed)
                        {
                            allComponentPass = false;
                            break;
                        }
                    }
                    if (allComponentPass)
                    {
                        thisPatternPasses = true;
                    }
                    else
                    {
                        break;
                    }
                }
                if (thisPatternPasses)
                    return true;
            }
            return false;
        }

        public enum PassMode
        {
            Undefined,
            AllNeedToPass,
            AtLeastOne
        }
        

        public static bool PassesRule(this IPersistIfc Entity, MvdRule DataNode, MvdPropertyRuleValue requirement = null, PassMode PassMode = PassMode.Undefined)
        {
            // todo: add controls for schema cardinality
            //

            IfcType EntType = IfcMetaData.IfcType(Entity);
#if SYSDEBUG
            Debug.WriteLine(string.Format("testing {0} for {1}", Entity.ToString(), DataNode.Type));
#endif
            string RuleId = "";
            bool bEvaluateRule = false;
            if (DataNode.Properties.ContainsKey("RuleID") && requirement != null)
            {
                RuleId = DataNode.Properties["RuleID"];
                bEvaluateRule = (requirement.Name == RuleId);
                // here the property is evaluated.
                if (bEvaluateRule)
                {
#if SYSDEBUG
                    Debug.Write("-- Evaluating");
#endif
                    if (Entity is IPersistIfcEntity)
                    {
                        Debug.Write(string.Format("Req: {0} Ent: {1}{2}\r\n", requirement.Name, Math.Abs(((IPersistIfcEntity)Entity).EntityLabel), EntType.ToString()));
                    }
                    if (requirement.Prop == "Type")
                    {
                        string cmpVal = IfcMetaData.IfcType(Entity).Name;
                        return (requirement.Val == cmpVal);
                    }
                }
            }
            

            if (DataNode.Type == "AttributeRule")
            {
                string propName = DataNode.Properties["AttributeName"];
                var prop = EntType.IfcProperties.Where(x => x.Value.PropertyInfo.Name == propName).FirstOrDefault().Value;
                if (prop == null) // otherwise test inverses
                {
                    prop = EntType.IfcInverses.Where(x => x.PropertyInfo.Name == propName).FirstOrDefault();
                }
                if (prop == null)
                {
#if SYSDEBUG
                    Debug.WriteLine("AttributeRule failed on null value");
#endif
                    return false;
                }

                object propVal = prop.PropertyInfo.GetValue(Entity, null);
                if (propVal == null)
                    return false;
                if (propVal != null)
                {
                    if (bEvaluateRule)
                    {
                        if (requirement.Prop == "Value")
                        {
                            string cmpVal = propVal.ToString();
                            bool retVal = (requirement.Val == cmpVal);
#if SYSDEBUG
                            if (requirement.Val.StartsWith(""))
                            {

                            }

                            if (!retVal)
                            {
                                Debug.WriteLine("AttributeRule failed on value");
                            }
#endif

                            return retVal;
                        }
                        else
                        {

                        }
                    }
                    else if (requirement == null && propVal is IPersistIfc)
                    {
                        var v = (IPersistIfc)propVal;
                        bool AnyChildFail = false;
                        foreach (var nestedRule in DataNode.NestedRules)
                        {
                            Debug.Indent();
                            if (!v.PassesRule(nestedRule))
                            {
                                AnyChildFail = true;
                                Debug.Unindent();
                                break;
                            }
                            Debug.Unindent();
                        }
                        return !AnyChildFail;
                    }

                    if (prop.IfcAttribute.IsEnumerable)
                    {
                        IEnumerable<object> propCollection = propVal as IEnumerable<object>;
                        if (propCollection == null)
                            return false;
                        foreach (var child in propCollection)
                        {
                            if (child is IPersistIfc)
                            {
                                // todo: this might actually have to return fail if any nested rule fail, not if the first passes.
                                foreach (var nestedRule in DataNode.NestedRules)
                                {
                                    Debug.Indent();
                                    var loopret = ((IPersistIfc)child).PassesRule(nestedRule, requirement, PassMode);
                                    Debug.Unindent();
                                    if (loopret)
                                        return true;
                                    
                                }
                            }
                            else
                            {
                                // What is this case?
                            }
                        }
                    }
                    else
                    {
                        IPersistIfc pe = propVal as IPersistIfc;
                        if (pe == null)
                        {
#if SYSDEBUG
                            Debug.WriteLine("AttributeRule failed: not found");
#endif
                            return false;
                        }
                        foreach (var nestedRule in DataNode.NestedRules)
                        {
                            if (pe.PassesRule(nestedRule, requirement))
                                return true;
                        }
                    }
                }
            }
            else if (DataNode.Type == "EntityRule")
            {
                string EName = DataNode.Properties["EntityName"];
                IfcType ENameType = IfcMetaData.IfcType(EName.ToUpperInvariant());
                if (ENameType != null)
                {
                    // run type validation only if type matches
                    if (!(ENameType == EntType || ENameType.NonAbstractSubTypes.Contains(EntType.Type)))
                    {
#if SYSDEBUG
                        Debug.WriteLine("EntityRule failed: expected " + EName + " found: " + EntType.ToString());
#endif
                        return false;
                    }
                    // if test is passed and no sub rules then return true
                    if (!DataNode.NestedRules.Any())
                        return true;
                }
                else
                {
                    Debug.WriteLine("Probably IFC4 Type specified:" + EName);
                }
                foreach (var subRule in DataNode.NestedRules)
                {
                    var passed = Entity.PassesRule(subRule, requirement);
                    if (passed)
                        return true;
                }
                
            }

            return false;
        }
    }
}
