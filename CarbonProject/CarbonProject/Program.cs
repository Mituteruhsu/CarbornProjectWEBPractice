using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// === Ū�������ܼ� ===
var AZURE_SQL_USER = Environment.GetEnvironmentVariable("AZURE_SQL_USER");
var AZURE_SQL_PWD = Environment.GetEnvironmentVariable("AZURE_SQL_PWD");

var rawConnStr = builder.Configuration.GetConnectionString("DefaultConnection")
    .Replace("{SQL_USER}", AZURE_SQL_USER)
    .Replace("{SQL_PWD}", AZURE_SQL_PWD);

// ��s�u�r��s�^�]�w
builder.Configuration["ConnectionStrings:DefaultConnection"] = rawConnStr;

// === MVC �򥻳]�w === Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
// �j�� session �P cookie �]�w
builder.Services.AddDistributedMemoryCache();   // �s��b�s�Ŷ�
// �Ы� session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // ���ݨD�վ�
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // production: Always
});

// === ��l�Ƹ�Ʈw ===
// builder.Configuration ���w�g�O�����᪺�s�u�r��
CarbonProject.Models.Members.Init(builder.Configuration);           // ��l�� Members �s�u�r��
CarbonProject.Models.ActionsRepository.Init(builder.Configuration); // ��l�� ActionsRepository �s�u�r��

// === �ҥ� app ===
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();   //�ϥ� session �����b UseRouting ��BUseEndpoints �e
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
