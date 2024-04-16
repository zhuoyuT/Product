using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product.Infra.Data;
using Product.Model;

namespace Product.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProductDetailsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductDetailsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/productdetails
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(string? name, decimal? minPrice, decimal? maxPrice, DateTime? startDate, DateTime? endDate)
        {
            var products = _context.ProductDetails.Where(p => p.IsActive).AsQueryable();
            if (!string.IsNullOrWhiteSpace(name))
                products = products.Where(p => p.Name.Contains(name));
            if (minPrice.HasValue)
                products = products.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                products = products.Where(p => p.Price <= maxPrice.Value);
            if (startDate.HasValue)
                products = products.Where(p => p.PostedDate >= startDate.Value);
            if (endDate.HasValue)
                products = products.Where(p => p.PostedDate <= endDate.Value);

            return Ok(await products.OrderByDescending(p => p.PostedDate).ToListAsync());
        }

        // POST: api/productdetails
        [HttpPost]
        public async Task<IActionResult> CreateProductDetail([FromBody] ProductDetail productDetail)
        {
            if (productDetail.Price > 10000)
                return BadRequest("Product price cannot exceed $10,000.");

            // Set posted date and initial status
            productDetail.PostedDate = DateTime.UtcNow;
            productDetail.Status = "Create";

            // If the product price requires approval, set IsActive to false
            bool requiresApproval = productDetail.Price > 5000;
            productDetail.IsActive = !requiresApproval;

            _context.ProductDetails.Add(productDetail);
            await _context.SaveChangesAsync();

            if (requiresApproval)
            {
                _context.ApprovalQueues.Add(new ApprovalQueue
                {
                    ProductId = productDetail.ProductDetailId,
                    RequestReason = "Price exceeds $5,000",
                    RequestDate = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(CreateProductDetail), new { id = productDetail.ProductDetailId }, productDetail);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProductPrice(int id, decimal newPrice)
        {
            var product = await _context.ProductDetails.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            if (!product.IsActive)
            {
                return BadRequest("Cannot update the price of an inactive product.");
            }



            if (newPrice > product.Price * 1.5m)
            {
                _context.ApprovalQueues.Add(new ApprovalQueue
                {
                    ProductId = id,
                    RequestReason = "Price increase > 50%",
                    RequestDate = DateTime.UtcNow,
                    OriginalPrice = product.Price,  // Store original price
                    OriginalStatus = product.Status
                });


                product.IsActive = false;  // Product is inactive until approved
            }
            product.Price = newPrice;
            product.Status = "Update";

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("approvals")]
        public async Task<ActionResult<IEnumerable<ApprovalQueue>>> GetAllApprovals()
        {
            return await _context.ApprovalQueues
                .OrderBy(a => a.RequestDate)
                .Include(a => a.Product)
                .ToListAsync();
        }

        [HttpPost("approve/{id}")]
        public async Task<IActionResult> ApproveOrReject(int id, bool isApproved)
        {
            var approvalQueueItem = await _context.ApprovalQueues
                .Include(a => a.Product)
                .FirstOrDefaultAsync(a => a.ApprovalQueueId == id);

            if (approvalQueueItem == null)
            {
                return NotFound();
            }

            var product = approvalQueueItem.Product;
            if (isApproved)
            {
                switch (product.Status)
                {
                    case "Create":
                        product.IsActive = true;
                        break;
                    case "Update":
                        product.IsActive = true;
                        break;
                    case "Delete":
                        _context.ProductDetails.Remove(product);
                        break;
                }
            }
            else
            {
                switch (product.Status)
                {
                    case "Create":
                        _context.ProductDetails.Remove(product);
                        break;
                    case "Update":
                        if (approvalQueueItem.OriginalPrice.HasValue)
                        {
                            product.Price = approvalQueueItem.OriginalPrice.Value;  // Reset to original price
                        }
                        if (!string.IsNullOrEmpty(approvalQueueItem.OriginalStatus))
                        {
                            product.Status = approvalQueueItem.OriginalStatus;  // Reset to original price
                        }
                        product.IsActive = true;
                        break;
                    case "Delete":
                        product.IsActive = true;  // Revert deletion request
                        break;
                }
            }

            // Remove the item from the approval queue
            _context.ApprovalQueues.Remove(approvalQueueItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
