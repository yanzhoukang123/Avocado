using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Azure.UpdateConfig
{
    class Program
    {
        //private static readonly string _CompanyName = "Azure Biosystems";
        //private static readonly string _ApplicationName = "Sapphire";
        private static string _InstallationType = string.Empty;
        private static string _InstallationModule = string.Empty;
        private static string _ConfigXmlPath = string.Empty;
        private static string _SysSettingsXmlPath = string.Empty;
        private static string _MethodsXmlPath = string.Empty;

        static int Main(string[] args)
        {
            // check if input arguments were supplied:
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: Azure.UpdateConfig <<InstallationOption> <InstallationModule> <ConfigPath> <SysSettingsPath>");
                return 1;
            }

            _InstallationType = args[0];    // Selected installation type (RGB/RGBNIR/NIR/NIR-Q)
            _InstallationModule = args[1];  // Selected module (Chemi/Phosphor/Chemi+Phosphor/None)
            _ConfigXmlPath = args[2];       // Full path to config.xml file to be modify
            _SysSettingsXmlPath = args[3];  // Full path to SysSettings.xml file to be modify
            _MethodsXmlPath = args[4];      // Full path to Method.xml file to be modify

            UpdateConfigXml();

            return 0;

        }

        /// <summary>
        /// Update config.xml file in ProgramData
        /// </summary>
        /// <param name="configPath"></param>
        /// <param name="filterPath"></param>
        /// <returns>returns true if the config file is modified</returns>
        static bool UpdateConfigXml()
        {
            bool bRetVal = false;

            if (!File.Exists(_ConfigXmlPath))
            {
                //Console.WriteLine("File doesn't exist");
                bRetVal = false;
            }
            else
            {
                bRetVal = UpdateDyeTypes(_ConfigXmlPath);
            }

            if (!File.Exists(_SysSettingsXmlPath))
            {
                //Console.WriteLine("File doesn't exist");
                bRetVal = false;
            }
            else
            {
                bRetVal = UpdateImagingTabs(_SysSettingsXmlPath);
            }

            if (!File.Exists(_MethodsXmlPath))
            {
                //Console.WriteLine("File doesn't exist");
                bRetVal = false;
            }
            else
            {
                bRetVal = UpdateMethods(_MethodsXmlPath);
            }

            return bRetVal;
        }

        static bool UpdateDyeTypes(string configPath)
        {
            bool bResult = false;
            bool bSaveDoc = false;

            XmlDocument xdoc = new XmlDocument();
            xdoc.PreserveWhitespace = true;
            xdoc.Load(configPath);

            XmlNodeList lightElements = xdoc.GetElementsByTagName("Dye");

            if (lightElements != null)
            {
                XmlNode commentNode = null;
                for (int i = 0; i < lightElements.Count; i++)
                {
                    if (lightElements[i].NodeType == XmlNodeType.Element)
                    {
                        string attrLaserName = lightElements[i].Attributes["Laser"].Value;

                        if (_InstallationType.ToUpper() == "RGB")
                        {
                            // No wavelength: 784
                            if (attrLaserName.Equals("LaserA"))
                            {
                                try
                                {
                                    commentNode = xdoc.CreateComment(lightElements[i].OuterXml);
                                    lightElements[i].ParentNode.ReplaceChild(commentNode, lightElements[i]);
                                    i--; // hack: for some reason it skips a node after a child is replaced.
                                    bSaveDoc = true;
                                }
                                catch
                                {
                                    bResult = false;
                                    bSaveDoc = false;
                                }
                            }
                        }
                        else if (_InstallationType.ToUpper() == "NIR")
                        {
                            // No wavelength: 488/520
                            if (attrLaserName.Equals("LaserD") || attrLaserName.Equals("LaserB"))
                            {
                                try
                                {
                                    commentNode = xdoc.CreateComment(lightElements[i].OuterXml);
                                    lightElements[i].ParentNode.ReplaceChild(commentNode, lightElements[i]);
                                    i--; // hack: for some reason it skips a node after a child is replaced.
                                    bSaveDoc = true;
                                }
                                catch
                                {
                                    bResult = false;
                                    bSaveDoc = false;
                                }
                            }
                        }
                        else if (_InstallationType.ToUpper() == "NIR-Q")
                        {
                            // No wavelength: 488
                            if (attrLaserName.Equals("LaserD"))
                            {
                                try
                                {
                                    commentNode = xdoc.CreateComment(lightElements[i].OuterXml);
                                    lightElements[i].ParentNode.ReplaceChild(commentNode, lightElements[i]);
                                    i--; // hack: for some reason it skips a node after a child is replaced.
                                    bSaveDoc = true;
                                }
                                catch
                                {
                                    bResult = false;
                                    bSaveDoc = false;
                                }
                            }
                        }
                    }
                }

                // save changes to file 
                if (bSaveDoc)
                {
                    try
                    {
                        xdoc.Save(configPath);
                        bResult = true;
                    }
                    catch (Exception e)
                    {
                        bResult = false;
                        Console.WriteLine("Updating config.xml error: {0}", e.Message);
                    }
                }
            }

            return bResult;
        }

        static bool UpdateImagingTabs(string configPath)
        {
            bool bResult = false;
            bool bSaveDoc = false;
            bool bIsFluorescenceVisible = false;
            //bool bIsPhosphorVisible = false;
            bool bIsChemiVisible = false;
            //bool bIsVisibleVisible = false;

            // RGB, NIR or NIR-Q selected
            if (_InstallationType.ToUpper() == "RGB" ||
                _InstallationType.ToUpper() == "NIR" ||
                _InstallationType.ToUpper() == "RGBNIR" ||
                _InstallationType.ToUpper() == "NIR-Q")
            {
                bIsFluorescenceVisible = true;
            }

            if (_InstallationModule.Equals("None", StringComparison.InvariantCultureIgnoreCase))
            {
                bIsChemiVisible = false;
            }
            else if (_InstallationModule.Equals("Chemi", StringComparison.InvariantCultureIgnoreCase))
            {
                bIsChemiVisible = true;
            }

            // Update SysSettings.xml file
            //
            XmlDocument xdoc = new XmlDocument();

            try
            {
                xdoc.PreserveWhitespace = true;
                xdoc.Load(configPath);

                XmlNodeList imagingTabElements = xdoc.GetElementsByTagName("ImagingTab");

                if (imagingTabElements != null)
                {
                    for (int i = 0; i < imagingTabElements.Count; i++)
                    {
                        if (imagingTabElements[i].NodeType == XmlNodeType.Element)
                        {
                            string attrImagingType = imagingTabElements[i].Attributes["ImagingType"].Value;
                            if (attrImagingType.Equals("Fluorescence", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (bIsFluorescenceVisible)
                                {
                                    if (Convert.ToBoolean(imagingTabElements[i].Attributes["IsVisible"].Value) == false)
                                    {
                                        imagingTabElements[i].Attributes["IsVisible"].Value = "True";
                                        bSaveDoc = true;
                                    }
                                }
                                else
                                {
                                    if (Convert.ToBoolean(imagingTabElements[i].Attributes["IsVisible"].Value) == true)
                                    {
                                        imagingTabElements[i].Attributes["IsVisible"].Value = "False";
                                        bSaveDoc = true;
                                    }
                                }
                            }
                            else if (attrImagingType.Equals("Chemiluminescence", StringComparison.InvariantCultureIgnoreCase) ||
                                    attrImagingType.Equals("Visible", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (attrImagingType.Equals("Visible", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (bIsChemiVisible)
                                    {
                                        if (Convert.ToBoolean(imagingTabElements[i].Attributes["IsVisible"].Value) == false)
                                        {
                                            imagingTabElements[i].Attributes["IsVisible"].Value = "True";
                                            bSaveDoc = true;
                                        }
                                    }
                                    else
                                    {
                                        if (Convert.ToBoolean(imagingTabElements[i].Attributes["IsVisible"].Value) == true)
                                        {
                                            imagingTabElements[i].Attributes["IsVisible"].Value = "False";
                                            bSaveDoc = true;
                                        }
                                    }
                                }
                                else if (attrImagingType.Equals("Chemiluminescence", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (bIsChemiVisible)
                                    {
                                        if (Convert.ToBoolean(imagingTabElements[i].Attributes["IsVisible"].Value) == false)
                                        {
                                            imagingTabElements[i].Attributes["IsVisible"].Value = "True";
                                            bSaveDoc = true;
                                        }
                                    }
                                    else
                                    {
                                        if (Convert.ToBoolean(imagingTabElements[i].Attributes["IsVisible"].Value) == true)
                                        {
                                            imagingTabElements[i].Attributes["IsVisible"].Value = "False";
                                            bSaveDoc = true;
                                        }
                                    }
                                }
                            }
                            /*else if (attrImagingType.Equals("PhosphorImaging", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (bIsPhosphorVisible)
                                {
                                    if (Convert.ToBoolean(imagingTabElements[i].Attributes["IsVisible"].Value) == false)
                                    {
                                        imagingTabElements[i].Attributes["IsVisible"].Value = "True";
                                        bSaveDoc = true;
                                    }
                                }
                                else
                                {
                                    if (Convert.ToBoolean(imagingTabElements[i].Attributes["IsVisible"].Value) == true)
                                    {
                                        imagingTabElements[i].Attributes["IsVisible"].Value = "False";
                                        bSaveDoc = true;
                                    }
                                }
                            }*/
                        }
                    }
                }
            }
            catch (Exception)
            {
                bResult = false;
                bSaveDoc = false;
            }

            if (bSaveDoc)
            {
                try
                {
                    xdoc.Save(configPath);
                    bResult = true;
                }
                catch (Exception e)
                {
                    bResult = false;
                    Console.WriteLine("Updating config.xml error: {0}", e.Message);
                }

            }

            return bResult;
        }

        static bool UpdateMethods(string configPath)
        {
            bool bResult = false;
            bool bSaveDoc = false;

            if (_InstallationType.ToUpper() == "RGB" ||
                _InstallationType.ToUpper() == "NIR" ||
                _InstallationType.ToUpper() == "NIR-Q")
            {
                XmlDocument xdoc = new XmlDocument();
                xdoc.PreserveWhitespace = true;
                xdoc.Load(configPath);

                XmlNodeList lightElements = xdoc.GetElementsByTagName("Method");

                if (lightElements != null)
                {
                    XmlNode commentNode = null;
                    for (int i = 0; i < lightElements.Count; i++)
                    {
                        if (lightElements[i].NodeType == XmlNodeType.Element)
                        {
                            string attrDisplayName = lightElements[i].Attributes["DisplayName"].Value;

                            // Comment out "4 Channel Western" method
                            if (attrDisplayName.IndexOf("4 Channel", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                try
                                {
                                    commentNode = xdoc.CreateComment(lightElements[i].OuterXml);
                                    lightElements[i].ParentNode.ReplaceChild(commentNode, lightElements[i]);
                                    i--; // hack: for some reason it skips a node after a child is replaced.
                                    bSaveDoc = true;
                                }
                                catch
                                {
                                    bResult = false;
                                    bSaveDoc = false;
                                }
                            }

                            if (_InstallationType.ToUpper() == "NIR" ||
                                _InstallationType.ToUpper() == "NIR-Q")
                            {
                                // Comment out "Visible Fluorescent Western" method
                                if (attrDisplayName.IndexOf("Visible Fluorescent Western", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    try
                                    {
                                        commentNode = xdoc.CreateComment(lightElements[i].OuterXml);
                                        lightElements[i].ParentNode.ReplaceChild(commentNode, lightElements[i]);
                                        i--; // hack: for some reason it skips a node after a child is replaced.
                                        bSaveDoc = true;
                                    }
                                    catch
                                    {
                                        bResult = false;
                                        bSaveDoc = false;
                                    }
                                }
                            }
                        }
                    }

                    // save changes to file 
                    if (bSaveDoc)
                    {
                        try
                        {
                            xdoc.Beautify();
                            xdoc.Save(configPath);
                            bResult = true;
                        }
                        catch (Exception e)
                        {
                            bResult = false;
                            Console.WriteLine("Updating config.xml error: {0}", e.Message);
                        }
                    }
                }
            }
            else
            {
                /*var doc = XElement.Load(configPath);
                var comments = doc.DescendantNodes().OfType<XComment>();

                if (comments != null)
                {
                    /*foreach (XComment comment in comments)
                    {
                        if (comment.Value.IndexOf("4 Channel Western", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            comment.Remove();
                            bSaveDoc = true;
                            break;
                       }
                    }
                }
                */

                XmlDocument xdoc = new XmlDocument();
                xdoc.PreserveWhitespace = true;
                xdoc.Load(configPath);

                XmlNodeList commentedNodes = xdoc.SelectNodes("//comment()");
                var commentNode = (from comment in commentedNodes.Cast<XmlNode>()
                                   where comment.Value.Contains("4 Channel Western")
                                   select comment).FirstOrDefault();

                if (commentNode != null)
                {
                    XmlReader nodeReader = XmlReader.Create(new StringReader(commentNode.Value));
                    XmlNode newNode = xdoc.ReadNode(nodeReader);
                    commentNode.ParentNode.ReplaceChild(newNode, commentNode);
                    bSaveDoc = true;
                }

                // save changes to file 
                if (bSaveDoc)
                {
                    try
                    {
                        /*StringBuilder sb = new StringBuilder();
                        XmlWriterSettings settings = new XmlWriterSettings
                        {
                            Indent = true,
                            IndentChars = "  ",
                            NewLineChars = "\r\n",
                            NewLineHandling = NewLineHandling.Replace
                        };
                        using (XmlWriter writer = XmlWriter.Create(sb, settings))
                        {
                            xdoc.Save(writer);
                        }*/

                        xdoc.Save(configPath);
                        bResult = true;
                    }
                    catch (Exception e)
                    {
                        bResult = false;
                        Console.WriteLine("Updating config.xml error: {0}", e.Message);
                    }
                }
            }

            return bResult;
        }


        /*public static void RemoveWithNextWhitespace(this XElement element)
        {
            IEnumerable<XText> textNodes
                = element.NodesAfterSelf()
                         .TakeWhile(node => node is XText).Cast<XText>();
            if (element.ElementsAfterSelf().Any())
            {
                // Easy case, remove following text nodes.
                textNodes.ToList().ForEach(node => node.Remove());
            }
            else
            {
                // Remove trailing whitespace.
                textNodes.TakeWhile(text => !text.Value.Contains("\n"))
                         .ToList().ForEach(text => text.Remove());
                // Fetch text node containing newline, if any.
                XText newLineTextNode
                    = element.NodesAfterSelf().OfType<XText>().FirstOrDefault();
                if (newLineTextNode != null)
                {
                    string value = newLineTextNode.Value;
                    if (value.Length > 1)
                    {
                        // Composite text node, trim until newline (inclusive).
                        newLineTextNode.AddAfterSelf(
                            new XText(value.Substring(value.IndexOf('\n') + 1)));
                    }
                    // Remove original node.
                    newLineTextNode.Remove();
                }
            }
            element.Remove();
        }*/

        /*public static XmlNodeList Scan(XmlNodeList nodeList)
        {
            List<XmlNode> toRemove = new List<XmlNode>();

            foreach (XmlNode xmlElement in nodeList)
            {
                string elementValue = xmlElement.OuterXml;
                if (elementValue.ToLower().Contains("rgb"))
                {
                    toRemove.Add(xmlElement);
                }
            }

            foreach (XmlNode xmlElement in toRemove)
            {
                XmlNode node = xmlElement.ParentNode;
                node.RemoveChild(xmlElement);
            }

            foreach (XmlNode xmlElement in nodeList)
            {
                string elementValue = xmlElement.OuterXml;
                if (string.IsNullOrWhiteSpace(elementValue))
                {
                    XmlNode node = xmlElement.ParentNode;
                    node.RemoveChild(xmlElement);
                }
            }

            return nodeList;
        }*/

        /*static string Beautify(this XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }*/

    }

    public static class XmlDocExtension
    {
        public static string ToIndentedString(this XmlDocument doc)
        {
            var stringWriter = new System.IO.StringWriter(new StringBuilder());
            var xmlTextWriter = new XmlTextWriter(stringWriter) { Formatting = Formatting.Indented, IndentChar = ' ' };
            doc.Save(xmlTextWriter);
            return stringWriter.ToString();
        }

        public static string Beautify(this XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }
            return sb.ToString();
        }

    }

}
