namespace AIDevs.Common;

public static class HtmlTools
{
  public static List<string> ExtractLinksFromHtml(string htmlContent)
  {
    var links = new List<string>();
    var htmlDocument = new HtmlAgilityPack.HtmlDocument();
    htmlDocument.LoadHtml(htmlContent);

    // Wybierz wszystkie elementy <a> z atrybutem href
    var linkNodes = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
    
    if (linkNodes != null)
    {
      foreach (var node in linkNodes)
      {
        var href = node.GetAttributeValue("href", "");
        if (!string.IsNullOrWhiteSpace(href))
        {
          links.Add(href);
        }
      }
    }

    return links;
  }
  
  public static List<string> ExtractImagesFromHtml(string htmlContent)
  {
    var imageUrls = new List<string>();
    var htmlDocument = new HtmlAgilityPack.HtmlDocument();
    htmlDocument.LoadHtml(htmlContent);

    // Wybierz wszystkie elementy <img> z atrybutem src
    var imgNodes = htmlDocument.DocumentNode.SelectNodes("//img[@src]");
    if (imgNodes != null)
    {
      foreach (var node in imgNodes)
      {
        var src = node.GetAttributeValue("src", "");
        if (!string.IsNullOrWhiteSpace(src))
        {
          imageUrls.Add(src);
        }
      }
    }

    // Dodatkowo możemy sprawdzić style z background-image
    var styleNodes = htmlDocument.DocumentNode.SelectNodes("//*[@style]");
    if (styleNodes != null)
    {
      foreach (var node in styleNodes)
      {
        var style = node.GetAttributeValue("style", "");
        if (style.Contains("background-image"))
        {
          // Znajdujemy URL w stylu background-image: url('...')
          var urlMatch = System.Text.RegularExpressions.Regex.Match(style, @"background-image:\s*url\(['""]?([^'""]+)['""]?\)");
          if (urlMatch.Success && urlMatch.Groups.Count > 1)
          {
            var url = urlMatch.Groups[1].Value;
            imageUrls.Add(url);
          }
        }
      }
    }

    return imageUrls;
  }
}