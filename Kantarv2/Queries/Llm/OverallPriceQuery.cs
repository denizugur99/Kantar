using Kantarv2.Dtos;
using MediatR;

namespace Kantarv2.Queries.Llm
{
    public class OverallPriceQuery:IRequest<Response<string>>
    {
        public string Prompt { get; set; }
    
    }
}
