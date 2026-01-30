using Kantarv2.Pagination;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Kantarv2.Dtos
{
    public class Response<T>
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T Data { get; set; }

        [JsonIgnore]
        public int StatusCode { get; set; }

        [JsonIgnore]
        public bool IsSuccess { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Errors { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationMaker Pagination { get; set; }

        public static Response<T> Success(int statuscode)
        {
            return new Response<T>
            {
                IsSuccess = true,
                StatusCode = statuscode,
                Data = default(T)
            };
        }
        public static Response<T> Success(int statuscode, T data,PaginationMaker? pagination=null)
        {
            return new Response<T>
            {
                IsSuccess = true,
                
                StatusCode = statuscode,
                Data = data,
                Pagination = pagination

            };
        }
        public static Response<T> Fail(int statuscode, List<string> Errors)
        {
            return new Response<T>
            {
                IsSuccess = false,
                StatusCode = statuscode,
                Errors = string.Join(", ", Errors)
            };
        }

        public static Response<T> Fail(int statuscode,string Errors)
        {
            return new Response<T>
            {
                IsSuccess = false,
                StatusCode = statuscode,
                Errors = Errors
            };
        }
        public static Response<T> Fail(ValidationResult result, int statusCode)
        {
            return new Response<T>
            {
                Errors = string.Join(";", result.ErrorMessage.ToList()),
                StatusCode = statusCode,
                IsSuccess = false
            };
        }


    }
}
    
      