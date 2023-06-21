using System.Net;
using AutoMapper;
using FluentValidation;
using MagicVilla_CouponAPI;
using MagicVilla_CouponAPI.Data;
using MagicVilla_CouponAPI.Models;
using MagicVilla_CouponAPI.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddAutoMapper(typeof(MappingConfig));
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

app.MapGet("/api/coupon", () =>
    {
        ApiResponse response = new();
        response.Result = CouponStore.couponList;
        response.IsSuccess = true;
        response.StatusCode = HttpStatusCode.OK;

        return Results.Ok(response);
    }).WithName("GetCoupons")
    .Produces<ApiResponse>();

app.MapGet("/api/coupon/{id:int}",
        (int id) =>
        {
            ApiResponse response = new();
            response.Result = CouponStore.couponList.FirstOrDefault(u => u.Id == id);
            response.IsSuccess = true;
            response.StatusCode = HttpStatusCode.OK;

            return Results.Ok(response);
        })
    .WithName("GetCoupon")
    .Produces<ApiResponse>(200);

app.MapPost("/api/coupon",
        async (IValidator<CouponCreateDto> validator, IMapper mapper, [FromBody] CouponCreateDto couponCDto) =>
        {
            ApiResponse response = new() { IsSuccess = false, StatusCode = HttpStatusCode.BadRequest };

            var validationResult = await validator.ValidateAsync(couponCDto);
            if (!validationResult.IsValid)
            {
                response.ErrorMessages.Add(validationResult.Errors.FirstOrDefault()?.ToString());
                return Results.BadRequest(response);
            }

            var coupon = mapper.Map<Coupon>(couponCDto);

            coupon.Id = CouponStore.couponList.MaxBy(u => u.Id)!.Id + 1;
            CouponStore.couponList.Add(coupon);
            var couponDto = mapper.Map<CouponDto>(coupon);

            response.Result = couponCDto;
            response.IsSuccess = true;
            response.StatusCode = HttpStatusCode.Created;

            return Results.CreatedAtRoute("GetCoupon", new { id = coupon.Id }, couponDto);
        })
    .WithName("CreateCoupon").Accepts<CouponCreateDto>("application/json")
    .Produces<ApiResponse>(201).Produces(400);

app.MapPut("/api/coupon/{id:int}", async (IValidator<CouponUpdateDto> validator, IMapper mapper, int id,
        [FromBody] CouponUpdateDto couponUpdateDto) =>
    {
        ApiResponse response = new() { IsSuccess = false, StatusCode = HttpStatusCode.BadRequest };
        var couponToUpdate = CouponStore.couponList.FirstOrDefault(u => u.Id == id);
        if (couponToUpdate is null) return Results.BadRequest(response);

        var validation = await validator.ValidateAsync(couponUpdateDto);

        if (!validation.IsValid)
        {
            response.ErrorMessages.Add(validation.Errors.FirstOrDefault()?.ToString());
            return Results.BadRequest(response);
        }

        var coupon = mapper.Map<Coupon>(couponUpdateDto);
        CouponStore.couponList.Remove(couponToUpdate);
        CouponStore.couponList.Add(coupon);

        var couponDto = mapper.Map<CouponDto>(coupon);
        response.Result = couponDto;
        response.IsSuccess = true;
        response.StatusCode = HttpStatusCode.Accepted;

        return Results.Ok(response);
    })
    .WithName("UpdateCoupon").Accepts<CouponUpdateDto>("application/json")
    .Produces<ApiResponse>(200).Produces(400);


app.MapDelete("/api/coupon/{id:int}", (int id) =>
    {
        ApiResponse response = new()
        {
            IsSuccess = false, StatusCode = HttpStatusCode.BadRequest,
            ErrorMessages = new List<string> { "Error, Invalid ID" }
        };
        var couponToRemove = CouponStore.couponList.FirstOrDefault(u => u.Id == id);
        if (couponToRemove is null) return Results.BadRequest(response);

        CouponStore.couponList.Remove(couponToRemove);

        response.Result = null;
        response.IsSuccess = true;
        response.StatusCode = HttpStatusCode.NoContent;
        return Results.Ok(response);
    })
    .WithName("UpdateCoupon").Accepts<CouponUpdateDto>("application/json")
    .Produces<ApiResponse>(200).Produces(400);

app.UseHttpsRedirection();

app.Run();