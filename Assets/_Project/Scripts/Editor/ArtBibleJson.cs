using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// A small strict JSON reader for docs/art/data/*.json and the ArtForge manifest, which
    /// <c>JsonUtility</c> cannot read (nested arrays of objects, fields that change type). Objects
    /// become Dictionary&lt;string, object&gt;, arrays List&lt;object&gt;, numbers double. Malformed input
    /// throws <see cref="FormatException"/>.
    /// </summary>
    public static class ArtBibleJson
    {
        public static object Parse(string json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));
            var reader = new Reader(json);
            object value = reader.ReadValue();
            reader.SkipWhitespace();
            if (!reader.AtEnd)
                throw reader.Error("trailing characters after the JSON value");
            return value;
        }

        /// <summary>The member <paramref name="key"/> of an object, or null when absent or not an object.</summary>
        public static object Get(object node, string key) =>
            node is Dictionary<string, object> map && map.TryGetValue(key, out object value) ? value : null;

        public static string GetString(object node, string key) => Get(node, key) as string;

        public static float? GetFloat(object node, string key) =>
            Get(node, key) is double number ? (float)number : (float?)null;

        public static List<object> GetList(object node, string key) =>
            Get(node, key) as List<object> ?? new List<object>();

        private sealed class Reader
        {
            private readonly string _text;
            private int _index;

            public Reader(string text) => _text = text;

            public bool AtEnd => _index >= _text.Length;

            public FormatException Error(string message) =>
                new FormatException($"JSON: {message} at offset {_index}.");

            public void SkipWhitespace()
            {
                while (!AtEnd && char.IsWhiteSpace(_text[_index]))
                    _index++;
            }

            public object ReadValue()
            {
                SkipWhitespace();
                if (AtEnd)
                    throw Error("unexpected end of input");

                char c = _text[_index];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || char.IsDigit(c))
                            return ReadNumber();
                        throw Error($"unexpected character '{c}'");
                }
            }

            private Dictionary<string, object> ReadObject()
            {
                var map = new Dictionary<string, object>();
                _index++; // {
                SkipWhitespace();
                if (!AtEnd && _text[_index] == '}')
                {
                    _index++;
                    return map;
                }

                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _text[_index] != '"')
                        throw Error("expected a member name");
                    string key = ReadString();
                    SkipWhitespace();
                    if (AtEnd || _text[_index] != ':')
                        throw Error("expected ':'");
                    _index++;
                    map[key] = ReadValue();
                    SkipWhitespace();
                    if (AtEnd)
                        throw Error("unterminated object");
                    char c = _text[_index++];
                    if (c == '}')
                        return map;
                    if (c != ',')
                        throw Error("expected ',' or '}'");
                }
            }

            private List<object> ReadArray()
            {
                var list = new List<object>();
                _index++; // [
                SkipWhitespace();
                if (!AtEnd && _text[_index] == ']')
                {
                    _index++;
                    return list;
                }

                while (true)
                {
                    list.Add(ReadValue());
                    SkipWhitespace();
                    if (AtEnd)
                        throw Error("unterminated array");
                    char c = _text[_index++];
                    if (c == ']')
                        return list;
                    if (c != ',')
                        throw Error("expected ',' or ']'");
                }
            }

            private string ReadString()
            {
                _index++; // opening quote
                var builder = new StringBuilder();
                while (true)
                {
                    if (AtEnd)
                        throw Error("unterminated string");
                    char c = _text[_index++];
                    if (c == '"')
                        return builder.ToString();
                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (AtEnd)
                        throw Error("unterminated escape");
                    char escape = _text[_index++];
                    switch (escape)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                            if (_index + 4 > _text.Length)
                                throw Error("truncated \\u escape");
                            builder.Append((char)int.Parse(_text.Substring(_index, 4), NumberStyles.HexNumber,
                                CultureInfo.InvariantCulture));
                            _index += 4;
                            break;
                        default:
                            throw Error($"unknown escape '\\{escape}'");
                    }
                }
            }

            private double ReadNumber()
            {
                int start = _index;
                while (!AtEnd && "+-0123456789.eE".IndexOf(_text[_index]) >= 0)
                    _index++;
                string token = _text.Substring(start, _index - start);
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    throw Error($"malformed number '{token}'");
                return value;
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_text, _index, word, 0, word.Length) != 0)
                    throw Error($"expected '{word}'");
                _index += word.Length;
            }
        }
    }
}
