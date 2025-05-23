﻿namespace Dempbot4.View.Pane
{
  using System.Linq;
  using Xceed.Wpf.AvalonDock.Layout;

  public class LayoutInitializer : ILayoutUpdateStrategy
  {
    public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
    {
      //AD wants to add the anchorable into destinationContainer
      //just for test provide a new anchorablepane 
      //if the pane is floating let the manager go ahead
      LayoutAnchorablePane destPane = destinationContainer as LayoutAnchorablePane;
      if (destinationContainer != null &&
          destinationContainer.FindParent<LayoutFloatingWindow>() != null)
        return false;

      var toolsPane = layout.Descendents().OfType<LayoutAnchorablePane>().FirstOrDefault(d => d.Name == "ToolsPane");
      if (toolsPane != null)
      {
        toolsPane.Children.Add(anchorableToShow);
        return true;
      }

      return false;

    }


    public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
    {
    }


    public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer)
    {
      return false;
    }

    public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
    {

    }
  }
}
