// See https://aka.ms/new-console-template for more information
using FileInfoExtensions;

Console.WriteLine("Hello, World!");

const string path = @"C:\Windows\System32\drivers\etc\hosts";
var fileInfo = new FileInfo(path);

string fileType = fileInfo.GetFileType();
//var ico = fileInfo.GetIcon(false);
string suffix = fileInfo.GetFileSizeSuffix(2);
string hex = fileInfo.ToHexString();
