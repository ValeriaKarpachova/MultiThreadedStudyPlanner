using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace WpfApp2.Services
{
    // ─── Журнал змін ────────────────────────────────────────────────────────────
    public class ChangeLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action     { get; set; } = "";   // ADD / UPDATE / DELETE
        public int     TaskId    { get; set; }
        public string  TaskName  { get; set; } = "";
        public string  Detail    { get; set; } = "";   // що саме змінилось

        public override string ToString() =>
            $"{Timestamp:yyyy-MM-dd HH:mm:ss} | {Action,-6} | #{TaskId} \"{TaskName}\" | {Detail}";
    }

    // ─── Власний бінарний формат (.spd – Student Planner Data) ──────────────────
    //  Header:  4 bytes magic "SPD\x01"  +  4 bytes record count
    //  Record:  [int32 id][int32 parentId(or -1)][int32 priority]
    //           [double estimatedHours]
    //           [byte isChecked]
    //           [int64 deadlineTicks (or -1)]
    //           [str name][str description][str taskType]
    //  String:  [int32 byteLen][UTF-8 bytes]

    public static class DataStorageService
    {
        // ── Шляхи ──────────────────────────────────────────────────────────────
        private static readonly string DataDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        private static readonly string DataFile   = Path.Combine(DataDir, "tasks.spd");
        private static readonly string CacheFile  = Path.Combine(DataDir, "tasks.cache");
        private static readonly string JournalFile= Path.Combine(DataDir, "changelog.log");

        private static readonly byte[] Magic = { (byte)'S', (byte)'P', (byte)'D', 0x01 };

        // ── Кеш у пам'яті ──────────────────────────────────────────────────────
        private static List<TaskItem>? _cache;
        private static DateTime        _cacheTime = DateTime.MinValue;
        private static readonly TimeSpan CacheTTL = TimeSpan.FromMinutes(5);
        private static readonly ReaderWriterLockSlim _lock = new();

        // ── Журнал змін ────────────────────────────────────────────────────────
        private static readonly List<ChangeLogEntry> _journal = new();
        public static IReadOnlyList<ChangeLogEntry> Journal => _journal.AsReadOnly();

        // ═══════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════════════════════════════

        public static void EnsureDirectory()
        {
            if (!Directory.Exists(DataDir))
                Directory.CreateDirectory(DataDir);
        }

        // ── Збереження ─────────────────────────────────────────────────────────
        public static void Save(IEnumerable<TaskItem> tasks)
        {
            EnsureDirectory();
            var list = tasks.ToList();

            _lock.EnterWriteLock();
            try
            {
                WriteSpd(list, DataFile);
                WriteCacheManifest(list);
                _cache     = list;
                _cacheTime = DateTime.Now;
            }
            finally { _lock.ExitWriteLock(); }

            AppLogger.Log(LogLevel.Info, "DataStorage",
                $"Збережено {list.Count} задач у {DataFile}");
        }

        // ── Завантаження ───────────────────────────────────────────────────────
        public static List<TaskItem> Load()
        {
            EnsureDirectory();

            // 1. Перевіряємо гарячий кеш
            _lock.EnterReadLock();
            try
            {
                if (_cache != null && DateTime.Now - _cacheTime < CacheTTL)
                {
                    AppLogger.Log(LogLevel.Debug, "DataStorage", "Дані з гарячого кешу");
                    return _cache;
                }
            }
            finally { _lock.ExitReadLock(); }

            // 2. Перевіряємо файловий кеш
            if (File.Exists(CacheFile) && File.Exists(DataFile))
            {
                var cacheDate = File.GetLastWriteTime(CacheFile);
                var dataDate  = File.GetLastWriteTime(DataFile);
                if (cacheDate >= dataDate)
                {
                    var cached = ReadCacheManifest();
                    if (cached != null)
                    {
                        _lock.EnterWriteLock();
                        try { _cache = cached; _cacheTime = DateTime.Now; }
                        finally { _lock.ExitWriteLock(); }
                        AppLogger.Log(LogLevel.Debug, "DataStorage", "Дані з файлового кешу");
                        return cached;
                    }
                }
            }

            // 3. Читаємо основний файл
            if (!File.Exists(DataFile))
            {
                AppLogger.Log(LogLevel.Warning, "DataStorage", "Файл даних не знайдено, повертаємо порожній список");
                return new List<TaskItem>();
            }

            var result = ReadSpd(DataFile);
            _lock.EnterWriteLock();
            try { _cache = result; _cacheTime = DateTime.Now; }
            finally { _lock.ExitWriteLock(); }

            AppLogger.Log(LogLevel.Info, "DataStorage", $"Завантажено {result.Count} задач з файлу");
            return result;
        }

        // ── Інвалідація кешу ───────────────────────────────────────────────────
        public static void InvalidateCache()
        {
            _lock.EnterWriteLock();
            try { _cache = null; _cacheTime = DateTime.MinValue; }
            finally { _lock.ExitWriteLock(); }
        }

        // ── Журнал змін ────────────────────────────────────────────────────────
        // ── Окремий лок для журналу змін ──────────────────────────────────────
        private static readonly SemaphoreSlim _journalLock = new(1, 1);

        public static void LogChange(string action, TaskItem task, string detail = "")
        {
            var entry = new ChangeLogEntry
            {
                Timestamp = DateTime.Now,
                Action    = action,
                TaskId    = task.Id,
                TaskName  = task.Name ?? "",
                Detail    = detail
            };

            lock (_journal) { _journal.Add(entry); }

            // Асинхронний запис — не блокує потік, але файл захищений семафором
            _ = WriteJournalAsync(entry);

            AppLogger.Log(LogLevel.Debug, "ChangeLog", entry.ToString());
        }

        private static async System.Threading.Tasks.Task WriteJournalAsync(ChangeLogEntry entry)
        {
            await _journalLock.WaitAsync();
            try
            {
                EnsureDirectory();
                await System.IO.File.AppendAllTextAsync(
                    JournalFile,
                    entry + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // Журнал не повинен ламати основну логіку
                AppLogger.Log(LogLevel.Warning, "ChangeLog",
                    $"Не вдалося записати в журнал: {ex.Message}");
            }
            finally { _journalLock.Release(); }
        }

        public static List<ChangeLogEntry> LoadJournalFromFile()
        {
            if (!File.Exists(JournalFile)) return new();
            var lines = File.ReadAllLines(JournalFile, Encoding.UTF8);
            // Парсимо рядки назад у об'єкти для перегляду
            var result = new List<ChangeLogEntry>();
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length < 4) continue;
                if (!DateTime.TryParse(parts[0].Trim(), out var ts)) continue;
                result.Add(new ChangeLogEntry
                {
                    Timestamp = ts,
                    Action    = parts[1].Trim(),
                    Detail    = parts.Length > 3 ? parts[3].Trim() : ""
                });
            }
            return result;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  PRIVATE – бінарний формат SPD
        // ═══════════════════════════════════════════════════════════════════════

        private static void WriteSpd(List<TaskItem> tasks, string path)
        {
            // Плоский список: спочатку кореневі, потім підзадачі
            var flat = tasks
                .SelectMany(t => new[] { t }.Concat(t.SubTasks))
                .ToList();

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs, Encoding.UTF8);

            bw.Write(Magic);
            bw.Write(flat.Count);

            foreach (var t in flat)
            {
                bw.Write(t.Id);
                bw.Write(t.ParentId ?? -1);
                bw.Write(t.Priority);
                bw.Write(t.EstimatedHours);
                bw.Write(t.IsChecked);
                bw.Write(t.Deadline.HasValue ? t.Deadline.Value.Ticks : -1L);
                WriteString(bw, t.Name        ?? "");
                WriteString(bw, t.Description ?? "");
                WriteString(bw, t.TaskType    ?? "");
            }
        }

        private static List<TaskItem> ReadSpd(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs, Encoding.UTF8);

            var magic = br.ReadBytes(4);
            if (!magic.SequenceEqual(Magic))
                throw new InvalidDataException("Невірний формат файлу SPD");

            int count = br.ReadInt32();
            var flat  = new List<TaskItem>(count);

            for (int i = 0; i < count; i++)
            {
                var t = new TaskItem
                {
                    Id             = br.ReadInt32(),
                    Priority       = 0,
                    EstimatedHours = 0
                };
                int parentRaw  = br.ReadInt32();
                t.ParentId     = parentRaw == -1 ? null : parentRaw;
                t.Priority     = br.ReadInt32();
                t.EstimatedHours = br.ReadDouble();
                t.IsChecked    = br.ReadBoolean();
                long ticks     = br.ReadInt64();
                t.Deadline     = ticks == -1 ? null : new DateTime(ticks);
                t.Name         = ReadString(br);
                t.Description  = ReadString(br);
                t.TaskType     = ReadString(br);
                flat.Add(t);
            }

            // Збираємо дерево
            var roots    = flat.Where(t => t.ParentId == null).ToList();
            var children = flat.Where(t => t.ParentId != null).ToList();
            foreach (var child in children)
            {
                var parent = roots.FirstOrDefault(r => r.Id == child.ParentId);
                parent?.SubTasks.Add(child);
            }
            return roots;
        }

        private static void WriteString(BinaryWriter bw, string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            bw.Write(bytes.Length);
            bw.Write(bytes);
        }

        private static string ReadString(BinaryReader br)
        {
            int len = br.ReadInt32();
            var bytes = br.ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }

        // ── Файловий кеш (простий CSV-маніфест для швидкої перевірки) ──────────
        private static void WriteCacheManifest(List<TaskItem> tasks)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# cache generated {DateTime.Now:O}");
            sb.AppendLine($"# count={tasks.Count}");
            foreach (var t in tasks)
                sb.AppendLine($"{t.Id},{t.Name?.Replace(",", "\\,")}," +
                              $"{t.Deadline:yyyy-MM-dd},{t.IsChecked}");
            File.WriteAllText(CacheFile, sb.ToString(), Encoding.UTF8);
        }

        private static List<TaskItem>? ReadCacheManifest()
        {
            // Маніфест лише для швидкої перевірки актуальності;
            // якщо він свіжіший за .spd — повертаємо null і читаємо .spd
            return null; // навмисно: кеш-маніфест використовується лише як sentinel
        }
    }
}
