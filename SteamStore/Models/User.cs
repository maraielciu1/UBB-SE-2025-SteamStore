﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class User { 
    public int UserId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public float WalletBalance { get; set; }
    public float PointsBalance { get; set; }
    public enum Role { Developer, User }

    public Role UserRole { get; set; }

    public User(int userId, string name, string email, float walletBalance, float pointsBalance, Role userRole)
    {
        UserId = userId;
        Name = name;
        Email = email;
        WalletBalance = walletBalance;
        PointsBalance = pointsBalance;
        UserRole = userRole;
    }
}

