using UpdateCenter.Api.Data;
using UpdateCenter.Api.Domain;

namespace UpdateCenter.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(UpdateDbContext db)
    {
        if (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Applications))
        {
            return;
        }

        db.Applications.AddRange(
            new AppApplication
            {
                Id = "APP-137-WIN",
                Name = "نرم‌افزار اپراتور ۱۳۷",
                Platform = AppPlatform.Windows,
                Description = "میز کار اپراتور سامانه ۱۳۷ شهرداری سبزوار",
                Active = true
            },
            new AppApplication
            {
                Id = "APP-SABZEVAR-ANDROID",
                Name = "سبزوار من – Android",
                Platform = AppPlatform.Android,
                Active = true
            },
            new AppApplication
            {
                Id = "APP-SABZEVAR-PWA",
                Name = "سبزوار من – PWA",
                Platform = AppPlatform.Pwa,
                Active = true
            },
            new AppApplication
            {
                Id = "APP-CAR-ANDROID",
                Name = "نرم‌افزار خودرو – Android",
                Platform = AppPlatform.Android,
                Active = true
            },
            new AppApplication
            {
                Id = "SERVICE-137-WIN",
                Name = "سرویس ۱۳۷ – Windows",
                Platform = AppPlatform.Service,
                Description = "سرویس ویندوزی سامانه ۱۳۷",
                Active = true
            }
        );

        await db.SaveChangesAsync();
    }
}
