using DocumentFormat.OpenXml.Packaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DigitalCap.Core.Helpers.Extensions
{
    public static class OpenXmlExtensions
    {
        // Based on https://github.com/OfficeDev/Open-Xml-PowerTools/blob/2f9134bd5abe0547fcf3d803b40b1401d6e58020/OpenXmlPowerTools/PtOpenXmlUtil.cs
        public static XDocument GetXDocument(this OpenXmlPart part)
        {
            if (part == null)
                throw new ArgumentNullException("part");

            var partXDocument = part.Annotation<XDocument>();
            if (partXDocument != null)
                return partXDocument;

            using (var partStream = part.GetStream())
                partXDocument = partStream.Length > 0
                    ? XDocument.Load(partStream)
                    : new XDocument { Declaration = new XDeclaration("1.0", "UTF-8", "yes") };

            part.AddAnnotation(partXDocument);

            return partXDocument;
        }

        public static void PutXDocument(this OpenXmlPart part, XDocument document)
        {
            if (part == null)
                throw new ArgumentNullException("part");

            if (document == null)
                throw new ArgumentNullException("document");

            using (var partStream = part.GetStream(FileMode.Create, FileAccess.Write))
            using (var partXmlWriter = XmlWriter.Create(partStream))
                document.Save(partXmlWriter);

            part.RemoveAnnotations<XDocument>();
            part.AddAnnotation(document);
        }
    }
}
