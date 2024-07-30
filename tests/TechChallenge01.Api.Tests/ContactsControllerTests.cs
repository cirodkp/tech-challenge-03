﻿using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using TechChallenge01.Api.Tests.Abstractions;
using TechChallenge01.Application.ViewModels;

namespace TechChallenge01.Api.Tests;

public class ContactsControllerTests : BaseFunctionalTests
{
    private readonly Faker _faker;
    private readonly FunctionalTestWebAppFactory _testsFixture;

    private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public ContactsControllerTests(FunctionalTestWebAppFactory testsFixture) : base(testsFixture)
    {
        _testsFixture = testsFixture;
        _faker = new Faker(locale: "pt_BR");
    }

    private string validPhoneNumber => "(11) 99999-9999";
    private string validPhoneNumberToUpdate => "(12) 98888-8888";
    private string invalidEmail => "john.emailinvalido.com";

    [Fact(DisplayName = "Deve inserir um contato com sucesso")]
    [Trait("Functional", "ContactsController")]
    public async Task Should_Return_ContactCreatedWithSuccess()
    {
        //Arrange
        InsertContactRequest insertContactRequest = new(_faker.Name.FullName(), validPhoneNumber, _faker.Internet.Email());

        //Act
        var response = await HttpClient.PostAsJsonAsync($"api/contacts", insertContactRequest);

        //Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        ContactResponse contactResponse = JsonSerializer.Deserialize<ContactResponse>(content, jsonOptions)!;
        contactResponse.Should().NotBeNull();
        contactResponse.Nome.Should().BeEquivalentTo(insertContactRequest.Nome);
        var formatedPhone = Regex.Replace(insertContactRequest.PhoneNumber, "[^0-9]+", "");
        contactResponse.PhoneNumber.Should().BeEquivalentTo(formatedPhone);
        contactResponse.Email.Should().BeEquivalentTo(insertContactRequest.Email);
    }

    [Fact(DisplayName = "Deve retornar uma lista com todos os contatos")]
    [Trait("Functional", "ContactsController")]
    public async Task Should_Return_ListWithAllContacts()
    {
        //Arrange 
        await Should_Return_ContactCreatedWithSuccess();

        //Act
        var response = await HttpClient.GetAsync($"api/contacts");

        //Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var contactResponse = JsonSerializer.Deserialize<List<ContactResponse>>(content, jsonOptions)!;
        contactResponse.Should().NotBeNull();
        contactResponse.Should().HaveCountGreaterThan(0);
    }

    [Fact(DisplayName = "Deve retornar uma lista com filtro de ddd")]
    [Trait("Functional", "ContactsController")]
    public async Task Should_Return_ListWithContactsFiltered()
    {
        //Arrange 
        await Should_Return_ContactCreatedWithSuccess();

        //Act
        var response = await HttpClient.GetAsync($"api/contacts?ddd=15");

        //Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var contactResponse = JsonSerializer.Deserialize<List<ContactResponse>>(content, jsonOptions)!;
        contactResponse.Should().NotBeNull();
        contactResponse.Should().HaveCount(0);
    }

    [Fact(DisplayName = "Deve atualizar um contato com sucesso")]
    [Trait("Functional", "ContactsController")]
    public async Task Should_Return_ContactUpdatedWithSuccess()
    {
        //Arrange
        await Should_Return_ContactCreatedWithSuccess();
        UpdateContactRequest updateContactRequest = new(1, _faker.Name.FullName(), validPhoneNumberToUpdate, _faker.Internet.Email());

        //Act
        var response = await HttpClient.PutAsJsonAsync($"api/contacts", updateContactRequest);

        //Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        ContactResponse contactResponse = JsonSerializer.Deserialize<ContactResponse>(content, jsonOptions)!;
        contactResponse.Should().NotBeNull();
        contactResponse.Nome.Should().BeEquivalentTo(updateContactRequest.Nome);
        var formatedPhone = Regex.Replace(updateContactRequest.PhoneNumber, "[^0-9]+", "");
        contactResponse.PhoneNumber.Should().BeEquivalentTo(formatedPhone);
        contactResponse.Email.Should().BeEquivalentTo(updateContactRequest.Email);
    }

    [Fact(DisplayName = "Deve retornar erro ao tentar atualizar um contato inexistente")]
    [Trait("Functional", "ContactsController")]
    public async Task Should_Return_ErrorToUpdateContactWhenNotExists()
    {
        //Arrange
        await Should_Return_ContactCreatedWithSuccess();
        UpdateContactRequest updateContactRequest = new(999, _faker.Name.FullName(), validPhoneNumberToUpdate, _faker.Internet.Email());

        //Act
        var response = await HttpClient.PutAsJsonAsync($"api/contacts", updateContactRequest);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        BadRequestErrorMessage badRequestError = JsonSerializer.Deserialize<BadRequestErrorMessage>(content, jsonOptions)!;
        badRequestError.Should().NotBeNull();
        badRequestError.message.Should().Be("Não foi possível localizar o cadastro do contato informado.");
    }

    [Fact(DisplayName = "Deve retornar erro ao tentar atualizar um contato com um e-mail inválido")]
    [Trait("Functional", "ContactsController")]
    public async Task Should_Return_ErrorToUpdateContactWhenEmailIsInvalid()
    {
        //Arrange
        await Should_Return_ContactCreatedWithSuccess();
        UpdateContactRequest updateContactRequest = new(1, _faker.Name.FullName(), validPhoneNumberToUpdate, invalidEmail);

        //Act
        var response = await HttpClient.PutAsJsonAsync($"api/contacts", updateContactRequest);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        ValidationProblemDetails validationProblemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(content, jsonOptions)!;
        validationProblemDetails.Should().NotBeNull();
        validationProblemDetails.Errors["Email"].Should().Contain("Este endereço de e-mail não é válido.");
    }

    [Fact(DisplayName = "Deve retornar erro ao tentar atualizar um contato com um e-mail com tamanho inválido")]
    [Trait("Functional", "ContactsController")]
    public async Task Should_Return_ErrorToUpdateContactWhenEmailLengthIsInvalid()
    {
        //Arrange
        await Should_Return_ContactCreatedWithSuccess();
        UpdateContactRequest updateContactRequest = new(1, _faker.Name.FullName(), validPhoneNumberToUpdate, $"{_faker.Lorem.Letter(39)}@dominio.com");

        //Act
        var response = await HttpClient.PutAsJsonAsync($"api/contacts", updateContactRequest);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        ValidationProblemDetails validationProblemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(content, jsonOptions)!;
        validationProblemDetails.Should().NotBeNull();
        validationProblemDetails.Errors["Email"].Should().Contain("Tamanho inválido, máximo de 50 caracteres.");
    }

    [Fact(DisplayName = "Deve retornar erro ao tentar atualizar um contato com um nome com tamanho inválido")]
    [Trait("Functional", "ContactsController")]
    public async Task Should_Return_ErrorToUpdateContactWhenNameLengthIsInvalid()
    {
        //Arrange
        await Should_Return_ContactCreatedWithSuccess();
        UpdateContactRequest updateContactRequest = new(1, _faker.Lorem.Letter(256), validPhoneNumberToUpdate, _faker.Internet.Email());

        //Act
        var response = await HttpClient.PutAsJsonAsync($"api/contacts", updateContactRequest);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        ValidationProblemDetails validationProblemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(content, jsonOptions)!;
        validationProblemDetails.Should().NotBeNull();
        validationProblemDetails.Errors["Nome"].Should().Contain("Tamanho inválido, máximo de 255 caracteres.");
    }

    [Fact(DisplayName = "Deve retornar erro ao tentar atualizar um contato com um número de telefone inválido")]
    [Trait("Functional", "ContactsController")]
    public async Task Should_Return_ErrorToUpdateContactWhenPhoneNumberIsInvalid()
    {
        //Arrange
        await Should_Return_ContactCreatedWithSuccess();
        UpdateContactRequest updateContactRequest = new(1, _faker.Name.FullName(), "(1234) 98888-8888", _faker.Internet.Email());

        //Act
        var response = await HttpClient.PutAsJsonAsync($"api/contacts", updateContactRequest);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        BadRequestErrorMessage badRequestError = JsonSerializer.Deserialize<BadRequestErrorMessage>(content, jsonOptions)!;
        badRequestError.Should().NotBeNull();
        badRequestError.message.Should().BeEquivalentTo("Número de telefone informado incorretamente, Modelo esperado: (dd) 99999-9999.");
    }

    [Fact(DisplayName = "Deve retornar erro ao tentar atualizar um contato com um número de telefone com tamanho menor do que esperado")]
    [Trait("Functional", "ContactsController")]
    public async Task Should_Return_ErrorToUpdateContactWhenPhoneNumberLengthIsInvalid()
    {
        //Arrange
        await Should_Return_ContactCreatedWithSuccess();
        UpdateContactRequest updateContactRequest = new(1, _faker.Name.FullName(), "1137355555", _faker.Internet.Email());

        //Act
        var response = await HttpClient.PutAsJsonAsync($"api/contacts", updateContactRequest);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        ValidationProblemDetails validationProblemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(content, jsonOptions)!;
        validationProblemDetails.Should().NotBeNull();
        validationProblemDetails.Errors["PhoneNumber"].Should().Contain("Tamanho inválido, deve ter entre 11 e 20 caracteres.");
    }

    [Fact(DisplayName = "Deve retornar erro ao tentar atualizar um contato com um número de telefone com tamanho maior do que esperado")]
    [Trait("Functional", "ContactsController")]
    public async Task Should_Return_ErrorToUpdateContactWhenPhoneNumberLengthIsBiggerThanExpected()
    {
        //Arrange
        await Should_Return_ContactCreatedWithSuccess();
        UpdateContactRequest updateContactRequest = new(1, _faker.Name.FullName(), "(11) 3567898888-8888856473", _faker.Internet.Email());

        //Act
        var response = await HttpClient.PutAsJsonAsync($"api/contacts", updateContactRequest);

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        ValidationProblemDetails validationProblemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(content, jsonOptions)!;
        validationProblemDetails.Should().NotBeNull();
        validationProblemDetails.Errors["PhoneNumber"].Should().Contain("Tamanho inválido, deve ter entre 11 e 20 caracteres.");
    }

    public class BadRequestErrorMessage
    {
        public string message { get; set; }
    }

}
