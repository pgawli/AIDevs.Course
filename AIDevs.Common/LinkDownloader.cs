namespace AIDevs.Common;

public class LinkDownloader
{
  private readonly string outputFolder;
  private readonly bool useExistingFiles;

  public LinkDownloader(string outputFolder, bool useExistingFiles = true)
  {
    this.outputFolder = outputFolder;
    this.useExistingFiles = useExistingFiles;
  }

  public async Task<string[]> DownloadLinks(string[] links, string baseUrl)
  {
    var downloadedFiled = new List<string>();
    using var client = new HttpClient();
    foreach (var link in links)
    {
      var downloadedFile = await DownloadLink(link, baseUrl, client);
      if (!string.IsNullOrEmpty(downloadedFile))
      {
        downloadedFiled.Add(downloadedFile);
      }
      await Task.Delay(100);
    }
    return downloadedFiled.ToArray();
  }

  private async Task<string> DownloadLink(string link, string baseUrl, HttpClient client)
  {
    var fullUrl = link;
    if (!link.StartsWith("http://") && !link.StartsWith("https://"))
    {
      // Relatywny URL - dodaj bazowy
      fullUrl = new Uri(new Uri(baseUrl), link).ToString();
    }

    var fileName = Path.GetFileName(link);
    var outputPath = Path.Combine(outputFolder, fileName);

    if (File.Exists(outputPath) && useExistingFiles)
    {
      Console.WriteLine($"File {fileName} already exists. Skipping.");
      return fileName;
    }

    Console.WriteLine($"Downloading {fullUrl} to {fileName}");
    var linkResponse = await client.GetAsync(fullUrl);
          
    if (linkResponse.IsSuccessStatusCode)
    {
      var content = await linkResponse.Content.ReadAsByteArrayAsync();
      await File.WriteAllBytesAsync(outputPath, content);
      return fileName;
    }
    
    Console.WriteLine($"Failed to download {fullUrl}: {linkResponse.StatusCode}");
    return string.Empty;
  }
}