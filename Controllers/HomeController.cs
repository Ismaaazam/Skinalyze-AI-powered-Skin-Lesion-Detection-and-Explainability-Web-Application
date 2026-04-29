using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QuestPDF.Fluent;
using Skinalyze.ViewModels;
using System.Net.Http.Headers;
using System.Text;

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
                            "https://web-production-55d23.up.railway.app/predict",
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
                    TempData["ToastError"] = "Invalid image detected. Please upload a clear skin lesion image.";
                    model.Prediction = null;
                    // Store the low-confidence model back so the view stays clean
                    return RedirectToAction("Upload");
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


        // =====================================================================
        //  HomeController.cs  —  Skinalyze
        //  Replace only the GeneratePdf action with this version.
        //  Everything else (constructor, Index, Upload GET/POST, etc.) stays the same.
        // =====================================================================

        // PASTE THIS ACTION to replace the existing GeneratePdf action:

        [HttpPost]
        public IActionResult GeneratePdf(ReportViewModel model)
        {
            // ✅ Guard: if model binding failed entirely, redirect gracefully
            if (model == null)
            {
                return RedirectToAction("ReportForm");
            }

            var prediction = HttpContext.Session.GetString("Prediction");
            var confidence = HttpContext.Session.GetString("Confidence");
            var severity = HttpContext.Session.GetString("Severity");
            var description = HttpContext.Session.GetString("Description");
            var recommendation = HttpContext.Session.GetString("Recommendation");
            var heatmap = HttpContext.Session.GetString("Heatmap");
            var uploadedImage = HttpContext.Session.GetString("UploadedImagePath");
            var heatmapExplanation = HttpContext.Session.GetString("HeatmapExplanation");
            var highestActivation = HttpContext.Session.GetString("HighestActivation");
            var coveragePercent = HttpContext.Session.GetString("CoveragePercent");
            var activeZones = HttpContext.Session.GetString("ActiveZones");

            try
            {
                var pdfBytes = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(36);
                        page.Content().Column(col =>
                        {
                            // Header
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Skinalyze")
                                        .FontSize(26).Bold()
                                        .FontColor(QuestPDF.Infrastructure.Color.FromHex("#0D5F4A"));
                                    c.Item().Text("AI-Assisted Skin Lesion Analysis Report")
                                        .FontSize(11)
                                        .FontColor(QuestPDF.Infrastructure.Color.FromHex("#4a5a55"));
                                });
                                row.AutoItem().Column(c =>
                                {
                                    c.Item().AlignRight()
                                        .Text($"Date: {DateTime.Now:dd MMMM yyyy}")
                                        .FontSize(10)
                                        .FontColor(QuestPDF.Infrastructure.Color.FromHex("#7a8a8e"));
                                });
                            });

                            col.Item().PaddingVertical(6)
                                .LineHorizontal(1)
                                .LineColor(QuestPDF.Infrastructure.Color.FromHex("#e4ede8"));

                            // Patient Details
                            col.Item().PaddingTop(12)
                                .Text("Patient Details").Bold().FontSize(13)
                                .FontColor(QuestPDF.Infrastructure.Color.FromHex("#0a1a14"));

                            col.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(3);
                                });

                                void AddRow(string label, string value)
                                {
                                    table.Cell().Padding(5)
                                        .Text(label).Bold().FontSize(11)
                                        .FontColor(QuestPDF.Infrastructure.Color.FromHex("#4a5a55"));
                                    table.Cell().Padding(5)
                                        .Text(value ?? "—").FontSize(11)
                                        .FontColor(QuestPDF.Infrastructure.Color.FromHex("#0a1a14"));
                                }

                                AddRow("Full Name", $"{model.FirstName} {model.LastName}");
                                AddRow("Age", model.Age.HasValue ? model.Age.Value.ToString() : "—"); // ✅ nullable-safe
                                AddRow("Occupation", model.Occupation ?? "—");
                                AddRow("Family History", model.FamilyHistory ?? "—");
                            });

                            col.Item().PaddingVertical(8)
                                .LineHorizontal(1)
                                .LineColor(QuestPDF.Infrastructure.Color.FromHex("#e4ede8"));

                            // Prediction Result
                            col.Item().PaddingTop(4)
                                .Text("Prediction Result").Bold().FontSize(13)
                                .FontColor(QuestPDF.Infrastructure.Color.FromHex("#0a1a14"));

                            col.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(3);
                                });

                                void AddRow(string label, string value)
                                {
                                    table.Cell().Padding(5)
                                        .Text(label).Bold().FontSize(11)
                                        .FontColor(QuestPDF.Infrastructure.Color.FromHex("#4a5a55"));
                                    table.Cell().Padding(5)
                                        .Text(value ?? "—").FontSize(11)
                                        .FontColor(QuestPDF.Infrastructure.Color.FromHex("#0a1a14"));
                                }

                                AddRow("Diagnosis", prediction ?? "N/A");
                                AddRow("Confidence", $"{confidence}%");
                                AddRow("Severity", severity ?? "N/A");
                                AddRow("Description", description ?? "N/A");
                                AddRow("Recommendation", recommendation ?? "N/A");
                            });

                            col.Item().PaddingVertical(8)
                                .LineHorizontal(1)
                                .LineColor(QuestPDF.Infrastructure.Color.FromHex("#e4ede8"));

                            // Images
                            col.Item().PaddingTop(4)
                                .Text("Image Analysis").Bold().FontSize(13)
                                .FontColor(QuestPDF.Infrastructure.Color.FromHex("#0a1a14"));

                            col.Item().PaddingTop(8).Row(row =>
                            {
                                if (!string.IsNullOrEmpty(uploadedImage))
                                {
                                    var imagePath = Path.Combine(
                                        Directory.GetCurrentDirectory(),
                                        "wwwroot",
                                        uploadedImage.TrimStart('/'));

                                    if (System.IO.File.Exists(imagePath))
                                    {
                                        row.RelativeItem().Column(c =>
                                        {
                                            c.Item().PaddingBottom(5)
                                                .Text("Uploaded Image").Bold().FontSize(10)
                                                .FontColor(QuestPDF.Infrastructure.Color.FromHex("#4a5a55"));
                                            c.Item().Width(200).Height(160).Image(imagePath);
                                        });
                                    }
                                }

                                if (!string.IsNullOrEmpty(heatmap))
                                {
                                    try
                                    {
                                        var heatmapBytes = Convert.FromBase64String(heatmap);
                                        row.RelativeItem().Column(c =>
                                        {
                                            c.Item().PaddingBottom(5)
                                                .Text("Grad-CAM Heatmap").Bold().FontSize(10)
                                                .FontColor(QuestPDF.Infrastructure.Color.FromHex("#4a5a55"));
                                            c.Item().Width(200).Height(160).Image(heatmapBytes);
                                        });
                                    }
                                    catch { /* skip if base64 invalid */ }
                                }
                            });

                            col.Item().PaddingVertical(8)
                                .LineHorizontal(1)
                                .LineColor(QuestPDF.Infrastructure.Color.FromHex("#e4ede8"));

                            // Heatmap Analysis
                            col.Item().PaddingTop(4)
                                .Text("Heatmap Analysis").Bold().FontSize(13)
                                .FontColor(QuestPDF.Infrastructure.Color.FromHex("#0a1a14"));

                            col.Item().PaddingTop(6).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(3);
                                });

                                void AddRow(string label, string value)
                                {
                                    table.Cell().Padding(5)
                                        .Text(label).Bold().FontSize(11)
                                        .FontColor(QuestPDF.Infrastructure.Color.FromHex("#4a5a55"));
                                    table.Cell().Padding(5)
                                        .Text(value ?? "—").FontSize(11)
                                        .FontColor(QuestPDF.Infrastructure.Color.FromHex("#0a1a14"));
                                }

                                AddRow("Interpretation", heatmapExplanation ?? "—");
                                AddRow("Activation", highestActivation ?? "—");
                                AddRow("Lesion Coverage", $"{coveragePercent}%");
                                AddRow("Active Zones", activeZones ?? "—");
                            });

                            // Footer
                            col.Item().PaddingTop(20)
                                .Text("⚠ This report is AI-generated and should not replace professional medical advice. Consult a qualified dermatologist.")
                                .FontSize(9).Italic()
                                .FontColor(QuestPDF.Infrastructure.Color.FromHex("#7a8a8e"));
                        });
                    });
                }).GeneratePdf();

                TempData["ShowThankYou"] = true;

                var safeName = $"{model.FirstName}_{model.LastName}"
                    .Replace(" ", "_")
                    .Replace("/", "")
                    .Replace("\\", "");

                var fileName = $"{safeName}_Skinalyze_Report.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return Content($"An error occurred while generating the report: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveDermatologistSuggestion(string SuggestionText)
        {
            try
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

                var data = new
                {
                    prediction,
                    confidence,
                    severity,
                    description,
                    recommendation,
                    dermatologist_suggestion = SuggestionText
                };

                var json =
                    JsonConvert.SerializeObject(data);

                var content =
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json");

                var client = new HttpClient();

                string apiKey = "sb_publishable_tukOXXJtisIrIYYOjnjuJA_8aWfpRF9";

                client.DefaultRequestHeaders.Add("apikey", apiKey);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");

                var response =
                    await client.PostAsync(
                        "https://nyumjjsfxmxylkmhlulw.supabase.co/rest/v1/dermatologist_feedback",
                        content);

                var responseText =
                    await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {

                    TempData["ToastSuccess"] =
                        "Your feedback has been noted down.";
                }
                else
                {

                    TempData["ToastError"] =
                        "Failed to save feedback: " +
                        responseText;
                }

                return RedirectToAction("Upload");
            }
            catch (Exception ex)
            {
                TempData["ToastError"] =
                    "Exception: " + ex.Message;

                return RedirectToAction("Upload");
            }
        }

        public IActionResult Insights()
        {
            return View();
        }

        public IActionResult ThankYou()
        {
            return View();
        }
    }
}
