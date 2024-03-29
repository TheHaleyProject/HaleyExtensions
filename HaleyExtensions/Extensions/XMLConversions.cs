﻿using System.Xml;
using System.Xml.Linq;


namespace Haley.Utils
{
    public static class XMLConversions
    {
        public static XmlDocument ToXMLDocument(this XDocument doc)
        {
            var xmlDocument = new XmlDocument();
            using (var xmlReader = doc.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }
            return xmlDocument;
        }

        public static XDocument ToXDocument(this XmlDocument doc)
        {
            using (var nodeReader = new XmlNodeReader(doc))
            {
                nodeReader.MoveToContent();
                return XDocument.Load(nodeReader);
            }
        }
    }
}
