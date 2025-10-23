namespace ai_cv_evaluator.Models.DTOs
{
    public class UploadRequest
    {
        public IFormFile Cv { get; set; }
        public IFormFile Report { get; set; }
    }
}
