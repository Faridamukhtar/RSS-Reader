using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Data.Sqlite;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml.Linq;
using BCrypt.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntiforgery();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "TurboCookie";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Adjust expiration as needed
    options.SlidingExpiration = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
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

// Load home page
app.MapGet("/", (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated ?? false)
    {
        return Results.File("feed.html", "text/html");
    }

    return Results.File("index.html", "text/html");
});

// Load landing page
app.MapGet("/home", (HttpContext context) =>
{
    var html =
        $@"
        <div class='text-center mt-5 pt-5'>
            <em style='font-size:6rem;' class='em-black'>Turbo</em><em style='font-size:6rem;' class='em-purple'>Feeds</em>
        </div>

        <div class='text-center'>
            Revolutionizing Your Browsing Experience
        </div>
        ";

    return Results.Content(html, "text/html");
});

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
    context.Response.Headers.Add("Pragma", "no-cache");
    context.Response.Headers.Add("Expires", "0");
    await next();
});

// Redirect authenticated users
app.Use(async (context, next) =>
{
    var path = context.Request.Path;

    if (path.StartsWithSegments("/signup") || path.StartsWithSegments("/login"))
    {
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            context.Response.Headers.Add("HX-Trigger", "replaceNavLinks");
            context.Response.Redirect("/feed");
            return;
        }
    }

    await next();
});

// Get Sign-Up Form
app.MapGet("/signup", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);

    var html =
        $@"
        <div class='container'>
            <h1 class='font-weight-bold'> Sign Up</h1>
            <br/>
            <form hx-post='/signup' hx-target='#error-signup' method='post'>
                <input name='{token.FormFieldName}' type='hidden' value='{token.RequestToken}' />
                <input type='text' name='email' class='form-control m-0' placeholder='Enter Your Email' required/> <br/><br/>
                <input type='password' name='password' class='form-control m-0' placeholder='Create a Password' required/> <br/><br/>
                <button type='submit' class='btn btn-primary m-0 mb-5'>Sign Up</button>
            </form>
            <div id='error-signup'></div>
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
        <div class='container'>
            <h1 class='font-weight-bold'>Login</h1>
            <br/>
            <form hx-post='/login' hx-target='#error-login' method='post'>
                <input name='{token.FormFieldName}' type='hidden' value='{token.RequestToken}' />
                <input type='email' name='email' class='form-control m-0' placeholder='Enter Your Email' required/> <br/><br/>
                <input type='password' name='password' class='form-control m-0' placeholder='Enter Your Password' required/> <br/><br/>
                <button type='submit' class='btn btn-primary m-0 mb-5'>Login</button>
            </form>
            <div id='error-login'></div>
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
    IAntiforgery antiforgery,
    ILogger<Program> logger) =>
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
                $@"
                <div class='alert-danger alert mb-3'>
                    User already exists
                </div>
                ";
        }
        else
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var sqlInsert = @"INSERT INTO user (email, password) VALUES (@Email, @Password)";
            var result = await connection.ExecuteAsync(sqlInsert, new { Email = email, Password = hashedPassword });

            if (result > 0)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    });

                html =
                $@"
                <div id='entire-page' hx-get='/feed' hx-swap='outerHTML' hx-target='#entire-page' hx-trigger='load'>
                    Loading feed...
                </div>
                ";
            }
            else
            {
                html =
                    $@"
                    <div class='alert-danger alert mb-3'>
                        Error during signup: Unable to insert user into database
                    </div>
                    ";
            }
        }
    }
    catch (Exception e)
    {
        logger.LogError(e, "Error during signup");
        html =
            $@"
            <div class='alert-danger alert mb-3'>
                Error during signup: {e.Message}
            </div>
            ";
    }

    return Results.Content(html, "text/html");
});


// Process login request
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
        var sql = @"SELECT email, password FROM user WHERE email = @Email";
        var user = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Email = email });

        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.password))
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email)
                };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                });


            html =
                $@"
                <div id='entire-page' hx-get='/feed' hx-swap='outerHTML' hx-target='#entire-page' hx-trigger='load'>
                    Loading feed...
                </div>
                ";
        }
        else
        {
            html =
                $@"
                <div class='alert-danger alert mb-3'>
                    Invalid credentials
                </div>
                ";
        }

    }
    catch (Exception e)
    {
        html =
            $@"
            <div class='alert-danger alert mb-3'>
                Error during login
            </div>
            ";
    }

    return Results.Content(html, "text/html");
});


//Feed Page (Logged in user)
app.MapGet("/feed", (HttpContext context) =>
{
    return Results.File("feed.html", "text/html");
}).RequireAuthorization();

// Load subscribed feeds' list
app.MapGet("/feeds", async (HttpContext context) =>
{
    var user = context.User;
    var userEmail = user.Identity?.Name;

    var sqlSubscriptions = @"SELECT subscription FROM subscriptions WHERE email = @Email";
    var subscribedFeeds = await connection.QueryAsync<string>(sqlSubscriptions, new { Email = userEmail });

    var html = "<ul class='list-group'>";
    foreach (var feedUrl in subscribedFeeds)
    {
        html += $"<li class='list-group-item'><a href='#' hx-get='/rss?url={feedUrl}' hx-target='#feed-content'>{feedUrl}</a></li>";
    }
    html += "</ul>";

    return Results.Content(html, "text/html");
}).RequireAuthorization();

// Load selected rss feed
app.MapGet("/rss", async (HttpContext context, [FromQuery] string url) =>
{
    using var httpClient = new HttpClient();
    var rssResponse = await httpClient.GetStringAsync(url);

    var rssXml = XDocument.Parse(rssResponse);
    var items = rssXml.Descendants("item").Select(item => new
    {
        Title = item.Element("title")?.Value,
        Link = item.Element("link")?.Value,
        Description = item.Element("description")?.Value,
        PubDate = item.Element("pubDate")?.Value
    });

    var html = "<div class='container mt-4'>";

    foreach (var item in items)
    {
        html += $@"
        <div class='card mb-4'>
            <div class='card-body'>
                <h5 class='card-title'><a href='{item.Link}' class='text-decoration-none em-purple'>{item.Title}</a></h5>
                <p class='card-text'>{item.Description}</p>
                <p class='card-text'><small class='text-muted'>{item.PubDate}</small></p>
            </div>
        </div>";
    }

    html += "</div>";

    return Results.Content(html, "text/html");
}).RequireAuthorization();



// Add feed endpoint
app.MapPost("/add-feed", async (HttpContext context) =>
{
    var user = context.User;
    var userEmail = user.Identity?.Name;
    var feedUrl = context.Request.Form["feedUrl"].ToString();

    var sqlInsert = @"INSERT INTO subscriptions (email, subscription) VALUES (@Email, @Subscription)";
    await connection.ExecuteAsync(sqlInsert, new { Email = userEmail, Subscription = feedUrl });

    context.Response.Redirect("/feed");
}).RequireAuthorization();

// Endpoint to get the list of feeds for the remove dropdown
app.MapGet("/feeds-to-remove", async (HttpContext context) =>
{
    var userEmail = context.User.Identity?.Name;

    var sqlSubscriptions = @"SELECT subscription FROM subscriptions WHERE email = @Email";
    var subscribedFeeds = await connection.QueryAsync<string>(sqlSubscriptions, new { Email = userEmail });
    var html = $@"
        <select class='form-select' id='feedUrlToRemove' name='feedUrlToRemove'>
        <option value=''>Select feed to remove</option>";

    foreach (var feedUrl in subscribedFeeds)
    {
        html += $"<option value='{feedUrl}'>{feedUrl}</option>";
    }

    html += "</select>";

    return Results.Content(html, "text/html");
}).RequireAuthorization();

// Remove feed endpoint
app.MapPost("/remove-feed", async (HttpContext context) =>
{
    var user = context.User;
    var userEmail = user.Identity?.Name;
    var feedToRemove = context.Request.Form["feedUrlToRemove"].ToString();

    var sqlDelete = @"DELETE FROM subscriptions WHERE email = @Email AND subscription = @Subscription";
    await connection.ExecuteAsync(sqlDelete, new { Email = userEmail, Subscription = feedToRemove });

    context.Response.Redirect("/feed");
}).RequireAuthorization();



// Logout endpoint
app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Cookies.Delete("TurboCookie");
    return Results.Redirect("/");
}).RequireAuthorization();

app.Run();
