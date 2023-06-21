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

app.MapGet("/hello", () => "HELLO");

app.MapGet("/api/coupon", (AppDbContext context) =>
    {
        ApiResponse response = new();
        response.Result = context.Coupons;
        response.IsSuccess = true;
        response.StatusCode = HttpStatusCode.OK;

        return Results.Ok(response);
    }).WithName("GetCoupons")
    .Produces<ApiResponse>();

app.MapGet("/api/coupon/{id:int}",
        async (AppDbContext context, int id) =>
        {
            ApiResponse response = new();
            response.Result = await context.Coupons.FirstOrDefaultAsync(u => u.Id == id);
            response.IsSuccess = true;
            response.StatusCode = HttpStatusCode.OK;

            return Results.Ok(response);
        })
    .WithName("GetCoupon")
    .Produces<ApiResponse>(200);

app.MapPost("/api/coupon",
        async (AppDbContext context, IValidator<CouponCreateDto> validator, IMapper mapper,
            [FromBody] CouponCreateDto couponCDto) =>
        {
            ApiResponse response = new() { IsSuccess = false, StatusCode = HttpStatusCode.BadRequest };

            var validationResult = await validator.ValidateAsync(couponCDto);
            if (!validationResult.IsValid)
            {
                response.ErrorMessages.Add(validationResult.Errors.FirstOrDefault()?.ToString());
                return Results.BadRequest(response);
            }

            var coupon = mapper.Map<Coupon>(couponCDto);

            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();
            var couponDto = mapper.Map<CouponDto>(coupon);

            response.Result = couponCDto;
            response.IsSuccess = true;
            response.StatusCode = HttpStatusCode.Created;

            return Results.CreatedAtRoute("GetCoupon", new { id = coupon.Id }, couponDto);
        })
    .WithName("CreateCoupon").Accepts<CouponCreateDto>("application/json")
    .Produces<ApiResponse>(201).Produces(400);

app.MapPut("/api/coupon/{id:int}", async (AppDbContext context, IValidator<CouponUpdateDto> validator, IMapper mapper,
        int id,
        [FromBody] CouponUpdateDto couponUpdateDto) =>
    {
        ApiResponse response = new()
            { IsSuccess = false, StatusCode = HttpStatusCode.BadRequest, ErrorMessages = { "Invalid Id For Coupon" } };
        if (!context.Coupons.Any(u => u.Id == id)) return Results.BadRequest(response);

        var validation = await validator.ValidateAsync(couponUpdateDto);

        if (!validation.IsValid)
        {
            response.ErrorMessages.Add(validation.Errors.FirstOrDefault()?.ToString());
            return Results.BadRequest(response);
        }

        var coupon = mapper.Map<Coupon>(couponUpdateDto);
        context.Coupons.Update(coupon);
        await context.SaveChangesAsync();

        var couponDto = mapper.Map<CouponDto>(coupon);
        response.Result = couponDto;
        response.IsSuccess = true;
        response.StatusCode = HttpStatusCode.Accepted;

        return Results.Ok(response);
    })
    .WithName("UpdateCoupon").Accepts<CouponUpdateDto>("application/json")
    .Produces<ApiResponse>(200).Produces(400);


app.MapDelete("/api/coupon/{id:int}", async (AppDbContext context, int id) =>
    {
        ApiResponse response = new()
        {
            IsSuccess = false, StatusCode = HttpStatusCode.BadRequest,
            ErrorMessages = new List<string> { "Error, Invalid ID" }
        };
        var couponToRemove = await context.Coupons.FirstOrDefaultAsync(u => u.Id == id);
        if (couponToRemove is null) return Results.BadRequest(response);

        context.Coupons.Remove(couponToRemove);
        await context.SaveChangesAsync();

        response.Result = null;
        response.IsSuccess = true;
        response.StatusCode = HttpStatusCode.NoContent;
        response.ErrorMessages = null;
        return Results.Ok(response);
    })
    .WithName("DeleteCoupon").Accepts<int>("application/json")
    .Produces<ApiResponse>(200).Produces(400);

app.UseHttpsRedirection();

app.Run();