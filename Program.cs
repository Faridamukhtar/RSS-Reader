using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Data.Sqlite;
using Dapper;
using System.ServiceModel.Syndication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using System.Xml;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

var _connectionString = "Data Source=RSS-db.db";

var connection = new SqliteConnection(_connectionString);
connection.Open();


app.MapGet("/", (HttpContext context) =>
{
    return Results.File("index.html", "text/html");
});

app.MapGet("/home", (HttpContext context) =>
{
    string html =
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


app.MapGet("/signup", (HttpContext context) =>
{
    string html =
        $"""
        <div class="text-center">
        SignUp content
        </div>
        """;

    return Results.Content(html, "text/html");

});

app.MapGet("/login", (HttpContext context) =>
{
    string html = $"""
    <div class="text-center">
    Login content
    </div>
    """;

    return Results.Content(html, "text/html");

});


//app.MapPost("/signup", async (Testimonial testimonial) =>
//{
//    try
//    {
//        var sql = @"INSERT INTO user (username, password) 
//                    VALUES (@Name, @DestinationName, @Rating, @Review)";
//        await connection.ExecuteAsync(sql, testimonial);
//        return Results.Ok();
//    }
//    catch (Exception ex)
//    {
//        return Results.Problem(ex.Message);
//    }
//});


app.Run();