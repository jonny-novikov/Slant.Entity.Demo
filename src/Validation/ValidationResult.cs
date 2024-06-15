using Slant.Entity.Demo.DomainModel;
using System.Collections.Generic;

namespace Slant.Entity.Demo.Validation;

/// <summary>
/// Specifies the severity of a rule.
/// </summary>
public enum Severity 
{
    /// <summary>
    /// Ошибка
    /// </summary>
    Error,
    /// <summary>
    /// Предупреждение
    /// </summary>
    Warning,
    /// <summary>
    /// Информация
    /// </summary>
    Info
}

public abstract record ValidationState(Severity Severity, string Field, string Message);

public record ValidationError(string Field, string Message) : ValidationState(Severity.Error, Field, Message);

public record ValidationWarning(string Field, string Message) : ValidationState(Severity.Warning, Field, Message);

public interface IValidationState<T>
{
    bool IsValid { get; }
        
    List<ValidationState> Errors { get; }

    List<ValidationState> Warnings { get; }
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
        public List<ValidationState> Errors { get; } = new();
        public List<ValidationState> Warnings { get; } = new();
    }
    
    internal class Validated<T> : IValidationState<T>
    {
        public Validated(IEnumerable<ValidationState> result)
        {
            ProcessResult(result);    
        }
        
        public Validated(params IEnumerable<ValidationState>[] results)
        {
            ProcessManyResults(results);
        }

        private void ProcessResult(IEnumerable<ValidationState> result)
        {
            foreach (var error in result)
            {
                switch (error.Severity)
                {
                    case Severity.Error:
                        Errors.Add(error);
                        break;
                    case Severity.Warning:
                        Warnings.Add(error);
                        break;
                    default:
                        IsValid = false;
                        break;
                }
            }
            IsValid = Errors.Count == 0;
        }

        private void ProcessManyResults(params IEnumerable<ValidationState>[] rules)
        {
            foreach (var result in rules)
            {
                ProcessResult(result);
            }
        }

        public bool IsValid { get; private set; } = true;

        public List<ValidationState> Errors { get; } = new();

        public List<ValidationState> Warnings { get; } = new();
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
            // return new Validated<User>(ValidateUserName(user), ValidateEmail(user));
            return new Validated<User>(validationRules);
        }
        
        private IEnumerable<ValidationState> ValidateUserName(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Name))
            {
                yield return new ValidationError(nameof(User.Name), "Username is required");
            }
            else if (user.Name.Length < 3)
            {
                yield return new ValidationWarning(nameof(User.Name), "Username is short");
            }
        }

        private IEnumerable<ValidationState> ValidateEmail(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains("@"))
            {
                yield return new ValidationError(nameof(User.Email), "A valid email is required");
            }
        }

        private IEnumerable<ValidationState> ValidateCreditScore(User user)
        {
            if (user.CreditScore < 0)
            {
                yield return new ValidationError(nameof(user.CreditScore),"CreditScore is invalid");
            }
            else if (user.CreditScore == 0)
            {
                yield return new ValidationWarning(nameof(User.CreditScore), "CreditScore was not calculated");
            }
        }
    }
}