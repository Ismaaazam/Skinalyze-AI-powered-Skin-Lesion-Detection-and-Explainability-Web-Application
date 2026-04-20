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

                    model.HeatmapExplanation =
                        result.heatmap_explanation;

                    model.HighestActivation =
                        result.highest_activation;

                    model.CoveragePercent =
                        result.coverage_percent.ToString();

                    model.ActiveZones =
                        result.active_zones.ToString();
                }

                double confidenceThreshold = 60.0;

                if (model.Confidence < confidenceThreshold)
                {
                    ModelState.AddModelError("",
                        "Invalid image detected. Please upload a clear skin lesion image.");

                    model.Prediction = null;
                    return View(model);
                }

                HttpContext.Session.SetString(
                    "Prediction",
                    model.Prediction);

                HttpContext.Session.SetString(
                    "Confidence",
                    model.Confidence.ToString());

                HttpContext.Session.SetString(
                    "Severity",
                    model.Severity);

                HttpContext.Session.SetString(
                    "Description",
                    model.Description);

                HttpContext.Session.SetString(
                    "Recommendation",
                    model.Recommendation);

                HttpContext.Session.SetString(
                    "Heatmap",
                    model.Heatmap);

                HttpContext.Session.SetString(
                    "HeatmapExplanation",
                    model.HeatmapExplanation ?? "");

                HttpContext.Session.SetString(
                    "HighestActivation",
                    model.HighestActivation);

                HttpContext.Session.SetString(
                    "CoveragePercent",
                    model.CoveragePercent);

                HttpContext.Session.SetString(
                    "ActiveZones",
                    model.ActiveZones);


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

                HttpContext.Session.SetString(
                    "UploadedImagePath",
                    model.UploadedImagePath);

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
            var prediction =
                HttpContext.Session.GetString("Prediction");

            var confidence =
                HttpContext.Session.GetString("Confidence");

            var severity =
                HttpContext.Session.GetString("Severity");

            var description =
                HttpContext.Session.GetString("Description");

            var recommendation =
                HttpContext.Session.GetString("Recommendation");

            var heatmap =
                HttpContext.Session.GetString("Heatmap");

            var uploadedImage =
                HttpContext.Session.GetString("UploadedImagePath");

            var heatmapExplanation =
                HttpContext.Session.GetString("HeatmapExplanation");

            var highestActivation =
                HttpContext.Session.GetString("HighestActivation");

            var coveragePercent =
                HttpContext.Session.GetString("CoveragePercent");

            var activeZones =
                HttpContext.Session.GetString("ActiveZones");
            try
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
                                    col.Item().Text("Skinalyze Report")
                                        .FontSize(20)
                                        .Bold();

                                    col.Item().Text(
                                        "Created by an AI Skin Lesion Detection Model");

                                    col.Item().PaddingTop(10);

                                    col.Item().Text("Patient Details")
                                        .Bold();

                                    col.Item().Text(
                                        $"Name: {model.FirstName} {model.LastName}");

                                    col.Item().Text(
                                        $"Age: {model.Age}");

                                    col.Item().Text(
                                        $"Occupation: {model.Occupation}");

                                    col.Item().Text(
                                        $"Family History: {model.FamilyHistory}");

                                    col.Item().PaddingTop(10);

                                    col.Item().Text("Prediction Result")
                                        .Bold();

                                    col.Item().Text(
                                        $"Disease: {prediction}");

                                    col.Item().Text(
                                        $"Confidence: {confidence}%");

                                    col.Item().Text(
                                        $"Severity: {severity}");

                                    col.Item().Text(
                                        $"Description: {description}");

                                    col.Item().Text(
                                        $"Recommendation: {recommendation}");

                                    // ===== ADD UPLOADED IMAGE =====

                                    col.Item().PaddingTop(10);

                                    col.Item().Row(row =>
                                    {
                                        if (!string.IsNullOrEmpty(uploadedImage))
                                        {
                                            var imagePath =
                                                Path.Combine(
                                                    Directory.GetCurrentDirectory(),
                                                    "wwwroot",
                                                    uploadedImage.TrimStart('/'));

                                            if (System.IO.File.Exists(imagePath))
                                            {
                                                row.RelativeItem().Column(c =>
                                                {
                                                    c.Item()
                                                        .PaddingBottom(5)
                                                        .Text("Uploaded Image")
                                                        .Bold();

                                                    c.Item()
                                                        .Width(200).Height(160)
                                                        .Image(imagePath);
                                                });
                                            }
                                        }

                                        if (!string.IsNullOrEmpty(heatmap))
                                        {
                                            try
                                            {
                                                var heatmapBytes =
                                                    Convert.FromBase64String(heatmap);

                                                row.RelativeItem().Column(c =>
                                                {
                                                    c.Item().Text("Grad-CAM Heatmap")
                                                        .Bold();

                                                    c.Item()
                                                        .Width(200).Height(160)
                                                        .Image(heatmapBytes);
                                                });
                                            }
                                            catch
                                            {

                                            }
                                        }
                                    });

                                    col.Item().PaddingTop(10);

                                    col.Item().Text("Heatmap Interpretation")
                                        .Bold();

                                    col.Item().Text(
                                        heatmapExplanation
                                    );

                                    col.Item().PaddingTop(10);

                                    col.Item().Text("Heatmap Analysis")
                                        .Bold();

                                    col.Item().Text(
                                        $"Activation: {highestActivation}");

                                    col.Item().Text(
                                        $"Lesion Coverage: {coveragePercent}%");

                                    col.Item().Text(
                                        $"Active Zones: {activeZones}");
                                });
                        });

                    }).GeneratePdf();


                TempData["ShowThankYou"] = true;

                return File(
                    pdfBytes,
                    "application/pdf",
                    "Skinalyze_Report.pdf");
            }
            catch
            {
                return Content("An error occured while generating the report.");
            }
        }

        public IActionResult ThankYou()
        {
            return View();
        }
    }
}
