using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuizMasterApplication.Services.Implementations;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.Identity;
using QuizMasterDomain.Interfaces;
using QuizMasterInfrastructure;
using QuizMasterInfrastructure.Data;
using QuizMasterInfrastructure.Identity;
using QuizMasterInfrastructure.Repositories;
using System.Security.Claims;
using System.Text;
using Microsoft.OpenApi;
using QuizMasterWeb.Middlewares;
namespace QuizMasterWeb
{
    public class Program
    {
        public  static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddEndpointsApiExplorer();

            //swagger
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Paste your JWT token here."
                });

                options.AddSecurityRequirement(document =>
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    });
            });

            ///// add context
            builder.Services.AddDbContext<QuizMasterDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            builder.Services.AddScoped<IQuizRepository, QuizRepository>();
            builder.Services.AddScoped<IQuizResultRepository, QuizResultRepository>();
            builder.Services.AddScoped<IQuizAttemptRepository, QuizAttemptRepository>();
            builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
            builder.Services.AddScoped<IAnswerOptionRepository, AnswerOptionRepository>();
            builder.Services.AddScoped<IStudentAnswerRepository, StudentAnswerRepository>();

            builder.Services.AddScoped<IAnswerOptionService, AnswerOptionService>();
            builder.Services.AddScoped<IQuestionService, QuestionService>();
            builder.Services.AddScoped<IQuizAttemptService, QuizAttemptService>();
            builder.Services.AddScoped<IQuizService, QuizService>();
            builder.Services.AddScoped<IQuizResultService, QuizResultService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            ///
            // identity:
            builder.Services.AddDataProtection();
            builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<QuizMasterDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.Configure<IdentityOptions>(options => //for email confirmation token
            {
                options.Tokens.EmailConfirmationTokenProvider =
                    TokenOptions.DefaultEmailProvider;
            });

            // jwt 
            var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured.");
            var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is not configured.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),

                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer, 
                        ValidateAudience = true,
                        ValidAudience = jwtAudience, 
                        ValidateLifetime = true,
                        RoleClaimType = ClaimTypes.Role
                    };
                });
            builder.Services.AddAuthorization();

            var app = builder.Build();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            await IdentitySeeder.SeedRolesAsync(app.Services);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
