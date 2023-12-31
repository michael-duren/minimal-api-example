using FluentValidation;
using MagicVilla_CouponAPI.Data;
using MagicVilla_CouponAPI.Models.DTO;

namespace MagicVilla_CouponAPI.Validation;

public class CouponCreateValidation : AbstractValidator<CouponCreateDto>
{
    private readonly AppDbContext _context;

    public CouponCreateValidation(AppDbContext context)
    {
        _context = context;
        RuleFor(model => model.Name).NotEmpty().Must(UniqueName).WithMessage("This Coupon Name already exists.");
        RuleFor(model => model.Percent).InclusiveBetween(1, 100);
    }

    private bool UniqueName(CouponCreateDto couponCreateDto, string name)
    {
        var foundName = _context.Coupons.FirstOrDefault(x => x.Name == name);
        return foundName is null ? true : false;
    }
}