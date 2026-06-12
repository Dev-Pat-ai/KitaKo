using KitaKo.Models;
using KitaKo.Services;
using Microsoft.AspNetCore.Mvc;

namespace KitaKo.Controllers
{
    [Route("api/[controller]")]
    public class StoredProductsController : AuthenticatedApiController
    {
        private readonly StoredProductsService _storedProductsService;
        private readonly IWebHostEnvironment _environment;

        public StoredProductsController(
            StoredProductsService storedProductsService,
            IWebHostEnvironment environment)
        {
            _storedProductsService = storedProductsService;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StoredProduct>>> GetProducts([FromQuery] string? search)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return Ok(await _storedProductsService.GetProductsAsync(userId, search));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StoredProduct>> GetProduct(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            var product = await _storedProductsService.GetProductAsync(userId, id);
            return product == null ? NotFound() : Ok(product);
        }

        [HttpPost]
        public async Task<ActionResult<StoredProduct>> PostProduct(StoredProductRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var product = await _storedProductsService.CreateProductAsync(userId, request);
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, StoredProductRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var product = await _storedProductsService.UpdateProductAsync(userId, id, request);
                return product == null ? NotFound() : Ok(product);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            return await _storedProductsService.ArchiveProductAsync(userId, id)
                ? NoContent()
                : NotFound();
        }

        [HttpPost("image")]
        public async Task<ActionResult> UploadProductImage(IFormFile image)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized();
            }

            if (image == null || image.Length == 0)
            {
                return BadRequest(new { message = "Please choose an image to upload." });
            }

            if (image.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "Product image must be 5 MB or smaller." });
            }

            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            var allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { message = "Only JPG, PNG, WEBP, and GIF images are allowed." });
            }

            var uploadRoot = Path.Combine(_environment.WebRootPath, "uploads", "products", "user");
            Directory.CreateDirectory(uploadRoot);

            var fileName = $"user_{userId}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadRoot, fileName);

            await using (var stream = System.IO.File.Create(filePath))
            {
                await image.CopyToAsync(stream);
            }

            return Ok(new { imageUrl = $"/uploads/products/user/{fileName}" });
        }
    }
}
