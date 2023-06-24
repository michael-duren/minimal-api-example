using System.Net;
using AutoMapper;
using FluentValidation;
using MagicVilla_CouponAPI.Models;
using MagicVilla_CouponAPI.Models.DTO;
using MagicVilla_CouponAPI.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace MagicVilla_CouponAPI.Endpoints;

public static class CouponEndpoints
{
    public static void ConfigureCouponEndpoints(this WebApplication app)
    {
        app.MapGet("/api/coupon", GetAllCoupon).WithName("GetCoupons")
            .Produces<ApiResponse>();

        app.MapGet("/api/coupon/{id:int}", GetCoupon)
            .WithName("GetCoupon")
            .Produces<ApiResponse>();

        app.MapPost("/api/coupon", CreateCoupon)
            .WithName("CreateCoupon").Accepts<CouponCreateDto>("application/json")
            .Produces<ApiResponse>(201).Produces(400);

        app.MapPut("/api/coupon/{id:int}", UpdateCoupon)
            .WithName("UpdateCoupon").Accepts<CouponUpdateDto>("application/json")
            .Produces<ApiResponse>().Produces(400);


        app.MapDelete("/api/coupon/{id:int}", DeleteCoupon)
            .WithName("DeleteCoupon").Accepts<int>("application/json")
            .Produces<ApiResponse>().Produces(400);
    }

    private static async Task<IResult> GetAllCoupon(ICouponRepository context)
    {
        ApiResponse response = new();
        response.Result = await context.GetAllAsync();
        response.IsSuccess = true;
        response.StatusCode = HttpStatusCode.OK;

        return Results.Ok(response);
    }

    private static async Task<IResult> GetCoupon(ICouponRepository context, int id)
    {
        ApiResponse response = new();
        response.Result = await context.GetAsync(id);
        response.IsSuccess = true;
        response.StatusCode = HttpStatusCode.OK;

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateCoupon(ICouponRepository context, IValidator<CouponCreateDto> validator,
        IMapper mapper,
        [FromBody] CouponCreateDto couponCDto)
    {
        ApiResponse response = new() { IsSuccess = false, StatusCode = HttpStatusCode.BadRequest };

        var validationResult = await validator.ValidateAsync(couponCDto);
        if (!validationResult.IsValid)
        {
            response.ErrorMessages.Add(validationResult.Errors.FirstOrDefault()?.ToString());
            return Results.BadRequest(response);
        }

        var coupon = mapper.Map<Coupon>(couponCDto);

        context.CreateAsync(coupon);
        await context.SaveAsync();
        var couponDto = mapper.Map<CouponDto>(coupon);

        response.Result = couponCDto;
        response.IsSuccess = true;
        response.StatusCode = HttpStatusCode.Created;

        return Results.CreatedAtRoute("GetCoupon", new { id = coupon.Id }, couponDto);
    }

    private static async Task<IResult> UpdateCoupon(ICouponRepository context, IValidator<CouponUpdateDto> validator,
        IMapper mapper,
        int id,
        [FromBody] CouponUpdateDto couponUpdateDto)
    {
        ApiResponse response = new()
        {
            IsSuccess = false, StatusCode = HttpStatusCode.BadRequest,
            ErrorMessages = { "Invalid Id For Coupon" }
        };

        var validation = await validator.ValidateAsync(couponUpdateDto);

        if (!validation.IsValid)
        {
            response.ErrorMessages.Add(validation.Errors.FirstOrDefault()?.ToString());
            return Results.BadRequest(response);
        }

        context.UpdateAsync(mapper.Map<Coupon>(couponUpdateDto));

        await context.SaveAsync();

        var couponDto = mapper.Map<CouponDto>(context.GetAsync(couponUpdateDto.Id));
        response.Result = couponDto;
        response.IsSuccess = true;
        response.StatusCode = HttpStatusCode.Accepted;

        return Results.Ok(response);
    }

    private static async Task<IResult> DeleteCoupon(ICouponRepository context, int id)
    {
        ApiResponse response = new()
        {
            IsSuccess = false, StatusCode = HttpStatusCode.BadRequest,
            ErrorMessages = new List<string> { "Error, Invalid ID" }
        };
        var couponToRemove = await context.GetAsync(id);
        if (couponToRemove is null) return Results.BadRequest(response);

        context.RemoveAsync(couponToRemove);
        await context.SaveAsync();

        response.Result = null;
        response.IsSuccess = true;
        response.StatusCode = HttpStatusCode.NoContent;
        response.ErrorMessages = null;
        return Results.Ok(response);
    }
}