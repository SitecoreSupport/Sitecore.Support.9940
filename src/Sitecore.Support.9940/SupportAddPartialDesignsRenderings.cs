using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.XA.Foundation.Presentation;
using Sitecore.XA.Foundation.Presentation.Pipelines.GetXmlBasedLayoutDefinition;

namespace Sitecore.Support.XA.Foundation.Presentation.Pipelines.GetXmlBasedLayoutDefinition
{
  public class AddPartialDesignsRenderings: Sitecore.XA.Foundation.Presentation.Pipelines.GetXmlBasedLayoutDefinition.AddPartialDesignsRenderings
  {

    private readonly string Prefix = "sxa";

    protected override XElement GetFromField(Item item)
    {
      LayoutField field = new LayoutField(item);
      XElement partialDesignRendering = XDocument.Parse(field.Value).Root;
      if (partialDesignRendering != null)
      {
        Dictionary<string, HashSet<string>> deviceWrappersSignatures = new Dictionary<string, HashSet<string>>();
        string partialDesignSignature = item[Templates.PartialDesign.Fields.Signature];
        foreach (XElement deviceRendering in partialDesignRendering.Descendants("d"))
        {
          HashSet<string> wrappersSignatures = new HashSet<string>();
          foreach (XElement rendering in deviceRendering.Descendants("r"))
          {
            //add partial design id attribute
            rendering.Add(new XAttribute("sid", item.ID));

            //wrapp partial design renderings with another element - partial design dynamic placeholder rendering
            string ph = rendering.Attribute("ph").Value;
            if (ph.Contains("/"))
            {
              string[] placeholderParts = ph.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
              string currentPhPrefix = placeholderParts.FirstOrDefault(p => p.Contains($"{Prefix}-"));
              bool isInheritedRendering = !string.IsNullOrWhiteSpace(currentPhPrefix) && GetAllBasePartialDesignSignatures(item).Contains(currentPhPrefix);

              if (!isInheritedRendering)
              {
                string wrapperPhPrefix = string.Format("/{0}/{2}-{1}", placeholderParts[0], partialDesignSignature, Prefix);
                rendering.Attribute("ph").Value = ph.Replace($"/{placeholderParts[0]}", wrapperPhPrefix);
                wrappersSignatures.Add(placeholderParts[0]);
              }
            }
            else
            {
              rendering.Attribute("ph").Value = string.Format("/{0}/{2}-{1}", ph, partialDesignSignature, Prefix);
              wrappersSignatures.Add(ph);
            }
          }
          if (wrappersSignatures.Any())
          {
            deviceWrappersSignatures.Add(deviceRendering.Attribute("id").Value, wrappersSignatures);
          }
        }

        if (deviceWrappersSignatures.Any())
        {
          foreach (XElement devideRendering in partialDesignRendering.Descendants("d").Where(d => d.DescendantNodes().Any()))
          {
            //add wrapper renderings to each non empty device
            foreach (string wrapperSignature in deviceWrappersSignatures[devideRendering.Attribute("id").Value])
            {
              XElement wrapper = CreateWrapperElement(item, wrapperSignature);
              // fix for 9940
              wrapper.SetAttributeValue("par", $"sid={item.ID}&ph={wrapperSignature}");
              devideRendering.Add(wrapper);
            }
          }
        }
      }

      return partialDesignRendering;
    }
  }
}