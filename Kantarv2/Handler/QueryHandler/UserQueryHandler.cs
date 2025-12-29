
using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Pagination;
using Kantarv2.Queries.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kantarv2.Handler.QueryHandler
{
    public class UserQueryHandler : IRequestHandler<ListAllQuery, Response<List<UserDto>>>,
        IRequestHandler<FindUserById,Response<UserDto>>
    {
        private readonly ILogger<UserQueryHandler> _logger;
        private readonly KantarDbContext _context;
        public UserQueryHandler(ILogger<UserQueryHandler> logger, KantarDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        public async Task<Response<List<UserDto>>> Handle(ListAllQuery request, CancellationToken cancellationToken)
        {
            try
            {

                var query = _context.Users.Where(x => (!x.IsDeleted) && (string.IsNullOrEmpty(request.Search) || x.Username.Trim().ToLower().Contains(request.Search.Trim().ToLower())||x.Email.Trim().ToLower().Contains(request.Search.Trim().ToLower())));
                int totalRecords = await query.CountAsync(cancellationToken);
                int PageNumber = request.PageNumber;
                int pageSize = request.PageSize;
                var Users =await query.Skip((PageNumber - 1) * pageSize).Take(pageSize).Select(x=> new UserDto
                {
                    Username = x.Username,
                    Email = x.Email,
                    Role = x.Role
                }).ToListAsync(cancellationToken);
                PaginationMaker pagination = new PaginationMaker()
                {
                    PageNumber = PageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords
                };
                _logger.LogInformation("Fetched all users successfully.");
                return Response<List<UserDto>>.Success(200, Users,pagination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users.");

                return Response<List<UserDto>>.Fail(500, ex.Message);
            }

        }

        public async Task<Response<UserDto>> Handle(FindUserById request, CancellationToken cancellationToken)
        {
            try
            {
                var query = await _context.Users.Where(u => !u.IsDeleted && u.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
                if (query == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", request.Id);
                    return Response<UserDto>.Fail(500, "User not found");
                }
                var userDto = new UserDto
                {
                    Username = query.Username,
                    Email = query.Email,
                    Role = query.Role
                };
                _logger.LogInformation("Fetched user with ID {UserId} successfully.", request.Id);
                return Response<UserDto>.Success(200, userDto);

            }
            catch (Exception)
            {

                return Response<UserDto>.Fail(500, "An error occurred while fetching the user.");
            }
        }
    }
}
