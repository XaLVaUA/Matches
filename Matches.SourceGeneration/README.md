# Matches.SourceGeneration

A C# source generator that automatically creates **discriminated union types** and `Match` methods from enums.

## Installation

Add the package to your project:

```bash
dotnet add package Matches.SourceGeneration
```

The generator will run automatically during build.

## Usage

The library provides two attributes:

- **`[DiscriminatedUnion]`** – marks an enum as a discriminated union.  
- **`[DiscriminatedUnionCase]`** – assigns a type to an enum case.  

When the project is built, interfaces, records, and helper methods are generated automatically.

---

### Example: Simple types

Mark the `ContactKind` enum as a discriminated union and assign types to its cases:

```csharp
[DiscriminatedUnion]
public enum ContactKind
{
    [DiscriminatedUnionCase(typeof(Email))]
    Email,

    [DiscriminatedUnionCase(typeof(Phone))]
    Phone,

    [DiscriminatedUnionCase(typeof(WebHook))]
    WebHook
}
```

Generated on build:

- `IContact` interface  
- `EmailContact`, `PhoneContact`, and `WebHookContact` records  
- `Contact` static class with factory and `Match` methods  

Usage example:

```csharp
const string message = "hmm";

IContact[] contacts =
[
    Contact.GetEmailContact(new Email("aaa@bbb.com")),
    Contact.GetPhoneContact(new Phone(380, 1234567)),
    Contact.GetWebHookContact(new WebHook(new Uri("https://okak.kot")))
];

foreach (var contact in contacts)
{
    var res = SendMessage(contact, message);
    Console.WriteLine(res);
}

static string SendMessage(IContact contact, string text) =>
    Contact.Match
    (
        contact,
        email   => $"'{text}' sent to {email.Address} email address",
        phone   => $"'{text}' sent to +{phone.Code}{phone.Number} phone number",
        webHook => $"'{text}' sent to {webHook.Uri} webhook URI"
    );
```

Output:

```bash
'hmm' sent to aaa@bbb.com email address
'hmm' sent to +3801234567 phone number
'hmm' sent to https://okak.kot/ webhook URI
```

---

### Example: Generic types

Bound generic types are supported the same way as simple ones.

To generate types with generic parameters, specify the **unbound generic type** and provide arguments in order:  
- a concrete type → binds the parameter  
- `null` or `GenericPlaceholder` → keeps it as a generic parameter in the generated type  

```csharp
[DiscriminatedUnion]
public enum WebRequestResultKind
{
    [DiscriminatedUnionCase(typeof(RequestData<,,>), typeof(string), null, typeof(GenericPlaceholder))]
    Success,

    [DiscriminatedUnionCase(typeof(List<>), (Type?)null)]
    Warnings,

    [DiscriminatedUnionCase(typeof(List<string>))]
    Errors
}

public record RequestData<TKey, TData, TItem>(TData Data) 
    where TData : IDictionary<TKey, TItem> 
    where TItem : struct;

public record WarningInfo(int Code, string Message);
```

---

### More examples

Check out the [Matches.Tests](https://github.com/XaLVaUA/Matches/tree/main/Matches.Tests) project for additional usage patterns.

---

## License

This project is dedicated to the public domain under the **Unlicense**.  
See the [LICENSE](LICENSE) file for details.
