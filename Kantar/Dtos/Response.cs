
using Kantar.Pagination;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Kantar.Dtos
{
    public class Response<T>
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T Data { get; set; }

        [JsonIgnore]
        public int StatusCode { get; set; }

        [JsonIgnore]
        public bool IsSuccessful { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Errors { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationMaker Pagination { get; set; }
        public static Response<T> Success(T data, int statusCode,PaginationMaker pagination=null)
        {
            return new Response<T> { Data = data, StatusCode = statusCode, IsSuccessful = true,Pagination=pagination };
        }
        public static Response<T> Success(int statusCode)
        {
            return new Response<T> { Data = default(T), StatusCode = statusCode, IsSuccessful = true };
        }
        public static Response<T> Fail(List<string> errors, int statusCode)
        {
            return new Response<T>
            {
                Errors = string.Join(";", errors),
                StatusCode = statusCode,
                IsSuccessful = false
            };
        }
        public static Response<T> Fail(string error, int statusCode)
        {
            return new Response<T> { Errors = error, StatusCode = statusCode, IsSuccessful = false };
        }
        public static Response<T> Fail(ValidationResult result, int statusCode)
        {
            return new Response<T>
            {
                Errors = string.Join(";", result.ErrorMessage.ToList()),
                StatusCode = statusCode,
                IsSuccessful = false
            };
        }

    } 

}

