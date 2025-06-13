using Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries;

public class GetUrlDetailsQuery : IRequest<UrlDetailsDto> 
{
    public string ShortCode { get; set; }

    public GetUrlDetailsQuery() 
    {
       
    }
}
