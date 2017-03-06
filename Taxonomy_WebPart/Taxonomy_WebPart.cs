using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Taxonomy;
using System.Collections.Generic;
using System.Web.Caching;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;
using System.Text;


namespace Taxonomy_WebPart.Taxonomy_WebPart
{
    [ToolboxItemAttribute(false)]
    public class Taxonomy_WebPart : WebPart
    {

        int rootTermCount = 0;

        public Taxonomy_WebPart()
        {
            OutputCacheTimeOut = 5;
        }


        string xmlOutput = "";

        Dictionary<string, int> itemCounter = new Dictionary<string, int>();
        int CurrDepth = 0;

        /// <summary>
        /// Key used to store the HTML output in the HTTP cache
        /// </summary>
        private string OutputCacheKey
        {
            get
            {
                return "Taxanomy" + this.UniqueID;
            }
        }


        // web part properties
        #region WebPartProperties
        private string _WebUrl = "";
        [Personalizable(PersonalizationScope.Shared), WebBrowsable(true),
        WebDisplayName("WebURL"),
        WebDescription("Enter the server relative url of the web that contains the list to be used. If left blank, the current web is assumed.")]
        public string WebUrl
        {
            get
            {
                return _WebUrl;
            }
            set
            {
                _WebUrl = value;
            }
        }

        private string _ListName = "";
        [Personalizable(PersonalizationScope.Shared), WebBrowsable(true),
        WebDisplayName("ListName"),
        WebDescription("Enter the name of the list that contains the items to be displayed via a taxonomy hierarchy.")]
        public string ListName
        {
            get
            {
                return _ListName;
            }
            set
            {
                _ListName = value;
            }
        }

        private string _FieldName = "";
        [Personalizable(PersonalizationScope.Shared), WebBrowsable(true),
        WebDisplayName("FieldName"),
        WebDescription("Enter the name of the field that specifes the managed meta-data column to be used.")]
        public string FieldName
        {
            get
            {
                return _FieldName;
            }
            set
            {
                _FieldName = value;
            }
        }

        private string _xslString = @"<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform' xmlns:msxsl='urn:schemas-microsoft-com:xslt' exclude-result-prefixes='msxsl'>
	<xsl:output method='html' indent='yes'/>
	<xsl:template name='terms' match='//termset'>
		<xsl:param name='d'/>
		<div id='accordion'>
			<xsl:for-each select='terms/term'>
				<xsl:call-template name='termtemplate'/>
			</xsl:for-each>
		</div>
	</xsl:template>
	<xsl:template name ='termtemplate'>
		<xsl:element name='div'>
			<xsl:attribute name='style'>padding-left:<xsl:value-of select='count(ancestor::*)*10'/>px;</xsl:attribute>
			<a href='#'>
				<xsl:value-of select='name'/> (<xsl:value-of select='itemcount'/>)
			</a>
			<xsl:for-each select='term'>
				<xsl:call-template name='termtemplate'/>
			</xsl:for-each>
		</xsl:element>
	</xsl:template>
</xsl:stylesheet>
";
        [Personalizable(PersonalizationScope.Shared), WebBrowsable(true),
        WebDisplayName("XSL String"),
        WebDescription("Enter the xsl to be used to transform the taxonomy xml")]
        public string xslString
        {
            get
            {
                return _xslString;
            }
            set
            {
                _xslString = value;
            }
        }

        private int _Depth = 2;
        [Personalizable(PersonalizationScope.Shared), WebBrowsable(true),
        WebDisplayName("Depth"),
        WebDescription("Enter the nunber of levels of the taxonomy to display. The default value is 2.")]
        public int Depth
        {
            get
            {
                return _Depth;
            }
            set
            {
                _Depth = value;
            }
        }

        [WebBrowsable(true), Personalizable(true), Category("Configuration"), DisplayName("Output Cache Timeout"), WebDisplayName("Output Cache Timeout"), Description("Number of minutes to hold the HTML output in cache memory.")]
        public int OutputCacheTimeOut { get; set; }

        #endregion



        protected override void Render(HtmlTextWriter writer)
        {

            if (String.IsNullOrEmpty(_ListName))
            {
                writer.Write("List not specified.\n\r");
                return;
            }
            if (String.IsNullOrEmpty(_FieldName))
            {
                writer.Write("Field not specified.\n\r");
                return;
            }


            // start processing
            try
            {

                SPWeb myWeb = SPContext.Current.Site.OpenWeb(WebUrl);
                SPList myList = myWeb.Lists[_ListName];

                SPQuery q = new SPQuery();
                q.Folder = myList.RootFolder;
                q.Query = "<Where><IsNotNull><FieldRef Name='" + _FieldName + "'></FieldRef></IsNotNull></Where>";
                // get the list items and item count by FieldName
                foreach (SPListItem item in myList.GetItems(q))
                {
                    // This gets the multi-value fields
                    try
                    {
                        TaxonomyFieldValueCollection itemTaxonomyFieldCollection = (TaxonomyFieldValueCollection)item[_FieldName];
                        foreach (TaxonomyFieldValue itemTaxonomyField in itemTaxonomyFieldCollection)
                        {
                            try
                            {
                                // increment our count
                                itemCounter[itemTaxonomyField.TermGuid]++;
                            }
                            catch
                            {
                                // add our item
                                itemCounter.Add(itemTaxonomyField.TermGuid, 1);
                            }
                        }
                    }
                    catch
                    {
                        // BlogCategory not populated
                    }

                    // this gets the single value fields
                    try
                    {
                        TaxonomyFieldValue itemTaxonomyField = (TaxonomyFieldValue)item[_FieldName];
                        try
                        {
                            // increment our count
                            itemCounter[itemTaxonomyField.TermGuid]++;
                        }
                        catch
                        {
                            // add our item
                            itemCounter.Add(itemTaxonomyField.TermGuid, 1);
                        }
                    }
                    catch
                    {
                        // BlogCategory not populated
                    }

                }


                TaxonomyField metaDataField = (TaxonomyField)myList.Fields.GetField(_FieldName);


                TaxonomySession session = new TaxonomySession(SPContext.Current.Site);

                TermStore termStore = session.TermStores[0];
                TermSet termSet = termStore.GetTermSet(metaDataField.TermSetId);

                // get our term hierarchy
                xmlOutput += "<termset><name>" + termSet.Name + "</name><terms>";
                GetRootTerms(termSet);
                xmlOutput += "</terms></termset>";

                // now we've assembled our xml string lets perform the transformation
                XmlDocument doc = new XmlDocument();

                //Spoof an HttpContext while fetching items to prevent user security
                //being applied to which profile fields are visible.
                HttpContext temp = HttpContext.Current;
                HttpContext.Current = null;
                doc.LoadXml(xmlOutput);
                HttpContext.Current = temp;

                //Transform the XML using the XSL stylesheet
                if (!String.IsNullOrEmpty(xslString))
                {
                    XslCompiledTransform trans = new XslCompiledTransform();
                    try
                    {
                        trans.Load(new XmlTextReader(new System.IO.StringReader(xslString)));
                    }
                    catch (System.Xml.Xsl.XsltException ex)
                    {
                        writer.Write("Unable to compile xslString: " + ex.Message + ", Line: " + ex.LineNumber + ", Position: " + ex.LinePosition + ".");
                        writer.Write("<a href=\"javascript:MSOTlPn_ShowToolPane2Wrapper('Edit', this, '" + this.ID + "')\">");
                        writer.Write("Click here to edit the properties of this web part.");
                        writer.Write("</a>");
                        return;
                    }

                    XmlNode items = doc.SelectSingleNode("/termset");

                    // write the transformed xml
                    if (OutputCacheTimeOut <= 0)
                    {
                        //If caching is disabled, transform directly to the output stream
                        trans.Transform(items, new XsltArgumentList(), writer);
                    }
                    else
                    {
                        //If caching is enabled, transform to a string buffer
                        StringBuilder sb = new StringBuilder();
                        trans.Transform(items, new XsltArgumentList(), new System.IO.StringWriter(sb));

                        //Write the buffer to the output
                        writer.Write(sb);

                        //Write a copy of the buffer to the cache
                        HttpContext.Current.Cache.Add(OutputCacheKey, sb.ToString(), null, DateTime.Now.AddMinutes(OutputCacheTimeOut),
                           Cache.NoSlidingExpiration, CacheItemPriority.AboveNormal, null);
                    }
                }
                else
                {
                    //no xsl defined so render the raw xml
                    writer.Write(xmlOutput);
                }

            }
            catch (Exception ex)
            {
                writer.Write("Error rendering web part: " + ex.Message);
            }
        }

        private void GetRootTerms(TermSet termSet)
        {
            // does the termset have a custom sort order
            string sortOrder = termSet.CustomSortOrder;
            if (!String.IsNullOrEmpty(sortOrder))
            {
                // we need to read the term in sequence defined by the sortOrder
                string[] termguids = sortOrder.Split(new char[] { ':' });
                foreach (string guidString in termguids)
                {
                    Term t = termSet.GetTerm(new Guid(guidString));
                    CurrDepth = 0;
                    outputTerm(t);
                    rootTermCount++;
                }
            }
            else
            {
                foreach (Term t in termSet.Terms)
                {
                    CurrDepth = 0;
                    outputTerm(t);
                    rootTermCount++;
                }
            }
        }

        private void GetTerms(Term term)
        {
            CurrDepth++;
            // does the term have a custom sort order
            string sortOrder = term.CustomSortOrder;
            if (!String.IsNullOrEmpty(sortOrder))
            {
                // we need to read the term in sequence defined by the sortOrder
                string[] termguids = sortOrder.Split(new char[] { ':' });
                foreach (string guidString in termguids)
                {
                    Term t = term.TermSet.GetTerm(new Guid(guidString));
                    outputTerm(t);
                }
            }
            else
            {
                foreach (Term t in term.Terms)
                {
                    outputTerm(t);
                }
            }
        }

        private void outputTerm(Term t)
        {

            xmlOutput += "<term>";
            xmlOutput += "<name>" + t.Name + "</name>";
            xmlOutput += "<rootTermID>" + rootTermCount.ToString() + "</rootTermID>";
            xmlOutput += "<termID>" + t.Id + "</termID>";
            xmlOutput += "<itemcount>" + GetTermCount(t.Id) + "</itemcount>";
            if (t.Terms.Count > 0 && CurrDepth < Depth - 1)
            {
                GetTerms(t);
            }
            xmlOutput += "</term>";
        }

        private int GetTermCount(Guid g)
        {
            int retval = 0;
            try
            {
                retval = itemCounter[g.ToString()];
            }
            catch { }
            return retval;
        }

    }

}
