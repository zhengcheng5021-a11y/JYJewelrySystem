using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Infrastructure
{
    /// <summary>
    /// 跨域扩展
    /// </summary>
    public static class CorsExtension
    {
        /// <summary>
        /// 跨域配置
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void AddCors(this IServiceCollection services, IConfiguration configuration)
        {
            var corsUrls = configuration.GetSection("corsUrls").Get<string[]>();

            //配置跨域
            services.AddCors(c =>
            {
                c.AddPolicy("Policy", policy =>
                {
                    var allowed = new HashSet<string>(corsUrls ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                    policy.SetIsOriginAllowed(origin => IsAllowedOrigin(origin, allowed))
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .AllowAnyMethod();//允许任意方法
                });
            });
        }

        private static bool IsAllowedOrigin(string origin, HashSet<string> allowed)
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            if (allowed.Contains(origin)) return true;
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || uri.Host == "127.0.0.1") return true;
            return uri.Host.Equals("servicewechat.com", StringComparison.OrdinalIgnoreCase);
        }
    }
}
