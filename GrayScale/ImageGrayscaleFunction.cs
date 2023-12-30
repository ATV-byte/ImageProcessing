using System;
using System.IO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Net;

namespace GrayScaleFunction
{
    public static class ImageGrayscaleFunction
    {
        [Function("ImageGrayscaleFunction")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("ImageGrayscaleFunction");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);

            try
            {
                // Read the binary content from the request
                using (Stream stream = req.Body)
                using (MemoryStream ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    ms.Seek(0, SeekOrigin.Begin);

                    // Process the image and convert to grayscale
                    using (var image = Image.Load(ms))
                    {
                        image.Mutate(x => x.Grayscale());

                        // Save the processed image to a new MemoryStream
                        using (MemoryStream outputMs = new MemoryStream())
                        {
                            image.Save(outputMs, new JpegEncoder());
                            outputMs.Seek(0, SeekOrigin.Begin);

                            // Set the Content-Disposition header to suggest the filename
                            response.Headers.Add("Content-Disposition", "attachment; filename=image_grayscale.jpg");

                            // Set the Content-Type header
                            response.Headers.Add("Content-Type", "image/jpeg");

                            // Copy the MemoryStream to the response body
                            await outputMs.CopyToAsync(response.Body);
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing image: {ex.Message}");

                // Create an error response with a 400 status code
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
        }
    }
}
