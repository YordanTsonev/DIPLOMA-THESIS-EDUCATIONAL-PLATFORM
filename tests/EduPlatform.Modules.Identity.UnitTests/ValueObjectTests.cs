using EduPlatform.BuildingBlocks.Domain;
using EduPlatform.Modules.Identity.Domain;
using Shouldly;

namespace EduPlatform.Modules.Identity.UnitTests;

public sealed class EmailTests
{
    [Theory]
    [InlineData("Ivan@Example.COM", "ivan@example.com")]
    [InlineData("  ivan@example.com  ", "ivan@example.com")]
    public void Addresses_are_normalised_so_one_person_cannot_register_twice(string input, string expected)
    {
        Email.Create(input).Value.ShouldBe(expected);
    }

    [Fact]
    public void Two_addresses_differing_only_in_case_are_equal()
    {
        Email.Create("IVAN@example.com").ShouldBe(Email.Create("ivan@example.com"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("ivan@")]
    [InlineData("two@at@example.com")]
    public void Structurally_impossible_addresses_are_rejected(string? input)
    {
        Should.Throw<DomainException>(() => Email.Create(input));
    }

    [Fact]
    public void An_over_long_address_is_rejected()
    {
        var tooLong = new string('a', Email.MaxLength) + "@example.com";

        Should.Throw<DomainException>(() => Email.Create(tooLong));
    }
}

public sealed class PersonNameTests
{
    [Fact]
    public void A_patronymic_is_optional()
    {
        PersonName.Create("Иван", null, "Георгиев").Full.ShouldBe("Иван Георгиев");
    }

    [Fact]
    public void All_three_parts_appear_in_the_display_form()
    {
        PersonName.Create("Иван", "Петров", "Георгиев").Full.ShouldBe("Иван Петров Георгиев");
    }

    [Fact]
    public void A_blank_patronymic_is_stored_as_absent_rather_than_as_whitespace()
    {
        PersonName.Create("Иван", "   ", "Георгиев").Middle.ShouldBeNull();
    }

    [Theory]
    [InlineData(null, "Георгиев")]
    [InlineData("", "Георгиев")]
    [InlineData("Иван", null)]
    [InlineData("Иван", "  ")]
    public void The_first_and_last_name_are_required(string? first, string? last)
    {
        Should.Throw<DomainException>(() => PersonName.Create(first, null, last));
    }

    [Fact]
    public void Names_are_compared_by_value()
    {
        PersonName.Create("Иван", "Петров", "Георгиев")
            .ShouldBe(PersonName.Create("Иван", "Петров", "Георгиев"));
    }
}
