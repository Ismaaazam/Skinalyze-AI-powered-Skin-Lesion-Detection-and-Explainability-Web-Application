namespace Skinalyze.ViewModels
{
    public class PredictionResultViewModel
    {
        public IFormFile? ImageFile { get; set; }

        public string? Prediction { get; set; }

        public double? Confidence { get; set; }

        public string? Severity { get; set; }

        public string? Description { get; set; }

        public string? Recommendation { get; set; }

        public string? Heatmap { get; set; }

        public string? UploadedImagePath { get; set; }

        public string? HighestActivation { get; set; }

        public string? CoveragePercent { get; set; }

        public string? ActiveZones { get; set; }
        public string? HeatmapExplanation { get; set; }
    }
}
