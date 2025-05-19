namespace Dempbot4.View.Pane
{
    using Dempbot4.ViewModel;
    using Dempbot4.ViewModel.Tools;
    using System.Windows;
    using System.Windows.Controls;
    using Xceed.Wpf.AvalonDock.Layout;

    class PanesTemplateSelector : DataTemplateSelector
    {
        public PanesTemplateSelector()
        {
        
        }

        public DataTemplate CodeEditViewTemplate
        {
            get;
            set;
        }

        public DataTemplate QuickGraphViewTemplate
        {
            get;
            set;
        }

        public DataTemplate ConsoleViewTemplate
        {
            get;
            set;
        }

        
        public DataTemplate FileViewTemplate
        {
            get;
            set;
        }

      

      
        public DataTemplate VariableViewTemplate
        {
            get;
            set;
        }

        public DataTemplate ChannelSelectorTemplate
        {
            get;
            set;
        }

        public DataTemplate CVWizardTemplate
        {
            get;
            set;
        }

        public DataTemplate IVWizardTemplate
        {
            get;
            set;
        }

        public DataTemplate RTWizardTemplate
        {
            get;
            set;
        }

        public DataTemplate ExperimentWizardTemplate
        {
            get;
            set;
        }

        public DataTemplate ScriptViewTemplate
        {
            get;
            set;
        }

        public DataTemplate AnalysisGraphTemplate
        {
            get;
            set;
        }
        public DataTemplate SingleAnalysisGraphTemplate
        {
            get;
            set;
        }


        public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {

            if (item is ScriptViewModel)
                return ScriptViewTemplate;

            if (item is CV_ViewModel)
                return CVWizardTemplate;

            if (item is IV_ViewModel)
                return IVWizardTemplate;

            if (item is RT_ViewModel)
                return RTWizardTemplate;

            if (item is Experiment_ViewModel)
                return ExperimentWizardTemplate;

            if (item is ChannelSetupViewModel)
                return ChannelSelectorTemplate;

            if (item is AnalysisGraphViewModel)
                return AnalysisGraphTemplate;

            if (item is SingleGraphViewModel)
                return SingleAnalysisGraphTemplate;

            if (item is VariableViewModel)
                return VariableViewTemplate;
            
            if (item is ConsoleViewModel)
                return ConsoleViewTemplate;

            if (item is QuickGraphViewModel)
              return  QuickGraphViewTemplate;
            
            if (item is CodeEditorViewModel)
                return CodeEditViewTemplate;

           

           

            return base.SelectTemplate(item, container);
        }
    }
}
