using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class CommunicationController : ControllerBase
{
    private readonly ILogger<CommunicationController> _logger;

    public CommunicationController(ILogger<CommunicationController> logger)
    {
        _logger = logger;
    }

    [HttpPost("webapp")]
    public async Task<IActionResult> ReceiveFromWebApp()
    {
        try
        {
            // Read the content from the request body
            using (var reader = new StreamReader(Request.Body))
            {
                var imagePath = await reader.ReadToEndAsync();
                _logger.LogInformation($"Received image path from Web App Microservice: {imagePath}");

                // Call Image Grayscale Function and get the processed image
                var processedImageStream = await CallImageGrayscaleFunction(imagePath);

                // Return the processed image as the response
                return File(processedImageStream, "image/jpeg");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing data from Web App Microservice: {ex.Message}");
            return StatusCode(500, "Internal Server Error");
        }
    }

    private async Task<Stream> CallImageGrayscaleFunction(string imagePath)
    {
        try
        {
            using (var client = new HttpClient())
            {
                // Specify the URL of the Azure Functions Image Grayscale Function
                var functionUrl = "http://localhost:7272/api/ImageGrayscaleFunction";

                // Read the image file from the specified path
                byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);

                // Create a ByteArrayContent with the image bytes
                var imageContent = new ByteArrayContent(imageBytes);

                // Set the Content-Type header
                imageContent.Headers.Remove("Content-Type");
                imageContent.Headers.Add("Content-Type", "image/jpeg");

                // Send a POST request to the Azure Functions Image Grayscale Function
                var response = await client.PostAsync(functionUrl, imageContent);

                // Check the response and handle accordingly
                if (response.IsSuccessStatusCode)
                {
                    // Read the response stream and return it
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    return responseStream;
                }
                else
                {
                    _logger.LogError($"Error calling Image Grayscale Function: {response.StatusCode}");
                    // Handle error as needed
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calling Image Grayscale Function: {ex.Message}");
            // Handle error as needed
            return null;
        }
    }


}
