using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace OmahaPokerServer
{
    public class UserValidationRules : AbstractValidator<UserRegistModel>
    {
        public UserValidationRules()
        {
            RuleFor(user => user.Nickname)
                .NotEmpty()
                .Matches("^[a-zA-Z]+$")
                .WithMessage("Nickname must contain only letters and no spaces.");
            RuleFor(user => user.Password)
                .NotEmpty()
                .Matches(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")
                .WithMessage("Password must be between 8 and 20 characters, at least one digit, special symbol, and upper case letter.");
        }
    }
}
