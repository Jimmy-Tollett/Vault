using System;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Spectre.Console;
using System.Linq;
namespace Vault
{
    class Program
    {
        static void Main(string[] args)
        {
            // Clear the terminal before rendering the UI
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("VAULT").Color(Color.Green));

            while (true)
            {
                var action = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select an action:")
                        .AddChoices(new[] { "Encrypt", "Decrypt", "Exit" }));

                if (action == "Exit")
                {
                    AnsiConsole.Write(new FigletText("Goodbye!").Color(Color.Red));
                    break;
                }

                if (action == "Encrypt")
                {
                    // Prompt for output file
                    var outputFile = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter the [green]encrypted output file name[/]:")
                            .Validate(n => string.IsNullOrWhiteSpace(n)
                                ? ValidationResult.Error("[red]Cannot be empty[/]")
                                : ValidationResult.Success()));

                    // Source selection
                    var sourceChoice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("Source input:")
                            .AddChoices("Text (console)", "File"));
                    
                    string? inputFile = null;
                    if (sourceChoice == "File")
                    {
                        inputFile = AnsiConsole.Prompt(
                            new TextPrompt<string>("Enter the [green]path of the plaintext file[/]:")
                                .Validate(n => !File.Exists(n)
                                    ? ValidationResult.Error("[red]File does not exist[/]")
                                    : ValidationResult.Success()));
                    }

                    EncryptFile(outputFile, inputFile);
                }
                else // Decrypt
                {
                    var inputFile = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter the [green]encrypted file name[/]:")
                            .Validate(n => !File.Exists(n)
                                ? ValidationResult.Error("[red]File does not exist[/]")
                                : ValidationResult.Success()));

                    bool saveToFile = AnsiConsole.Confirm("Save decrypted output to file?");
                    string? outputFile = null;
                    if (saveToFile)
                    {
                        outputFile = AnsiConsole.Prompt(
                            new TextPrompt<string>("Enter the [green]output file name[/]:")
                                .Validate(n => string.IsNullOrWhiteSpace(n)
                                    ? ValidationResult.Error("[red]Cannot be empty[/]")
                                    : ValidationResult.Success()));
                    }

                    DecryptFile(inputFile, outputFile);
                }
            }
        }
        static void EncryptFile(string outputFileName, string? inputFileName = null){
            try{
                using (var outFs = new FileStream(outputFileName, FileMode.Create))
                using (var inStream = inputFileName != null
                    ? new FileStream(inputFileName, FileMode.Open)
                    : Console.OpenStandardInput())
                {
                    // For console input: read everything into memory first so we can encrypt after the user is done typing
                    byte[]? plainBytes = null;
                    if (inputFileName == null)
                    {
                        var memStream = new MemoryStream();
                        inStream.CopyTo(memStream);
                        plainBytes = memStream.ToArray();
                    }

                    using (Aes aes = Aes.Create())
                    {
                        // Prompt for password securely
                        var password = AnsiConsole.Prompt(
                            new TextPrompt<string>("Enter your [green]password[/]:")
                                .PromptStyle("red")
                                .Secret());

                        // Generate a random salt
                        byte[] salt = new byte[16];
                        using (var rng = RandomNumberGenerator.Create())
                        {
                            rng.GetBytes(salt);
                        }

                        // Derive a 256-bit key using PBKDF2
                        var kdf = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
                        aes.Key = kdf.GetBytes(32);

                        // Generate IV and write salt + IV
                        aes.GenerateIV();
                        outFs.Write(salt, 0, salt.Length);
                        outFs.Write(aes.IV, 0, aes.IV.Length);

                        if (inputFileName == null)
                        {
                            // Encrypt the collected console input
                            AnsiConsole.Status()
                                .Spinner(Spinner.Known.Dots)
                                .Start("Encrypting…", ctx =>
                                {
                                    using var cryptoStream = new CryptoStream(outFs, aes.CreateEncryptor(), CryptoStreamMode.Write);
                                    cryptoStream.Write(plainBytes!, 0, plainBytes!.Length);
                                });
                        }
                        else
                        {
                            // Stream-encrypt from the input file
                            AnsiConsole.Status()
                                .Spinner(Spinner.Known.Dots)
                                .Start("Encrypting…", ctx =>
                                {
                                    using var cryptoStream = new CryptoStream(outFs, aes.CreateEncryptor(), CryptoStreamMode.Write);
                                    byte[] buffer = new byte[1024];
                                    int bytesRead;
                                    while ((bytesRead = inStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        cryptoStream.Write(buffer, 0, bytesRead);
                                    }
                                });
                        }

                        AnsiConsole.MarkupLine("[bold green]Encryption complete![/]");
                        var table = new Table().AddColumn("Field").AddColumn("Value")
                            .AddRow("Salt", Convert.ToBase64String(salt))
                            .AddRow("IV",   Convert.ToBase64String(aes.IV))
                            .AddRow("Output File", outputFileName);
                        AnsiConsole.Write(table);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler(ex);
            }
        }
        static void DecryptFile(string inputFileName, string? outputFileName = null)
        {
            try
            {
                using (var inFs = new FileStream(inputFileName, FileMode.Open))
                {
                    using (Aes aes = Aes.Create())
                    {
                        // Read salt and IV from file
                        byte[] salt = new byte[16];
                        inFs.ReadExactly(salt);
                        byte[] iv = new byte[16];
                        inFs.ReadExactly(iv);
                        aes.IV = iv;

                        // Prompt for password securely
                        var password = AnsiConsole.Prompt(
                            new TextPrompt<string>("Enter your [green]password[/]:")
                                .PromptStyle("red")
                                .Secret());

                        // Derive key with PBKDF2 using the stored salt
                        var kdf = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
                        aes.Key = kdf.GetBytes(32);

                        string decryptedText = string.Empty;

                        AnsiConsole.Status()
                            .Spinner(Spinner.Known.Dots)
                            .Start("Decrypting…", ctx =>
                            {
                                using (CryptoStream cryptoStream = new(inFs, aes.CreateDecryptor(), CryptoStreamMode.Read))
                                {
                                    using (StreamReader reader = new(cryptoStream))
                                    {
                                        decryptedText = reader.ReadToEnd();
                                    }
                                }
                            });

                        AnsiConsole.MarkupLine("[bold green]Decryption successful![/]");
                        if (outputFileName != null)
                        {
                            File.WriteAllText(outputFileName, decryptedText);
                            AnsiConsole.MarkupLine($"[green]Decrypted output saved to[/] [yellow]{outputFileName}[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]Decrypted text below:[/]");
                            var escaped = Markup.Escape(decryptedText);
                            AnsiConsole.MarkupLine(escaped);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler(ex);
            }
        }
        static void ErrorHandler(Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            AnsiConsole.Markup("[grey]Press any key to exit.[/]");
            Console.ReadKey();
        }
    }
}
