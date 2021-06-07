using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ink.Parsed;
using Choice = Ink.Parsed.Choice;
using Story = Ink.Parsed.Story;

namespace InkPlugin
{
    internal class ExtraDataOutputPlugin : Ink.IPlugin
    {
        public ExtraDataOutputPlugin ()
        {
        }

        public void PostParse(Story parsedStory, string storyInputFile, string storyOutputFile)
        {
            if(parsedStory == null)
                return;
            
            var choiceTextList = new List<Text>();
            var lineTextList = new List<Text>();

            var choiceJsonList = new List<object>();
            var lineJsonList = new List<object>();
            var knotJsonList = new List<object>();
            
            var allChoices = parsedStory.FindAll<Choice>(choice => choice.debugMetadata != null && choice.debugMetadata.fileName == storyInputFile);
            foreach(Choice choice in allChoices)
            {
                Knot knot = FindKnotParent(choice);

                if(choice.startContent != null)
                {
                    Text firstText = choice.startContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceTextList.Add(firstText);
                        lineTextList.Add(firstText);
                    }
                }
                else if(choice.choiceOnlyContent != null)
                {
                    Text firstText = choice.choiceOnlyContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceTextList.Add(firstText);
                    }
                }
                else if(choice.innerContent != null)
                {
                    Text firstText = choice.innerContent.content[0] as Text;
                    if(firstText != null)
                    {
                        choiceTextList.Add(firstText);
                        lineTextList.Add(firstText);
                    }
                }
            }

            var allText = parsedStory.FindAll<Text>(text => text.debugMetadata != null && text.debugMetadata.fileName == storyInputFile);
            foreach(Text text in allText)
            {
                if(text.text != null && text.text.Trim(' ', '\n', '\t').Length > 0)
                {
                    if(text.text.StartsWith("Scene:")
                       || text.text.StartsWith("Trigger:")
                       || text.text.StartsWith("<Trigger:")
                       || text.text.StartsWith("Action:"))
                        continue;

                    Knot knot = FindKnotParent(text);

                    if(text.parent != null && text.parent.typeName == "StringExpression")
                    {
#if DEBUG
                        Console.WriteLine("Skipping {0} because text.parent is StringExpression", text);
#endif
                        continue;
                    }

                    // If this text has already been processed as a choice,
                    // it will already have a key at the start - don't re-process and
                    // add an unnecessary entry.
                    if(!choiceTextList.Exists(t => string.Equals(t.text, text.text)))
                    {
                        lineTextList.Add(text);
                    }
                }
            }

            foreach(Text choiceText in choiceTextList)
            {
                choiceJsonList.Add(choiceText.text);
            }

            foreach(Text lineText in lineTextList)
            {
                lineJsonList.Add(lineText.text);
            }

            var allKnots = parsedStory.FindAll<Knot>(knot => knot.debugMetadata != null && knot.debugMetadata.fileName == storyInputFile);
            foreach(var knot in allKnots)
            {
                knotJsonList.Add(knot.name);
            }

            Dictionary<string, object> dataDictionary = new Dictionary<string, object>
            {
                {"ChoiceList", choiceJsonList},
                {"LineList", lineJsonList},
                {"KnotList", knotJsonList}
            };

            var jsonString =  SimpleJson.DictionaryToText(dataDictionary);

            string outputFile = storyOutputFile.Replace(".json", "_extradata.json");

            File.WriteAllText(outputFile, jsonString, System.Text.Encoding.UTF8);

#if DEBUG
            Console.Read();
#endif
        }

        private static Knot FindKnotParent(Ink.Parsed.Object choice)
        {
            Ink.Parsed.Object parent = choice.parent;
            while(parent != null)
            {
                if(parent is Knot)
                    return parent as Knot;

                parent = parent.parent;
            }

            return null;
        }

        public void PostExport(Ink.Parsed.Story parsedStory, Ink.Runtime.Story runtimeStory)
        {

        }
        
        // We've copied the old version of SimpleJson into here,
        // before it got changed to no longer be compatible with ExtraDataOutputPlugin - JSB
        
        /// <summary>
        /// Simple custom JSON serialisation implementation that takes JSON-able System.Collections that
        /// are produced by the ink engine and converts to and from JSON text.
        /// </summary>
        internal static class SimpleJson
        {
            public static string DictionaryToText (Dictionary<string, object> rootObject)
            {
                return new Writer (rootObject).ToString ();
            }

            public static Dictionary<string, object> TextToDictionary (string text)
            {
                return new Reader (text).ToDictionary ();
            }

            class Reader
            {
                public Reader (string text)
                {
                    _text = text;
                    _offset = 0;

                    SkipWhitespace ();

                    _rootObject = ReadObject ();
                }

                public Dictionary<string, object> ToDictionary ()
                {
                    return (Dictionary<string, object>)_rootObject;
                }

                bool IsNumberChar (char c)
                {
                    return c >= '0' && c <= '9' || c == '.' || c == '-' || c == '+' || c == 'E' || c == 'e';
                }

                bool IsFirstNumberChar(char c)
                {
                    return c >= '0' && c <= '9' || c == '-' || c == '+';
                }

                object ReadObject ()
                {
                    var currentChar = _text [_offset];

                    if( currentChar == '{' )
                        return ReadDictionary ();
                    
                    else if (currentChar == '[')
                        return ReadArray ();

                    else if (currentChar == '"')
                        return ReadString ();

                    else if (IsFirstNumberChar(currentChar))
                        return ReadNumber ();

                    else if (TryRead ("true"))
                        return true;

                    else if (TryRead ("false"))
                        return false;

                    else if (TryRead ("null"))
                        return null;

                    throw new System.Exception ("Unhandled object type in JSON: "+_text.Substring (_offset, 30));
                }

                Dictionary<string, object> ReadDictionary ()
                {
                    var dict = new Dictionary<string, object> ();

                    Expect ("{");

                    SkipWhitespace ();

                    // Empty dictionary?
                    if (TryRead ("}"))
                        return dict;

                    do {

                        SkipWhitespace ();

                        // Key
                        var key = ReadString ();
                        Expect (key != null, "dictionary key");

                        SkipWhitespace ();

                        // :
                        Expect (":");

                        SkipWhitespace ();

                        // Value
                        var val = ReadObject ();
                        Expect (val != null, "dictionary value");

                        // Add to dictionary
                        dict [key] = val;

                        SkipWhitespace ();

                    } while ( TryRead (",") );

                    Expect ("}");

                    return dict;
                }

                List<object> ReadArray ()
                {
                    var list = new List<object> ();

                    Expect ("[");

                    SkipWhitespace ();

                    // Empty list?
                    if (TryRead ("]"))
                        return list;

                    do {

                        SkipWhitespace ();

                        // Value
                        var val = ReadObject ();

                        // Add to array
                        list.Add (val);

                        SkipWhitespace ();

                    } while (TryRead (","));

                    Expect ("]");

                    return list;
                }

                string ReadString ()
                {
                    Expect ("\"");

                    var sb = new StringBuilder();

                    for (; _offset < _text.Length; _offset++) {
                        var c = _text [_offset];

                        if (c == '\\') {
                            // Escaped character
                            _offset++;
                            if (_offset >= _text.Length) {
                                throw new Exception("Unexpected EOF while reading string");
                            }
                            c = _text[_offset];
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/': // Yes, JSON allows this to be escaped
                                    sb.Append(c);
                                    break;
                                case 'n':
                                    sb.Append('\n');
                                    break;
                                case 't':
                                    sb.Append('\t');
                                    break;
                                case 'r':
                                case 'b':
                                case 'f':
                                    // Ignore other control characters
                                    break;
                                case 'u':
                                    // 4-digit Unicode
                                    if (_offset + 4 >=_text.Length) {
                                        throw new Exception("Unexpected EOF while reading string");
                                    }
                                    var digits = _text.Substring(_offset + 1, 4);
                                    int uchar;
                                    if (int.TryParse(digits, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.InvariantCulture, out uchar)) {
                                        sb.Append((char)uchar);
                                        _offset += 4;
                                    } else {
                                        throw new Exception("Invalid Unicode escape character at offset " + (_offset - 1));
                                    }
                                    break;
                                default:
                                    // The escaped character is invalid per json spec
                                    throw new Exception("Invalid Unicode escape character at offset " + (_offset - 1));
                            }
                        } else if( c == '"' ) {
                            break;
                        } else {
                            sb.Append(c);
                        }
                    }

                    Expect ("\"");
                    return sb.ToString();
                }

                object ReadNumber ()
                {
                    var startOffset = _offset;

                    bool isFloat = false;
                    for (; _offset < _text.Length; _offset++) {
                        var c = _text [_offset];
                        if (c == '.' || c == 'e' || c == 'E') isFloat = true;
                        if (IsNumberChar (c))
                            continue;
                        else
                            break;
                    }

                    string numStr = _text.Substring (startOffset, _offset - startOffset);

                    if (isFloat) {
                        float f;
                        if (float.TryParse (numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) {
                            return f;
                        }
                    } else {
                        int i;
                        if (int.TryParse (numStr, out i)) {
                            return i;
                        }
                    }

                    throw new System.Exception ("Failed to parse number value: "+numStr);
                }

                bool TryRead (string textToRead)
                {
                    if (_offset + textToRead.Length > _text.Length)
                        return false;
                    
                    for (int i = 0; i < textToRead.Length; i++) {
                        if (textToRead [i] != _text [_offset + i])
                            return false;
                    }

                    _offset += textToRead.Length;

                    return true;
                }

                void Expect (string expectedStr)
                {
                    if (!TryRead (expectedStr))
                        Expect (false, expectedStr);
                }

                void Expect (bool condition, string message = null)
                {
                    if (!condition) {
                        if (message == null) {
                            message = "Unexpected token";
                        } else {
                            message = "Expected " + message;
                        }
                        message += " at offset " + _offset;

                        throw new System.Exception (message);
                    }
                }

                void SkipWhitespace ()
                {
                    while (_offset < _text.Length) {
                        var c = _text [_offset];
                        if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                            _offset++;
                        else
                            break;
                    }
                }

                string _text;
                int _offset;

                object _rootObject;
            }

            class Writer
            {
                public Writer (object rootObject)
                {
                    _sb = new StringBuilder ();

                    WriteObject (rootObject);
                }

                void WriteObject (object obj)
                {
                    if (obj is int) {
                        _sb.Append ((int)obj);
                    } else if (obj is float) {
                        string floatStr = ((float)obj).ToString(System.Globalization.CultureInfo.InvariantCulture);
                        if( floatStr == "Infinity" ) {
                            _sb.Append("3.4E+38"); // JSON doesn't support, do our best alternative
                        } else if (floatStr == "-Infinity") {
                            _sb.Append("-3.4E+38");
                        } else if ( floatStr == "NaN" ) {
                            _sb.Append("0.0"); // JSON doesn't support, not much we can do
                        } else {
                            _sb.Append(floatStr);
                            if (!floatStr.Contains(".") && !floatStr.Contains("E")) 
                                _sb.Append(".0"); // ensure it gets read back in as a floating point value
                        }
                    } else if( obj is bool) {
                        _sb.Append ((bool)obj == true ? "true" : "false");
                    } else if (obj == null) {
                        _sb.Append ("null");
                    } else if (obj is string) {
                        string str = (string)obj;
                        _sb.EnsureCapacity(_sb.Length + str.Length + 2);
                        _sb.Append('"');

                        foreach (var c in str)
                        {
                            if (c < ' ')
                            {
                                // Don't write any control characters except \n and \t
                                switch (c)
                                {
                                    case '\n':
                                        _sb.Append("\\n");
                                        break;
                                    case '\t':
                                        _sb.Append("\\t");
                                        break;
                                }
                            }
                            else
                            {
                                switch (c)
                                {
                                    case '\\':
                                    case '"':
                                        _sb.Append('\\').Append(c);
                                        break;
                                    default:
                                        _sb.Append(c);
                                        break;
                                }
                            }
                        }

                        _sb.Append('"');
                    } else if (obj is Dictionary<string, object>) {
                        WriteDictionary ((Dictionary<string, object>)obj);
                    } else if (obj is List<object>) {
                        WriteList ((List<object>)obj);
                    }else {
                        throw new System.Exception ("ink's SimpleJson writer doesn't currently support this object: " + obj);
                    }
                }

                void WriteDictionary (Dictionary<string, object> dict)
                {
                    _sb.Append ("{");

                    bool isFirst = true;
                    foreach (var keyValue in dict) {

                        if (!isFirst) _sb.Append (",");

                        _sb.Append ("\"");
                        _sb.Append (keyValue.Key);
                        _sb.Append ("\":");

                        WriteObject (keyValue.Value);

                        isFirst = false;
                    }

                    _sb.Append ("}");
                }

                void WriteList (List<object> list)
                {
                    _sb.Append ("[");

                    bool isFirst = true;
                    foreach (var obj in list) {
                        if (!isFirst) _sb.Append (",");

                        WriteObject (obj);

                        isFirst = false;
                    }

                    _sb.Append ("]");
                }

                public override string ToString ()
                {
                    return _sb.ToString ();
                }


                StringBuilder _sb;
            }
        }
    }
}
