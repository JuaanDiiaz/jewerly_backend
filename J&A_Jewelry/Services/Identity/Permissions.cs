using System;

namespace J_A_Jewelry.Services.Identity;

/// <summary>
/// Permission codes, one per UI section/area. Roles carry a comma-joined
/// subset; '*' grants everything (shipped only on the Administrator role).
/// The Flutter app mirrors this list in lib/models/permissions.dart —
/// ADD A CODE HERE AND TOGETHER THERE (and a policy is generated
/// automatically; menu gating reads the same code from the app).
/// </summary>
public static class Permissions
{
    public const string Sales = "sales";         // New Sale (POS) + Sales History
    public const string Inventory = "inventory"; // Movements + Physical Count
    public const string Prices = "prices";       // per-list price workspace
    public const string Purchases = "purchases";
    public const string Customers = "customers"; // customers + their payments
    public const string Products = "products";   // products (+ images, category links)
    public const string Reports = "reports";
    /// <summary>Master data + tax rates + suppliers + log viewer.</summary>
    public const string AdminData = "admin";
    /// <summary>Users + roles management itself.</summary>
    public const string UsersAdmin = "users";
    public const string Wildcard = "*";

    public static readonly string[] All =
    {
        Sales, Inventory, Prices, Purchases, Customers,
        Products, Reports, AdminData, UsersAdmin
    };
}