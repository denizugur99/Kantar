﻿
using Kantar.Command;
using Kantar.DAL;
using Kantar.Dtos;
using Kantar.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace Kantar.Handler.Product
{
    public class ProductCommandHandler : IRequestHandler<AddProductCommand, Response<NoContent>>,
                                          
                                         IRequestHandler<ShipProductCommand, Response<NoContent>>
    {
        private readonly KantarDbContext _context;

        public ProductCommandHandler(KantarDbContext context)
        {
            _context = context;
        }

        public async Task<Response<NoContent>> Handle(AddProductCommand request, CancellationToken cancellationToken)
        {
            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
            try
            { 
                var product = new ProductKantar();
                var query = await _context.UnitPrice.Where(x=> !x.IsDeleted).FirstOrDefaultAsync(x => x.Name.Trim().ToLower().Equals(request.ProductName.Trim().ToLower()));
                if (query == null) {
                    if (request.Price == 0)
                    {
                        return Response<NoContent>.Fail("Lütfen bir product name yada price girin price yada ürün bulunamadı", 500);
                    }
                var price=new UnitPrice();
                    price.Name=request.ProductName; 
                    price.Price=request.Price;
                    product.UnitPrice = price;
                    await _context.UnitPrice.AddAsync(price);
                }
                else
                {                    
                    product.UnitPrice = query;
                }
                product.Weight=request.Weight;
                product.DateTime = DateTime.UtcNow;
                if(request.Price != 0) {
                product.TotalPrice=-request.Price*product.Weight;
                }
                else
                {
                product.TotalPrice = product.Weight * -product.UnitPrice.Price;
                }
                var lastProduct = _context.Products
                                           .Where(x => !x.IsDeleted)
                                           .OrderByDescending(p => p.DateTime)
                                           .FirstOrDefault(x=>x.UnitPrice.Name.Trim().ToLower().Equals(product.UnitPrice.Name));
                if (lastProduct != null)
                {
                  product.Devir = product.TotalPrice + lastProduct.Devir;
                }
                else
                {
                    product.Devir = product.TotalPrice;
                }
                
                
                await _context.Products.AddAsync(product);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Response<NoContent>.Success(204);

            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return Response<NoContent>.Fail("HATA", 500);
            }

        }
        

        public async Task<Response<NoContent>> Handle(ShipProductCommand request, CancellationToken cancellationToken)
        {
            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var product = new ProductKantar();
                var query = await _context.UnitPrice.Where(x => !x.IsDeleted).FirstOrDefaultAsync(x => x.Name.Trim().ToLower().Equals(request.ProductName.Trim().ToLower()));
                if (query == null)
                {
                    if (request.Price == 0)
                    {
                        return Response<NoContent>.Fail("Lütfen bir product name yada price girin price yada ürün bulunamadı", 500);
                    }
                    var price = new UnitPrice();
                    price.Name = request.ProductName;
                    price.Price = request.Price;
                    product.UnitPrice = price;
                    await _context.UnitPrice.AddAsync(price);
                }
                else
                {
                    product.UnitPrice = query;
                }
                product.Weight = -request.Weight;
                product.DateTime = DateTime.UtcNow;
                if (request.Price != 0)
                {
                    product.TotalPrice = -request.Price * product.Weight;
                }
                else
                {
                    product.TotalPrice = product.Weight  * -product.UnitPrice.Price;
                }
                var lastProduct = _context.Products
                                           .Where(x => !x.IsDeleted)
                                           .OrderByDescending(p => p.DateTime)
                                           .FirstOrDefault(x => x.UnitPrice.Name.Trim().ToLower().Equals(product.UnitPrice.Name));
                if (lastProduct != null)
                {
                    product.Devir = product.TotalPrice + lastProduct.Devir;
                }
                else
                {
                    product.Devir = product.TotalPrice;
                }


                await _context.Products.AddAsync(product);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Response<NoContent>.Success(204);

            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return Response<NoContent>.Fail("HATA", 500);
            }
        }
    }
}
