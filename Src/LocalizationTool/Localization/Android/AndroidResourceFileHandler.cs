﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace apcurium.Tools.Localization.Android
{
    //\mk-taxi\Src\Mobile\Android\Resources\Values\String.xml
    public class AndroidResourceFileHandler : ResourceFileHandlerBase
    {
        public AndroidResourceFileHandler(string filePath)
            : base(filePath)
        {
            var document = XElement.Load(filePath);

            foreach (var localization in document.Elements().Where(e => e.Name.ToString().Equals("string", StringComparison.OrdinalIgnoreCase)))
            {
                var key = localization.FirstAttribute.Value;

                TryAdd(key, Decode(localization.Value));
            }
        }

        public AndroidResourceFileHandler(string filePath, IDictionary<string, string> dictionary)
            : base(filePath, dictionary)
        {

        }

        protected string Decode(string text)
        {
            return DecodeAndroid(DecodeXML(text));
        }

        private string DecodeAndroid(string text)
        {
            return text.Replace("\\'", "'");
        }

        protected virtual string DecodeXML(string text)
        {
            //Others invalid characters does not look to be escaped...
            //encodedXml = xml.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
            return text.Replace("&lt;", "<").Replace("&gt;", ">");
        }

        protected override string GetFileText()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<resources>\r\n");

            //<string name="ApplicationName">TaxiHail</string>
            foreach (var resource in this)
            {
                stringBuilder.AppendFormat("  <string name=\"{0}\">{1}</string>\r\n", resource.Key, Encode(resource.Value));
            }

            stringBuilder.Append("</resources>");

            return stringBuilder.ToString();
        }

        protected virtual string EncodeXML(string text)
        {
            //Others invalid characters does not look to be escaped...
            //encodedXml = xml.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
            return text.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        protected virtual string EncodeAndroid(string text)
        {
            return text.Replace("'", "\\'");
        }

        protected virtual string Encode(string text)
        {
            return EncodeAndroid(EncodeXML(text));
        }
    }
}
