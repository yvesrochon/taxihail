﻿using System.Linq;
using System.Xml;

namespace apcurium.Tools.Localization
{
    public static class CsProjHelper
    {
        public static void IncludeFile(string projectPath, string filePath, string parentNodeName)
        {
            var csProjDocument = new XmlDocument();
            csProjDocument.Load(projectPath);

            XmlNode root = csProjDocument.DocumentElement;
            if (root != null)
            {
                XmlElement node = csProjDocument.CreateElement(parentNodeName, root.NamespaceURI);
                node.SetAttribute("Include", filePath);

                foreach (var child in root.ChildNodes)
                {
                    var childNode = child as XmlNode;
                    if (childNode != null
                        && childNode.Name == "ItemGroup"
                        && childNode.ChildNodes.OfType<XmlNode>().Any(c => c.Name == parentNodeName))
                    {
                        // Append the new resource with the other BundleResource tags
                        if (!childNode.ChildNodes.OfType<XmlNode>().Contains(node))
                        {
                            childNode.AppendChild(node);
                        }
                        break;
                    }
                }

                csProjDocument.Save(projectPath);
            }
        }
    }
}
