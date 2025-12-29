using Kantarv2.Command.UnitPrice;
using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Entities;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Security.Claims;

namespace Kantarv2.Handler.CommandHandler
{
    public class PriceCommandHandler : IRequestHandler<AddUnitPrice, Response<UnitPriceDto>>,
        IRequestHandler<UpdateUnitPrice, Response<UnitPriceDto>>,
        IRequestHandler<DeleteUnitPrice, Response<NoContent>>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly KantarDbContext _context;
        private readonly ILogger<PriceCommandHandler> _logger;
        private readonly IConnectionMultiplexer _redis;
        public PriceCommandHandler(IHttpContextAccessor httpContextAccessor, KantarDbContext context, ILogger<PriceCommandHandler> logger,IConnectionMultiplexer connectionMultiplexer)
        {
            _httpContextAccessor = httpContextAccessor;
            _redis = connectionMultiplexer;
            _context = context;
            _logger = logger;
        }
        public async Task<Response<UnitPriceDto>> Handle(AddUnitPrice request, CancellationToken cancellationToken)
        {
            try
            {

                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "Bilinmeyen Kullanıcı";
                var query = await _context.UnitPrices.FirstOrDefaultAsync(x => !x.IsDeleted && x.Name.Trim().ToLower().Equals(request.Name.Trim().ToLower()));
                if (query == null)
                {
                    var unitPrice = new UnitPrice()
                    {
                        Id = Guid.NewGuid(),
                        Name = request.Name,
                        Price = request.Price


                    };
                    await _context.UnitPrices.AddAsync(unitPrice);
                    _logger.LogInformation("Unit Price added successfully by {UserId}: {UnitPriceName}",userId, unitPrice.Name);
                    await _context.SaveChangesAsync();
                    var server = _redis.GetServer(_redis.GetEndPoints().First());
                    var database = _redis.GetDatabase();

                   
                    var keys = server.Keys(pattern: "Kantarv2_Analysis:*").ToArray();

                    if (keys.Any())
                    {
                        
                        await database.KeyDeleteAsync(keys);
                    }
                    return Response<UnitPriceDto>.Success(200, new UnitPriceDto
                    {
                        Id=unitPrice.Id,
                        Name = unitPrice.Name,
                        Price = unitPrice.Price,
                    });
                }
                else
                {
                    _logger.LogWarning("Unit Price with name {UnitPriceName} already exists.", request.Name);
                    return Response<UnitPriceDto>.Fail(400, $"Unit Price with name {request.Name} already exists.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding Unit Price: {ErrorMessage}", ex.Message);
                return Response<UnitPriceDto>.Fail(500, "An error occurred while processing your request.");

            }


        }

        public async Task<Response<UnitPriceDto>> Handle(UpdateUnitPrice request, CancellationToken cancellationToken)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "Bilinmeyen Kullanıcı";
            var query = await _context.UnitPrices.FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == request.Id, cancellationToken);
            if (query == null)
            {
                _logger.LogWarning("Unit Price with ID {UnitPriceId} not found.", request.Id);
                return Response<UnitPriceDto>.Fail(404, $"Unit Price with ID {request.Id} not found.");
            }
            else
            {
                query.Price = request.Price;
                _context.UnitPrices.Update(query);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Unit Price with ID {UnitPriceId} updated by {userId} successfully.", request.Id, userId);
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var database = _redis.GetDatabase();


                var keys = server.Keys(pattern: "Kantarv2_Analysis:*").ToArray();

                if (keys.Any())
                {

                    await database.KeyDeleteAsync(keys);
                }
                return Response<UnitPriceDto>.Success(200, new UnitPriceDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    Price = query.Price
                });
            }
        }

        public async Task<Response<NoContent>> Handle(DeleteUnitPrice request, CancellationToken cancellationToken)
        {
            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "Bilinmeyen Kullanıcı";
                var user= await _context.UnitPrices.FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == request.Id, cancellationToken);
                if (user == null)
                {
                    _logger.LogWarning("Unit Price with ID {UnitPriceId} not found.", request.Id);
                    return Response<NoContent>.Fail(404, $"Unit Price with ID {request.Id} not found.");
                }
                else
                {
                    user.IsDeleted = true;
                    _context.UnitPrices.Update(user);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Unit Price with ID {UnitPriceId} deleted by {userıd} successfully.", request.Id,userId);
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
            catch (Exception ex)
            {

               _logger.LogError(ex, "An error occurred while deleting Unit Price with ID {UnitPriceId}: {ErrorMessage}", request.Id, ex.Message);
                return Response<NoContent>.Fail(500, "An error occurred while processing your request.");
            }
        }
    }
}
