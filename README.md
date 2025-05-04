# Vault

Vault is a secure, cross-platform file encryption CLI built in **C#** with **.NET 9**, using [Spectre.Console](https://github.com/spectreconsole/spectre-console) for a modern terminal experience.


## 🔐 Features

- AES‑256‑GCM encryption with PBKDF2 (100,000 iterations + random salt)  
- Secure password prompts with backspace and masking  
- Encrypt from console or files; decrypt to terminal or save to disk  
- Fully interactive CLI using Spectre.Console: spinners, tables, colored prompts  
- Self-contained, single-file executables—no .NET install required  

---

## 📦 Download

Prebuilt executables (no .NET install required) are available in the [`publish/`](publish/) folder:

- **Windows** (`win-x64`) → `Vault.exe`  
- **Linux** (`linux-x64`) → `Vault`  
- **macOS** (`osx-x64`) → `Vault`  

---

## 🚀 Usage

Run Vault from your terminal and follow the prompts:

```bash
./Vault         # or Vault.exe on Windows
```

You can:
	•	Encrypt a message or file into a secure binary
	•	Decrypt a binary file using the correct password
	•	Choose to output decrypted text to the console or save it

---

Try It Yourself

Included in this repository is a sample encrypted file:

📂 White Rabbit
🔑 Passcode: Alice

Decrypt it using Vault to reveal the full text of Lewis Carroll’s *Through the Looking-Glass*.

---

Build From Source

You’ll need the .NET 9 SDK.

```bash
git clone https://github.com/Jimmy-Tollett/Vault.git
cd Vault
dotnet build
dotnet run
```

To create a single-file executable for any platform:

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true /p:PublishTrimmed=true
```

Replace win-x64 with linux-x64, osx-x64, etc. as needed.
