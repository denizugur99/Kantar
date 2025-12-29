using Kantarv2.Command.Product;
using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Entities;
using Kantarv2.Enums;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Security.Claims;

namespace Kantarv2.Handler.CommandHandler
{
    public class ProductComandHandler : IRequestHandler<AddProduct, Response<AddProductDto>>,
		
		IRequestHandler<DeleteProduct, Response<NoContent>>,
		IRequestHandler<ShipProduct, Response<AddProductDto>>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly ILogger<ProductComandHandler> _logger;
		private readonly KantarDbContext _context;
        private readonly IConnectionMultiplexer _redis;

        public ProductComandHandler(IHttpContextAccessor httpContextAccessor,  ILogger<ProductComandHandler> logger, KantarDbContext context,IConnectionMultiplexer connectionMultiplexer)
		{
            _httpContextAccessor = httpContextAccessor;
           _redis = connectionMultiplexer;
            _logger = logger;
			_context = context;
        }

        public async Task<Response<AddProductDto>> Handle(AddProduct request, CancellationToken cancellationToken)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "Bilinmeyen Kullanıcı";
            var transaction = await _context.Database.BeginTransactionAsync();
            try
			{
                var UnitPrice = await _context.UnitPrices.FirstOrDefaultAsync(x => (!x.IsDeleted) && x.Id.Equals(request.UnitId));
                if (UnitPrice == null)
                {
                    _logger.LogError("Unit Price not found for UnitId: {UnitId}", request.UnitId);
                    return Response<AddProductDto>.Fail(404, "Unit Price not found");
                }
                Product product = new()
                {
                    Id = Guid.NewGuid(),
                    Weight = request.Weight,
                    CreatedDate = DateTime.UtcNow,
                    UnitPrice = UnitPrice,
					Status=ProductStatus.Delivered,
                    IsDeleted = false
                };
				
                if (!request.Price.HasValue) {

					product.Price = UnitPrice.Price;
					product.TotalPrice = -UnitPrice.Price * request.Weight;

                }
				else
				{
					product.Price = request.Price.Value;
					product.TotalPrice = -request.Price.Value * request.Weight;

                }
                await _context.Products.AddAsync(product);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                AddProductDto dto = new()
                {
                    Id = product.Id,
                    Price = product.Price,
                    Weight = product.Weight,
                    TotalPrice = -product.TotalPrice,
                    Name = UnitPrice.Name
                };
                _logger.LogInformation("Product created with Id: {ProductId} by {userıd}", product.Id,userId);
                return Response<AddProductDto>.Success(201, dto);

            }
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred while creating product");
				return Response<AddProductDto>.Fail(500, "Internal Server Error");

            }
        }



        public async Task<Response<NoContent>> Handle(DeleteProduct request, CancellationToken cancellationToken)
        {
			try
			{
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "Bilinmeyen Kullanıcı";

                var product =await _context.Products.FirstOrDefaultAsync(p =>!p.IsDeleted&&p.Id.Equals(request.Id));
			
				if (product == null)
				{
					_logger.LogError("Product not found for Id: {ProductId}", request.Id);
					return Response<NoContent>.Fail(404, "Product not found");
                }
				else
				{
                    var query = await _context.Products.Include(x => x.UnitPrice).Where(p => !p.IsDeleted && p.UnitPrice.Equals(product.UnitPrice)).ToListAsync();
					double totalWeight=0;
					foreach (var item in query) { 
					 totalWeight=+item.Weight;
					}
					if (totalWeight-product.Weight < 0) {

						_logger.LogWarning("Cant delete this product.Weight cant be negative");
                        return Response<NoContent>.Fail(404, "Cant delete this product.Weight cant be negative");
					}
					else
					{
                        product.IsDeleted = true;
                        _context.Products.Update(product);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Product deleted with Id: {ProductId} by {userId}", request.Id,userId);
                        var server = _redis.GetServer(_redis.GetEndPoints().First());
                        var database = _redis.GetDatabase();


                        var keys = server.Keys(pattern: "Kantarv2_Analysis:*").ToArray();

                        if (keys.Any())
                        {

                            await database.KeyDeleteAsync(keys);
                        }
                        return Response<NoContent>.Success(204);
                    }
                   
                }


            }
			catch (Exception ex)
			{

				_logger.LogError(ex, "Error occurred while deleting product with Id: {ProductId}", request.Id);
				return Response<NoContent>.Fail(500, "Internal Server Error");
            }
        }

        public async Task<Response<AddProductDto>> Handle(ShipProduct request, CancellationToken cancellationToken)
        {
			var transaction = await _context.Database.BeginTransactionAsync();
            try
			{
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "Bilinmeyen Kullanıcı";
                var UnitPrice = await _context.UnitPrices.FirstOrDefaultAsync(x => (!x.IsDeleted) && x.Id.Equals(request.UnitId));
                if (UnitPrice == null)
                {
                    _logger.LogError("Unit Price not found for UnitId: {UnitId}", request.UnitId);
                    return Response<AddProductDto>.Fail(404, "Unit Price not found");
                }
				var TotalWeight= await _context.Products.Include(x=>x.UnitPrice).Where(x=>!x.IsDeleted&&x.UnitPrice.Id.Equals(UnitPrice.Id)).GroupBy(x=>x.UnitPrice).Select(g=>g.Sum(x=>x.Weight)).FirstOrDefaultAsync();
				if (TotalWeight+ (-request.Weight) < 0)
				{
					_logger.LogError("Insufficient weight available to ship for UnitId: {UnitId}", request.UnitId);
					return Response<AddProductDto>.Fail(400, "Insufficient weight available to ship,please check");

                }
                var product = new Product();
				product.Id = Guid.NewGuid();
				product.UnitPrice = UnitPrice;
				product.IsDeleted = false;
				product.Weight = -request.Weight;
				product.CreatedDate = DateTime.UtcNow;	
                if (request.Price.HasValue)
				{
					product.Price = request.Price.Value;
					product.TotalPrice = -request.Price.Value * product.Weight;
                }
				else
				{
					product.Price = UnitPrice.Price;
					product.TotalPrice = -UnitPrice.Price * product.Weight;

                }
				product.Status= ProductStatus.Shipped;
                await _context.Products.AddAsync(product);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                AddProductDto dto = new()
				{
					Id = product.Id,
					Name = product.UnitPrice.Name,
					Price = product.Price,
					Weight = product.Weight,
					TotalPrice = product.TotalPrice
                };
				_logger.LogInformation("Product shipped with Id: {ProductId} by {userId}", dto.Id,userId);
				return Response<AddProductDto>.Success(201, dto);


            }
			catch (Exception)
			{

				_logger.LogError("Error occurred while shipping product");
				await transaction.RollbackAsync();
				return Response<AddProductDto>.Fail(500, "Internal Server Error");
            }
        }
    }
}
