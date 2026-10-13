using Leyi.Application.Features.Onboarding.CompleteOnboarding;
using Leyi.Application.Features.Onboarding.UpdateLearningGoal;
using Microsoft.Extensions.DependencyInjection;

namespace Leyi.Infrastructure.LearningProfile;

internal static class LearningProfileDependencyInjection
{
    /// <summary>Adapters of the learning profile features (CompleteOnboarding, UpdateLearningGoal).</summary>
    public static IServiceCollection AddLearningProfile(this IServiceCollection services)
    {
        services.AddScoped<IOnboardingContentResolver, OnboardingContentResolver>();
        services.AddScoped<IOnboardingStore, OnboardingStore>();
        services.AddScoped<ILearningGoalStore, LearningGoalStore>();
        return services;
    }
}
