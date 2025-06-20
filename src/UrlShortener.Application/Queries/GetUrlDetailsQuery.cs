﻿using Application.DTOs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Queries;

public record GetUrlDetailsQuery : IRequest<UrlDetailsDto> 
{
    public string ShortCode { get; init; }

    public GetUrlDetailsQuery() 
    {
       
    }
}
