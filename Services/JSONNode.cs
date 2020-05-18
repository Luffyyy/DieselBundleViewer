using DieselEngineFormats.ScriptData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DieselBundleViewer.Services
{
    public class JSONNode : ScriptDataNode
    {
        public JSONNode() : base() { }

        public JSONNode(string meta, string data, object index, int indent = 0) : base(meta, data, index, indent) { }
        public JSONNode(string meta, float[] data, object index, int indent = 0) : base(meta, data, index, indent) { }
        public JSONNode(string meta, float data, object index, int indent = 0) : base(meta, data, index, indent) { }
        public JSONNode(string meta, bool data, object index, int indent = 0) : base(meta, data, index, indent) { }
        public JSONNode(string meta, Dictionary<string, object> data, object index, int indent = 0) : base(meta, data, index, indent) { }
        public JSONNode(StreamReader data) : base(data) { }

        public override string ToString()
        {
            if (this.meta != "value_node")
            {
                StringBuilder indentation = new StringBuilder();
                for (int i = 0; i < this.indent; i++)
                    indentation.Append("\t");

                StringBuilder sb = new StringBuilder();

                sb.Append("{\n");

                if (this.meta != "table" && (string)this.index != this.meta)
                    sb.Append(indentation.ToString() + "\t\"_meta\" : \"" + this.meta + "\"" + (this.attributes.Count == 0 && this.children.Count == 0 ? "" : ",") + "\n");

                //foreach (ScriptDataNode child in this.children)
                for (int i = 0; i < this.children.Count; i++)
                {
                    ScriptDataNode child = this.children[i];

                    sb.Append(indentation.ToString() + "\t");

                    if (this.index != null)
                        sb.Append("\"" + child.index.ToString() + "\" : ");

                    sb.Append(child);
                    sb.Append((this.attributes.Count == 0 && i == this.children.Count - 1 ? "" : ",") + "\n");
                }

                //foreach (KeyValuePair<string, object> kvp in this.attributes)
                List<KeyValuePair<string, object>> attributeList = this.attributes.ToList();

                for (int i = 0; i < this.attributes.Count; i++)
                {
                    KeyValuePair<string, object> kvp = attributeList[i];

                    sb.Append(indentation.ToString() + "\t\"" + kvp.Key + "\" : ");

                    if (kvp.Value is float[])
                    {
                        sb.Append("\"");
                        float[] vals = kvp.Value as float[];

                        if (vals.Length == 3)
                        {
                            sb.Append("Vector3(");
                            for (int j = 0; j < 3; j++)
                                sb.Append(vals[j] + (j == 2 ? ")" : ", "));
                        }
                        else if (vals.Length == 4)
                        {
                            for (int j = 0; j < 3; j++)
                                sb.Append(vals[j] + (j == 2 ? "" : ", "));
                            //Quaternion quat = new Quaternion(vals[1], vals[2], vals[3], vals[0]);
                            //Vector3 vec = quat.Xyz;
                            //Quaternion quat = new Quaternion(vals[3], vals[2], vals[0], vals[1]);
                            //Vector3 vec = quat.ToEulerAnglesInDegrees();

                            //sb.Append("Rotation(" + (-vec.x).ToString() + ", " + vec.y.ToString() + ", " + (-vec.z).ToString() + ")");
                        }
                        sb.Append("\"");
                    }
                    else if (kvp.Value is bool)
                        sb.Append(((bool)kvp.Value ? "true" : "false"));
                    else if (kvp.Value is float)
                        sb.Append(kvp.Value);
                    else
                        sb.Append("\"" + kvp.Value + "\"");

                    sb.Append((i == this.attributes.Count - 1 ? "" : ",") + "\n");
                }

                sb.Append(indentation.ToString() + "}");

                return sb.ToString();
            }
            else
            {
                return this.data.ToString();
            }


        }

        public override void FromString(StreamReader data)
        {
            bool closed = false;

            string line = data.ReadLine();
            int line_pos = 0;

            if (String.IsNullOrWhiteSpace(line))
                return;

            for (; line_pos < line.Length; line_pos++)
            {
                if (line[line_pos] == '<')
                {
                    line_pos++;
                    break;
                }
            }

            if (!closed && line_pos < line.Length && line[line_pos] == '/')
            {
                closed = true;
                line_pos++;
            }

            StringBuilder meta = new StringBuilder();

            for (; line_pos < line.Length; line_pos++)
            {
                if (line[line_pos] != ' ' && line[line_pos] != '=' && line[line_pos] != '/' && line[line_pos] != '>')
                {
                    meta.Append(line[line_pos]);
                }
                else
                {
                    line_pos++;
                    break;
                }
            }

            this.meta = meta.ToString();

            if (line_pos < line.Length)
            {
                StringBuilder attrib_name = new StringBuilder();
                StringBuilder attrib_data = new StringBuilder();

                for (; line_pos < line.Length; line_pos++)
                {
                    if (line[line_pos] == '/' || line[line_pos] == '>')
                    {
                        break;
                    }

                    if (line[line_pos] == ' ')
                    {
                        continue;
                    }

                    for (; line_pos < line.Length; line_pos++)
                    {
                        if (line[line_pos] != '=')
                        {
                            attrib_name.Append(line[line_pos]);
                        }
                        else
                        {
                            line_pos++;
                            break;
                        }
                    }

                    if (line[line_pos] == '"')
                        line_pos++;

                    for (; line_pos < line.Length; line_pos++)
                    {
                        if (line[line_pos] != '"')
                        {
                            attrib_data.Append(line[line_pos]);
                        }
                        else
                        {
                            break;
                        }
                    }

                    Boolean data_bool;
                    if (Boolean.TryParse(attrib_data.ToString(), out data_bool))
                    {
                        this.attributes.Add(attrib_name.ToString(), data_bool);
                        attrib_name.Clear();
                        attrib_data.Clear();
                        continue;
                    }

                    float data_float;
                    if (float.TryParse(attrib_data.ToString(), out data_float))
                    {
                        this.attributes.Add(attrib_name.ToString(), data_float);
                        attrib_name.Clear();
                        attrib_data.Clear();
                        continue;
                    }

                    List<float> data_floats = new List<float>();
                    bool isFloatArray = true;
                    if (attrib_data.ToString().Split(' ').Length > 1)
                    {
                        string[] splits = attrib_data.ToString().Split(' ');

                        foreach (String spl in splits)
                        {
                            float test_out;
                            if (float.TryParse(spl, out test_out))
                            {
                                data_floats.Add(test_out);
                            }
                            else
                            {
                                isFloatArray = false;
                            }

                        }

                        if (isFloatArray)
                        {
                            this.attributes.Add(attrib_name.ToString(), data_floats.ToArray());
                            attrib_name.Clear();
                            attrib_data.Clear();
                            continue;
                        }
                    }

                    this.attributes.Add(attrib_name.ToString(), attrib_data.ToString());
                    attrib_name.Clear();
                    attrib_data.Clear();

                }
            }

            if (!closed && line_pos < line.Length && line[line_pos] == '/')
            {
                closed = true;
                line_pos++;
            }

            if (!closed)
            {
                JSONNode child;
                while (!((child = new JSONNode(data)).meta).Equals(this.meta))
                    this.children.Add(child);
            }
        }
    }

}
