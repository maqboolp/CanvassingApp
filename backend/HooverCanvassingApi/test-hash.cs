using Microsoft.AspNetCore.Identity;

var password = "Admin@123";
var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(null, password);
Console.WriteLine(hash);
