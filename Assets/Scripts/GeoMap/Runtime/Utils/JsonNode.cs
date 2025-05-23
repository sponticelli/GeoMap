using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GeoMap.Utils
{
    public class JsonNode
    {
        const int MAX_DEPTH = 1000;
        const string INFINITY = "\"INFINITY\"";
        const string NEGINFINITY = "\"NEGINFINITY\"";
        const string NaN = "\"NaN\"";
        public static char[] WHITESPACE = new char[] { ' ', '\r', '\n', '\t' };

        public enum Type
        {
            NULL,
            STRING,
            NUMBER,
            OBJECT,
            ARRAY,
            BOOL
        }

        public bool isContainer => (type == Type.ARRAY || type == Type.OBJECT);

        public JsonNode parent;
        public Type type = Type.NULL;

        public int Count
        {
            get
            {
                if (list == null)
                    return -1;
                return list.Count;
            }
        }
        
        public List<JsonNode> list;
        public List<string> keys;
        public string str;

        public double n;
        public float f => (float)n;

        public bool b;

        public delegate void AddJSONConents(JsonNode self);

        public static JsonNode nullJO => new(Type.NULL); //an empty, null object

        public static JsonNode obj => new(Type.OBJECT); //an empty object

        public static JsonNode arr => new(Type.ARRAY); //an empty array

        public JsonNode(Type t)
        {
            type = t;
            switch (t)
            {
                case Type.ARRAY:
                    list = new List<JsonNode>();
                    break;
                case Type.OBJECT:
                    list = new List<JsonNode>();
                    keys = new List<string>();
                    break;
            }
        }

        public JsonNode(bool b)
        {
            type = Type.BOOL;
            this.b = b;
        }

        public JsonNode(float f)
        {
            type = Type.NUMBER;
            n = f;
        }

        public JsonNode(Dictionary<string, string> dic)
        {
            type = Type.OBJECT;
            keys = new List<string>();
            list = new List<JsonNode>();
            foreach (KeyValuePair<string, string> kvp in dic)
            {
                keys.Add(kvp.Key);
                list.Add(new JsonNode { type = Type.STRING, str = kvp.Value });
            }
        }

        public JsonNode(Dictionary<string, JsonNode> dic)
        {
            type = Type.OBJECT;
            keys = new List<string>();
            list = new List<JsonNode>();
            foreach (KeyValuePair<string, JsonNode> kvp in dic)
            {
                keys.Add(kvp.Key);
                list.Add(kvp.Value);
            }
        }

        public JsonNode(AddJSONConents content)
        {
            content.Invoke(this);
        }

        public JsonNode(JsonNode[] objs)
        {
            type = Type.ARRAY;
            list = new List<JsonNode>(objs);
        }

        /// Convenience function for creating a JsonNode containing a string.
        /// This is not part of the constructor so that malformed JSON data doesn't just turn into a string object
        public static JsonNode StringObject(string val)
        {
            return new JsonNode { type = Type.STRING, str = val };
        }

        public void Absorb(JsonNode obj)
        {
            list.AddRange(obj.list);
            keys.AddRange(obj.keys);
            str = obj.str;
            n = obj.n;
            b = obj.b;
            type = obj.type;
        }

        public JsonNode()
        {
        }

        #region PARSE

        public JsonNode(string str, bool strict = false)
        {
            //create a new JsonNode from a string (this will also create any children, and parse the whole string)
            if (str != null)
            {
                str = str.Trim(WHITESPACE);
                if (strict)
                {
                    if (str[0] != '[' && str[0] != '{')
                    {
                        type = Type.NULL;
                        Debug.LogWarning("Improper (strict) JSON formatting.  First character must be [ or {");
                        return;
                    }
                }

                if (str.Length > 0)
                {
                    if (string.Compare(str, "true", true) == 0)
                    {
                        type = Type.BOOL;
                        b = true;
                    }
                    else if (string.Compare(str, "false", true) == 0)
                    {
                        type = Type.BOOL;
                        b = false;
                    }
                    else if (string.Compare(str, "null", true) == 0)
                    {
                        type = Type.NULL;
                    }
                    else if (str == INFINITY)
                    {
                        type = Type.NUMBER;
                        n = double.PositiveInfinity;
                    }
                    else if (str == NEGINFINITY)
                    {
                        type = Type.NUMBER;
                        n = double.NegativeInfinity;
                    }
                    else if (str == NaN)
                    {
                        type = Type.NUMBER;
                        n = double.NaN;
                    }
                    else if (str[0] == '"')
                    {
                        type = Type.STRING;
                        this.str = str.Substring(1, str.Length - 2);
                    }
                    else
                    {
                        if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out n))
                        {
                            type = Type.NUMBER;
                        }
                        else
                        {
                            int token_tmp = 1;
                            /*
                             * Checking for the following formatting (www.json.org)
                             * object - {"field1":value,"field2":value}
                             * array - [value,value,value]
                             * value - string	- "string"
                             *		 - number	- 0.0
                             *		 - bool		- true -or- false
                             *		 - null		- null
                             */
                            int offset = 0;
                            switch (str[offset])
                            {
                                case '{':
                                    type = Type.OBJECT;
                                    keys = new List<string>();
                                    list = new List<JsonNode>();
                                    break;
                                case '[':
                                    type = Type.ARRAY;
                                    list = new List<JsonNode>();
                                    break;
                                default:
                                    type = Type.NULL;
                                    Debug.LogWarning("improper JSON formatting:" + str);
                                    return;
                            }

                            string propName = "";
                            bool openQuote = false;
                            bool inProp = false;
                            int depth = 0;
                            while (++offset < str.Length)
                            {
                                if (System.Array.IndexOf<char>(WHITESPACE, str[offset]) > -1)
                                    continue;
                                if (str[offset] == '\"')
                                {
                                    if (openQuote)
                                    {
                                        if (!inProp && depth == 0 && type == Type.OBJECT)
                                            propName = str.Substring(token_tmp + 1, offset - token_tmp - 1);
                                        openQuote = false;
                                    }
                                    else
                                    {
                                        if (depth == 0 && type == Type.OBJECT)
                                            token_tmp = offset;
                                        openQuote = true;
                                    }
                                }

                                if (openQuote)
                                    continue;
                                if (type == Type.OBJECT && depth == 0)
                                {
                                    if (str[offset] == ':')
                                    {
                                        token_tmp = offset + 1;
                                        inProp = true;
                                    }
                                }

                                if (str[offset] == '[' || str[offset] == '{')
                                {
                                    depth++;
                                }
                                else if (str[offset] == ']' || str[offset] == '}')
                                {
                                    depth--;
                                }

                                //if  (encounter a ',' at top level)  || a closing ]/}
                                if ((str[offset] == ',' && depth == 0) || depth < 0)
                                {
                                    inProp = false;
                                    string inner = str.Substring(token_tmp, offset - token_tmp).Trim(WHITESPACE);
                                    if (inner.Length > 0)
                                    {
                                        if (type == Type.OBJECT)
                                            keys.Add(propName);
                                        list.Add(new JsonNode(inner));
                                    }

                                    token_tmp = offset + 1;
                                }
                            }
                        }
                    }
                }
                else type = Type.NULL;
            }
            else type = Type.NULL; //If the string is missing, this is a null
        }

        #endregion

        public bool IsNumber => type == Type.NUMBER;

        public bool IsNull => type == Type.NULL;

        public bool IsString => type == Type.STRING;

        public bool IsBool => type == Type.BOOL;

        public bool IsArray => type == Type.ARRAY;

        public bool IsObject => type == Type.OBJECT;

        public void Add(bool val)
        {
            Add(new JsonNode(val));
        }

        public void Add(float val)
        {
            Add(new JsonNode(val));
        }

        public void Add(int val)
        {
            Add(new JsonNode(val));
        }

        public void Add(string str)
        {
            Add(StringObject(str));
        }

        public void Add(AddJSONConents content)
        {
            Add(new JsonNode(content));
        }

        public void Add(JsonNode obj)
        {
            if (obj)
            {
                //Don't do anything if the object is null
                if (type != Type.ARRAY)
                {
                    type = Type.ARRAY; //Congratulations, son, you're an ARRAY now
                    if (list == null)
                        list = new List<JsonNode>();
                }

                list.Add(obj);
            }
        }

        public void AddField(string name, bool val)
        {
            AddField(name, new JsonNode(val));
        }

        public void AddField(string name, float val)
        {
            AddField(name, new JsonNode(val));
        }

        public void AddField(string name, int val)
        {
            AddField(name, new JsonNode(val));
        }

        public void AddField(string name, AddJSONConents content)
        {
            AddField(name, new JsonNode(content));
        }

        public void AddField(string name, string val)
        {
            AddField(name, StringObject(val));
        }

        public void AddField(string name, JsonNode obj)
        {
            if (obj)
            {
                //Don't do anything if the object is null
                if (type != Type.OBJECT)
                {
                    keys = new List<string>();
                    if (type == Type.ARRAY)
                    {
                        for (int i = 0; i < list.Count; i++)
                            keys.Add(i + "");
                    }
                    else if (list == null)
                        list = new List<JsonNode>();

                    type = Type.OBJECT; //Congratulations, son, you're an OBJECT now
                }

                keys.Add(name);
                list.Add(obj);
            }
        }

        public void SetField(string name, bool val)
        {
            SetField(name, new JsonNode(val));
        }

        public void SetField(string name, float val)
        {
            SetField(name, new JsonNode(val));
        }

        public void SetField(string name, int val)
        {
            SetField(name, new JsonNode(val));
        }

        public void SetField(string name, JsonNode obj)
        {
            if (HasField(name))
            {
                list.Remove(this[name]);
                keys.Remove(name);
            }

            AddField(name, obj);
        }

        public void RemoveField(string name)
        {
            if (keys.IndexOf(name) > -1)
            {
                list.RemoveAt(keys.IndexOf(name));
                keys.Remove(name);
            }
        }

        public delegate void FieldNotFound(string name);

        public delegate void GetFieldResponse(JsonNode obj);

        public bool GetField(ref bool field, string name, bool fallback)
        {
            if (GetField(ref field, name))
            {
                return true;
            }

            field = fallback;
            return false;
        }

        public bool GetField(ref bool field, string name, FieldNotFound fail = null)
        {
            if (type == Type.OBJECT)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    field = list[index].b;
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public bool GetField(ref double field, string name, double fallback)
        {
            if (GetField(ref field, name))
            {
                return true;
            }

            field = fallback;
            return false;
        }

        public bool GetField(ref double field, string name, FieldNotFound fail = null)
        {
            if (type == Type.OBJECT)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    field = list[index].n;
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public bool GetField(ref int field, string name, int fallback)
        {
            if (GetField(ref field, name))
            {
                return true;
            }

            field = fallback;
            return false;
        }

        public bool GetField(ref int field, string name, FieldNotFound fail = null)
        {
            if (type == Type.OBJECT)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    field = (int)list[index].n;
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public bool GetField(ref uint field, string name, uint fallback)
        {
            if (GetField(ref field, name))
            {
                return true;
            }

            field = fallback;
            return false;
        }

        public bool GetField(ref uint field, string name, FieldNotFound fail = null)
        {
            if (type == Type.OBJECT)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    field = (uint)list[index].n;
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public bool GetField(ref string field, string name, string fallback)
        {
            if (GetField(ref field, name))
            {
                return true;
            }

            field = fallback;
            return false;
        }

        public bool GetField(ref string field, string name, FieldNotFound fail = null)
        {
            if (type == Type.OBJECT)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    field = list[index].str;
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public void GetField(string name, GetFieldResponse response, FieldNotFound fail = null)
        {
            if (response != null && type == Type.OBJECT)
            {
                int index = keys.IndexOf(name);
                if (index >= 0)
                {
                    response.Invoke(list[index]);
                    return;
                }
            }

            if (fail != null) fail.Invoke(name);
        }

        public JsonNode GetField(string name)
        {
            if (type == Type.OBJECT)
                for (int i = 0; i < keys.Count; i++)
                    if ((string)keys[i] == name)
                        return (JsonNode)list[i];
            return null;
        }

        public bool HasFields(string[] names)
        {
            foreach (string name in names)
                if (!keys.Contains(name))
                    return false;
            return true;
        }

        public bool HasField(string name)
        {
            if (type == Type.OBJECT)
                for (int i = 0; i < keys.Count; i++)
                    if ((string)keys[i] == name)
                        return true;
            return false;
        }

        public void Clear()
        {
            type = Type.NULL;
            if (list != null)
                list.Clear();
            if (keys != null)
                keys.Clear();
            str = "";
            n = 0;
            b = false;
        }

        public JsonNode Copy()
        {
            return new JsonNode(print());
        }

        /*
         * The Merge function is experimental. Use at your own risk.
         */
        public void Merge(JsonNode obj)
        {
            MergeRecur(this, obj);
        }

        /// <summary>
        /// Merge object right into left recursively
        /// </summary>
        /// <param name="left">The left (base) object</param>
        /// <param name="right">The right (new) object</param>
        static void MergeRecur(JsonNode left, JsonNode right)
        {
            if (left.type == Type.NULL)
                left.Absorb(right);
            else if (left.type == Type.OBJECT && right.type == Type.OBJECT)
            {
                for (int i = 0; i < right.list.Count; i++)
                {
                    string key = (string)right.keys[i];
                    if (right[i].isContainer)
                    {
                        if (left.HasField(key))
                            MergeRecur(left[key], right[i]);
                        else
                            left.AddField(key, right[i]);
                    }
                    else
                    {
                        if (left.HasField(key))
                            left.SetField(key, right[i]);
                        else
                            left.AddField(key, right[i]);
                    }
                }
            }
            else if (left.type == Type.ARRAY && right.type == Type.ARRAY)
            {
                if (right.Count > left.Count)
                {
                    Debug.LogError("Cannot merge arrays when right object has more elements");
                    return;
                }

                for (int i = 0; i < right.list.Count; i++)
                {
                    if (left[i].type == right[i].type)
                    {
                        //Only overwrite with the same type
                        if (left[i].isContainer)
                            MergeRecur(left[i], right[i]);
                        else
                        {
                            left[i] = right[i];
                        }
                    }
                }
            }
        }

        public string print()
        {
            return print(0);
        }

        #region STRINGIFY

        public string print(int depth)
        {
            //Convert the JsonNode into a string
            if (depth++ > MAX_DEPTH)
            {
                Debug.Log("reached max depth!");
                return "";
            }

            string str = "";
            switch (type)
            {
                case Type.STRING:
                    str = "\"" + this.str + "\"";
                    break;
                case Type.NUMBER:

                    if (double.IsInfinity(n))
                        str = INFINITY;
                    else if (double.IsNegativeInfinity(n))
                        str = NEGINFINITY;
                    else if (double.IsNaN(n))
                        str = NaN;

                    else
                        str += n;
                    break;

                case Type.OBJECT:
                    str = "{";
                    if (list.Count > 0)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            string key = (string)keys[i];
                            JsonNode obj = (JsonNode)list[i];
                            if (obj)
                            {
                                str += "\"" + key + "\":";
                                str += obj.print(depth) + ",";
                            }
                        }
                        str = str.Substring(0, str.Length - 1);
                    }
                    str += "}";
                    break;
                case Type.ARRAY:
                    str = "[";
                    if (list.Count > 0)
                    {
                        foreach (JsonNode obj in list)
                        {
                            if (obj)
                            {
                                str += obj.print(depth) + ",";
                            }
                        }
                        str = str.Substring(0, str.Length - 1);
                    }

                    str += "]";
                    break;
                case Type.BOOL:
                    if (b)
                        str = "true";
                    else
                        str = "false";
                    break;
                case Type.NULL:
                    str = "null";
                    break;
            }

            return str;
        }

        #endregion

        public static implicit operator WWWForm(JsonNode obj)
        {
            WWWForm form = new WWWForm();
            for (int i = 0; i < obj.list.Count; i++)
            {
                string key = i + "";
                if (obj.type == Type.OBJECT)
                    key = obj.keys[i];
                string val = obj.list[i].ToString();
                if (obj.list[i].type == Type.STRING)
                    val = val.Replace("\"", "");
                form.AddField(key, val);
            }

            return form;
        }

        public JsonNode this[int index]
        {
            get
            {
                if (list.Count > index) return (JsonNode)list[index];
                return null;
            }
            set
            {
                if (list.Count > index)
                    list[index] = value;
            }
        }

        public JsonNode this[string index]
        {
            get => GetField(index);
            set => SetField(index, value);
        }

        public override string ToString()
        {
            return print();
        }

        public Dictionary<string, string> ToDictionary()
        {
            if (type == Type.OBJECT)
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                for (int i = 0; i < list.Count; i++)
                {
                    JsonNode val = (JsonNode)list[i];
                    switch (val.type)
                    {
                        case Type.STRING: result.Add((string)keys[i], val.str); break;
                        case Type.NUMBER: result.Add((string)keys[i], val.n + ""); break;
                        case Type.BOOL: result.Add((string)keys[i], val.b + ""); break;
                        default:
                            Debug.LogWarning("Omitting object: " + (string)keys[i] + " in dictionary conversion");
                            break;
                    }
                }

                return result;
            }

            Debug.LogWarning("Tried to turn non-Object JsonNode into a dictionary");

            return null;
        }

        public static implicit operator bool(JsonNode o)
        {
            return (object)o != null;
        }
    }
}