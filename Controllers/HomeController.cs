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
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upload()
        {
            return View(new PredictionResultViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Upload(
            PredictionResultViewModel model)
        {
            if (model.ImageFile != null &&
                model.ImageFile.Length > 0)
            {
                using (var content =
                       new MultipartFormDataContent())
                {
                    var stream =
                        model.ImageFile.OpenReadStream();

                    var fileContent =
                        new StreamContent(stream);

                    fileContent.Headers.ContentType =
                        new MediaTypeHeaderValue(
                            "image/jpeg");

                    content.Add(
                        fileContent,
                        "image",
                        model.ImageFile.FileName);

                    var response =
                        await _httpClient.PostAsync(
                            "http://127.0.0.1:5000/predict",
                            content);

                    var json =
                        await response.Content
                        .ReadAsStringAsync();

                    dynamic result =
                        JsonConvert.DeserializeObject(json);

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
            }

            var uploadsFolder =
                Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot/uploads"
    );

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
                    uniqueFileName
                );

            using (var fileStream =
                   new FileStream(filePath,
                   FileMode.Create))
            {
                await model.ImageFile.CopyToAsync(fileStream);
            }

            model.UploadedImagePath =
                "/uploads/" + uniqueFileName;

            return View(model);
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
