
using System.ComponentModel.DataAnnotations;

using MathCore.WinAPI.Windows;

var bmp = Screenshoter.TakeScreenshot();

var file = new FileInfo("test.bmp");

bmp.Save(file.FullName);

file.Execute();

Console.WriteLine("End!");

// https://ru.stackoverflow.com/questions/1429141/Как-сделать-скриншота-экрана-и-окна-с-помощью-directx-c
