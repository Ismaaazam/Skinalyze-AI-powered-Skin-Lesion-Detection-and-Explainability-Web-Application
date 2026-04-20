using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
        public IActionResult About()
        {
            return View();
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

            return View(model);
        }
    }
}
