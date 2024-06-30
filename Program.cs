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
using static System.Net.Mime.MediaTypeNames;

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


app.MapGet("/signup", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);

    var html =
        $@"
        <div class=""container"">
            <h1 class=""font-weight-bold""> Sign Up</h1>
            <br/>
            <form hx-get=""/signup"">
                <input name=""{token.FormFieldName}"" type=""hidden"" value=""{token.RequestToken}"" />
                <input type=""text"" name=""email"" class=""form-control m-0"" placeholder=""Enter Your Email"" required/> <br/><br/>
                <input type=""text"" name=""password"" class=""form-control m-0"" placeholder=""Create a Password"" required/> <br/><br/>
                <button type=""submit"" class=""btn btn-primary m-0"">Sign Up</button>
            </form>
        </div>
        ";

    return Results.Content(html, "text/html");
});

app.MapGet("/login", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);

    var html =
        $@"
        <div class=""container"">
            <h1 class=""font-weight-bold"">Login</h1>
            <br/>
            <form hx-get=""/login"">
                <input name=""{token.FormFieldName}"" type=""hidden"" value=""{token.RequestToken}"" />
                <input type=""text"" name=""email"" class=""form-control m-0"" placeholder=""Enter Your Email"" required/> <br/><br/>
                <input type=""text"" name=""password"" class=""form-control m-0"" placeholder=""Enter Your Password"" required/> <br/><br/>
                <button type=""submit"" class=""btn btn-primary m-0"">Login</button>
            </form>
        </div>
        ";

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