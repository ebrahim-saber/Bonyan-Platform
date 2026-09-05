using FluentValidation;
using ContractingPlatform.Application.DTOs.Projects;
using ContractingPlatform.Application.DTOs.Bids;
using ContractingPlatform.Application.DTOs.Auth;
using ContractingPlatform.Application.DTOs.Reviews;

namespace ContractingPlatform.Application.Validators;

public class CreateProjectDtoValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("عنوان المشروع مطلوب")
            .MinimumLength(5).WithMessage("عنوان المشروع يجب ألا يقل عن 5 أحرف")
            .MaximumLength(150).WithMessage("عنوان المشروع يجب ألا يتجاوز 150 حرفاً");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("وصف المشروع وتفاصيل الأعمال مطلوبة")
            .MinimumLength(20).WithMessage("يرجى كتابة وصف تفصيلي لا يقل عن 20 حرفاً");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("يرجى اختيار تصنيف الخدمة المناسب");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("يرجى تحديد المدينة");

        RuleFor(x => x.District)
            .NotEmpty().WithMessage("يرجى تحديد الحي");

        RuleFor(x => x.ExpectedBudgetMax)
            .GreaterThanOrEqualTo(x => x.ExpectedBudgetMin ?? 0)
            .When(x => x.ExpectedBudgetMax.HasValue && x.ExpectedBudgetMin.HasValue)
            .WithMessage("الحد الأقصى للميزانية يجب أن يكون أكبر من أو يساوي الحد الأدنى");
    }
}

public class CreateBidDtoValidator : AbstractValidator<CreateBidDto>
{
    public CreateBidDtoValidator()
    {
        RuleFor(x => x.ProjectRequestId)
            .GreaterThan(0).WithMessage("معرّف المشروع غير صالح");

        RuleFor(x => x.ProposedPrice)
            .GreaterThan(0).WithMessage("قيمة العرض المالي يجب أن تكون أكبر من صفر");

        RuleFor(x => x.DurationDays)
            .GreaterThan(0).WithMessage("مدة التنفيذ بالأيام يجب أن تكون يوماً واحداً على الأقل");

        RuleFor(x => x.Notes)
            .NotEmpty().WithMessage("يرجى كتابة تفاصيل وخطاب العرض المقدم للعميل")
            .MinimumLength(15).WithMessage("خطاب العرض يجب ألا يقل عن 15 حرفاً");
    }
}

public class RegisterClientDtoValidator : AbstractValidator<RegisterClientDto>
{
    public RegisterClientDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("الاسم الكامل مطلوب");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("البريد الإلكتروني غير صحيح");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("رقم الجوال مطلوب");
        RuleFor(x => x.City).NotEmpty().WithMessage("المدينة مطلوبة");
        RuleFor(x => x.District).NotEmpty().WithMessage("الحي مطلوب");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("كلمة المرور يجب ألا تقل عن 6 خانات");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("تأكيد كلمة المرور غير متطابق");
    }
}

public class RegisterContractorDtoValidator : AbstractValidator<RegisterContractorDto>
{
    public RegisterContractorDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("اسم المسؤول / المفوض مطلوب");
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("اسم الشركة أو المؤسسة مطلوب");
        RuleFor(x => x.CommercialRegistrationNo).NotEmpty().WithMessage("رقم السجل التجاري مطلوب");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("البريد الإلكتروني غير صحيح");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("رقم الجوال مطلوب");
        RuleFor(x => x.City).NotEmpty().WithMessage("المدينة الرئيسية مطلوبة");
        RuleFor(x => x.Bio).NotEmpty().MinimumLength(20).WithMessage("نبذة عن المنشأة وسابقة الأعمال مطلوبة (20 حرفاً على الأقل)");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("كلمة المرور يجب ألا تقل عن 6 خانات");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("تأكيد كلمة المرور غير متطابق");
    }
}

public class CreateReviewDtoValidator : AbstractValidator<CreateReviewDto>
{
    public CreateReviewDtoValidator()
    {
        RuleFor(x => x.ProjectContractId).GreaterThan(0);
        RuleFor(x => x.OverallRating).InclusiveBetween(1, 5).WithMessage("التقييم يجب أن يكون بين 1 و 5 نجوم");
        RuleFor(x => x.QualityRating).InclusiveBetween(1, 5);
        RuleFor(x => x.PunctualityRating).InclusiveBetween(1, 5);
        RuleFor(x => x.CommunicationRating).InclusiveBetween(1, 5);
    }
}
