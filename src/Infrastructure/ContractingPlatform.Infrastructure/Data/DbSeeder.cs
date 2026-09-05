using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ContractingPlatform.Domain.Entities;
using ContractingPlatform.Domain.Enums;

namespace ContractingPlatform.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // 1. Seed Roles
        string[] roles = { nameof(UserType.Admin), nameof(UserType.Contractor), nameof(UserType.Client) };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Admin User
        var adminEmail = "admin@contracting.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "مدير النظام العام",
                UserType = UserType.Admin,
                EmailConfirmed = true,
                PhoneNumber = "0500000001",
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, nameof(UserType.Admin));
            }
        }

        // 3. Seed Categories & Services if empty
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new()
                {
                    NameAr = "أعمال البناء والتشطيب",
                    NameEn = "Building & Finishing",
                    DescriptionAr = "خدمات المقاولات العامة، التشطيب المتكامل، الترميم، وبناء الملاحق",
                    IconCss = "bi-building",
                    DisplayOrder = 1,
                    Services = new List<ServiceItem>
                    {
                        new() { NameAr = "تشطيب متكامل تسليم مفتاح", NameEn = "Turnkey Finishing", DescriptionAr = "تنفيذ كامل أعمال التشطيبات الداخلية والخارجية" },
                        new() { NameAr = "ترميم وتجديد العقارات", NameEn = "Renovation & Restoration", DescriptionAr = "صيانة شاملة للمباني القائمة وإعادة تأهيلها" },
                        new() { NameAr = "بناء ملاحق وتوسعات", NameEn = "Annex & Extension Construction", DescriptionAr = "بناء ملاحق علوية وأرضية ومظلات خرسانية" }
                    }
                },
                new()
                {
                    NameAr = "الأعمال الكهربائية والإنارة",
                    NameEn = "Electrical & Lighting",
                    DescriptionAr = "تأسيس وصيانة شبكات الكهرباء، إنارة المنازل، وأنظمة الطاقة",
                    IconCss = "bi-lightning-charge",
                    DisplayOrder = 2,
                    Services = new List<ServiceItem>
                    {
                        new() { NameAr = "تأسيس وتمديد شبكات الكهرباء", NameEn = "Electrical Wiring Setup", DescriptionAr = "سحب أسلاك وتوزيع القواطع الكهربائية" },
                        new() { NameAr = "تركيب الإنارة والديكورات الضوئية", NameEn = "Lighting Installation", DescriptionAr = "تركيب الثريات والسبوت لايت والإنارة المخفية" },
                        new() { NameAr = "صيانة لوحات التوزيع والأعطال", NameEn = "Distribution Board Maintenance", DescriptionAr = "إصلاح قواطع الكهرباء ودوائر الشورت" }
                    }
                },
                new()
                {
                    NameAr = "أعمال السباكة والصرف",
                    NameEn = "Plumbing & Drainage",
                    DescriptionAr = "تأسيس وصيانة شبكات المياه والصرف الصحي وتركيب الأطقم",
                    IconCss = "bi-droplet-half",
                    DisplayOrder = 3,
                    Services = new List<ServiceItem>
                    {
                        new() { NameAr = "تأسيس خطوط التغذية والصرف", NameEn = "Plumbing Rough-In", DescriptionAr = "تمديد أنابيب المياه الحرارية والصرف المعلق" },
                        new() { NameAr = "تركيب أطقم الحمامات والمطابخ", NameEn = "Sanitary Ware Installation", DescriptionAr = "تركيب المغاسل والمراحيض والخلاطات والسخانات" },
                        new() { NameAr = "كشف ومعالجة تسربات المياه", NameEn = "Leak Detection & Repair", DescriptionAr = "كشف إلكتروني لتسربات المياه مع الضمان" }
                    }
                },
                new()
                {
                    NameAr = "التكييف والتهوية",
                    NameEn = "HVAC & Ventilation",
                    DescriptionAr = "توريد وتركيب وصيانة مكيفات الاسبليت والمركزي والدكت",
                    IconCss = "bi-snow",
                    DisplayOrder = 4,
                    Services = new List<ServiceItem>
                    {
                        new() { NameAr = "تركيب وتمديد مكيفات سبليت", NameEn = "Split AC Installation", DescriptionAr = "تمديد نحاس وتركيب الوحدات الداخلية والخارجية" },
                        new() { NameAr = "تنظيف وصيانة دورية للمكيفات", NameEn = "AC Cleaning & Maintenance", DescriptionAr = "غسيل المكيفات بالضغط وشحن الفريون" },
                        new() { NameAr = "تنفيذ دكت التكييف المركزي", NameEn = "Central AC Ductwork", DescriptionAr = "تصنيع وتركيب مجاري الهواء والعوازل" }
                    }
                },
                new()
                {
                    NameAr = "الدهانات والديكورات الحديثة",
                    NameEn = "Painting & Modern Decor",
                    DescriptionAr = "دهانات داخلية وخارجية، بديل الرخام، بديل الخشب، والجبس بورد",
                    IconCss = "bi-palette",
                    DisplayOrder = 5,
                    Services = new List<ServiceItem>
                    {
                        new() { NameAr = "دهانات داخلية وبروفايل خارجي", NameEn = "Interior & Exterior Painting", DescriptionAr = "تنفيذ أحدث ألوان وأشكال الدهانات بمواد مقاومة" },
                        new() { NameAr = "أسقف جبس بورد وفواصل جدارية", NameEn = "Gypsum Board & Partitions", DescriptionAr = "تصميم وتنفيذ ديكورات الأسقف الجبسية المعلقة" },
                        new() { NameAr = "تركيب بديل الخشب وبديل الرخام", NameEn = "Wood & Marble Alternative", DescriptionAr = "تكسيات جدارية عصرية بتشطيبات راقية" }
                    }
                },
                new()
                {
                    NameAr = "العوازل المائية والحرارية",
                    NameEn = "Water & Thermal Insulation",
                    DescriptionAr = "عزل أسطح فوم وممبرين، عزل خزانات مياه، وحمامات ومطابخ",
                    IconCss = "bi-shield-check",
                    DisplayOrder = 6,
                    Services = new List<ServiceItem>
                    {
                        new() { NameAr = "عزل أسطح بولي يوريثان (فوم)", NameEn = "Polyurethane Foam Insulation", DescriptionAr = "عزل مائي وحراري مزدوج مع الضمان المعتمد" },
                        new() { NameAr = "عزل خزانات المياه الإيبوكسي", NameEn = "Epoxy Tank Insulation", DescriptionAr = "معالجة التشققات وعزل خزانات الشرب بمواد صحية" }
                    }
                }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 4. Seed Demo Contractor
        var contractorEmail = "contractor@builder.com";
        var contractorUser = await userManager.FindByEmailAsync(contractorEmail);
        if (contractorUser == null)
        {
            contractorUser = new ApplicationUser
            {
                UserName = contractorEmail,
                Email = contractorEmail,
                FullName = "م. فهد القحطاني (شركة إعمار للمقاولات)",
                UserType = UserType.Contractor,
                EmailConfirmed = true,
                PhoneNumber = "0555123456",
                IsActive = true
            };

            var createRes = await userManager.CreateAsync(contractorUser, "Contractor@123456");
            if (createRes.Succeeded)
            {
                await userManager.AddToRoleAsync(contractorUser, nameof(UserType.Contractor));

                var profile = new ContractorProfile
                {
                    UserId = contractorUser.Id,
                    CompanyName = "مؤسسة إعمار البناء للمقاولات العامة والتشطيب",
                    CommercialRegistrationNo = "1010899450",
                    TaxNumber = "300589123400003",
                    Bio = "مؤسسة معتمدة ومتخصصة في أعمال المقاولات العامة والتشطيبات الفاخرة للفلل والمكاتب التجارية، خبرة تتجاوز 12 عاماً في السوق السعودي، تنفيذ وفق كود البناء السعودي مع ضمان شامل على الأعمال.",
                    YearsOfExperience = 12,
                    City = "الرياض",
                    District = "حي الصحافة",
                    CoverageCities = "الرياض, الخرج, الدرعية",
                    VerificationStatus = VerificationStatus.Approved,
                    VerifiedAt = DateTime.UtcNow,
                    Rating = 4.95m,
                    TotalReviews = 28,
                    IsAvailable = true
                };

                await context.ContractorProfiles.AddAsync(profile);
                await context.SaveChangesAsync();

                // Link some services to contractor
                var firstCategoryServices = await context.ServiceItems.Take(3).ToListAsync();
                foreach (var s in firstCategoryServices)
                {
                    await context.ContractorServices.AddAsync(new ContractorService
                    {
                        ContractorProfileId = profile.Id,
                        ServiceItemId = s.Id
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        // 5. Seed Demo Client
        var clientEmail = "client@home.com";
        var clientUser = await userManager.FindByEmailAsync(clientEmail);
        if (clientUser == null)
        {
            clientUser = new ApplicationUser
            {
                UserName = clientEmail,
                Email = clientEmail,
                FullName = "عبدالله بن ناصر",
                UserType = UserType.Client,
                EmailConfirmed = true,
                PhoneNumber = "0544987654",
                IsActive = true
            };

            var createRes = await userManager.CreateAsync(clientUser, "Client@123456");
            if (createRes.Succeeded)
            {
                await userManager.AddToRoleAsync(clientUser, nameof(UserType.Client));

                var clientProfile = new ClientProfile
                {
                    UserId = clientUser.Id,
                    City = "الرياض",
                    District = "حي الملقا",
                    AddressDetails = "شارع الأمير تركي بن عبدالعزيز الأول"
                };

                await context.ClientProfiles.AddAsync(clientProfile);
                await context.SaveChangesAsync();

                // Seed a sample project request for the client
                var finishingCat = await context.Categories.FirstOrDefaultAsync();
                if (finishingCat != null)
                {
                    var sampleProject = new ProjectRequest
                    {
                        ClientProfileId = clientProfile.Id,
                        CategoryId = finishingCat.Id,
                        Title = "تشطيب فيلا سكنية دورين وملحق - مساحة 450 م²",
                        Description = "مطلوب مقاول تشطيب عالي الجودة لتنفيذ كامل أعمال اللياسة، والجبس بورد، والسباكة والكهرباء والدهانات لفيلا جديدة دورين وملحق بالملقا. يفضل من لديه سابقة أعمال مشرفة ويمكن معاينتها على أرض الواقع.",
                        City = "الرياض",
                        District = "الملقا",
                        DetailedAddress = "الملقا - مخطط حطين النموذجي",
                        ExpectedBudgetMin = 180000m,
                        ExpectedBudgetMax = 260000m,
                        DesiredExecutionDate = DateTime.UtcNow.AddDays(15),
                        Status = ProjectStatus.OpenForBids,
                        BidsCount = 1,
                        ViewsCount = 14
                    };

                    await context.ProjectRequests.AddAsync(sampleProject);
                    await context.SaveChangesAsync();

                    // Add a sample bid from our contractor
                    var contractorProfile = await context.ContractorProfiles.FirstOrDefaultAsync();
                    if (contractorProfile != null)
                    {
                        var sampleBid = new Bid
                        {
                            ProjectRequestId = sampleProject.Id,
                            ContractorProfileId = contractorProfile.Id,
                            ProposedPrice = 215000m,
                            DurationDays = 75,
                            MaterialCost = 135000m,
                            LaborCost = 80000m,
                            Notes = "السلام عليكم ورحمة الله، اطلعت على المواصفات ومستعدون للمعاينة الميدانية غداً. السعر يشمل توريد مواد معتمدة كود البناء السعودي مع ضمان سنتين للأعمال الإنشائية والتشطيب وضمان 10 سنوات على السباكة والعوازل.",
                            Status = BidStatus.Submitted,
                            SubmittedAt = DateTime.UtcNow.AddHours(-3)
                        };

                        await context.Bids.AddAsync(sampleBid);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        // 6. Seed sample engineering attachments for the demo project if none exist
        var demoProj = await context.ProjectRequests.Include(p => p.Attachments).FirstOrDefaultAsync();
        if (demoProj != null && !demoProj.Attachments.Any())
        {
            demoProj.Attachments.Add(new ProjectAttachment
            {
                FileName = "مخطط_معماري_فيلا_الملقا_معتمد.pdf",
                FilePath = "https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 2450000
            });
            demoProj.Attachments.Add(new ProjectAttachment
            {
                FileName = "المسقط_الأفقي_والواجهات_التنفيذية.dwg",
                FilePath = "/uploads/projects/sample_facade_blueprint.dwg",
                ContentType = "application/acad",
                FileSizeBytes = 5820000
            });
            demoProj.Attachments.Add(new ProjectAttachment
            {
                FileName = "صورة_الموقع_الحالي_قبل_بدء_التشطيب.jpg",
                FilePath = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=800&q=80",
                ContentType = "image/jpeg",
                FileSizeBytes = 1240000
            });

            await context.SaveChangesAsync();
        }
    }
}

