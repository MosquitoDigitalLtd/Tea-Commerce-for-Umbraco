﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Xml;
using TeaCommerce.Api.Infrastructure.Ping;
using TeaCommerce.Api.Persistence;
using umbraco;
using umbraco.cms.businesslogic.packager;
using Umbraco.Core.Configuration;

namespace TeaCommerce.Umbraco.Configuration.Infrastructure.Ping {
  public class CmsPingDataProvider : ICmsPingDataProvider {

    private readonly IDatabaseFactory _databaseFactory;

    public CmsPingDataProvider( IDatabaseFactory databaseFactory ) {
      _databaseFactory = databaseFactory;
    }

    public CmsPingData GetPingData() {
      CmsPingData pingData = null;

      InstalledPackage package = InstalledPackage.GetAllInstalledPackages().SingleOrDefault( ip => ip.Data.Name.Equals( "Tea Commerce" ) || ip.Data.Name.Equals( "teacommerce" ) );

      if ( package != null ) {
        string teaCommerceVersion = package.Data.Version;
        const string cms = "Umbraco";
        string cmsVersion = UmbracoVersion.Current.ToString();
        string databaseTechnology = _databaseFactory.Get().DatabaseType.ToString();
        string technology = "ASP.NET Web Forms";
        List<string> renderingEngines = new List<string>();

        XmlNode renderingEngineXml = UmbracoSettings._umbracoSettings.SelectSingleNode( "//templates/defaultRenderingEngine" );
        if ( renderingEngineXml != null && renderingEngineXml.InnerText.ToLowerInvariant() == "mvc" ) {
          technology = "ASP.NET MVC";
        }

        if ( technology == "ASP.NET MVC" ) {
          renderingEngines.Add( "Razor" );
        } else {
          string macroScriptsFolderPath = HostingEnvironment.MapPath( "~/macroScripts" );
          if ( macroScriptsFolderPath != null && Directory.Exists( macroScriptsFolderPath ) && Directory.GetFiles( macroScriptsFolderPath, "*.cshtml", SearchOption.AllDirectories ).Any() ) {
            renderingEngines.Add( "Razor" );
          }
        }

        string xsltFolderPath = HostingEnvironment.MapPath( "~/xslt" );
        if ( xsltFolderPath != null && Directory.Exists( xsltFolderPath ) && Directory.GetFiles( xsltFolderPath, "*.xslt", SearchOption.AllDirectories ).Any() ) {
          renderingEngines.Add( "XSLT" );
        }

        pingData = new CmsPingData( teaCommerceVersion, cms, cmsVersion, databaseTechnology, technology ) {
          RenderingEngines = renderingEngines
        };
      }

      return pingData;
    }
  }
}