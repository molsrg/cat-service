using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Threading.Tasks;

namespace CatImageService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UrlController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public UrlController(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetCatImage([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest("URL cannot be empty");
            }

            try
            {
                var response = await _httpClient.GetAsync(url);
                int statusCode = (int)response.StatusCode;

                // Проверка, является ли URL ссылкой на изображение
                if (response.Content.Headers.ContentType.MediaType.StartsWith("image"))
                {
                    var originalImageBytes = await response.Content.ReadAsByteArrayAsync();
                    return File(originalImageBytes, response.Content.Headers.ContentType.MediaType);
                }

                // Проверка кэша
                if (_cache.TryGetValue(statusCode, out byte[] cachedImage))
                {
                    return File(cachedImage, "image/jpeg");
                }

                // Получение картинки с котиком в зависимости от статус-кода
                byte[] catImageBytes = await GetCatImageBytes(statusCode.ToString());

                _cache.Set(statusCode, catImageBytes, TimeSpan.FromMinutes(30));
                return File(catImageBytes, "image/jpeg");
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, $"Error making HTTP request: {ex.Message}");
            }
        }

        private async Task<byte[]> GetCatImageBytes(string statusCode)
        {
            string catImageUrl = $"https://http.cat/{statusCode}.jpg";
            var catImageResponse = await _httpClient.GetAsync(catImageUrl);

            if (!catImageResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch cat image for status code {statusCode}");
            }

            return await catImageResponse.Content.ReadAsByteArrayAsync();
        }
    }
}