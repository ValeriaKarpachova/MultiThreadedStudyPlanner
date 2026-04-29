using System.Windows;
using System.Windows.Controls;

namespace WpfApp2.Services
{
    public class TaskTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? SimpleTemplate { get; set; }
        public DataTemplate? SplitTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is TaskItem task)
                return task.IsSplit ? SplitTemplate : SimpleTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}