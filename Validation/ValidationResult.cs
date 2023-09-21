using Slant.Entity.Demo.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slant.Entity.Demo.Validation;

public enum ErrorType
{
    Blocking,
    Warning
}

public record ValidationError(ErrorType ErrorType, string Field, string Message);

public record BlockingError(string Field, string Message) : ValidationError(ErrorType.Blocking, Field, Message);

public record ValidationWarning(string Field, string Message) : ValidationError(ErrorType.Warning, Field, Message);

/*
public class ValidationResult
{
    public bool IsValid => Errors.All(e => e.ErrorType != ErrorType.Blocking);
    
    public List<ValidationError> Errors { get; init; } = new();

    public IEnumerable<ValidationError> Warnings => Errors.Where(x => x.ErrorType == ErrorType.Warning);
}
*/

public interface IValidationState<T>
{
    bool IsValid { get; }
        
    List<ValidationError> Errors { get; }
        
    List<ValidationError> Warnings { get; }
}

public class UserValidationService
{
    public IValidationState<User> ValidateUser(User user)
    {
        var validatedUser = new UserValidator().Validate(user);
        return validatedUser;
    }
    
    sealed class NotValidated<T> : IValidationState<T>
    {
        public bool IsValid => false;
        public List<ValidationError> Errors { get; } = new();
        public List<ValidationError> Warnings { get; } = new();
    }
    
    internal class Validated<T> : IValidationState<T>
    {
        public Validated(IEnumerable<ValidationError> result)
        {
            ProcessErrors(result);    
        }
        
        public Validated(IEnumerable<ValidationError>[] results)
        {
            ProcessManyErrors(results);    
        }

        private void ProcessErrors(IEnumerable<ValidationError> result)
        {
            foreach (var error in result)
            {
                switch (error.ErrorType)
                {
                    case ErrorType.Blocking:
                        Errors.Add(error);
                        break;
                    case ErrorType.Warning:
                        Warnings.Add(error);
                        break;
                    default:
                        IsValid = false;
                        break;
                }
            }
            IsValid = Errors.Count == 0;
        }

        private void ProcessManyErrors(params IEnumerable<ValidationError>[] rules)
        {
            foreach (var result in rules)
            {
                ProcessErrors(result);
            }
        }

        public bool IsValid { get; private set; } = true;

        public List<ValidationError> Errors { get; } = new();

        public List<ValidationError> Warnings { get; } = new();
    }

    sealed class UserValidator
    {
        public Validated<User> Validate(User user)
        {
            var validationRules = new[]
            {
                ValidateUserName(user),
                ValidateEmail(user),
                ValidateCreditScore(user)
            };
            return new Validated<User>(validationRules);
        }
        
        private IEnumerable<ValidationError> ValidateUserName(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Name))
            {
                yield return new BlockingError(nameof(User.Name), "Username is required");
            }
            else if (user.Name.Length < 3)
            {
                yield return new ValidationWarning(nameof(User.Name), "Username is short");
            }
        }

        private IEnumerable<ValidationError> ValidateEmail(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains("@"))
            {
                yield return new ValidationError(ErrorType.Blocking, nameof(User.Email), "A valid email is required");
            }
        }

        private IEnumerable<ValidationError> ValidateCreditScore(User user)
        {
            if (user.CreditScore < 0)
            {
                yield return new BlockingError(nameof(user.CreditScore),"CreditScore is invalid");
            }
            else if (user.CreditScore == 0)
            {
                yield return new ValidationWarning(nameof(User.CreditScore), "CreditScore was not calculated");
            }
        }
    }
}