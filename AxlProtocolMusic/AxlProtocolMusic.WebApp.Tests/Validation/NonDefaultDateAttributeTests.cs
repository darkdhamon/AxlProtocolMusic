using System.ComponentModel.DataAnnotations;
using AxlProtocolMusic.WebApp.Validation;

namespace AxlProtocolMusic.WebApp.Tests.Validation;

[TestFixture]
public sealed class NonDefaultDateAttributeTests
{
    [Test]
    public void IsValid_WhenDateIsNonDefault_ReturnsSuccess()
    {
        var attribute = new NonDefaultDateAttribute();
        var context = new ValidationContext(new object());

        var result = attribute.GetValidationResult(new DateTime(2026, 8, 22), context);

        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_WhenDateIsDefault_UsesProvidedErrorMessage()
    {
        var attribute = new NonDefaultDateAttribute { ErrorMessage = "Release date is required." };
        var context = new ValidationContext(new object()) { DisplayName = "Release date" };

        var result = attribute.GetValidationResult(default, context);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ErrorMessage, Is.EqualTo("Release date is required."));
    }

    [Test]
    public void IsValid_WhenDateIsDefault_FallsBackToDisplayNameWhenErrorMessageNotProvided()
    {
        var attribute = new NonDefaultDateAttribute();
        var context = new ValidationContext(new object()) { DisplayName = "Publication date" };

        var result = attribute.GetValidationResult(default, context);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ErrorMessage, Is.EqualTo("Publication date is required."));
    }
}
