﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Data.Sqlite;
using Dapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntiforgery();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

var _connectionString = "Data Source=RSS.db";
var connection = new SqliteConnection(_connectionString);
connection.Open();

app.MapGet("/", (HttpContext context) =>
{
    return Results.File("index.html", "text/html");
});

app.MapGet("/home", (HttpContext context) =>
{
    var html =
        $"""
        <div class="text-center mt-5 pt-5">
            <em style="font-size:6rem;" class="em-black">Turbo</em><em style="font-size:6rem;" class="em-purple">Feeds</em>
        </div>

        <div class="text-center">
            Revolutionizing Your Browsing Experience
        </div>
        """;

    return Results.Content(html, "text/html");
});

// Get Sign-Up Form
app.MapGet("/signup", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);

    var html =
        $@"
        <div class=""container"">
            <h1 class=""font-weight-bold""> Sign Up</h1>
            <br/>
            <form hx-post=""/signup"" hx-target=""#error-signup"" method=""post"">
                <input name=""{token.FormFieldName}"" type=""hidden"" value=""{token.RequestToken}"" />
                <input type=""text"" name=""email"" class=""form-control m-0"" placeholder=""Enter Your Email"" required/> <br/><br/>
                <input type=""password"" name=""password"" class=""form-control m-0"" placeholder=""Create a Password"" required/> <br/><br/>
                <button type=""submit"" class=""btn btn-primary m-0 mb-5"">Sign Up</button>
            </form>
            <div id=""error-signup""></div>
        </div>
        ";

    return Results.Content(html, "text/html");
});

// Get Login Form
app.MapGet("/login", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);

    var html =
        $@"
        <div class=""container"">
            <h1 class=""font-weight-bold"">Login</h1>
            <br/>
            <form hx-post=""/login"" hx-target=""#error-login"" method=""post"">
                <input name=""{token.FormFieldName}"" type=""hidden"" value=""{token.RequestToken}"" />
                <input type=""email"" name=""email"" class=""form-control m-0"" placeholder=""Enter Your Email"" required/> <br/><br/>
                <input type=""password"" name=""password"" class=""form-control m-0"" placeholder=""Enter Your Password"" required/> <br/><br/>
                <button type=""submit"" class=""btn btn-primary m-0 mb-5"">Login</button>
            </form>
            <div id=""error-login""></div>
        </div>
        ";

    return Results.Content(html, "text/html");
});

// Process Sign-Up Request
app.MapPost("/signup", async (
    HttpRequest request,
    HttpContext context,
    [FromForm] string email,
    [FromForm] string password,
    IAntiforgery antiforgery) =>
{
    var html = "";

    try
    {
        await antiforgery.ValidateRequestAsync(context);
        var sqlCheck = @"SELECT COUNT(*) FROM user WHERE email = @Email";
        var userCount = await connection.ExecuteScalarAsync<int>(sqlCheck, new { Email = email });

        if (userCount > 0)
        {
            html =
                $"""
                <div class="alert-danger alert mb-3">
                User already exists
                </div>
                """;
        }
        else
        {
            var sqlInsert = @"INSERT INTO user (email, password) VALUES (@Email, @Password)";
            var result = await connection.ExecuteAsync(sqlInsert, new { Email = email, Password = password });
            if (result > 0)
            {
                //TODO: replace with redirection
                html = "sign up successful";
            }
            else
            {
                html =
                    $"""
                    <div class="alert-danger alert mb-3">
                        Error during signup
                    </div>
                    """;
            }
        }

    }
    catch(Exception e)
    {
        html =
            $"""
            <div class="alert-danger alert mb-3">
                Error during signup
            </div>
            """;

    }

    return Results.Content(html, "text/html");


});

// Process Login Request
app.MapPost("/login", async (
    HttpRequest request,
    HttpContext context,
    [FromForm] string email,
    [FromForm] string password,
    IAntiforgery antiforgery) =>
{
    var html = "";

    try
    {
        await antiforgery.ValidateRequestAsync(context);
        var sql = @"SELECT email FROM user WHERE email = @Email AND password = @Password";
        var user = await connection.QueryFirstOrDefaultAsync<string>(sql, new { Email = email, Password = password });

        if (user != null)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, email) };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            html="Login Successful!";//TODO: replace with redirection
        }
        else
        {
            html =
                $"""
                <div class="alert-danger alert mb-3">
                    Invalid credentials
                </div>
                """;
        }

    }
    catch (Exception e)
    {
        html =
            $"""
            <div class="alert-danger alert mb-3">
                Error during login
            </div>
            """;

    }

    return Results.Content(html, "text/html");

});


app.Run();
