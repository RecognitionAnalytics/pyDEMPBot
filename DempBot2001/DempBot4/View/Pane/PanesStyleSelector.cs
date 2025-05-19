namespace Dempbot4.View.Pane
{
    using Dempbot4.ViewModel;
    using Dempbot4.ViewModel.Base;
    using System.Windows;
    using System.Windows.Controls;

    class PanesStyleSelector : StyleSelector
    {
        public Style ToolStyle
        {
            get;
            set;
        }

        public Style FileStyle
        {
            get;
            set;
        }

        


        public override System.Windows.Style SelectStyle(object item, System.Windows.DependencyObject container)
        {
            

            if (item is ToolViewModel)
            {
                return ToolStyle;
            }

            if (item is DocumentTypePaneVM)
            {
                return FileStyle;
            }
            return base.SelectStyle(item, container);
        }
    }
}
