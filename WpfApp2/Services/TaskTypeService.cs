using Microsoft.Xaml.Behaviors.Layout;

namespace WpfApp2.Services
{
    public static class TaskTypeService
    {
        public static int GetImportance(string type)
        {
            return type switch
            {
                "Exam" or "Diploma Project" => 10,
                "Test" or "Internship" => 9,
                "Course Project" or "Research Paper" => 8,
                "Laboratory Work" or "Thesis" => 7,
                "Presentation" or "Essay" => 6,
                "Independent Study" or "Homework" => 5,
                "Practical Class" or "Seminar" => 4,
                "Lecture" or "Quiz" => 2,
                _ => 3
            };
        }

        public static double GetDefaultHours(string type)
        {
            return type switch
            {
                "Diploma Project" => 80,
                "Course Project" => 25,
                "Research Paper" => 20,
                "Internship" => 120,
                "Exam" => 12,
                "Test" => 6,
                "Quiz" => 2,
                "Laboratory Work" => 4,
                "Practical Class" => 3,
                "Seminar" => 2,
                "Homework" => 2,
                "Essay" => 5,
                "Presentation" => 6,
                "Independent Study" => 3,
                "Lecture" => 1,
                _ => 3
            };
        }

        public static List<string> GetDefaultSubTaskNames(string? type, int parts)
        {
            var templates = type switch
            {
                "Diploma Project" => new[] {
            "Вступ", "Розділ 1. Аналіз предметної області",
            "Розділ 2. Проектування", "Розділ 3. Реалізація",
            "Розділ 4. Тестування", "Висновки", "Список літератури" },

                "Course Project" => new[] {
            "Вступ", "Теоретична частина",
            "Практична частина", "Висновки" },

                "Research Paper" => new[] {
            "Вступ", "Огляд літератури",
            "Методологія", "Результати", "Висновки" },

                "Exam" => new[] {
            "Повторення теорії", "Практичні задачі",
            "Пробний екзамен", "Фінальне повторення" },

                "Test" => new[] {
            "Повторення матеріалу", "Практика", "Самоперевірка" },

                "Internship" => new[] {
            "Ознайомлення", "Основна робота",
            "Звіт", "Захист" },

                "Laboratory Work" => new[] {
            "Підготовка", "Виконання", "Оформлення звіту" },

                "Essay" or "Presentation" => new[] {
            "Збір матеріалу", "Структура та план",
            "Написання/створення", "Редагування" },

                _ => Enumerable.Range(1, parts)
                               .Select(i => $"Частина {i}")
                               .ToArray()
            };

            // Берём нужное количество частей из шаблона или обрезаем/дополняем
            var result = templates.Take(parts).ToList();
            while (result.Count < parts)
                result.Add($"Частина {result.Count + 1}");

            return result;
        }
    }
}