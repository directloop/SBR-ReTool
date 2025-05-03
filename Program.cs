using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class FileEntry
{
    public string name { get; set; }
    public string url { get; set; }
}

class JsonData
{
    public List<string> changelog { get; set; }
    public List<FileEntry> files { get; set; }
}

class Program
{
    static Dictionary<string, string> strings;
    static string windowTitle = "SBR-ReTool";

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Вывод названия программы
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(@"
                                  SBR-ReTool                                                                                
                            by directloop & noxygalaxy
");
        Console.ResetColor();

        // Выбор языка
        string lang = SelectLanguage();

        // Локализация
        strings = lang == "ru" ? GetRussianStrings() : GetEnglishStrings();

        // URL для загрузки JSON
        string jsonUrl = lang == "ru" ? "https://code4444.directlooped.workers.dev/" : "https://enversionretool.directlooped.workers.dev/";
        
        string tempPath = Path.GetTempPath();
        string jsonFile = Path.Combine(tempPath, "files.json");

        try
        {
            using (HttpClient client = new HttpClient())
            {
                Console.WriteLine(strings["loading_json"]);
                string jsonString = await client.GetStringAsync(jsonUrl);
                File.WriteAllText(jsonFile, jsonString);
                Console.WriteLine(strings["json_loaded"]);
            }

            string json = File.ReadAllText(jsonFile);
            JsonData data = JsonSerializer.Deserialize<JsonData>(json);

            if (data == null || data.changelog == null || data.files == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(strings["error"] + ": Ошибка в структуре данных JSON. Данные не были правильно загружены.");
                Console.ResetColor();
                return;
            }

            if (data.changelog.Count == 0)
            {
                Console.WriteLine(strings["no_changelog"]);
                Console.WriteLine(strings["press_any_key"]);
                Console.ReadKey();  // Ожидаем нажатие клавиши
                return;
            }
            else
            {
                Console.WriteLine(strings["changelog"]);
                foreach (var line in data.changelog)
                {
                    Console.WriteLine(" - " + line);
                }
            }

            if (data.files.Count == 0)
            {
                Console.WriteLine(strings["no_files"]);
            }
            else
            {
                Console.WriteLine("\n" + strings["available_files"]);
                for (int i = 0; i < data.files.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {data.files[i].name}");
                }

                Console.Write("\n" + strings["enter_file_number"]);
                if (int.TryParse(Console.ReadLine(), out int choice) &&
                    choice >= 1 && choice <= data.files.Count)
                {
                    var selectedFile = data.files[choice - 1];

                    string targetDirectory;
                    while (true)
                    {
                        Console.Write(strings["enter_folder_path"]);
                        targetDirectory = Console.ReadLine().Trim('"');

                        if (!Directory.Exists(targetDirectory))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(strings["dir_not_exist"] + "\n");
                            Console.ResetColor();
                            continue;
                        }

                        string requiredDllPath = Path.Combine(targetDirectory, "Assembly-CSharp.dll");
                        if (!File.Exists(requiredDllPath))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(strings["dll_not_found"] + "\n");
                            Console.ResetColor();
                            continue;
                        }

                        // Удаление старого файла Assembly-CSharp.dll
                        string dllFilePath = Path.Combine(targetDirectory, "Assembly-CSharp.dll");

                        if (File.Exists(dllFilePath))
                        {
                            try
                            {
                                Console.WriteLine(strings["deleting_old"]);
                                File.Delete(dllFilePath);
                            }
                            catch (Exception delEx)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"❌ {strings["error"]} при удалении файла {dllFilePath}: {delEx.Message}");
                                Console.ResetColor();
                                return;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Файл Assembly-CSharp.dll не найден. Новый файл будет загружен.");
                        }

                        break;
                    }

                    // Путь к новому файлу будет всегда с именем "Assembly-CSharp.dll"
                    // Используем название окна (windowTitle) как часть имени файла
                    string targetPath = Path.Combine(targetDirectory, "Assembly-CSharp.dll");


                    if (File.Exists(targetPath))
                    {
                        try
                        {
                            Console.WriteLine(strings["deleting_old"]);
                            File.Delete(targetPath);
                        }
                        catch (Exception delEx)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"❌ {strings["error"]} при удалении файла: {delEx.Message}");
                            Console.ResetColor();
                            return;
                        }
                    }

                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            Console.WriteLine("\n" + strings["downloading"]);
                            var fileBytes = await client.GetByteArrayAsync(selectedFile.url);
                            File.WriteAllBytes(targetPath, fileBytes);
                            Console.WriteLine(strings["file_saved"]);
                        }
                    }
                    catch (Exception writeEx)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"❌ {strings["error"]} при сохранении файла: {writeEx.Message}");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.WriteLine(strings["invalid_choice"]);
                }
            }

            File.Delete(jsonFile); // Удаляем временный JSON
        }
        catch (Exception ex)
        {
            Console.WriteLine(strings["error"] + ": " + ex.Message);
        }

        Console.WriteLine("\n" + strings["press_any_key"]);
        Console.ReadKey();
    }

    static string SelectLanguage()
    {
        Console.WriteLine("Choose language / Выберите язык: [en/ru]");
        string input = Console.ReadLine().Trim().ToLower();
        if (input != "ru" && input != "en")
        {
            Console.WriteLine("Defaulting to English...");
            input = "en";
        }
        return input;
    }

    static Dictionary<string, string> GetRussianStrings() => new Dictionary<string, string>
    {
        { "loading_json", "Загружается JSON-файл..." },
        { "json_loaded", "JSON успешно загружен.\n" },
        { "changelog", "📋 Список изменений:" },
        { "available_files", "📦 Доступные файлы для скачивания:" },
        { "enter_file_number", "Введите номер файла для загрузки: " },
        { "enter_folder_path", "Введите путь к папке назначения: " },
        { "dir_not_exist", "❌ Указанная директория не существует." },
        { "dll_not_found", "❌ Не найден обязательный файл: Assembly-CSharp.dll" },
        { "deleting_old", "Удаляется старый файл..." },
        { "downloading", "⬇️ Загрузка файла..." },
        { "file_saved", "✅ Файл успешно сохранён" },
        { "invalid_choice", "❌ Неверный выбор." },
        { "no_changelog", "Нет доступных изменений." },
        { "no_files", "Нет доступных файлов для скачивания." },
        { "error", "Ошибка" },
        { "press_any_key", "Нажмите любую клавишу для выхода..." }
    };

    static Dictionary<string, string> GetEnglishStrings() => new Dictionary<string, string>
    {
        { "loading_json", "Downloading JSON file..." },
        { "json_loaded", "JSON successfully downloaded.\n" },
        { "changelog", "📋 Changelog:" },
        { "available_files", "📦 Available files for download:" },
        { "enter_file_number", "Enter file number to download: " },
        { "enter_folder_path", "Enter target folder path: " },
        { "dir_not_exist", "❌ The specified directory does not exist." },
        { "dll_not_found", "❌ Required file not found: Assembly-CSharp.dll" },
        { "deleting_old", "Removing old file..." },
        { "downloading", "⬇️ Downloading file..." },
        { "file_saved", "✅ File successfully saved" },
        { "invalid_choice", "❌ Invalid choice." },
        { "no_changelog", "No available changelog." },
        { "no_files", "No available files for download." },
        { "error", "Error" },
        { "press_any_key", "Press any key to exit..." }
    };
}
