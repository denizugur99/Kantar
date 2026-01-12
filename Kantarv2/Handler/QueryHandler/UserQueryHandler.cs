
using Kantarv2.DAL;
using Kantarv2.Dtos;
using Kantarv2.Entities;
using Kantarv2.Pagination;
using Kantarv2.Queries.User;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kantarv2.Handler.QueryHandler
{
    public class UserQueryHandler : IRequestHandler<ListAllQuery, Response<List<UserDto>>>,
        IRequestHandler<FindUserById,Response<UserDto>>
    {
        private readonly ILogger<UserQueryHandler> _logger;
        private readonly UserManager<User> _userManager;

        public UserQueryHandler(ILogger<UserQueryHandler> logger, UserManager<User> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }
        public async Task<Response<List<UserDto>>> Handle(ListAllQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Query users with UserManager
                var query = _userManager.Users.Where(x => !x.IsDeleted &&
                    (string.IsNullOrEmpty(request.Search) ||
                     x.UserName.ToLower().Contains(request.Search.Trim().ToLower()) ||
                     x.Email.ToLower().Contains(request.Search.Trim().ToLower())));

                int totalRecords = await query.CountAsync(cancellationToken);
                int PageNumber = request.PageNumber;
                int pageSize = request.PageSize;

                // Get users with pagination
                var users = await query
                    .Skip((PageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                // Map to DTOs and get roles for each user
                var userDtos = new List<UserDto>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userDtos.Add(new UserDto
                    {
                        Username = user.UserName,
                        Email = user.Email,
                        Role = roles.FirstOrDefault() ?? string.Empty // Get first role or empty string
                    });
                }

                PaginationMaker pagination = new PaginationMaker()
                {
                    PageNumber = PageNumber,
                    PageSize = pageSize,
                    TotalRecords = totalRecords
                };

                _logger.LogInformation("Fetched all users successfully.");
                return Response<List<UserDto>>.Success(200, userDtos, pagination);
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
                // Find user by ID
                var user = await _userManager.Users
                    .Where(u => !u.IsDeleted && u.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", request.Id);
                    return Response<UserDto>.Fail(500, "User not found");
                }

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                var userDto = new UserDto
                {
                    Username = user.UserName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? string.Empty
                };

                _logger.LogInformation("Fetched user with ID {UserId} successfully.", request.Id);
                return Response<UserDto>.Success(200, userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching the user.");
                return Response<UserDto>.Fail(500, "An error occurred while fetching the user.");
            }
        }
    }
}
