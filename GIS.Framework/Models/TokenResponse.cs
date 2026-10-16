namespace GIS.Framework.Models
{
    public class TokenResponse
    {
        public int Status { get; set; }
        public string status { get; set; }
        public TokenData Data { get; set; }
        public string authtoken { get; set; }
        public string TokenExpiry { get; set; }
        public string ErrorDetails { get; set; }
        public ErrorResponse error { get; set; }
    }

    public class ErrorResponse
    {
        public string message { get; set; }
    }
}
