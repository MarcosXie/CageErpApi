using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using FlyGates.Application.Entities.Storage;
using FlyGates.Infraestructure.Configuration;
using FlyGates.Infraestructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlyGates.Infraestructure.Extensions;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AwsS3Options>(configuration.GetSection(AwsS3Options.SectionName));
        services.Configure<MediaRetentionOptions>(configuration.GetSection(MediaRetentionOptions.SectionName));

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AwsS3Options>>().Value;
            var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
            var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region) };
            return new AmazonS3Client(credentials, config);
        });

        services.AddSingleton<IMediaStorageService, S3MediaStorageService>();
        services.AddHostedService<S3LifecycleSyncHostedService>();
    }
}