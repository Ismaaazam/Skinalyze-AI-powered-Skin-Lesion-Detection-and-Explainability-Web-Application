using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QuestPDF.Fluent;
using Skinalyze.ViewModels;
using System.Net.Http.Headers;

namespace Skinalyze.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;

        public HomeController()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(20);
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upload()
        {
            return View(new PredictionResultViewModel());
        }
        public IActionResult About()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Upload(PredictionResultViewModel model)
        {
            try
            {
                // ===== FILE EXIST CHECK =====
                if (model.ImageFile == null || model.ImageFile.Length == 0)
                {
                    ModelState.AddModelError("",
                        "Please select an image to upload.");
                    return View(model);
                }

                // ===== EXTENSION VALIDATION =====
                var allowedExtensions =
                    new[] { ".png", ".jpg", ".jpeg", ".jfif" };

                var fileExtension =
                    Path.GetExtension(model.ImageFile.FileName)
                    .ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("",
                        "Invalid file type. Only .png, .jpg, .jpeg, .jfif images are allowed.");
                    return View(model);
                }

                // ===== FILE SIZE VALIDATION =====
                long maxFileSize = 10 * 1024 * 1024; // 10MB

                if (model.ImageFile.Length > maxFileSize)
                {
                    ModelState.AddModelError("",
                        "File size exceeds 10MB limit. Please upload a smaller image.");
                    return View(model);
                }

                // ===== CALL FLASK API =====
                using (var content = new MultipartFormDataContent())
                {
                    var stream = model.ImageFile.OpenReadStream();

                    var fileContent =
                        new StreamContent(stream);

                    fileContent.Headers.ContentType =
                        new MediaTypeHeaderValue("image/jpeg");

                    content.Add(
                        fileContent,
                        "image",
                        model.ImageFile.FileName);

                    HttpResponseMessage response;

                    try
                    {
                        response = await _httpClient.PostAsync(
                            "http://127.0.0.1:5000/predict",
                            content);
                    }
                    catch (HttpRequestException)
                    {
                        ModelState.AddModelError("",
                            "Connection to prediction server lost. Please restart the API and try again.");
                        return View(model);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError("",
                            "Prediction server error. Please try again.");
                        return View(model);
                    }

                    var json =
                        await response.Content.ReadAsStringAsync();

                    dynamic result;

                    try
                    {
                        result =
                            JsonConvert.DeserializeObject(json);
                    }
                    catch
                    {
                        ModelState.AddModelError("",
                            "Invalid response from prediction server.");
                        return View(model);
                    }


                    // ===== ASSIGN RESULTS =====
                    model.Prediction =
                        result.prediction;

                    model.Confidence =
                        result.confidence;

                    model.Severity =
                        result.severity;

                    model.Description =
                        result.description;

                    model.Recommendation =
                        result.recommendation;

                    model.Heatmap =
                        result.heatmap;
                }

                double confidenceThreshold = 60.0;

                if (model.Confidence < confidenceThreshold)
                {
                    ModelState.AddModelError("",
                        "Invalid image detected. Please upload a clear skin lesion image.");

                    model.Prediction = null;
                    return View(model);
                }

                // ===== SAVE FILE LOCALLY =====
                var uploadsFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/uploads");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName =
                    Guid.NewGuid().ToString()
                    + "_"
                    + model.ImageFile.FileName;

                var filePath =
                    Path.Combine(
                        uploadsFolder,
                        uniqueFileName);

                using (var fileStream =
                       new FileStream(
                           filePath,
                           FileMode.Create))
                {
                    await model.ImageFile
                        .CopyToAsync(fileStream);
                }

                model.UploadedImagePath =
                    "/uploads/" + uniqueFileName;

                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError("",
                    "Unexpected error occurred during upload. Please try again in 5 to 10 seconds.");
                return View(model);
            }
        }

        public IActionResult ReportForm()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GeneratePdf(ReportViewModel model)
        {
            var pdfBytes =
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(30);

                        page.Content()
                            .Column(col =>
                            {
                                col.Item().Text(
                                    "Skinalyze Report")
                                    .FontSize(20)
                                    .Bold();

                                col.Item().Text(
                                    "Created by an AI Skin Lesion Detection Model");

                                col.Item().PaddingTop(10);

                                col.Item().Text(
                                    $"Name: {model.FirstName} {model.LastName}");

                                col.Item().Text(
                                    $"Age: {model.Age}");

                                col.Item().Text(
                                    $"Occupation: {model.Occupation}");

                                col.Item().Text(
                                    $"Family History: {model.FamilyHistory}");
                            });
                    });

                }).GeneratePdf();


            TempData["ShowThankYou"] = true;

            return File(
                pdfBytes,
                "application/pdf",
                "Skinalyze_Report.pdf");
        }

        public IActionResult ThankYou()
        {
            return View();
        }
    }
}
