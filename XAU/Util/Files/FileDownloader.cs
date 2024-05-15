using System.ComponentModel;
using System.IO;
using System.Net.Http;

public class FileDownloader : IDisposable
{
    private readonly HttpClient httpClient;

    public FileDownloader()
    {
        httpClient = new HttpClient();
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    public async Task DownloadFileAsync(string url, string destinationFilePath, Action<object, AsyncCompletedEventArgs>? updateToolCallback = null)
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            using (Stream contentStream = await response.Content.ReadAsStreamAsync())
            {
                using (FileStream fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    await contentStream.CopyToAsync(fileStream);
                }
            }

            // Invoke the provided callback method upon successful download
            updateToolCallback?.Invoke(this, new AsyncCompletedEventArgs(null, false, null));
        }
        catch (Exception ex)
        {
            // Handle exceptions
            Console.WriteLine($"Error downloading file: {ex.Message}");
        }
    }
}
