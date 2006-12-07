/***************************************************************************
 *  PipelineVariable.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

namespace Banshee.AudioProfiles
{
    public enum PipelineVariableControlType 
    {
        Text,
        Slider,
        Combo
    }
    
    public class PipelineVariable
    {
        private PipelineVariableControlType control_type;
        private string id;
        private string name;
        private string unit;
        private string default_value;
        private string current_value;
        private double min_value;
        private double max_value;
        private double step_value;
        private Abakos.Compiler.Expression step_expression;
        private Dictionary<string, string> possible_values = new Dictionary<string, string>();
        private List<string> possible_values_keys = new List<string>();
        private Dictionary<string, string> transformations = new Dictionary<string, string>();
        private bool advanced;
    
        internal PipelineVariable(XmlNode node)
        {
            id = node.Attributes["id"].Value.Trim();
            name = node.SelectSingleNode("name").InnerText.Trim();
            control_type = StringToControlType(node.SelectSingleNode("control-type").InnerText.Trim());
            
            try {
                unit = node.SelectSingleNode("unit").InnerText.Trim();
            } catch {
            }
            
            try {
                XmlNode advanced_node = node.SelectSingleNode("advanced");
                if(advanced_node != null) {
                    advanced = ParseAdvanced(advanced_node.InnerText);
                }
            } catch {
            }

            default_value = ReadValue(node, "default-value");
            min_value = ToDouble(ReadValue(node, "min-value"));
            max_value = ToDouble(ReadValue(node, "max-value"));
            step_value = ToDouble(ReadValue(node, "step-value"));
            
            if(default_value != null && default_value != String.Empty && (current_value == null ||
                current_value == String.Empty)) {
                current_value = default_value;
            }
            
            try {
                string step_expression_str = node.SelectSingleNode("step-expression").InnerText;
                step_expression = new Abakos.Compiler.Expression(step_expression_str);
            } catch {
            }

            foreach(XmlNode possible_value_node in node.SelectNodes("possible-values/value")) {
                try {
                    string value = possible_value_node.Attributes["value"].Value.Trim();
                    string display = possible_value_node.InnerText.Trim();

                    if(!possible_values.ContainsKey(value)) {
                        possible_values.Add(value, display);
                        possible_values_keys.Add(value);
                    }
                } catch {
                }
            }

            foreach(XmlNode transformation_node in node.SelectNodes("transformation")) {
                try {
                    string transformation_id = transformation_node.Attributes["id"].Value.Trim();
                    string expression = transformation_node.InnerText.Trim();

                    if(!transformations.ContainsKey(transformation_id)) {
                        transformations.Add(transformation_id, expression);
                    }
                } catch {
                }
            }
        }

        private static string ReadValue(XmlNode node, string name)
        {
            try {
                string str = node.SelectSingleNode(name).InnerText.Trim();
                return str == String.Empty ? null : str;
            } catch {
            }

            return null;
        }

        private static double ToDouble(string str)
        {
            try {
                return Convert.ToDouble(str, ProfileManager.CultureInfo);
            } catch {
            }

            return 0.0;
        }

        private static PipelineVariableControlType StringToControlType(string str)
        {
            switch(str.ToLower()) {
                case "combo": return PipelineVariableControlType.Combo; 
                case "slider": return PipelineVariableControlType.Slider;
                case "text": 
                default:
                    return PipelineVariableControlType.Text;
            }
        }

        internal static bool ParseAdvanced(string advanced)
        {
            if(advanced == null || advanced.Trim() == String.Empty) {
                return true;
            }
            
            switch(advanced.Trim().ToLower()) {
                case "true":
                case "yes":
                case "1":
                case "advanced":
                    return true;
                default:
                    return false;
            }
        }

        public double EvaluateTransformation(string id)
        {
            Abakos.Compiler.Expression expression = new Abakos.Compiler.Expression(transformations[id]);
            expression.DefineVariable("$_", Convert.ToDouble(CurrentValue, ProfileManager.CultureInfo));
            return expression.EvaluateNumeric();
        }

        public string ID {
            get { return id; }
            set { id = value; }
        }

        public string Name {
            get { return name; }
            set { name = value; }
        }

        public string Unit {
            get { return unit; }
            set { unit = value; }
        }

        public PipelineVariableControlType ControlType {
            get { return control_type; }
            set { control_type = value; }
        }
        
        public bool Advanced {
            get { return advanced; }
            set { advanced = value; }
        }
        
        public string DefaultValue {
            get { return default_value; }
            set { default_value = value; }
        }

        public string CurrentValue {
            get { return current_value; }
            set { current_value = value; }
        }

        public double? DefaultValueNumeric {
            get {
                try {
                    return DefaultValue == null || DefaultValue == String.Empty ?
                        (double?)null :
                        Convert.ToDouble(DefaultValue, ProfileManager.CultureInfo);
                } catch {
                    return null;
                }
            }

            set { DefaultValue = Convert.ToString(value, ProfileManager.CultureInfo); }
        }

        public double? CurrentValueNumeric {
            get {
                try {
                    return CurrentValue == null || CurrentValue == String.Empty ?
                        (double?)null :
                        Convert.ToDouble(CurrentValue, ProfileManager.CultureInfo);
                } catch {
                    return null;
                }
            }

            set { CurrentValue = Convert.ToString(value, ProfileManager.CultureInfo); }
        }

        public double MinValue {
            get { return min_value; }
            set { min_value = value; }
        }

        public double MaxValue {
            get { return max_value; }
            set { max_value = value; }
        }

        public double StepValue {
            get { return step_value; }
            set { step_value = value; }
        }

        public bool HasStepExpression {
            get { return step_expression != null; }
        }

        public double EvaluateStepExpression(double input)
        {
            step_expression.DefineVariable("$min", Convert.ToDouble(MinValue, ProfileManager.CultureInfo));
            step_expression.DefineVariable("$max", Convert.ToDouble(MaxValue, ProfileManager.CultureInfo));
            step_expression.DefineVariable("$step", Convert.ToDouble(StepValue, ProfileManager.CultureInfo));
            step_expression.DefineVariable("$this", input);
            
            try {
                return step_expression.EvaluateNumeric();
            } catch(Exception e) {
                Console.WriteLine(e);
                throw e;
            }
        }
        
        public IDictionary<string, string> PossibleValues {
            get { return possible_values; }
        }
        
        public ICollection<string> PossibleValuesKeys {
            get { return possible_values_keys; }
        }
        
        public int PossibleValuesCount {
            get { return possible_values.Count; }
        }

        public IEnumerable<KeyValuePair<string, string>> Transformations {
            get { return transformations; }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(String.Format("\tID            = {0}\n", ID));
            builder.Append(String.Format("\tName          = {0}\n", Name));
            builder.Append(String.Format("\tControl Type  = {0}\n", ControlType));
            builder.Append(String.Format("\tAdvanced      = {0}\n", Advanced));
            builder.Append(String.Format("\tDefault Value = {0}\n", DefaultValue));
            builder.Append(String.Format("\tCurrent Value = {0}\n", CurrentValue));
            builder.Append(String.Format("\tMin Value     = {0}\n", MinValue));
            builder.Append(String.Format("\tMax Value     = {0}\n", MaxValue));
            builder.Append(String.Format("\tStep Value    = {0}\n", StepValue));
            builder.Append(String.Format("\tPossible Values:\n"));
            
            foreach(KeyValuePair<string, string> value in PossibleValues) {
                builder.Append(String.Format("\t\t{0} => {1}\n", value.Value, value.Key));
            }

            builder.Append(String.Format("\tTransformations:\n"));

            foreach(KeyValuePair<string, string> value in Transformations) {
                builder.Append(String.Format("\t\t{0} => {1}\n", value.Key, value.Value));
            }

            builder.Append("\n");
            
            return builder.ToString();
        }
    }
}
