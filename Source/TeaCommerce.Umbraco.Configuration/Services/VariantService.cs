﻿using System.Collections.Generic;
using System.Linq;
using Autofac;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeaCommerce.Api.Dependency;
using TeaCommerce.Api.Models;
using TeaCommerce.Api.Services;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using TeaCommerce.Umbraco.Configuration.Variant;
using TeaCommerce.Umbraco.Configuration.Variant.Product;

namespace TeaCommerce.Umbraco.Configuration.Services {
  public class VariantService : IVariantService {

    public string CacheKey = "TeaCommerceVariants";

    public static IVariantService Instance { get { return DependencyContainer.Instance.Resolve<IVariantService>(); } }

    public VariantPublishedContent GetVariant( long storeId, IPublishedContent content, string variantId, bool onlyValid = true ) {
      IEnumerable<VariantPublishedContent> variants = GetVariants( storeId, content, onlyValid );

      return variants.FirstOrDefault( v => v.VariantId == variantId );
    }

    public VariantPublishedContent GetVariant( long storeId, IContent content, string variantId, bool onlyValid = true ) {
      IEnumerable<VariantPublishedContent> variants = GetVariants( storeId, content, onlyValid );

      return variants.FirstOrDefault( v => v.VariantId == variantId );
    }

    public IEnumerable<VariantPublishedContent> GetVariants( long storeId, IPublishedContent content, bool onlyValid = true ) {
      Dictionary<int, List<VariantPublishedContent>> variantsForProducts = CacheService.Instance.Get<Dictionary<int, List<VariantPublishedContent>>>( CacheKey + "-" + storeId );
      if ( variantsForProducts == null ) {
        variantsForProducts = new Dictionary<int, List<VariantPublishedContent>>();
        CacheService.Instance.Set( CacheKey + "-" + storeId, variantsForProducts );
      }

      List<VariantPublishedContent> variants = new List<VariantPublishedContent>();

      if ( content != null && !variantsForProducts.TryGetValue( content.Id, out variants ) ) {
        Store store = StoreService.Instance.Get( storeId );

        string variantsJson = content.GetPropertyValue<string>( store.ProductSettings.ProductVariantPropertyAlias );

        variants = ParseVariantJson( variantsJson, content );

        if ( onlyValid ) {
          variants = variants.Where( v => !v.Validation.DuplicatesFound && !v.Validation.HolesInVariants ).ToList();
        }
        variantsForProducts.Add( content.Id, variants );
      }

      return variants;
    }

    public IEnumerable<VariantPublishedContent> GetVariants( long storeId, IContent content, bool onlyValid ) {
      List<VariantPublishedContent> variants = new List<VariantPublishedContent>();
      UmbracoHelper umbracoHelper = new UmbracoHelper( UmbracoContext.Current );

      if ( content != null ) {
        Store store = StoreService.Instance.Get( storeId );

        string variantsJson = content.GetValue<string>( store.ProductSettings.ProductVariantPropertyAlias );
        IPublishedContent parentContent = umbracoHelper.TypedContent( content.Id );
        variants = ParseVariantJson( variantsJson, parentContent );
      }

      return variants;
    }

    public IEnumerable<VariantAttributeGroup> GetVariantGroups( IEnumerable<VariantPublishedContent> variants ) {
      List<VariantAttributeGroup> attributeGroups = new List<VariantAttributeGroup>();

      foreach ( VariantPublishedContent variant in variants ) {
        foreach ( Combination combination in variant.Combinations ) {
          VariantAttributeGroup attributeGroup = attributeGroups.FirstOrDefault( ag => ag.Id == combination.GroupId );

          if ( attributeGroup == null ) {
            attributeGroup = new VariantAttributeGroup { Id = combination.GroupId, Name = combination.GroupName };
            attributeGroups.Add( attributeGroup );
          }

          if ( attributeGroup.Attributes.All( a => a.Id != combination.Id ) ) {
            attributeGroup.Attributes.Add( new VariantAttribute { Id = combination.Id, Name = combination.Name } );
          }
        }
      }

      return attributeGroups;
    }

    public string GetVariantJson( IEnumerable<VariantPublishedContent> variants ) {
      Dictionary<string, dynamic> jsonVariants = new Dictionary<string, dynamic>();

      foreach ( VariantPublishedContent variant in variants ) {
        jsonVariants.Add( variant.VariantId, variant.Combinations.Select( c => c.Id ) );
      }

      return JsonConvert.SerializeObject( jsonVariants );
    }

    private List<VariantPublishedContent> ParseVariantJson( string json, IPublishedContent parentContent ) {
      List<VariantPublishedContent> variants = new List<VariantPublishedContent>();
      if ( !string.IsNullOrEmpty( json ) ) {
        List<Variant.Product.Variant> productVariants = JObject.Parse( json ).SelectToken( "variants" ).ToObject<List<Variant.Product.Variant>>();

        foreach ( Variant.Product.Variant variant in productVariants ) {
          PublishedContentType publishedContentType = PublishedContentType.Get( PublishedItemType.Content, variant.DocumentTypeAlias );

          variants.Add( new VariantPublishedContent( variant, publishedContentType, parentContent ) );
        }
      }
      return variants;
    }
  }
}
