using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class ImageController : ControllerBase
{
    private readonly ILogger<ImageController> _logger;
    private readonly string _communicationApiUrl;

    public ImageController(ILogger<ImageController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _communicationApiUrl = configuration["CommunicationApiUrl"];
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(IFormFile image)
    {
        if (image == null || image.Length == 0)
        {
            return BadRequest("Invalid image file.");
        }

        try
        {
            // Save the image locally or upload it to storage
            // For simplicity, this example saves it to wwwroot/Images
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Images", image.FileName);
            using (var stream = new FileStream(imagePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            // Call Communication Microservice
            var processedImage = await CallCommunicationMicroservice(imagePath);

            // Return the processed image as the response
            return File(processedImage, "image/jpeg");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error uploading image: {ex.Message}");
            return StatusCode(500, "Internal Server Error");
        }
    }

    private async Task<byte[]> CallCommunicationMicroservice(string imagePath)
    {
        try
        {
            using (var client = new HttpClient())
            {
                // Specify the URL of the Communication Microservice
                var microserviceUrl = "https://localhost:7263/api/communication/webapp";

                // Create a StringContent to send the image path in the request body
                var content = new StringContent(imagePath, Encoding.UTF8, "text/plain");

                // Send the image path to the Communication Microservice
                var response = await client.PostAsync(microserviceUrl, content);

                // Check the response and handle accordingly
                if (response.IsSuccessStatusCode)
                {
                    // Read the response stream and return it as byte array
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    _logger.LogError($"Error calling Communication Microservice: {response.StatusCode}");
                    // Handle error as needed
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error calling Communication Microservice: {ex.Message}");
            // Handle error as needed
            return null;
        }
    }
}
