﻿namespace Identity.API.Models
{
    public class User
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string PhoneNumber { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }

        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpireDate { get; set; }
    }
}
