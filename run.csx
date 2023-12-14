// Import necessary libraries
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
 
// Content moderation key - replace with proper handling (e.g., environment variables)
public static string contentModerationKey = "zzzzzzzzzzzzzzzzzzzzzzzzzz";
 
// Function triggered when a blob is processed
public static void Run(Stream myBlob, string name, Stream outputBlob, TraceWriter log)
{
    // Log blob information
    log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
    
    // Check image moderation status and apply watermark
    bool result = IsGoodByModerator(myBlob, name, log);
    
    // Log image moderation result
    log.Info("Image Moderation: " + (result ? "Passed" : "Failed"));
    
    try
    {
        // Add watermark and copy to output
        string watermarkText = "TechBrij";
        WriteWatermark(watermarkText, myBlob, outputBlob);
        log.Info("Added watermark and copied successfully");
    }
    catch(Exception ex)
    {
        // Log any errors during watermarking
        log.Info("Watermark Error: " + ex.ToString());
    }    
}

// Method to validate that there must be one person only in the image
public static bool IsGoodByFaceModerator(Stream image, string contentType, TraceWriter log)
{
    // Check faces using Azure Cognitive Services
    try
    {
        var url = "https://southcentralus.api.cognitive.microsoft.com/contentmoderator/moderate/v1.0/ProcessImage/FindFaces";
        Task<string> task = Task.Run<string>(async () => await GetHttpResponseString(url, contentModerationKey, image, contentType));
        string result = task.Result;
        if (String.IsNullOrEmpty(result))
        {
            return false;
        }
        else
        {
            dynamic json = JValue.Parse(result);
            return ((bool)json.Result && json.Count == 1);
        }
    }
    catch (Exception ex)
    {
        // Log any face API errors
        log.Error("Face API Error: " + ex.ToString());
        return false;
    }
}

// Method to filter adult or racy content
public static bool IsGoodByImageModerator(Stream image, string contentType, TraceWriter log)
{
    // Check content using Azure Cognitive Services
    try
    {
        var url = "https://southcentralus.api.cognitive.microsoft.com/contentmoderator/moderate/v1.0/ProcessImage/Evaluate";
        Task<string> task = Task.Run<string>(async () => await GetHttpResponseString(url, contentModerationKey, image, contentType));
        string result = task.Result;
        if (String.IsNullOrEmpty(result))
        {
            return false;
        }
        else
        {
            dynamic json = JValue.Parse(result);
            return (!((bool)json.IsImageAdultClassified || (bool)json.IsImageRacyClassified));
        }
    }
    catch (Exception ex)
    {
        // Log any content API errors
        log.Error("Content API Error: " + ex.ToString());
        return false;
    }
}

// Image validation combining face and content moderation
public static bool IsGoodByModerator(Stream image, string name, TraceWriter log)
{
    // Get content type from file extension
    string contentType = GetConentType(name);

    // Check face moderation
    if (IsGoodByFaceModerator(image, contentType, log))
    {
        log.Info("Face Moderation: Passed");
        return IsGoodByImageModerator(image, contentType, log);
    }
    else
    {
        log.Info("Face Moderation: Failed");
        return false;
    }
}

// Method to add text as watermark 
private static void WriteWatermark(string watermarkContent, Stream originalImage, Stream newImage)
{
    // Add watermark to the original image
    originalImage.Position = 0;
    using (Image inputImage = Image.FromStream(originalImage, true))
    {
        using (Graphics graphic = Graphics.FromImage(inputImage))
        {
            // Set watermark properties
            Font font = new Font("Georgia", 36, FontStyle.Bold);
            SizeF textSize = graphic.MeasureString(watermarkContent, font);

            // Calculate watermark position
            float xCenterOfImg = (inputImage.Width / 2);
            float yPosFromBottom = (int)(inputImage.Height * 0.90) - (textSize.Height / 2);

            // Set graphics quality
            graphic.SmoothingMode = SmoothingMode.HighQuality;
            graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphic.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            // Define string format and brushes for the watermark
            StringFormat StrFormat = new StringFormat();
            StrFormat.Alignment = StringAlignment.Center;

            SolidBrush semiTransBrush2 = new SolidBrush(Color.FromArgb(153, 0, 0, 0));
            graphic.DrawString(watermarkContent, font, semiTransBrush2, xCenterOfImg + 1, yPosFromBottom + 1, StrFormat);

            SolidBrush semiTransBrush = new SolidBrush(Color.FromArgb(153, 255, 255, 255));
            graphic.DrawString(watermarkContent, font, semiTransBrush, xCenterOfImg, yPosFromBottom, StrFormat);

            // Save the watermarked image
            graphic.Flush();
            inputImage.Save(newImage, ImageFormat.Jpeg);
        }
    }
}

// Method to get response as string for HTTP request
public static async Task<string> GetHttpResponseString(String url, String subscriptionKey, Stream image, string contentType)
{
    // Get response from HTTP request
    using (var ms = new MemoryStream())
    {
        image.Position = 0;
        image.CopyTo(ms);
        ms.Position = 0;
        using (var client = new HttpClient())
        {
            var content = new StreamContent(ms);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            var httpResponse = await client.PostAsync(url, content);

            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                return await httpResponse.Content.ReadAsStringAsync();
            }
        }
    }
    return null;
}

// Method to get content type from file extension
public static string GetConentType(string fileName)
{
    // Get content type based on file extension
    string name = fileName.ToLower();
    string contentType = "image/jpeg";
    if (name.EndsWith("png"))
    {
        contentType = "image/png";
    }
    else if (name.EndsWith("gif"))
    {
        contentType = "image/gif";
    }
    else if (name.EndsWith("bmp"))
    {
        contentType = "image/bmp";
    }
    return contentType;
}
