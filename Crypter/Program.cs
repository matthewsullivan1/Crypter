using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Security;


namespace Crypter
{
    public class Crypter
    {
        public static void Main()
        {
            // Note: Only tested on simple .NET 4.7.2 executable. Packed exe is placed in the publish folder of the 
            // output directory. Would need to compile with additional references for different exes 


            // targetOutputDir is the target directory for the compiled stub project directory
            // targetBin is the target binary to encrypt
            // add checks to ensure the directories exist
            string targetOutputDir = "C:\\Users\\18163\\Desktop\\crypter\\";
            string testAssembly2Bin = "C:\\Users\\18163\\source\\new\\TestAssembly2\\TestAssembly2\\bin\\x64\\Debug\\TestAssembly2.exe";
            string targetBin = "C:\\Users\\18163\\source\\new\\Crypter\\Crypter\\bin.exe";

            // generate a unique name, create a file to the path specified, and copy the static stub template to that file
            // where to write the stub source code
            string stubPath = Utils.GenerateStub(targetOutputDir, ".cs");

            // encrypt target binary 
            var (key, iv) = Utils.GenerateKeyAndIV();
            byte[] targetBytes = File.ReadAllBytes(targetBin);
            byte[] encrypted = Utils.Encrypt(targetBytes, key, iv);

            // Embed the encrypted bytes, decryption key, and IV as a base64 string into the stub source file at the placeholder locations
            // Then create a .csproj file so that it can be published into a standalone exe after compilation
            Stub.EmbedDataInStub(stubPath, encrypted, key, iv);
            Stub.CreateCsProjFile(targetOutputDir, stubPath);
            
            if (Stub.CompileStub(stubPath, targetOutputDir))
            {
                // dotnet publish after successful compilation
                string csprojPath = Path.Combine(targetOutputDir, Path.GetFileNameWithoutExtension(stubPath) + ".csproj");
                Stub.DotnetPublish(csprojPath, targetOutputDir);
            }
        }
    }

    public class Stub
    {
        public static void EmbedDataInStub(string filePath, byte[] encryptedData, byte[] key, byte[] iv)
        {

            // Filepath -> Path to stub resource file, not target binary
            string fileContent = File.ReadAllText(filePath);

            string encryptedBase64 = Convert.ToBase64String(encryptedData);
            string keyBase64 = Convert.ToBase64String(key);
            string ivBase64 = Convert.ToBase64String(iv);

            // embed the encrypted bytes of the exe, key, and IV into the stub source file
            fileContent = fileContent.Replace("/*ENCRYPTED_DATA*/", $"Convert.FromBase64String(\"{encryptedBase64}\")");
            fileContent = fileContent.Replace("/*KEY*/", $"Convert.FromBase64String(\"{keyBase64}\")");
            fileContent = fileContent.Replace("/*IV*/", $"Convert.FromBase64String(\"{ivBase64}\")");
            try
            {
                File.WriteAllText(filePath, fileContent);
                Console.WriteLine("Data embedd successful\n");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to write to the file:");
                Console.Error.WriteLine(ex.Message);
            }
        }

        public static bool CompileStub(string sourceFilePath, string outputDirectory)
        {

            string fileName = Path.GetFileNameWithoutExtension(sourceFilePath) + ".exe";
            string outputPath = Path.Combine(outputDirectory, fileName);

            string sourceCode = File.ReadAllText(sourceFilePath);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceCode);

            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            var references = new MetadataReference[]
            {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Console.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(typeof(CryptoStream).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MemoryStream).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.InteropServices.Marshal).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SuppressUnmanagedCodeSecurityAttribute).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                Path.GetRandomFileName(),
                syntaxTrees: new[] { tree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.ConsoleApplication)
            );

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);
                if (!result.Success)
                {
                    foreach (var diagnostic in result.Diagnostics.Where(diag => diag.IsWarningAsError || diag.Severity == DiagnosticSeverity.Error))
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    return false;
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        ms.WriteTo(fileStream);
                    }
                    Console.WriteLine("Compilation successful\n");
                    return true;
                }
            }
        }

        public static void CreateCsProjFile(string outputDirectory, string sourceFilePath)
        {
            string projectName = Path.GetFileNameWithoutExtension(sourceFilePath);
            string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
        <RuntimeIdentifier>win-x64</RuntimeIdentifier>
        <PublishSingleFile>true</PublishSingleFile>
        <SelfContained>true</SelfContained>
    </PropertyGroup>
</Project>";

            try
            {
                File.WriteAllText(Path.Combine(outputDirectory, $"{projectName}.csproj"), csprojContent);
                Console.WriteLine(".csproj successful\n");
            } 
            catch(Exception ex)
            {
                Console.Error.WriteLine("Failed to write .csproj file:");
                Console.Error.WriteLine(ex.Message);
            }
        }
        public static void DotnetPublish(string csprojPath, string outputDirectory)
        {
            string publishDirectory = Path.Combine(outputDirectory, "publish");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish \"{csprojPath}\" -c Release -r win-x64 --self-contained -o \"{publishDirectory}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = psi })
            {
                process.Start();
                process.WaitForExit();  // Wait for the process to complete before reading outputs

                string output = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("dotnet publish successful\n");
                    Console.WriteLine(output);
                }
                else
                {
                    Console.Error.WriteLine("Failed to publish executable");
                    Console.Error.WriteLine(errors); 
                }
            }
        }



    }
}