<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:output method="html" indent="yes" omit-xml-declaration="yes" />
  <xsl:variable name="listId" select="listid"/>
  
  <xsl:template name="global" match="//global">
    <xsl:param name="d" />
      <xsl:for-each select="termset">
        <xsl:call-template name="terms" />
      </xsl:for-each>
  </xsl:template>
  
  
  <xsl:template name="terms" >
        <xsl:param name="d" />
        <div id="accordion">
            <xsl:for-each select="terms/term">
                <xsl:call-template name="termtemplate" />
            </xsl:for-each>
        </div>
  </xsl:template>
  
  
  <xsl:template name="termtemplate">
        <xsl:element name="div">
            <xsl:attribute name="style">padding-left:<xsl:value-of select="count(ancestor::*)*10" />px;</xsl:attribute>
            <a>
				        <xsl:attribute name="href">/_layouts/Categories.aspx?FieldName=Wiki_x0020_Page_x0020_Categories&amp;ListId=<xsl:value-of select="listId"/>&amp;FieldValue=<xsl:value-of select="termID"/></xsl:attribute>
                <xsl:value-of select="name" /> (<xsl:value-of select="itemcount" />)
			      </a>
            <xsl:for-each select="term">
                <xsl:call-template name="termtemplate" />
            </xsl:for-each>
        </xsl:element>
   </xsl:template>
  
</xsl:stylesheet>

<!--
Original
<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:output method="html" indent="yes" omit-xml-declaration="yes" />
  <xsl:template name="terms" match="//termset">
    <xsl:param name="d" />
    <div id="accordion">
      <xsl:for-each select="terms/term">
        <xsl:call-template name="termtemplate" />
      </xsl:for-each>
    </div>
  </xsl:template>
  <xsl:template name="termtemplate">
    <xsl:element name="div">
      <xsl:attribute name="style">
        padding-left:<xsl:value-of select="count(ancestor::*)*10" />px;
      </xsl:attribute>
      <a>
        <xsl:attribute name="href">
          /_layouts/Categories.aspx?FieldName=Wiki_x0020_Page_x0020_Categories&amp;ListId=c3f60195-0fdc-4253-9b0d-f342476a5e0a&amp;FieldValue=<xsl:value-of select="termID"/>
        </xsl:attribute>
        <xsl:value-of select="name" /> (<xsl:value-of select="itemcount" />)
      </a>
      <xsl:for-each select="term">
        <xsl:call-template name="termtemplate" />
      </xsl:for-each>
    </xsl:element>
  </xsl:template>
</xsl:stylesheet>-->