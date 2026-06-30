using System.Text.Json;

namespace HotelBooking.Services
{
    public class ImgBbService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ImgBbService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            // La API key se lee de una variable de entorno en producción
            _apiKey = Environment.GetEnvironmentVariable("IMGBB_API_KEY")
                ?? config["ImgBB:ApiKey"]
                ?? "";
        }

        public async Task<string?> SubirImagenAsync(IFormFile archivo)
        {
            if (string.IsNullOrEmpty(_apiKey))
                throw new Exception("ImgBB API Key no configurada");

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());

            var contenido = new MultipartFormDataContent
            {
                { new StringContent(_apiKey), "key" },
                { new StringContent(base64), "image" }
            };

            var respuesta = await _httpClient.PostAsync("https://api.imgbb.com/1/upload", contenido);

            if (!respuesta.IsSuccessStatusCode)
                return null;

            var json = await respuesta.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            // La URL directa de la imagen subida
            var url = doc.RootElement
                .GetProperty("data")
                .GetProperty("url")
                .GetString();

            return url;
        }
    }
}