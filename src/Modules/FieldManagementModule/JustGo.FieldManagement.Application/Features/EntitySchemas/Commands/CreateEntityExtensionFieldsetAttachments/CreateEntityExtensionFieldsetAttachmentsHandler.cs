using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JustGo.FieldManagement.Application.Features.EntitySchemas.Commands.CreateEntityExtensionFieldsetAttachments
{
    public class CreateEntityExtensionFieldsetAttachmentsHandler : IRequestHandler<CreateEntityExtensionFieldsetAttachmentsCommand, int>
    {
        private readonly IWriteRepositoryFactory _writeRepository;
        private readonly IWebHostEnvironment _env;
        public CreateEntityExtensionFieldsetAttachmentsHandler(IWriteRepositoryFactory writeRepository, IWebHostEnvironment env)
        {
            _writeRepository = writeRepository;
            _env = env;
        }

        public async Task<int> Handle(CreateEntityExtensionFieldsetAttachmentsCommand request, CancellationToken cancellationToken)
        {
            foreach (var fieldId in request.FieldIds)
            {
                if (request.Data.ContainsKey(fieldId.ToString()))
                {
                    var result = request.Data[fieldId.ToString()];
                    string[] attachmentPaths = result.ToString().Split('|');
                    foreach (var attachmentPath in attachmentPaths)
                    {
                        if(!string.IsNullOrWhiteSpace(attachmentPath))
                        {
                            break;
                        }
                        if (attachmentPath.ToLower().StartsWith("temp") || attachmentPath.ToLower().IndexOf("copy|", StringComparison.Ordinal) > -1)
                        {
                            bool isCopy = attachmentPath.IndexOf("copy|", StringComparison.Ordinal) > -1;
                            string[] parts = isCopy ? attachmentPath.Split('|') : null;
                            int souceDocId = isCopy ? Convert.ToInt32(parts[1]) : 0;
                            var fileName = isCopy ? parts[2] : Path.GetFileName(attachmentPath);

                            fileName = fileName.Replace("temp_", "");
                            string[] attachmentPathParts = attachmentPath.Split('/');

                            var sourcePath = isCopy ? MapPath(string.Format("~/store/fieldmanagementattachment/{0}/", fieldId) + "/" + fileName) : MapPath("~/store/Temp/fieldmanagementattachment/attachments/" + fileName);
                            var destinationPath = MapPath(string.Format("~/store/fieldmanagementattachment/{0}/", fieldId));


                            if (!Exists(destinationPath))
                                CreateDirectory(destinationPath);

                            if (isCopy)
                            {
                                await CopyPasteAsync(sourcePath, destinationPath + fileName, cancellationToken);
                            }
                            else if (Exists(sourcePath))
                            {
                                await MoveToAsync(sourcePath, destinationPath + fileName, cancellationToken);
                            }
                        }
                    }
                }
            }
            return 1;
        }
        private string MapPath(string path)
        {
            if (path.ToLower().IndexOf("~/store") == -1)
                path = Path.Combine("~/store", path);
            string fullPath = Path.Combine(_env.WebRootPath, path);
            return path;
        }
        private bool Exists(string fullPath)
        {
            if (File.Exists(fullPath)) return true;
            if (Directory.Exists(fullPath)) return true;
            return false;
        }
        private void CreateDirectory(string fullPath)
        {
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
        }
        private async Task CopyPasteAsync(string srcFullPath, string dstFullPath, CancellationToken cancellationToken = default)
        {
            if (File.Exists(srcFullPath) && !File.Exists(dstFullPath)) // It's a file
            {
                EnsurePath(dstFullPath);

                await using var sourceStream = new FileStream(srcFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, useAsync: true);
                await using var destinationStream = new FileStream(dstFullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 8192, useAsync: true);
                await sourceStream.CopyToAsync(destinationStream, cancellationToken);
                return;
            }

            if (Directory.Exists(srcFullPath)) // It's a directory
            {
                if (!dstFullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    dstFullPath += Path.DirectorySeparatorChar;

                if (!Directory.Exists(dstFullPath))
                    Directory.CreateDirectory(dstFullPath); // still synchronous

                var files = Directory.GetFileSystemEntries(srcFullPath);
                foreach (string element in files)
                {
                    var destPath = Path.Combine(dstFullPath, Path.GetFileName(element));

                    if (Directory.Exists(element))
                        await CopyPasteAsync(element, destPath, cancellationToken); // recursive async
                    else
                    {
                        await using var sourceStream = new FileStream(element, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, useAsync: true);
                        await using var destinationStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);
                        await sourceStream.CopyToAsync(destinationStream, cancellationToken);
                    }
                }
            }
        }
        private void CopyPaste(string srcfullPath, string dstfullPath)
        {
            if (File.Exists(srcfullPath) && !File.Exists(dstfullPath)) // its a file
            {
                EnsurePath(dstfullPath);
                File.Copy(srcfullPath, dstfullPath);
            }
            if (Directory.Exists(srcfullPath)) // its a directory
            {
                string[] Files;
                if (dstfullPath[dstfullPath.Length - 1] != Path.DirectorySeparatorChar)
                    dstfullPath += Path.DirectorySeparatorChar;
                if (!Directory.Exists(dstfullPath)) Directory.CreateDirectory(dstfullPath);
                Files = Directory.GetFileSystemEntries(srcfullPath);
                foreach (string Element in Files)
                {
                    if (Directory.Exists(Element))
                        CopyPaste(Element, dstfullPath + Path.GetFileName(Element));
                    else
                        File.Copy(Element, dstfullPath + Path.GetFileName(Element), true);
                }
            }
        }
        private void EnsurePath(string path)
        {
            string dir = path.Substring(0, path.LastIndexOf(@"\") + 1);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        private void MoveTo(string srcfullPath, string dstfullPath)
        {
            string dir = dstfullPath.Substring(0, dstfullPath.LastIndexOf(@"\"));
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            if (File.Exists(srcfullPath))
            {
                File.Move(srcfullPath, dstfullPath);
            }
            if (Directory.Exists(srcfullPath))
            {
                Directory.Move(srcfullPath, dstfullPath);
            }
        }
        private async Task MoveToAsync(string srcFullPath, string dstFullPath, CancellationToken cancellationToken = default)
        {
            string dir = Path.GetDirectoryName(dstFullPath)!;

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir); // No async version
            }

            if (File.Exists(srcFullPath))
            {
                await using var sourceStream = new FileStream(srcFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, useAsync: true);
                await using var destinationStream = new FileStream(dstFullPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);
                await sourceStream.CopyToAsync(destinationStream, cancellationToken);
                File.Delete(srcFullPath); // still synchronous
                return;
            }

            if (Directory.Exists(srcFullPath))
            {
                await CopyDirectoryAsync(srcFullPath, dstFullPath, cancellationToken);
                Directory.Delete(srcFullPath, recursive: true); // still synchronous
            }
        }
        private async Task CopyDirectoryAsync(string sourceDir, string destDir, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(destDir); // No async version

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                await using var sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, useAsync: true);
                await using var destinationStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);
                await sourceStream.CopyToAsync(destinationStream, cancellationToken);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(directory));
                await CopyDirectoryAsync(directory, destSubDir, cancellationToken);
            }
        }


    }
}
