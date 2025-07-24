#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.AspNetCore.Identity, 2.2.0"

using Microsoft.AspNetCore.Identity;

var password = "Admin@123";
var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(null, password);

Console.WriteLine($"Password hash for '{password}': {hash}");